﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections;
using System.Windows.Navigation;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using System.Windows.Media.Animation;
using System.IO;
using System.Drawing;
using System.Windows.Threading;
using slidingMenu;
using Visualizer;

namespace TempoMonkey
{
	/// <summary>
	/// Interaction logic for FreeFormMode.xaml
	/// </summary>
    public partial class FreeFormMode : Page, KinectPage
	{
        KinectGesturePlayer freePlayer, freePlayer2;
		bool _isPaused = false;
        ArrayList _nameList = new ArrayList();
        string _type;
        public mySlider VolumeSlider, PitchSlider, TempoSlider;
		public Spectrum Visualizer;

        public void initCommon()
        {
            PauseCircle.Stroke = System.Windows.Media.Brushes.White;
            PauseCircle.StrokeThickness = 8;

            waveFormContainers[0] = SongContainer0;
            waveFormContainers[1] = SongContainer1;
            waveFormContainers[2] = SongContainer2;
            waveFormContainers[0].Visibility = System.Windows.Visibility.Hidden;
            waveFormContainers[1].Visibility = System.Windows.Visibility.Hidden;
            waveFormContainers[2].Visibility = System.Windows.Visibility.Hidden;
            SongTitles[0] = SongTitle0;
            SongTitles[1] = SongTitle1;
            SongTitles[2] = SongTitle2;
            SongTitles[0].Text = null;
            SongTitles[1].Text = null;
            SongTitles[2].Text = null;

			Visualizer = new Spectrum(mainCanvas);
			Visualizer.RegisterSoundPlayer();
            MainWindow.setManipulating(true);
        }

        public void initSliders()
        {
            VolumeSlider = new mySlider("Volume", 10, 100, 25, 250);
            PitchSlider = new mySlider("Pitch", 0, 100, 50, 250);
            TempoSlider = new mySlider("Tempo", 40, 200, 100, 250);

            Canvas.SetLeft(VolumeSlider, 450);
            Canvas.SetLeft(PitchSlider, 450);
            Canvas.SetLeft(TempoSlider, 450);

            Canvas.SetTop(VolumeSlider, 200);
            Canvas.SetTop(PitchSlider, 300);
            Canvas.SetTop(TempoSlider, 400);

            /*
            mainCanvas.Children.Add(VolumeSlider);
            mainCanvas.Children.Add(PitchSlider);
            mainCanvas.Children.Add(TempoSlider);
             * */
        }

        BrushConverter bc = new BrushConverter();
        TextBlock[] SongTitles = new TextBlock[3];
        Panel[] waveFormContainers = new Panel[3];
        int[] positions = { 260, 525, 850 };
        public int currentTrackIndex
        {
            set
            {
                waveFormContainers[_currentTrackIndex].Visibility = System.Windows.Visibility.Hidden;
                SongTitles[_currentTrackIndex].Foreground = ((System.Windows.Media.Brush)bc.ConvertFrom("#666"));
                _currentTrackIndex = value;
                waveFormContainers[_currentTrackIndex].Visibility = System.Windows.Visibility.Visible;
                SongTitles[_currentTrackIndex].Foreground = ((System.Windows.Media.Brush)bc.ConvertFrom("#FFF"));
                Canvas.SetLeft(BlueDot, positions[_currentTrackIndex]);
                Processing.Audio.SwapTrack(_currentTrackIndex);
            }
            get
            {
                return _currentTrackIndex;
            }
        }
        
        public void initWaveForm(Panel waveFormContainer, string uri,
            Visualizer.Timeline.WaveformTimeline.CompletionCallback callback = null)
        {
            Visualizer.Timeline.WaveformTimeline wave = new Visualizer.Timeline.WaveformTimeline(waveFormContainer, uri, callback);
            wave.Draw();
			// This might be more appropriate to call after Audio.Play() is called, but currently there is no
			// reference to these wave objects.  Tracking checks if the Audio is playing, so it shouldn't break.
			wave.StartTracking();
        }

