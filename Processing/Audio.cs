﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio;
using NAudio.Wave;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace Processing
{
	/// <summary>
	/// Public interface (singleton) for exposing audio manipulation methods
	/// </summary>
	public class Audio
	{
		#region Private variables

		private static SoundTouchSharp _soundTouchSharp;
		private static WaveOut _waveOutDevice;
		private static Dictionary<string, AudioStream> _audioStreams = new Dictionary<string, AudioStream>();
		private static WaveChannel32 _currentWaveChannel;

		private static Mp3FileReader _waveReader;
		private static BlockAlignReductionStream _blockAlignStream;
		private static WaveChannel32 _waveChannel;
		private static AdvancedBufferedWaveProvider _inputProvider;

		// Constants
		private const int MaxSongs = 3;
		private const int BufferSamples = 5 * 2048;
		private const int BusyQueuedBuffersThreshold = 3;

		// State variables
		private static bool tempoChanged = false;
		private static bool pitchChanged = false;
		private static bool volumeChanged = false;
		private static float newTempo;
		private static float newPitch;
		private static float newVolume;

		private static bool Started = false;

		private static TimeSpan DefaultEndMarker = TimeSpan.Zero;
		private static volatile bool stopWorker = false;

		// Threads
		private static Thread audioProcessingThread;
		
		#endregion

		public static bool isInitialized = false;

		#region Initialization Methods
		public static void Initialize()
		{
			// This should help prevent playback hiccups
			Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;

			// Initialize the SoundTouchSharp library
			_soundTouchSharp = new SoundTouchSharp();
			_soundTouchSharp.CreateInstance();

			isInitialized = true;
		}

		/// <summary>
		/// Load the processing library with an .mp3 file
		/// </summary>
		/// <param name="fileName">The filename of the song to load</param>
		public static void LoadFile(string fileName)
		{
			if (fileName.EndsWith(".mp3"))
			{
				if (_audioStreams.Count() < MaxSongs)
				{
					InitializeNAudio(fileName);
					if (_currentWaveChannel == null)
					{
						//  This is the first audio file loaded
						_currentWaveChannel = _waveChannel;
					}
					_audioStreams.Add(fileName, new AudioStream(_waveChannel));
				}
				else
				{
					throw new Exception("Too many files loaded.  Load at most three distinct songs.");
				}
			}
			else
			{
				throw new InvalidOperationException("Unsupported file extension");
			}
		}

		/// <summary>
		/// Removes a song from the list of playable songs
		/// </summary>
		/// <param name="fileName">The filename of the song to remove</param>
		public static void UnLoadFile(string fileName)
		{
			if (fileName.EndsWith(".mp3"))
			{
				try
				{
					_audioStreams.Remove(fileName);
				}
				catch (System.Exception)
				{
					throw new ArgumentException(String.Format("File of name '{0}' could not be removed", fileName));
				}
			}
			else
			{
				throw new InvalidOperationException("Unsupported file extension");
			}
		}
		#endregion

		#region Cleanup Methods
		public static void Cleanup()
		{
			if (_inputProvider != null)
			{
				if (_inputProvider is IDisposable)
				{
					(_inputProvider as IDisposable).Dispose();
				}
				_inputProvider = null;
			}

			if (_currentWaveChannel != null)
			{
				_currentWaveChannel.Dispose();
				_currentWaveChannel = null;
			}

			if (_blockAlignStream != null)
			{
				_blockAlignStream.Dispose();
				_blockAlignStream = null;
			}

			if (_waveReader != null)
			{
				_waveReader.Dispose();
				_waveReader = null;
			}

			if (_waveOutDevice != null)
			{
				_waveOutDevice.Dispose();
				_waveOutDevice = null;
			}
		}
		#endregion

		#region Basic Audio Methods
		public static void Play()
		{
			if (_waveOutDevice.PlaybackState == PlaybackState.Stopped || _waveOutDevice.PlaybackState == PlaybackState.Paused)
			{
				_waveOutDevice.Play();
			}
		}

		public static void Pause()
		{
			if (_waveOutDevice.PlaybackState == PlaybackState.Playing)
			{
				// Stop seems to work better than _device.Pause() here..., and doesn't reset the track
				_waveOutDevice.Stop();
			}
		}

		public static void Reset()
		{
			_currentWaveChannel.CurrentTime = TimeSpan.FromSeconds(0);
		}

		/// <summary>
		/// Change the currently playing track
		/// </summary>
		/// <param name="fileName">The filename of the song to start playing</param>
		/// <param name="keepOldPosition">Maintain the position of the track being swapped out (defaults to true)</param>
		/// <param name="resetNewPosition">Begin at the last stopping position of the track being swapped in (defaults to true)</param>
		public static void SwapTrack(string fileName, bool keepOldPosition = true, bool keepNewPosition = true)
		{
			_currentWaveChannel = _audioStreams[fileName].Stream;
		}
		#endregion

		#region Manipulations
		/// <summary>
		/// Change the volume of the current song.  Range: [0, 100], StepSize: 1.
		/// </summary>
		/// <returns>The new volume</returns>
		public static void ChangeVolume(float value)
		{
			volumeChanged = true;
			newVolume = value / 100.0f;
		}

		/// <summary>
		/// Change the tempo (BPM) of the current song.  Range: [10, 200], StepSize: 10.
		/// </summary>
		/// <returns>The new tempo</returns>
		public static void ChangeTempo(double value)
		{
			tempoChanged = true;
			newTempo = (float)(value / 100.0f);
		}

		/// <summary>
		/// Change the pitch of the current song.  Range: [-96, 96], StepSize: 8.
		/// </summary>
		/// <returns>The new pitch</returns>
		public static void ChangePitch(double value)
		{
			pitchChanged = true;
			newPitch = (float)(value / 8);
		}

		/// <summary>
		/// Change the play position of the current song
		/// </summary>
		/// <returns>The new current time of the song</returns>
		public static void Seek(long offset)
		{
			if (offset > 0)
			{
				_currentWaveChannel.CurrentTime.Add(TimeSpan.FromSeconds(offset));
			}
			else
			{
				_currentWaveChannel.CurrentTime.Subtract(TimeSpan.FromSeconds(offset));
			}
		}

		#endregion

		#region Private Methods

		private static void InitializeNAudio(string fileName)
		{
			// Create the output device to play on
			_waveOutDevice = new WaveOut();

			// Create the input wave
			_waveReader = new Mp3FileReader(fileName);
			_blockAlignStream = new BlockAlignReductionStream(_waveReader);
			_waveChannel = new WaveChannel32(_blockAlignStream);

			// Create the input provider
			_inputProvider = new AdvancedBufferedWaveProvider(_waveChannel.WaveFormat);
			_inputProvider.MaxQueuedBuffers = 100;

			_soundTouchSharp.SetSampleRate(_waveChannel.WaveFormat.SampleRate);
			_soundTouchSharp.SetChannels(_waveChannel.WaveFormat.Channels);

			_soundTouchSharp.SetTempoChange(0);
			_soundTouchSharp.SetPitchSemiTones(0);
			_soundTouchSharp.SetRateChange(0);

			_soundTouchSharp.SetTempo(1.0f);
			_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_USE_QUICKSEEK, 0);
			ApplyTimeStretchProfiles();

			// Initialize the device
			_waveOutDevice.Init(_inputProvider);
			_waveOutDevice.Play();
		}

		private static void ApplyTimeStretchProfiles(bool useDefault = true)
		{
			if (useDefault)
			{
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_AA_FILTER_LENGTH, 0);
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_USE_AA_FILTER, 0);
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_OVERLAP_MS, 0);
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_SEQUENCE_MS, 0);
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_SEEKWINDOW_MS, 0);	
			}
			else
			{
				// Hardcoded TimeStretchProfile "PracticeSharp Optimum"
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_AA_FILTER_LENGTH, 128);
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_USE_AA_FILTER, 1);
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_OVERLAP_MS, 20);
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_SEQUENCE_MS, 20);
				_soundTouchSharp.SetSetting(SoundTouchSharp.SoundTouchSettings.SETTING_SEEKWINDOW_MS, 80);
			}
		}

		/// <summary>
		/// Primary audio processing method; this is where the samples are read in and manipulations are applied
		/// </summary>
		private static void ProcessAudio()
		{
			WaveFormat format = _currentWaveChannel.WaveFormat;
			int bufferSecondLength = format.SampleRate * format.Channels;
			byte[] inputBuffer = new byte[BufferSamples * sizeof(float)];
			byte[] soundTouchOutBuffer = new byte[BufferSamples * sizeof(float)];

			ByteAndFloatsConverter convertInputBuffer = new ByteAndFloatsConverter { Bytes = inputBuffer };
			ByteAndFloatsConverter convertOutputBuffer = new ByteAndFloatsConverter { Bytes = soundTouchOutBuffer };
			uint outBufferSizeFloats = (uint)convertOutputBuffer.Bytes.Length / (uint)(sizeof(float) * format.Channels);

			int bytesRead;
			int floatsRead;
			uint samplesProcessed = 0;
			int bufferIndex = 0;
			TimeSpan actualEndMarker = TimeSpan.Zero;
			bool loop = false;

			while (_currentWaveChannel.Position < _currentWaveChannel.Length)
			{
				_currentWaveChannel.Volume = 0.25f;
				if (Started)
				{
					_currentWaveChannel.CurrentTime = TimeSpan.FromSeconds(0);
					Started = true;
				}

				bytesRead = _currentWaveChannel.Read(convertInputBuffer.Bytes, 0, convertInputBuffer.Bytes.Length);
				floatsRead = bytesRead / ((sizeof(float)) * _currentWaveChannel.WaveFormat.Channels);

				actualEndMarker = DefaultEndMarker;
				if (!loop || actualEndMarker == TimeSpan.Zero)
				{
					actualEndMarker = _currentWaveChannel.TotalTime;
				}

				if (_currentWaveChannel.CurrentTime > actualEndMarker)
				{
					_soundTouchSharp.Clear();
					_inputProvider.Flush();
					_currentWaveChannel.Flush();

					if (!stopWorker)
					{
						while (!stopWorker && samplesProcessed != 0)
						{
							SetSoundSharpValues();

							samplesProcessed = _soundTouchSharp.ReceiveSamples(convertOutputBuffer.Floats, outBufferSizeFloats);
							if (samplesProcessed > 0)
							{
								TimeSpan currentBufferTime = _currentWaveChannel.CurrentTime;
								_inputProvider.AddSamples(convertOutputBuffer.Bytes, 0, (int)samplesProcessed * (sizeof(float)) * _currentWaveChannel.WaveFormat.Channels, currentBufferTime);
							}
							samplesProcessed = _soundTouchSharp.ReceiveSamples(convertOutputBuffer.Floats, outBufferSizeFloats);
						}
					}
				}

				SetSoundSharpValues();

				_soundTouchSharp.PutSamples(convertInputBuffer.Floats, (uint)floatsRead);
				do
				{
					samplesProcessed = _soundTouchSharp.ReceiveSamples(convertOutputBuffer.Floats, outBufferSizeFloats);
					if (samplesProcessed > 0)
					{
						TimeSpan currentBufferTime = _currentWaveChannel.CurrentTime;
						_inputProvider.AddSamples(convertOutputBuffer.Bytes, 0, (int)samplesProcessed * (sizeof(float)) * _currentWaveChannel.WaveFormat.Channels, currentBufferTime);

						while (!stopWorker && _inputProvider.GetQueueCount() > BusyQueuedBuffersThreshold)
						{
							Thread.Sleep(10);
						}
						bufferIndex += 1;
					}
				} while (!stopWorker && samplesProcessed != 0);
			}

			// End of the audio file
			_waveOutDevice.Stop();
			if (!stopWorker && _currentWaveChannel.CurrentTime < actualEndMarker)
			{
				_currentWaveChannel.CurrentTime = actualEndMarker;
			}
			_soundTouchSharp.Clear();
		}

		private static void SetSoundSharpValues()
		{
			if (tempoChanged)
			{
				_soundTouchSharp.SetTempo(newTempo);
				tempoChanged = false;
			}

			if (pitchChanged)
			{
				_soundTouchSharp.SetPitchSemiTones(newPitch);
				pitchChanged = false;
			}

			if (volumeChanged)
			{
				_waveOutDevice.Volume = newVolume;
				volumeChanged = false;
			}
		}

		#endregion

		#region Private Helper Classes
		/// <summary>
		/// Helper class to consolidate an output stream's bitstream + its current play time
		/// </summary>
		internal class AudioStream
		{
			public WaveChannel32 Stream;
			public TimeSpan CurrentTime;

			public AudioStream(WaveChannel32 outputStream)
			{
				Stream = outputStream;
				CurrentTime = TimeSpan.FromSeconds(0);
			}
		}
		#endregion
	}
}
