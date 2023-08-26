using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TimelineCreator
{
    /// <summary>
    /// Represents a timeline document in the form of a <see cref="TabControl"/> tab.
    /// </summary>
    public class TimelineTab : TabItem
    {
        private bool hasUnsavedChanges = false;

        /// <summary>
        /// Gets whether any changes have been made to the document since it was last saved.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get { return hasUnsavedChanges; }

            private set
            {
                hasUnsavedChanges = value;

                // Add an asterisk to the displayed title of the tab
                Header = hasUnsavedChanges ? $"* {Title}" : Title;
            }
        }

        /// <summary>
        /// Path to the file that is being used to store the document on disk. This can only be changed using
        /// <see cref="SaveDocumentAs(string)"/>.
        /// </summary>
        public string? FilePath { get; private set; } = null;

        private string title = "Untitled Document";

        /// <summary>
        /// Title of the document.
        /// </summary>
        public string Title
        {
            get { return title; }

            set
            {
                title = value;
                HasUnsavedChanges = true;
            }
        }

        private string description = "";

        /// <summary>
        /// Description section of the document.
        /// </summary>
        public string Description
        {
            get { return description; }

            set
            {
                description = value;
                HasUnsavedChanges = true;
            }
        }

        private TimeZoneInfo timeZone = TimeZoneInfo.Local;

        /// <summary>
        /// Time zone that all times in the document are stored in.
        /// </summary>
        public TimeZoneInfo TimeZone
        {
            get { return timeZone; }

            set
            {
                timeZone = value;
                HasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// The <see cref="TimelineCreator.Timeline"/> control that the tab is using to render the document's timeline.
        /// </summary>
        public readonly Timeline Timeline;


        private TimelineTab(bool isFromFile)
        {
            Timeline = new Timeline()
            {
                Margin = new Thickness(10),
                FontSize = 14,
                TimelineWidth = 30
            };

            if (!isFromFile)
                Timeline.Items.CollectionChanged += Items_CollectionChanged;

            Content = Timeline;
            Header = Title;
        }

        /// <summary>
        /// Creates a tab containing a new, empty document.
        /// </summary>
        public static TimelineTab NewDocument()
        {
            return new TimelineTab(false);
        }

        /// <summary>
        /// Creates a tab containing the contents of an existing timeline document.
        /// </summary>
        public static TimelineTab OpenDocument(string filePath)
        {
            // TODO: Validation of file contents before reading

            dynamic documentJson = JObject.Parse(File.ReadAllText(filePath));

            TimelineTab tab = new(true)
            {
                title = documentJson["title"],
                description = documentJson["description"],

                timeZone = TimeZoneInfo.GetSystemTimeZones()
                    .First(tzi => tzi.Id == (string)documentJson.timeZone)
            };

            foreach (dynamic itemJson in documentJson.items)
            {
                TimelineItem item = new()
                {
                    DateTime = DateTime.ParseExact((string)itemJson.time, "MM/dd/yyyy HH:mm:ss", null),
                    Text = itemJson.text
                };

                tab.AddPropertyChangedHandler(item);
                tab.Timeline.Items.Add(item);
            }

            tab.AddCollectionChangedHandler();
            tab.Timeline.ResetZoom();

            tab.FilePath = filePath;
            tab.Header = tab.Title;
            return tab;
        }

        /// <summary>
        /// Saves the timeline document to a specific/new location.
        /// </summary>
        public void SaveDocumentAs(string filePath)
        {
            FilePath = filePath;
            SaveDocument();
        }

        /// <summary>
        /// Saves the timeline document to its current file path.
        /// </summary>
        public void SaveDocument()
        {
            if (FilePath == null)
                throw new InvalidOperationException("Document has never been saved.");

            dynamic documentJson = new JObject();
            documentJson.title = Title;
            documentJson.description = Description;
            documentJson.timeZone = TimeZone.Id;
            documentJson.items = new JArray();

            foreach (TimelineItem item in Timeline.Items)
            {
                dynamic itemJson = new JObject();
                itemJson.time = item.DateTime;
                itemJson.text = item.Text;
                documentJson.items.Add(itemJson);
            }

            File.WriteAllText(FilePath, JsonConvert.SerializeObject(documentJson, Formatting.Indented));
            HasUnsavedChanges = false;
        }

        private void AddCollectionChangedHandler()
        {
            Timeline.Items.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (TimelineItem item in e.NewItems!)
                    item.PropertyChanged += Item_PropertyChanged;
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (TimelineItem item in e.OldItems!)
                    item.PropertyChanged -= Item_PropertyChanged;
            }

            HasUnsavedChanges = true;
        }

        private void AddPropertyChangedHandler(TimelineItem item)
        {
            item.PropertyChanged += Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Invoked whenever item time or text changes
            HasUnsavedChanges = true;
        }
    }
}
