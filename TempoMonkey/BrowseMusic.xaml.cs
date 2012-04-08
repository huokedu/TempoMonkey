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

namespace TempoMonkey
{
    /// <summary>
    /// Interaction logic for BrowseMusic.xaml
    /// </summary>
    public partial class BrowseMusic : Page
    {
        ArrayList musicAddrList = new ArrayList();
        ArrayList musicList = new ArrayList();
        Boolean selectionDone;
        int direction;
        Boolean isReady;

        public BrowseMusic()
        {
            InitializeComponent();
            this.slidingMenu.initializeMenu("Music");
            selectionDone = false;
            direction = 999;
            isReady = false;
        }

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

        bool musicChooser = false;
        public bool getMusicChoose(){
            return musicChooser;
        }
        //Tell the MainWindow if the cursor is on the button.
        public Boolean isSelectionReady()
        {
            musicChooser = isMenuSelectionValid();
            return isReady;
        }

        public bool isMenuSelectionValid() //function used when push gesture is performed
        {
            if (slidingMenu.hasCurrentSelectedBox())
            {
                foreach (var name in musicList)
                {
                    if (((string)name).Equals(slidingMenu.getName()))
                    {

                        return false;
                    }
                }
                if (musicAddrList.Count == 3)
                {
                    return false;
                }
                else
                {
                    direction = 5;
                    isReady = true;
                    return true;
                }
            }
            return false;
        }



        public void addingToMusicAddrList() 
        {
            musicAddrList.Add(this.slidingMenu.getAddress());
        }

        public void addingToMusicList()
        {
            Label myLabel = new Label();
            myLabel.Content = this.slidingMenu.getName();
            selectedMusicList.Children.Add(myLabel);
            musicList.Add(this.slidingMenu.getName());
        }

        public void deletingMusic()
        {
            int i = musicList.Count - 1;            
            selectedMusicList.Children.RemoveAt(i);
            musicAddrList.RemoveAt(i);
            musicList.RemoveAt(i);
        }


        public Boolean isSelectionDone()
        {
            return selectionDone;
        }

        public ArrayList getMusicAddrList()
        {
            return musicAddrList;
        }

        public ArrayList getMusicList()
        {
            return musicList;
        }

        private void done_MouseEnter(object sender, MouseEventArgs e)
        {
            setSelectionStatus(true);
            selectionDone = true;
            direction = 4;
        }
        private void done_MouseLeave(object sender, MouseEventArgs e)
        {
            setSelectionStatus(false);
            selectionDone = false;
        }

        private void back_MouseEnter(object sender, MouseEventArgs e)
        {
            setSelectionStatus(true);
            direction = 3;
        }

        private void back_MouseLeave(object sender, MouseEventArgs e)
        {
            setSelectionStatus(false);           
        }

        private void delete_MouseEnter(object sender, MouseEventArgs e)
        {
            setSelectionStatus(true);
            direction = 9;
        }

        private void delete_MouseLeave(object sender, MouseEventArgs e)
        {
            setSelectionStatus(false);
        }

    }
}
