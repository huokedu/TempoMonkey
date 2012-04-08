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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;


namespace tempoMonkey
{
    /// <summary>
    /// Interaction logic for FreeFormMode.xaml
    /// </summary>
    public partial class FreeFormMode : Page
    {
        BrushConverter bc = new BrushConverter();
        KinectGesturePlayer freePlayer;
        bool isPaused = false;

        public void freeAllFramesReady(object sender, AllFramesReadyEventArgs e){
            Skeleton skeleton = KinectGesturePlayer.getFristSkeleton(e);
            if (skeleton != null)
            {
                if (!isPaused)
                {
                    freePlayer.skeletonReady(e, skeleton);
                }
            }
        }

        //Handlers
        void pauseTrackingHandler(bool exist)
        {
            pause.Fill = exist ? (Brush)bc.ConvertFrom("GREEN") : (Brush)bc.ConvertFrom("RED");
            if (isPaused)
            {
                return;
            }

            if (exist)
            {
                Pause();
            }
        }

        int previousTrack = 2;
        void changeTrackHandler(double value)
        {
            DebugBox.Content = value.ToString();
            if (value < 250 && previousTrack != 1)
            {
                Track.Content = "On Track 1";
                previousTrack = 1;
                //Go as Track 1
            }
            else if (value > 450 && previousTrack != 3)
            {
                Track.Content = "On Track 3";
                previousTrack = 3;
                //Go as Track 3
            }
            else if( value >= 250 && value <= 450 && previousTrack != 2)
            {
                Track.Content = "On Track 2";
                previousTrack = 2;
                //Go as Track 2
            }
        }

        void volumeChangeHandler(double change)
        {
            Canvas.SetTop(VolumePos, Canvas.GetTop(VolumePos) + change);
        }

        void volumeTrackingHandler(bool exist)
        {
            VolumePos.Fill = exist ? (Brush)bc.ConvertFrom("GREEN") : (Brush)bc.ConvertFrom("RED");
        }

        void tempoChangeHandler(double change)
        {
            Canvas.SetTop(TempoPos, Canvas.GetTop(TempoPos) + change);
        }

        void tempoTrackingHandler(bool exist)
        {
            TempoPos.Fill = exist ? (Brush)bc.ConvertFrom("GREEN") : (Brush)bc.ConvertFrom("RED");
        }

        void seekChangeHandler(double change)
        {
            Canvas.SetLeft(SeekPos, Canvas.GetLeft(SeekPos) + change);
        }

        void seekTrackingHandler(bool exist)
        {
            SeekPos.Fill = exist ? (Brush)bc.ConvertFrom("GREEN") : (Brush)bc.ConvertFrom("RED");
        }

        void pitchChangeHandler(double change)
        {
            Canvas.SetTop(PitchPos, Canvas.GetTop(PitchPos) + change);
        }

        void pitchTrackingHandler(bool exist)
        {
            PitchPos.Fill = exist ? (Brush)bc.ConvertFrom("GREEN") : (Brush)bc.ConvertFrom("RED");
        }

        public FreeFormMode(ArrayList addrList, ArrayList nameList)
        {
            System.Windows.Forms.Cursor.Show();
            InitializeComponent();
            freePlayer = new KinectGesturePlayer();
            freePlayer.registerCallBack(freePlayer.kinectGuideListener, pauseTrackingHandler, changeTrackHandler);
            freePlayer.registerCallBack(freePlayer.handsAboveHeadListener, pitchTrackingHandler, pitchChangeHandler);
            freePlayer.registerCallBack(freePlayer.handSwingListener, seekTrackingHandler, seekChangeHandler);
            freePlayer.registerCallBack(freePlayer.fistsPumpListener, tempoTrackingHandler, tempoChangeHandler);
            freePlayer.registerCallBack(freePlayer.handsWidenListener, volumeTrackingHandler, volumeChangeHandler);
        }

        public void unPause()
        {
            isPaused = false;
            Border.Visibility = System.Windows.Visibility.Hidden;
            Resume.Visibility = System.Windows.Visibility.Hidden;
            Quit.Visibility = System.Windows.Visibility.Hidden;
            System.Windows.Forms.Cursor.Hide();
        }

        public void Pause()
        {
            isPaused = true;
            Border.Visibility = System.Windows.Visibility.Visible;
            Resume.Visibility = System.Windows.Visibility.Visible;
            Quit.Visibility = System.Windows.Visibility.Visible;
            System.Windows.Forms.Cursor.Show();
        }

        private void ResumeEnter(object sender, MouseEventArgs e){
            setSelectionStatus(true);
            direction = 6;
        }

        private void ResumeLeave(object sender, MouseEventArgs e)
        {
            setSelectionStatus(false);
        }

        private void QuitEnter(object sender, MouseEventArgs e)
        {
            setSelectionStatus(true);
            direction = 7;
        }

        private void QuitLeave(object sender, MouseEventArgs e)
        {
            setSelectionStatus(false);
        }

        //Tell the MainWindow which menu button has been selected
        public int getSelectedMenu()
        {
            return direction;
        }


        private void setSelectionStatus(Boolean value)
        {
            isReady = value;
            if (!isReady)
            {
                RaiseEvent(new RoutedEventArgs(MainWindow.resetTimer));
            }
        }

        //Tell the MainWindow if the cursor is on the button.
        public Boolean isSelectionReady()
        {
            return isReady;
        }

        int direction = 999;
        bool isReady = false;
    }
}
