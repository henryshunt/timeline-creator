using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TimelineRenderer
{
    public partial class MainWindow : Window
    {
        private bool hasUnsavedChanges = true;
        private string? docFilePath = null;

        private readonly List<TimelineItem> items = new()
        {
            //new TimelineItem(new DateTime(2023, 1, 23, 12, 0, 0), "Start"),
            //new TimelineItem(new DateTime(2023, 1, 23, 13, 0, 0), "Middle"),
            //new TimelineItem(new DateTime(2023, 1, 23, 14, 0, 0), "End")

            new TimelineItem(new DateTime(2023, 1, 23, 12, 52, 0), "Venting started between rightmost tanks"),
            new TimelineItem(new DateTime(2023, 1, 23, 12, 58, 0), "Venting stopped"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 15, 0), "LOX chillers venting started"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 16, 0), "LOX chillers venting stopped"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 16, 0), "LN2 pump priming, CH4 side"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 23, 0), "Venting begins from LOX chiller side"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 37, 0), "CH4 chillers look full"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 37, 0), "Whisps from OLM vent (*)"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 40, 0), "LOX chillers venting stopped"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 44, 0), "Venting from tower QD level starts (*)"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 52, 0), "LOX chillers venting start"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 58, 0), "Tower QD level vent stopped"),
            new TimelineItem(new DateTime(2023, 1, 23, 13, 59, 0), "Venting from CH4 chillers starts"),
            new TimelineItem(new DateTime(2023, 1, 23, 14, 04, 0), "OLM vent stopped"),
            new TimelineItem(new DateTime(2023, 1, 23, 14, 09, 0), "Engine chill bund vent started. Not constant"),
            new TimelineItem(new DateTime(2023, 1, 23, 14, 15, 0), "Tower vent started"),
            new TimelineItem(new DateTime(2023, 1, 23, 14, 22, 0), "Flow greatly slowed on tower vent"),
            new TimelineItem(new DateTime(2023, 1, 23, 14, 25, 0), "Venting started from other side of QD arm level"),
            new TimelineItem(new DateTime(2023, 1, 23, 14, 30, 0), "Tenuous tower venting still. Almost none"),
            new TimelineItem(new DateTime(2023, 1, 23, 14, 35, 0), "Frost on ship (*)"),
            new TimelineItem(new DateTime(2023, 1, 23, 14, 42, 0), "Ship engine chill started"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 10, 0), "Ship looks full (*)"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 16, 0), "Booster engine chill started"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 17, 0), "Ship engine chill started"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 29, 0), "Tower QD level vent"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 32, 0), "Major vent from methane tank"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 34, 0), "Booster QD drainback"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 35, 0), "LOX chillers stopped"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 35, 0), "Another major vent from methane tank"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 37, 0), "CH4 chillers stopped"),
            new TimelineItem(new DateTime(2023, 1, 23, 15, 51, 0), "Recondenser vent started"),
            new TimelineItem(new DateTime(2023, 1, 23, 16, 00, 0), "OLM vent started, stopped")
        };

        private TimelineItem? selectedItem = null;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < TimeZoneInfo.GetSystemTimeZones().Count; i++)
            {
                timeZoneComboBox.Items.Add(TimeZoneInfo.GetSystemTimeZones()[i]);

                if (TimeZoneInfo.GetSystemTimeZones()[i].Id == TimeZoneInfo.Local.Id)
                {
                    timeZoneComboBox.SelectedIndex = i;
                }
            }

            NewDocument();

            foreach (TimelineItem item in items)
            {
                theTimeline.Items.Add(item);
            }
        }

        private void NewTimelineButton_Click(object sender, RoutedEventArgs e)
        {
            if (hasUnsavedChanges)
            {
                MessageBoxResult result = MessageBox.Show(
                    "You have unsaved changes. Continue?", "", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.No)
                    return;
            }

            NewDocument();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (docFilePath == null)
            {
                SaveFileDialog saveDialog = new()
                {
                    Filter = "JSON Files (*.json) | *.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    try
                    {
                        docFilePath = saveDialog.FileName;
                        SaveDocument();
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("Unable to save file.");
                    }
                }
            }
            else
            {
                SaveDocument();
            }
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            theTimeline.ResetZoom();
        }

        private void AddTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DateTime.ParseExact(addTimeTextBox.Text, "dd/MM/yyyy HH:mm:ss", null);
                addAddButton.IsEnabled = true;
            }
            catch (FormatException)
            {
                addAddButton.IsEnabled = false;
            }
        }

        private void AddNowButton_Click(object sender, RoutedEventArgs e)
        {
            addTimeTextBox.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void AddAddButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime time = DateTime.ParseExact(addTimeTextBox.Text, "dd/MM/yyyy HH:mm:ss", null);
            TimelineItem item = new(time, addTextTextBox.Text)
            {
                IsApproximate = (bool)addApproxCheckBox.IsChecked
            };

            theTimeline.Items.Add(item);
            theTimeline.Render(true);

            addApproxCheckBox.IsChecked = false;
            addTimeTextBox.Clear();
            addTextTextBox.Clear();
        }

        private void AddTextTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddAddButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void TheTimeline_ItemSelected(object sender, TimelineItemSelectedEventArgs e)
        {
            selectedItem = e.Item;
            editApproxCheckBox.IsChecked = e.Item.IsApproximate;
            editTimeTextBox.Text = e.Item.DateTime.ToString();
            editTextTextBox.Text = e.Item.Text;

            editExpander.IsExpanded = true;
        }

        private void EditTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DateTime.ParseExact(editTimeTextBox.Text, "dd/MM/yyyy HH:mm:ss", null);
                editSaveButton.IsEnabled = true;
            }
            catch (FormatException)
            {
                editSaveButton.IsEnabled = false;
            }
        }

        private void EditNowButton_Click(object sender, RoutedEventArgs e)
        {
            editTimeTextBox.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void EditSaveButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime time = DateTime.ParseExact(editTimeTextBox.Text, "dd/MM/yyyy HH:mm:ss", null);
            selectedItem!.DateTime = time;
            selectedItem.Text = editTextTextBox.Text;
            theTimeline.Render(true);

            editTimeTextBox.Clear();
            editTextTextBox.Clear();

            editExpander.IsExpanded = false;
        }

        private void EditTextTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EditSaveButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void DocTitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetHasChanged(true);
        }

        private void DocDescripTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetHasChanged(true);
        }

        #region Document
        private void NewDocument()
        {
            SetHasChanged(true);

            docFilePath = null;
            docTitleTextBox.Text = "Untitled Document";
            docDescripTextBox.Text = string.Empty;
            theTimeline.Items.Clear();

            theTimeline.Render(false);
        }

        private void SetHasChanged(bool hasChanged)
        {
            if (hasChanged)
            {
                hasUnsavedChanges = true;
                saveButton.Content = "Save (*)";
            }
            else
            {
                hasUnsavedChanges = false;
                saveButton.Content = "Save";
            }
        }

        private void SaveDocument()
        {
            dynamic documentJson = new JObject();
            documentJson.title = docTitleTextBox.Text;
            documentJson.description = docDescripTextBox.Text;
            documentJson.items = new JArray();

            foreach (TimelineItem item in theTimeline.Items)
            {
                dynamic itemJson = new JObject();
                itemJson.dateTime = item.DateTime;
                itemJson.isApproximate = item.IsApproximate;
                itemJson.text = item.Text;

                documentJson.items.Add(itemJson);
            }

            string jsonString = JsonConvert.SerializeObject(documentJson, Formatting.Indented);
            File.WriteAllText(docFilePath, jsonString);

            SetHasChanged(false);
        }
        #endregion
    }
}
