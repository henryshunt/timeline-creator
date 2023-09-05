using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TimelineCreator.Controls;

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

            NewButton_Click(this, new RoutedEventArgs());
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            int unsavedCount = 0;
            foreach (TimelineTab tab in theTabControl.Items)
            {
                if (tab.HasUnsavedChanges)
                {
                    unsavedCount++;
                }
            }

            if (unsavedCount > 0)
            {
                string message = $"You have unsaved changes on {unsavedCount} ";
                message += (unsavedCount == 1 ? "tab" : "tabs") + ". Continue?";

                if (MessageBox.Show(message, "Timeline Creator", MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    e.Cancel = true;
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
            TimelineTab newTab = TimelineTab.NewDocument();
            newTab.Timeline.PreviewMouseDoubleClick += Timeline_PreviewMouseDoubleClick;
            newTab.Timeline.SelectionChanged += Timeline_SelectionChanged;

            theTabControl.Items.Add(newTab);
            theTabControl.SelectedIndex = theTabControl.Items.Count - 1;
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new()
            {
                Filter = "JSON Files (*.json) | *.json"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    TimelineTab newTab = await TimelineTab.OpenDocument(openDialog.FileName);
                    newTab.Timeline.PreviewMouseDoubleClick += Timeline_PreviewMouseDoubleClick;
                    newTab.Timeline.SelectionChanged += Timeline_SelectionChanged;

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

                    theTabControl.Items.Add(newTab);
                    theTabControl.SelectedIndex = theTabControl.Items.Count - 1;
                }
                catch (IOException)
                {
                    MessageBox.Show("Error opening file.");
                }
                catch (InvalidFileException)
                {
                    MessageBox.Show("File contents are invalid.");
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedTab().FilePath == null)
            {
                string fileName = docTitleTextBox.Text;
                if (fileName != string.Empty)
                {
                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        fileName = fileName.Replace(c, '-');
                    }
                }
                else
                {
                    fileName = "Untitled Timeline";
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
                        MessageBox.Show("Error saving file.", "Timeline Creator");
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
            ItemDialog dialog = new((TimeZoneInfo)timeZoneComboBox.SelectedItem)
            {
                TZeroTime = tZeroTimeField.Value,
                IsTZeroMode = tZeroCheckBox.IsChecked == true,
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                GetSelectedTab().Timeline.Items.Add(dialog.Item);

                // Centre view range on the new item if it's outside the current view
                if (dialog.Item.DateTime < GetSelectedTab().Timeline.GetViewRange().Item1 ||
                    dialog.Item.DateTime > GetSelectedTab().Timeline.GetViewRange().Item2)
                {
                    TimeSpan halfViewRange = (GetSelectedTab().Timeline.GetViewRange().Item2 -
                                              GetSelectedTab().Timeline.GetViewRange().Item1) / 2;

                    GetSelectedTab().Timeline.GoToViewRange(dialog.Item.DateTime - halfViewRange,
                                                            dialog.Item.DateTime + halfViewRange);
                }

                GetSelectedTab().Timeline.SelectedItem = dialog.Item;
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
                if (MessageBox.Show("You have unsaved changes. Continue?", "Timeline Creator",
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

        private void WidthNumeric_ValueChanged(object? sender, NumericValueChangedEventArgs e)
        {
            GetSelectedTab().TimelineWidth = ((NumericField)sender!).Value;
        }

        private void T0TimeField_ValueChanged(object? sender, DateTimeValueChangedEventArgs e)
        {
            GetSelectedTab().TZeroTime = tZeroTimeField.Value;
        }

        private void T0ModeCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            GetSelectedTab().TZeroMode = ((CheckBox)sender).IsChecked == true;
        }

        private void DocTitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetSelectedTab().Title = ((TextBox)sender).Text;
            Title = $"{GetSelectedTab().Header} - Timeline Creator";
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
                widthNumeric.ValueChanged -= WidthNumeric_ValueChanged;
                tZeroTimeField.ValueChanged -= T0TimeField_ValueChanged;
                tZeroCheckBox.Checked -= T0ModeCheckBox_CheckedChanged;
                tZeroCheckBox.Unchecked -= T0ModeCheckBox_CheckedChanged;
                docTitleTextBox.TextChanged -= DocTitleTextBox_TextChanged;
                docDescripTextBox.TextChanged -= DocDescripTextBox_TextChanged;
                timeZoneComboBox.SelectionChanged -= TimeZoneComboBox_SelectionChanged;

                // Only clear values if we're not immediately going to change them again below
                if (e.AddedItems.Count > 0)
                {
                    Title = "Timeline Creator";
                    widthNumeric.Value = 0;
                    tZeroTimeField.Value = null;
                    tZeroCheckBox.IsChecked = false;
                    docTitleTextBox.Text = null;
                    docDescripTextBox.Text = null;
                    timeZoneComboBox.SelectedItem = TimeZoneInfo.Local;
                }
            }

            if (e.AddedItems.Count > 0)
            {
                TimelineTab addedTab = (TimelineTab)e.AddedItems[0]!;
                Title = $"{addedTab.Header} - Timeline Creator";

                widthNumeric.Value = addedTab.TimelineWidth;
                tZeroTimeField.Value = addedTab.TZeroTime;
                tZeroCheckBox.IsChecked = addedTab.TZeroMode;
                docTitleTextBox.Text = addedTab.Title;
                docDescripTextBox.Text = addedTab.Description;
                timeZoneComboBox.SelectedItem = addedTab.TimeZone;

                widthNumeric.ValueChanged += WidthNumeric_ValueChanged;
                tZeroTimeField.ValueChanged += T0TimeField_ValueChanged;
                tZeroCheckBox.Checked += T0ModeCheckBox_CheckedChanged;
                tZeroCheckBox.Unchecked += T0ModeCheckBox_CheckedChanged;
                docTitleTextBox.TextChanged += DocTitleTextBox_TextChanged;
                docDescripTextBox.TextChanged += DocDescripTextBox_TextChanged;
                timeZoneComboBox.SelectionChanged += TimeZoneComboBox_SelectionChanged;
            }
        }

        private void Timeline_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((Timeline)sender).SelectedItem != null)
            {
                new ItemDialog((TimeZoneInfo)timeZoneComboBox.SelectedItem, ((Timeline)sender).SelectedItem!)
                {
                    TZeroTime = tZeroTimeField.Value,
                    IsTZeroMode = tZeroCheckBox.IsChecked == true,
                    Owner = this
                }.ShowDialog();
            }
            else
            {
                AddItemButton_Click(this, new RoutedEventArgs());
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

                TimeSpan diff = ((TimelineItem)e.AddedItems[0]!).DateTime -
                                ((TimelineItem)e.RemovedItems[0]!).DateTime;
                MessageBox.Show($"Difference is {diff.Duration().ToString("h'h 'm'm 's's'")}");
            }
        }
        #endregion
    }
}
