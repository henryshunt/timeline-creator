﻿using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TimelineCreator
{
    public partial class MainWindow : Window
    {
        private TimelineTab? selectedTab => (TimelineTab)theTabControl.SelectedItem;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timeZoneComboBox.ItemsSource = TimeZoneInfo.GetSystemTimeZones();
            timeZoneComboBox.SelectedItem = TimeZoneInfo.Local;

            //TimelineTab tab = TimelineTab.OpenDocument(@"C:\Users\Henry\Starship OFT Timeline 2023-04-20.json");
            //tab.ContextMenu = Resources["tabContextMenu"] as ContextMenu;
            //theTabControl.Items.Add(tab);

            theTabControl.Items.Add(TimelineTab.NewDocument());

            //TimelineTab document = TimelineTab.NewDocument();
            //document.Timeline.Items.Add(new TimelineItem() { DateTime = new DateTime(2023, 7, 13, 0, 0, 0) });
            //document.Timeline.Items.Add(new TimelineItem() { DateTime = new DateTime(2023, 7, 13, 12, 0, 0) });
            //document.Timeline.Items.Add(new TimelineItem() { DateTime = new DateTime(2023, 7, 14, 0, 0, 0) });
            //theTabControl.Items.Add(document);

            theTabControl.SelectedIndex = theTabControl.Items.Count - 1;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            foreach (TimelineTab tab in theTabControl.Items)
            {
                if (tab.HasUnsavedChanges)
                {
                    if (MessageBox.Show("You have unsaved changes. Continue?", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                    }

                    break;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsRepeat == false)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
                {
                    NewButton_Click(this, new RoutedEventArgs());
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
                {
                    OpenButton_Click(this, new RoutedEventArgs());
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
                {
                    SaveButton_Click(this, new RoutedEventArgs());
                }
                else if (e.Key == Key.Escape)
                {
                    if (selectedTab?.Timeline.SelectedItem != null)
                    {
                        selectedTab.Timeline.SelectedItem = null;
                    }
                }
                else if (e.Key == Key.Delete)
                {
                    if (selectedTab?.Timeline.SelectedItem != null)
                    {
                        selectedTab.Timeline.Items.Remove(selectedTab.Timeline.SelectedItem);
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D0)
                {
                    selectedTab?.Timeline.ResetZoom();
                }
            }
        }

        #region Main Controls
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            theTabControl.Items.Add(TimelineTab.NewDocument());
            theTabControl.SelectedIndex = theTabControl.Items.Count - 1;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new()
            {
                Filter = "JSON Files (*.json) | *.json"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    // If a single new, empty document is open then remove it
                    if (theTabControl.Items.Count == 1 &&
                        ((TimelineTab)theTabControl.Items[0]).HasUnsavedChanges == false &&
                        ((TimelineTab)theTabControl.Items[0]).FilePath == null)
                    {
                        theTabControl.Items.RemoveAt(0);
                    }
                    else
                    {
                        // If the selected file is already open then select that tab
                        foreach (TimelineTab tab in theTabControl.Items)
                        {
                            if (tab.FilePath == openDialog.FileName)
                            {
                                theTabControl.SelectedItem = tab;
                                return;
                            }
                        }
                    }

                    theTabControl.Items.Add(TimelineTab.OpenDocument(openDialog.FileName));
                    theTabControl.SelectedIndex = theTabControl.Items.Count - 1;
                }
                catch (IOException)
                {
                    MessageBox.Show("Error opening file.");
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTab?.FilePath == null)
            {
                SaveFileDialog saveDialog = new()
                {
                    Filter = "JSON Files (*.json) | *.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    try
                    {
                        selectedTab?.SaveDocumentAs(saveDialog.FileName);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("Error saving file.");
                    }
                }
            }
            else
            {
                selectedTab.SaveDocument();
            }
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            ItemDialog itemDialog = new((TimeZoneInfo)timeZoneComboBox.SelectedItem) { Owner = this };

            if (itemDialog.ShowDialog() == true)
            {
                // Add item at correct position in time order
                int i = 0;
                for (; i < selectedTab!.Timeline.Items.Count; i++)
                {
                    if (selectedTab.Timeline.Items[i].DateTime > itemDialog.Item.DateTime)
                        break;
                }

                selectedTab.Timeline.Items.Insert(i, itemDialog.Item);
            }
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            selectedTab?.Timeline.ResetZoom();
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTab != null)
            {
                if (selectedTab.HasUnsavedChanges)
                {
                    if (MessageBox.Show("You have unsaved changes. Continue?", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                int tabIndex = theTabControl.Items.IndexOf(selectedTab);
                theTabControl.Items.Remove(selectedTab);

                if (theTabControl.Items.Count == 0)
                {
                    theTabControl.SelectedItem = null;
                }
                else if (theTabControl.Items.Count >= tabIndex)
                {
                    theTabControl.SelectedIndex = tabIndex;
                }
                else if (theTabControl.Items.Count - 1 == tabIndex)
                {
                    theTabControl.SelectedIndex = tabIndex - 1;
                }
            }
        }
        #endregion

        #region Tabs
        private void TheTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                TimelineTab removedTab = (TimelineTab)e.RemovedItems[0]!;
                Title = "Timeline Creator";

                removedTab.Timeline.SelectedItem = null;
                removedTab.Timeline.SelectionChanged -= Timeline_SelectionChanged;
                removedTab.Timeline.MouseDoubleClick -= Timeline_MouseDoubleClick;

                docTitleTextBox.TextChanged -= DocTitleTextBox_TextChanged;
                docTitleTextBox.Text = null;
                docDescripTextBox.TextChanged -= DocDescripTextBox_TextChanged;
                docDescripTextBox.Text = null;
                timeZoneComboBox.SelectionChanged -= TimeZoneComboBox_SelectionChanged;
                timeZoneComboBox.SelectedItem = TimeZoneInfo.Local;
            }

            if (e.AddedItems.Count > 0)
            {
                TimelineTab addedTab = (TimelineTab)e.AddedItems[0]!;
                Title = $"{addedTab.Title} - Timeline Creator";

                addedTab.Timeline.SelectionChanged += Timeline_SelectionChanged;
                addedTab.Timeline.MouseDoubleClick += Timeline_MouseDoubleClick;

                docTitleTextBox.Text = addedTab.Title;
                docTitleTextBox.TextChanged += DocTitleTextBox_TextChanged;
                docDescripTextBox.Text = addedTab.Description;
                docDescripTextBox.TextChanged += DocDescripTextBox_TextChanged;
                timeZoneComboBox.SelectedItem = addedTab.TimeZone;
                timeZoneComboBox.SelectionChanged += TimeZoneComboBox_SelectionChanged;
            }
        }

        private void Timeline_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && e.RemovedItems.Count == 1 &&
                Keyboard.Modifiers == ModifierKeys.Control)
            {
                TimeSpan diff = ((TimelineItem)e.AddedItems[0]!).DateTime - ((TimelineItem)e.RemovedItems[0]!).DateTime;
                MessageBox.Show($"Difference is {diff.Duration().ToString("h'h 'm'm 's's'")}");
                //((Timeline)sender!).SelectedItem = null;
            }
        }

        private void Timeline_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((Timeline)sender).SelectedItem != null)
            {
                new ItemDialog((TimeZoneInfo)timeZoneComboBox.SelectedItem, ((Timeline)sender).SelectedItem!)
                {
                    Owner = this
                }.ShowDialog();
            }
        }

        private void CloseTabMenu_Click(object sender, RoutedEventArgs e)
        {
            TimelineTab tab = (TimelineTab)sender;
            int tabIndex = theTabControl.Items.IndexOf(tab);
            theTabControl.Items.Remove(tab);

            if (theTabControl.Items.Count == 0)
                theTabControl.SelectedItem = null;
            else if (theTabControl.Items.Count >= tabIndex)
                theTabControl.SelectedIndex = tabIndex;
            else if (theTabControl.Items.Count - 1 == tabIndex)
                theTabControl.SelectedIndex = tabIndex - 1;
        }
        #endregion

        private void DocTitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedTab != null)
            {
                selectedTab.Title = ((TextBox)sender).Text;
            }
        }

        private void DocDescripTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedTab != null)
            {
                selectedTab.Description = ((TextBox)sender).Text;
            }
        }

        private void TimeZoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedTab != null)
            {
                selectedTab.TimeZone = (TimeZoneInfo)((ComboBox)sender).SelectedItem;
            }
        }

        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedTab != null)
            {
                int percentage = (int)Math.Round(((Slider)sender).Value);
                widthLabel.Text = percentage + "%";
                selectedTab.Timeline.TimelineWidth = percentage;
            }
        }
    }
}