        public void initBuddyForm(string address, string name)
        {
            _type = "Buddy"; 
            initCommon();

            // Load and set the song titles
            SongTitles[1].Text = name;
            Processing.Audio.LoadFile(address);

            // NavigationService.Navigate(MainWindow.freeFormPage);
            // Initlaize the wave form
            initWaveForm(waveFormContainers[1], address, delegate()
            {
                MainWindow.currentPage = MainWindow.freeFormPage;
                MainWindow.loadingPage.NavigationService.Navigate(MainWindow.currentPage);

                // Sets the current track & also plays it
                currentTrackIndex = 1;
                Processing.Audio.Play(0);
            });
            _nameList.Add(address);

            // connected to gestures
            freePlayer = new KinectGesturePlayer();
            freePlayer.registerCallBack(freePlayer.kinectGuideListener, pauseTrackingHandler, null);
            freePlayer.registerCallBack(freePlayer.handsAboveHeadListener, pitchTrackingHandler, pitchChangeHandler);
            freePlayer.registerCallBack(freePlayer.leanListener, tempoTrackingHandler, tempoChangeHandler);
            freePlayer.registerCallBack(freePlayer.handsWidenListener, volumeTrackingHandler, volumeChangeHandler);

            freePlayer2 = new KinectGesturePlayer();
            freePlayer2.registerCallBack(freePlayer2.kinectGuideListener, pauseTrackingHandler, null);
            freePlayer2.registerCallBack(freePlayer2.handsAboveHeadListener, pitchTrackingHandler2, pitchChangeHandler);
            freePlayer2.registerCallBack(freePlayer2.leanListener, tempoTrackingHandler2, tempoChangeHandler);
            freePlayer2.registerCallBack(freePlayer2.handsWidenListener, volumeTrackingHandler2, volumeChangeHandler);
        }

        

        public void initSoloForm(ArrayList addrList, ArrayList nameList){
            _type = "Solo"; 
            initCommon();
            int doneCount = 0;
            // Load and set the song titles
            for( int i=0; i < addrList.Count; i++)
			{
               
                string address = addrList[i] as String;
                string name = nameList[i] as String;
                _nameList.Add(name);
                SongTitles[i].Text = name;
                Processing.Audio.LoadFile(address);

                // Initlaize the wave form
                initWaveForm(waveFormContainers[i], address, delegate()
                {
                    doneCount++;
                    if (doneCount == _nameList.Count)
                    {
                        MainWindow.currentPage = MainWindow.freeFormPage;
                        MainWindow.loadingPage.NavigationService.Navigate(MainWindow.currentPage);

                        // Sets the current track & also plays it
                        currentTrackIndex = _nameList.Count > 1 ? 1 : 0;
                        Processing.Audio.Play(currentTrackIndex);
                    }
                });
			}

            // connected to gestures
            freePlayer = new KinectGesturePlayer();
			freePlayer.registerCallBack(freePlayer.kinectGuideListener, pauseTrackingHandler, pauseChangeHandler);           
			freePlayer.registerCallBack(freePlayer.handsAboveHeadListener, pitchTrackingHandler, pitchChangeHandler);
			freePlayer.registerCallBack(freePlayer.leanListener, tempoTrackingHandler, tempoChangeHandler);
			freePlayer.registerCallBack(freePlayer.handsWidenListener, volumeTrackingHandler, volumeChangeHandler);
            freePlayer.registerCallBack(freePlayer.trackMoveListener, changeTrackTrackingHandler, null);
        }

        public void tearDown()
        {
            freePlayer = null;
            freePlayer2 = null;
            MainWindow.setManipulating(false);
			Processing.Audio.End();
        }

        NavigationButton quitButton, resumeButton;
		public FreeFormMode()
		{
			InitializeComponent();
            InitializeAvatars();
            initSliders();

            quitButton = new NavigationButton(QuitButton, delegate()
            {
                tearDown();
                return MainWindow.homePage;
            });

            resumeButton = new NavigationButton(ResumeButton, delegate()
            {
                Resume();
                return null;
            });
		}

