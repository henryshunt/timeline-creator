using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace TimelineCreator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timeZoneComboBox.ItemsSource = TimeZoneInfo.GetSystemTimeZones();
            timeZoneComboBox.SelectedItem = TimeZoneInfo.Local;

            //TimelineTab tab = TimelineTab.OpenDocument(
            //    "C:/Users/Henry/Documents/Timelines/B9 Static Fire 2023-08-25.json");
            //theTabControl.Items.Add(tab);

            theTabControl.Items.Add(TimelineTab.NewDocument());
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
                    GetSelectedTab().Timeline.SelectedItem = null;
                }
                else if (e.Key == Key.Delete)
                {
                    if (GetSelectedTab().Timeline.SelectedItem != null)
                    {
                        GetSelectedTab().Timeline.Items.Remove(GetSelectedTab().Timeline.SelectedItem!);
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D0)
                {
                    GetSelectedTab().Timeline.ResetZoom();
                }
                else if (e.Key == Key.Home) // Move view range to start at first item
                {
                    if (GetSelectedTab().Timeline.Items.Count > 0)
                    {
                        (DateTime, DateTime) viewRangeBounds = GetSelectedTab().Timeline.GetViewRange();
                        TimeSpan viewRange = viewRangeBounds.Item2 - viewRangeBounds.Item1;

                        GetSelectedTab().Timeline.GoToViewRange(GetSelectedTab().Timeline.Items[0].DateTime,
                            GetSelectedTab().Timeline.Items[0].DateTime + viewRange);
                    }
                }
                else if (e.Key == Key.End) // Move view range to end at last item
                {
                    if (GetSelectedTab().Timeline.Items.Count > 0)
                    {
                        (DateTime, DateTime) viewRangeBounds = GetSelectedTab().Timeline.GetViewRange();
                        TimeSpan viewRange = viewRangeBounds.Item2 - viewRangeBounds.Item1;

                        GetSelectedTab().Timeline.GoToViewRange(
                            GetSelectedTab().Timeline.Items.Last().DateTime - viewRange,
                            GetSelectedTab().Timeline.Items.Last().DateTime);
                    }
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
            if (GetSelectedTab().FilePath == null)
            {
                string fileName = docTitleTextBox.Text;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '-');
                }

                SaveFileDialog saveDialog = new()
                {
                    Filter = "JSON Files (*.json) | *.json",
                    FileName = fileName,
                };

                if (saveDialog.ShowDialog() == true)
                {
                    try
                    {
                        GetSelectedTab().SaveDocumentAs(saveDialog.FileName);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("Error saving file.");
                    }
                }
            }
            else
            {
                GetSelectedTab().SaveDocument();
            }
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            ItemDialog itemDialog = new((TimeZoneInfo)timeZoneComboBox.SelectedItem) { Owner = this };

            if (itemDialog.ShowDialog() == true)
            {
                // Add item at correct position in time-based ordering
                int i = 0;
                for (; i < GetSelectedTab().Timeline.Items.Count; i++)
                {
                    if (GetSelectedTab().Timeline.Items[i].DateTime > itemDialog.Item.DateTime)
                    {
                        break;
                    }
                }

                GetSelectedTab().Timeline.Items.Insert(i, itemDialog.Item);

                // Centre view range on the new item if it's outside the current view
                if (itemDialog.Item.DateTime < GetSelectedTab().Timeline.GetViewRange().Item1 ||
                    itemDialog.Item.DateTime > GetSelectedTab().Timeline.GetViewRange().Item2)
                {
                    TimeSpan viewRangeHalf = (GetSelectedTab().Timeline.GetViewRange().Item2 -
                        GetSelectedTab().Timeline.GetViewRange().Item1) / 2;

                    GetSelectedTab().Timeline.GoToViewRange(itemDialog.Item.DateTime - viewRangeHalf,
                        itemDialog.Item.DateTime + viewRangeHalf);
                }

                GetSelectedTab().Timeline.SelectedItem = itemDialog.Item;
            }
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedTab().Timeline.ResetZoom();
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedTab().HasUnsavedChanges)
            {
                if (MessageBox.Show("You have unsaved changes. Continue?", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

            int tabIndex = theTabControl.Items.IndexOf(GetSelectedTab());
            theTabControl.Items.Remove(GetSelectedTab());

            if (theTabControl.Items.Count == 0)
            {
                NewButton_Click(this, new RoutedEventArgs());
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

        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            GetSelectedTab().TimelineWidth = (int)Math.Round(((Slider)sender).Value);
        }

        private void T0TimeField_ValueChanged(object? sender, DateTimeValueChangedEventArgs e)
        {
            GetSelectedTab().TZeroTime = t0TimeField.Value;
        }

        private void T0ModeCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            GetSelectedTab().TZeroMode = ((CheckBox)sender).IsChecked == true;
        }

        private void DocTitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetSelectedTab().Title = ((TextBox)sender).Text;
        }

        private void DocDescripTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetSelectedTab().Description = ((TextBox)sender).Text;
        }

        private void TimeZoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetSelectedTab().TimeZone = (TimeZoneInfo)((ComboBox)sender).SelectedItem;
        }
        #endregion

        #region Tabs
        private TimelineTab GetSelectedTab() => (TimelineTab)theTabControl.SelectedItem;

        private void TheTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                TimelineTab removedTab = (TimelineTab)e.RemovedItems[0]!;
                Title = "Timeline Creator";

                removedTab.Timeline.SelectedItem = null;
                removedTab.Timeline.SelectionChanged -= Timeline_SelectionChanged;
                removedTab.Timeline.MouseDoubleClick -= Timeline_MouseDoubleClick;

                widthSlider.ValueChanged -= WidthSlider_ValueChanged;
                widthSlider.Value = 0;
                t0TimeField.ValueChanged -= T0TimeField_ValueChanged;
                t0TimeField.Value = null;
                t0ModeCheckBox.Checked -= T0ModeCheckBox_CheckedChanged;
                t0ModeCheckBox.Unchecked -= T0ModeCheckBox_CheckedChanged;
                t0ModeCheckBox.IsChecked = false;
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

                widthSlider.Value = addedTab.TimelineWidth;
                widthSlider.ValueChanged += WidthSlider_ValueChanged;
                t0TimeField.Value = addedTab.TZeroTime;
                t0TimeField.ValueChanged += T0TimeField_ValueChanged;
                t0ModeCheckBox.IsChecked = addedTab.TZeroMode;
                t0ModeCheckBox.Checked += T0ModeCheckBox_CheckedChanged;
                t0ModeCheckBox.Unchecked += T0ModeCheckBox_CheckedChanged;
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
            // Calculate time difference if control-clicking on two items
            if (e.AddedItems.Count == 1 && e.RemovedItems.Count == 1 &&
                Keyboard.Modifiers == ModifierKeys.Control)
            {
                // If we don't do this, a stack overflow will happen because the line after this
                // will invoke SelectionChanged again, and the condition above will match again, which
                // will invoke it again, and so on.
                ((Timeline)sender!).SelectedItem = null;

                ((Timeline)sender!).SelectedItem = (TimelineItem)e.RemovedItems[0]!;
                TimeSpan diff = ((TimelineItem)e.AddedItems[0]!).DateTime - ((TimelineItem)e.RemovedItems[0]!).DateTime;
                MessageBox.Show($"Difference is {diff.Duration().ToString("h'h 'm'm 's's'")}");
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
            else
            {
                e.Handled = true; // Prevents dragging happening when moving mouse after dialog closes
                AddItemButton_Click(this, new RoutedEventArgs());
            }
        }
        #endregion
    }
}
