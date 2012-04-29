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
using System.IO;
using System.Windows.Media.Animation;
using slidingMenu;

namespace TempoMonkey
{
    /// <summary>
    /// Interaction logic for BrowseMusic.xaml
    /// </summary>
    public partial class BrowseMusic : Page, SelectionPage
    {

        string _type;
        int sizeOfBox = 100;
        List<box> mySelections = new List<box>();
        Grid myGrid;
        int gridRows, gridCols;

		NavigationButton backButton, doneButton;

        public void initBrowseMusic(string type)
        {
            _type = type;
        }

        public BrowseMusic()
        {
            InitializeComponent();
            addGrid((int)MainWindow.height, (int)MainWindow.width);
            addItemsToGrid();
			// Create navigation buttons
			backButton = new NavigationButton(BackButton, delegate()
			{
			    return MainWindow.homePage;
			});
            
            doneButton = new NavigationButton(DoneButton, delegate(){
                if (mySelections.Count == 0)
                {
                        return null;
                } else 
                {
                    ArrayList musicAddrList = new ArrayList();
                    ArrayList musicList = new ArrayList();

                    foreach (box selection in mySelections)
                    {
                        musicAddrList.Add(selection.address);
                        musicList.Add(selection.name);
                    }

                    if (_type == "Buddy")
                    {
                        ((FreeFormMode)MainWindow.freeFormPage).initBuddyForm( 
                            ((string)musicAddrList[0]), 
                            ((string)musicList[0]));
                        MainWindow.isManipulating = true;
                    }

                    else if (_type == "Solo")
                    {
                        ((FreeFormMode)MainWindow.freeFormPage).initSoloForm(musicAddrList, musicList);
                        MainWindow.isManipulating = true;
                    }

                    else
                    {
                        throw new Exception();
                    }
                    return MainWindow.freeFormPage;
                }
            });

        }

        #region Grid stuff
        /* Creates a grid dyanmically with demensions equal to (height/100) by (width/100) */
        private void addGrid(int height, int width)
        {
            myGrid = new Grid();

            int sizeofCell = sizeOfBox + sizeOfBox / 5;
            int heightOffSet = 120;
            int widthOffSet = 70;
            gridRows = (height - heightOffSet) / sizeofCell;
            gridCols = (width - widthOffSet) / sizeofCell;

            for (int i = 0; i < gridCols; i += 1)
            {
                ColumnDefinition row = new ColumnDefinition();
                row.Width = new System.Windows.GridLength(sizeofCell + sizeofCell / 3);
                myGrid.ColumnDefinitions.Add(row);
            }

            for (int j = 0; j < gridRows; j += 1)
            {
                RowDefinition row = new RowDefinition();
                row.Height = new System.Windows.GridLength(sizeofCell + sizeofCell / 3);
                myGrid.RowDefinitions.Add(row);
            }

            mainCanvas.Children.Add(myGrid);
            Canvas.SetLeft(myGrid, widthOffSet);
            Canvas.SetTop(myGrid, heightOffSet);
        }

        private void addItemsToGrid()
        {
            int index = 0;
            foreach (string filepath in Directory.GetFiles(@"..\..\Resources\Music", "*.mp3"))
            {
                int colspot = index % gridRows;
                int rowspot = index / gridRows;
                string filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
                addToBox(filename, filepath, rowspot, colspot);
                index += 1;
            }
            myGrid.UpdateLayout();
        }

        private void addToBox(string name, string address, int rowspot, int colspot) // instantiate a box instance
        {
            box littleBox = new box(sizeOfBox, this);

            littleBox.MouseEnter += Mouse_Enter;
            littleBox.MouseLeave += Mouse_Leave;

            littleBox.boxName = name;
            littleBox.address = address;
            littleBox.name = name;
            string path = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\Images\\Album_Art\\" + name + ".jpg";
            littleBox.setImage(path);

            Grid.SetRow(littleBox, rowspot);
            Grid.SetColumn(littleBox, colspot);
            myGrid.Children.Add(littleBox);
        }
        #endregion

        public void unSelectBox(box Box)
        {
            Box.unHighlightBox();
            mySelections.Remove(Box);
        }

        public void SelectBox(box Box)
        {
            Box.highlightBox();
            bool tooMuch = (_type == "Solo" && mySelections.Count >= 3) || (_type == "Buddy" && mySelections.Count >= 1);
            if (tooMuch)
            {
                unSelectBox(mySelections[0]);
                mySelections.Add(Box);
            }
            else
            {
                mySelections.Add(Box);
            }
        }

        public void Click()
        {
            box currentlySelectedBox = (box)MainWindow.currentlySelectedObject;
            if (!mySelections.Contains(currentlySelectedBox))
            {
                SelectBox(currentlySelectedBox);
            }
            else
            {
                unSelectBox(currentlySelectedBox);
            }
        }

        #region Mouse Events
		private void Back_Enter(object sender, MouseEventArgs args)
		{
			MainWindow.MouseEnter(backButton);
		}

        private void Mouse_Enter(object sender, MouseEventArgs e)
        {
            MainWindow.Mouse_Enter(sender, e);
        }

        private void Mouse_Leave(object sender, MouseEventArgs e)
        {
            MainWindow.Mouse_Leave(sender, e);
        }

        private void Done_Leave(object sender, MouseEventArgs e)
        {
            MainWindow.Mouse_Leave(sender, e);
        }

        private void Done_Enter(object sender, MouseEventArgs e)
        {
            MainWindow.MouseEnter(doneButton);
        }

        #endregion

    }
}
