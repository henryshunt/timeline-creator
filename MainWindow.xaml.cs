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
            AddKeyboardShortcuts();

            timeZoneComboBox.ItemsSource = TimeZoneInfo.GetSystemTimeZones();
            timeZoneComboBox.SelectedItem = TimeZoneInfo.Local;

            NewButton_Click(this, new RoutedEventArgs());
        }

        private void AddKeyboardShortcuts()
        {
            // TODO: I'd prefer if these didn't repeat when a key is held down

            RoutedCommand newCommand = new();
            newCommand.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(newCommand, NewButton_Click));
            RoutedCommand openCommand = new();
            openCommand.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(openCommand, OpenButton_Click));

            RoutedCommand tabCommand = new();
            tabCommand.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(tabCommand, (sender, e) =>
            {
                if (theTabControl.Items.Count > 1)
                {
                    theTabControl.SelectedIndex =
                        (theTabControl.SelectedIndex + 1) % theTabControl.Items.Count;
                }
            }));

            RoutedCommand saveCommand = new();
            saveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(saveCommand, SaveButton_Click));
            RoutedCommand addItemCommand = new();
            addItemCommand.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(addItemCommand, AddItemButton_Click));

            RoutedCommand escapeCommand = new();
            escapeCommand.InputGestures.Add(new KeyGesture(Key.Escape));
            CommandBindings.Add(new CommandBinding(escapeCommand, (sender, e) =>
            {
                if (searchTextBox.IsFocused && searchTextBox.Text.Length > 0)
                {
                    searchTextBox.Text = string.Empty;
                }
                else
                {
                    GetSelectedTab().Timeline.SelectedItem = null;
                }
            }));

            RoutedCommand deleteCommand = new();
            deleteCommand.InputGestures.Add(new KeyGesture(Key.Delete));
            CommandBindings.Add(new CommandBinding(deleteCommand, (sender, e) =>
            {
                if (GetSelectedTab().Timeline.SelectedItem != null)
                {
                    GetSelectedTab().Timeline.Items.Remove(GetSelectedTab().Timeline.SelectedItem!);
                }
            }));

            RoutedCommand zeroCommand = new();
            zeroCommand.InputGestures.Add(new KeyGesture(Key.D0, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(zeroCommand, (sender, e) =>
            {
                GetSelectedTab().Timeline.ResetZoom();
            }));

            RoutedCommand homeCommand = new();
            homeCommand.InputGestures.Add(new KeyGesture(Key.Home, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(homeCommand, (sender, e) =>
            {
                if (GetSelectedTab().Timeline.Items.Count > 0)
                {
                    // Move view range to start at first item
                    (DateTime, DateTime) viewRangeBounds = GetSelectedTab().Timeline.GetViewRange();
                    TimeSpan viewRange = viewRangeBounds.Item2 - viewRangeBounds.Item1;

                    GetSelectedTab().Timeline.GoToViewRange(GetSelectedTab().Timeline.Items[0].DateTime,
                        GetSelectedTab().Timeline.Items[0].DateTime + viewRange);
                }
            }));

            RoutedCommand endCommand = new();
            endCommand.InputGestures.Add(new KeyGesture(Key.End, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(endCommand, (sender, e) =>
            {
                if (GetSelectedTab().Timeline.Items.Count > 0)
                {
                    // Move view range to end at last item
                    (DateTime, DateTime) viewRangeBounds = GetSelectedTab().Timeline.GetViewRange();
                    TimeSpan viewRange = viewRangeBounds.Item2 - viewRangeBounds.Item1;

                    GetSelectedTab().Timeline.GoToViewRange(
                        GetSelectedTab().Timeline.Items.Last().DateTime - viewRange,
                        GetSelectedTab().Timeline.Items.Last().DateTime);
                }
            }));

            RoutedCommand searchCommand = new();
            searchCommand.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(searchCommand, (sender, e) =>
            {
                searchTextBox.Focus();
                searchTextBox.SelectAll();
            }));
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

        #region Main Controls
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            theTabControl.Items.Add(TimelineTab.NewDocument(this));
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
                    TimelineTab newTab = await TimelineTab.OpenDocument(openDialog.FileName, this);

                    // If a single new, empty document is open then remove it
                    if (theTabControl.Items.Count == 1 &&
                        ((TimelineTab)theTabControl.Items[0]).HasUnsavedChanges == false &&
                        ((TimelineTab)theTabControl.Items[0]).FilePath == string.Empty)
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
            if (GetSelectedTab().FilePath == string.Empty)
            {
                SaveFileDialog saveDialog = new()
                {
                    FileName = "Untitled Timeline",
                    Filter = "JSON Files (*.json) | *.json",
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
            GetSelectedTab().Dispose();
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
            GetSelectedTab().IsTZeroModeEnabled = ((CheckBox)sender).IsChecked == true;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetSelectedTab().SearchPhrase = searchTextBox.Text;
            searchResCountText.Text = GetSelectedTab().SearchResultCount.ToString();
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
                removedTab.HeaderChanged -= SelectedTab_HeaderChanged;
                widthNumeric.ValueChanged -= WidthNumeric_ValueChanged;
                tZeroTimeField.ValueChanged -= T0TimeField_ValueChanged;
                tZeroCheckBox.Checked -= T0ModeCheckBox_CheckedChanged;
                tZeroCheckBox.Unchecked -= T0ModeCheckBox_CheckedChanged;
                searchTextBox.TextChanged -= SearchTextBox_TextChanged;
                docDescripTextBox.TextChanged -= DocDescripTextBox_TextChanged;
                timeZoneComboBox.SelectionChanged -= TimeZoneComboBox_SelectionChanged;

                // Only clear values if we're not immediately going to change them again below
                if (e.AddedItems.Count > 0)
                {
                    Title = "Timeline Creator";
                    widthNumeric.Value = 0;
                    tZeroTimeField.Value = null;
                    tZeroCheckBox.IsChecked = false;
                    searchTextBox.Text = string.Empty;
                    searchResCountText.Text = "0";
                    docDescripTextBox.Text = string.Empty;
                    timeZoneComboBox.SelectedItem = TimeZoneInfo.Local;
                }
            }

            if (e.AddedItems.Count > 0)
            {
                TimelineTab addedTab = (TimelineTab)e.AddedItems[0]!;
                Title = $"{addedTab.Header} - Timeline Creator";

                widthNumeric.Value = addedTab.TimelineWidth;
                tZeroTimeField.Value = addedTab.TZeroTime;
                tZeroCheckBox.IsChecked = addedTab.IsTZeroModeEnabled;
                searchTextBox.Text = addedTab.SearchPhrase;
                searchResCountText.Text = addedTab.SearchResultCount.ToString();
                docDescripTextBox.Text = addedTab.Description;
                timeZoneComboBox.SelectedItem = addedTab.TimeZone;

                addedTab.HeaderChanged += SelectedTab_HeaderChanged;
                widthNumeric.ValueChanged += WidthNumeric_ValueChanged;
                tZeroTimeField.ValueChanged += T0TimeField_ValueChanged;
                tZeroCheckBox.Checked += T0ModeCheckBox_CheckedChanged;
                tZeroCheckBox.Unchecked += T0ModeCheckBox_CheckedChanged;
                searchTextBox.TextChanged += SearchTextBox_TextChanged;
                docDescripTextBox.TextChanged += DocDescripTextBox_TextChanged;
                timeZoneComboBox.SelectionChanged += TimeZoneComboBox_SelectionChanged;
            }
        }

        private void SelectedTab_HeaderChanged(object? sender, HeaderChangedEventArgs e)
        {
            Title = $"{e.Header} - Timeline Creator";
        }
        #endregion
    }
}