        public void allFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (!_isPaused)
            {
                if (_type == "Solo")
                {
                    Skeleton skeleton = KinectGesturePlayer.getFristSkeleton(e);
                    if (skeleton != null)
                    {
                        freePlayer.skeletonReady(e, skeleton);
                    }
                }
                else if (_type == "Buddy")
                {
                    Skeleton[] skeletons = KinectGesturePlayer.getFirstTwoSkeletons(e);
                    Skeleton leftSkeleton;
                    Skeleton rightSkeleton;
                    if (skeletons == null)
                    {
                        return;
                    }
                    if (skeletons.Length == 0)
                    {
                        return;
                    }
                    else if (skeletons.Length == 1)
                    {
                        leftSkeleton = skeletons[0];
                        freePlayer.skeletonReady(e, leftSkeleton);
                    }
                    else
                    {
                        leftSkeleton = skeletons[0];
                        rightSkeleton = skeletons[1];
                        freePlayer.skeletonReady(e, leftSkeleton);
                        freePlayer2.skeletonReady(e, rightSkeleton);
                    }
                }
            }
        }

		private Dictionary<string, BitmapImage> loadedImages = new Dictionary<string, BitmapImage>();
		private BitmapImage InitializeResource(Bitmap source, string name)
		{
			BitmapImage image = new BitmapImage();
			MemoryStream stream = new MemoryStream();
			source.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
			image.BeginInit();
			image.StreamSource = new MemoryStream(stream.ToArray());
			image.EndInit();
			loadedImages.Add(name, image);
			return image;
		}

		public void InitializeAvatars()
		{
			// Active images
			InitializeResource(Properties.Resources.volume_avatar, "volumeAvatar");
			InitializeResource(Properties.Resources.seek_avatar, "seekAvatar");
			InitializeResource(Properties.Resources.pitch_avatar, "pitchAvatar");
			InitializeResource(Properties.Resources.tempo_avatar, "tempoAvatar");
            // InitializeResource(Properties.Resources.seek_avatar, "pauseAvatar");


			// Disabled images
			volumeAvatar.Source = InitializeResource(Properties.Resources.volume_avatar_disabled, "volumeAvatarDisabled");
			seekAvatar.Source = InitializeResource(Properties.Resources.seek_avatar_disabled, "seekAvatarDisabled");
			pitchAvatar.Source = InitializeResource(Properties.Resources.pitch_avatar_disabled, "pitchAvatarDisabled");
			tempoAvatar.Source = InitializeResource(Properties.Resources.tempo_avatar_disabled, "tempoAvatarDisabled");
            // PauseAvatar.Source = InitializeResource(Properties.Resources.seek_avatar, "pauseAvatar");
		}

		public void SetAvatarState(bool active, System.Windows.Controls.Image imageControl, BitmapImage image)
		{
			imageControl.Source = image;
			imageControl.Height = active ? 75 : 50;
			imageControl.Width = active ? 75 : 50;
			Canvas.SetTop(imageControl, Canvas.GetTop(imageControl) + (active ? -12.5 : 12.5));
			Canvas.SetLeft(imageControl, Canvas.GetLeft(imageControl) + (active ? -12.5 : 12.5));
		}

		#region Gesture Handlers
		void pauseTrackingHandler(bool exist)
		{
			if (_isPaused)
			{
				return;
			}

			if (exist)
			{
				Pause();
			}
		}

