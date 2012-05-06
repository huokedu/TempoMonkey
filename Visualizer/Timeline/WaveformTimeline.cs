﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using NAudio.Wave;
using Processing;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Visualizer.Timeline
{
	public class WaveformTimeline
	{
		#region Private variables
		private Panel Container;
		private Path Timeline;
		private Brush WaveformFill = Brushes.RoyalBlue;
		private float TopOffset = 0;
		private float LeftOffset = 20;
		private double TotalLength;

		private BackgroundWorker worker = new BackgroundWorker();
		private CompletionCallback OnCompletion;
		private Sampler InputSampler;

		private Rectangle CurrentPosition;
		private double TotalTime = 0.0;

		private string FileName;
		private float[] WaveformData;
		private float[] FullLevelData;
		#endregion

		public delegate void CompletionCallback();

		public WaveformTimeline(Panel container, string fileName, CompletionCallback callback = null)
		{
			Container = container;
			FileName = fileName;
			OnCompletion = callback;

			CurrentPosition = new Rectangle
			{
				Height = container.Height,
				Width = 4,
				Fill = Brushes.Black,
				Opacity = 0.9,
				Margin = new System.Windows.Thickness(LeftOffset, TopOffset, 0, 0)
			};
			// Add the tracking bar to the container
			Container.Children.Add(CurrentPosition);
		}

		// Destructor
		public void Destroy()
		{
			Container.Children.Remove(Timeline);
			Container.Children.Remove(CurrentPosition);
		}

		#region Public Methods
		
		/// <summary>
		/// Change the fill color of the path
		/// </summary>
		/// <param name="fill">Brush color to change to</param>
		public void SetFill(Brush fill)
		{
			WaveformFill = fill;
		}

		public void SetOffset(float offsetTop, float offsetLeft)
		{
			TopOffset = offsetTop;
			LeftOffset = offsetLeft;
		}

		public void Draw()
		{
			// Create the sampler that we will use to pull data
			InputSampler = new Sampler((int)FFTDataSize.FFT2048);

			worker.DoWork += new DoWorkEventHandler(worker_DoWork);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
			worker.WorkerSupportsCancellation = true;

			// Start the background worker
			worker.RunWorkerAsync();
		}

		/// <summary>
		/// Sets up a timer to hook into the Audio library for tracking the position of the current song
		/// </summary>
		public void StartTracking()
		{
			DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
			timer.Interval = TimeSpan.FromMilliseconds(100);
			timer.Tick += new EventHandler(timer_Tick);
			timer.Start();
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			if (Audio.IsPlaying && Audio.CurrentTrack == FileName)
			{
				// Only track when the audio library is playing the song that this 
				// waveform is matched up with
				WaveChannel32 stream = Audio.CurrentStream;
				if (TotalTime == 0.0)
				{
					TotalTime = stream.TotalTime.TotalSeconds;
				}
				double offset = (stream.CurrentTime.TotalSeconds / TotalTime) * TotalLength;
				CurrentPosition.Margin = new System.Windows.Thickness(offset + LeftOffset, TopOffset, 0, 0);
			}
		}

		#endregion



		#region Private Helpers

		private void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			Mp3FileReader reader = new Mp3FileReader(FileName);
			WaveChannel32 channel = new WaveChannel32(reader);
			channel.Sample += new EventHandler<SampleEventArgs>(channel_Sample);

			int points = 2000;

			int frameLength = (int)FFTDataSize.FFT2048;
			int frameCount = (int)((double)channel.Length / (double)frameLength);
			int waveformLength = frameCount * 2;
			byte[] readBuffer = new byte[frameLength];

			float maxLeftPointLevel = float.MinValue;
			float maxRightPointLevel = float.MinValue;
			int currentPointIndex = 0;
			float[] waveformCompressedPoints = new float[points];
			List<float> waveformData = new List<float>();
			List<int> waveMaxPointIndexes = new List<int>();

			for (int i = 1; i <= points; i++)
			{
				waveMaxPointIndexes.Add((int)Math.Round(waveformLength * ((double)i / (double)points), 0));
			}
			int readCount = 0;
			while (currentPointIndex * 2 < points)
			{
				channel.Read(readBuffer, 0, readBuffer.Length);

				waveformData.Add(InputSampler.LeftMax);
				waveformData.Add(InputSampler.RightMax);

				if (InputSampler.LeftMax > maxLeftPointLevel)
					maxLeftPointLevel = InputSampler.LeftMax;
				if (InputSampler.RightMax > maxRightPointLevel)
					maxRightPointLevel = InputSampler.RightMax;

				if (readCount > waveMaxPointIndexes[currentPointIndex])
				{
					waveformCompressedPoints[(currentPointIndex * 2)] = maxLeftPointLevel;
					waveformCompressedPoints[(currentPointIndex * 2) + 1] = maxRightPointLevel;
					maxLeftPointLevel = float.MinValue;
					maxRightPointLevel = float.MinValue;
					currentPointIndex++;
				}
				if (readCount % 3000 == 0)
				{
					WaveformData = (float[])waveformCompressedPoints.Clone();
				}

				if (worker.CancellationPending)
				{
					e.Cancel = true;
					break;
				}
				readCount++;
			}

			FullLevelData = waveformData.ToArray();
			WaveformData = (float[])waveformCompressedPoints.Clone();

			// Cleanup
			channel.Close();
			channel.Dispose();
			channel = null;
			reader.Close();
			reader.Dispose();
			reader = null;	
		}

		private void channel_Sample(object sender, SampleEventArgs e)
		{
			InputSampler.Add(e.Left, e.Right);
		}

		private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			// Draw the path object into the container
			PathFigure figure = new PathFigure();
			double yOffset = Container.Height / 2;
			figure.StartPoint = new System.Windows.Point(0, yOffset);
			double thickness = (2 * (Container.Width - LeftOffset - 5)) / WaveformData.Length;
			double x = 0;

			PolyLineSegment leftSegment = new PolyLineSegment();
			PolyLineSegment rightSegment = new PolyLineSegment();
			leftSegment.Points.Add(new System.Windows.Point(0, yOffset));
			rightSegment.Points.Add(new System.Windows.Point(0, yOffset));
			for (int i = 0; i < WaveformData.Length; i += 2)
			{
				x = (i / 2) * thickness;
				leftSegment.Points.Add(new System.Windows.Point(x, yOffset + (WaveformData[i] * 25)));
				rightSegment.Points.Add(new System.Windows.Point(x, yOffset - (WaveformData[i] * 25)));
			}
			figure.Segments.Add(leftSegment);
			figure.Segments.Add(rightSegment);

			PathGeometry geometry = new PathGeometry();
			geometry.Figures.Add(figure);

			Timeline = new Path();
			Timeline.Fill = WaveformFill;
			Timeline.Data = geometry;

			TotalLength = x;

            //Strech it out, to fit the container
            // path.Data.Transform = new ScaleTransform(Container.Width / x, 1);

			Container.Children.Insert(0, Timeline);
			if (Container is Canvas)
			{
				Canvas.SetLeft(Timeline, LeftOffset);
				Canvas.SetTop(Timeline, TopOffset);
			}
			else
			{
				// Default to margin
				Timeline.Margin = new System.Windows.Thickness(LeftOffset, TopOffset, 0, 0);
			}

			// Alert any callbacks that were registered
			if (OnCompletion != null)
			{
				OnCompletion();
			}
		}

		#endregion
	}
}