        public void pauseChangeHandler(double angle)
        {
            if (angle > 0 && angle < 360)
            {
                PauseCircle.Visibility = System.Windows.Visibility.Visible;
                PauseLabel.Visibility = System.Windows.Visibility.Visible;

                double x = Canvas.GetLeft(PauseLabel) + 25;
                double y = Canvas.GetTop(PauseLabel) + 15;

                System.Windows.Point center = new System.Windows.Point(x, y);

                System.Windows.Point endPoint = new System.Windows.Point(center.X + 40 * Math.Sin(angle / 180.0 * Math.PI), center.Y - 40 * Math.Cos(angle / 180.0 * Math.PI));

                PathFigure figure = new PathFigure();
                figure.StartPoint = new System.Windows.Point(center.X, center.Y - 40);

                figure.Segments.Add(new ArcSegment(
                    endPoint,
                    new System.Windows.Size(40, 40),
                    0,
                    angle >= 180,
                    SweepDirection.Clockwise,
                    true
                ));

                PathGeometry geometry = new PathGeometry();
                geometry.Figures.Add(figure);

                PauseCircle.Data = geometry;

            } else {
                PauseCircle.Visibility = System.Windows.Visibility.Hidden;
                PauseLabel.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        void changeTrackTrackingHandler(bool right)
        {
            if (right)
            {
                if (currentTrackIndex < _nameList.Count - 1)
                {
                    currentTrackIndex += 1;
                }
            }
            else
            {
                if (currentTrackIndex != 0)
                {
                    currentTrackIndex -= 1;
                }
            }
        }

		int _currentTrackIndex;

		void volumeChangeHandler(double change)
		{
			VolumeSlider.Value -= change;
			Processing.Audio.ChangeVolume(VolumeSlider.Value);
		}

		void volumeTrackingHandler(bool exist)
		{
            VolumeSlider.player1Exists = exist;
			SetAvatarState(exist, volumeAvatar, exist ? loadedImages["volumeAvatar"] : loadedImages["volumeAvatarDisabled"]);
		}

        void volumeTrackingHandler2(bool exist)
        {
            VolumeSlider.player2Exists = exist;
        }

		void tempoChangeHandler(double change)
		{
			TempoSlider.Value += change / 2;
			Processing.Audio.ChangeTempo(TempoSlider.Value);
		}


		void tempoTrackingHandler(bool exist)
		{
            TempoSlider.player1Exists = exist;
			SetAvatarState(exist, tempoAvatar, exist ? loadedImages["tempoAvatar"] : loadedImages["tempoAvatarDisabled"]);
		}

        void tempoTrackingHandler2(bool exist)
        {
            TempoSlider.player2Exists = exist;
        }

        void pitchTrackingHandler(bool exist)
        {
            PitchSlider.player1Exists = exist; 
            SetAvatarState(exist, pitchAvatar, exist ? loadedImages["pitchAvatar"] : loadedImages["pitchAvatarDisabled"]);
        }

        void pitchTrackingHandler2(bool exist)
        {
            PitchSlider.player2Exists = exist; 
        }

		bool wasSeeking = false;
		void seekChangeHandler(double change)
		{
			//SeekSlider.Value += change; //UNCOMMENT THIS IF WE WANT TO SEEK
		}

		void seekTrackingHandler(bool exist)
		{
			if (exist)
			{
				wasSeeking = true;
			}
			else
			{
				if (wasSeeking)
				{
					//Processing.Audio.Seek(SeekSlider.Value); // UNCOMMENT THIS OUT IF WE WANT TO SEEK IN THE FUTURE
				}
				wasSeeking = false;
			}

			SetAvatarState(exist, seekAvatar, exist ? loadedImages["seekAvatar"] : loadedImages["seekAvatarDisabled"]);
		}

		void pitchChangeHandler(double change)
		{
			PitchSlider.Value -= change * 3;
			Processing.Audio.ChangePitch(PitchSlider.Value);
		}

		public void Resume()
		{
			_isPaused = false;
            Processing.Audio.Resume();

            mainCanvas.Background = new SolidColorBrush(Colors.Black);
            PauseOverlay.Visibility = System.Windows.Visibility.Hidden;
            MainWindow.setManipulating(true);

		}

        public void Pause()
        {
            _isPaused = true;
            Processing.Audio.Pause();

            mainCanvas.Background = new SolidColorBrush(Colors.Gray);
            PauseOverlay.Visibility = System.Windows.Visibility.Visible;
            MainWindow.setManipulating(false);
        }
		#endregion


    }
}
