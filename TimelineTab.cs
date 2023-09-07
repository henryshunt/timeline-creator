﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TimelineCreator.Controls;
using JsonSchema = NJsonSchema.JsonSchema;

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
            get => hasUnsavedChanges;

            private set
            {
                hasUnsavedChanges = value;

                // Include an asterisk in the tab header if there are unsaved changes
                if (hasUnsavedChanges)
                {
                    Header = FilePath ==
                        string.Empty ? "* Untitled Timeline" : $"* {Path.GetFileName(FilePath)}";

                    HeaderChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Header = FilePath ==
                        string.Empty ? "Untitled Timeline" : Path.GetFileName(FilePath);

                    HeaderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Path to the file that is being used to store the document on disk. This can only be changed using
        /// <see cref="SaveDocumentAs(string)"/>.
        /// </summary>
        public string FilePath { get; private set; } = string.Empty;

        private string description = string.Empty;

        /// <summary>
        /// Description section of the document.
        /// </summary>
        public string Description
        {
            get => description;

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
            get => timeZone;

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

        public int TimelineWidth
        {
            get => Timeline.MaxTimelineWidth;
            set => Timeline.MaxTimelineWidth = value;
        }

        private bool tZeroMode = false;
        public bool TZeroMode
        {
            get => tZeroMode;

            set
            {
                tZeroMode = value;
                Timeline.TZeroTime = value ? TZeroTime : null;
            }
        }

        private DateTime? tZeroTime = null;
        public DateTime? TZeroTime
        {
            get => tZeroTime;

            set
            {
                tZeroTime = value;
                Timeline.TZeroTime = TZeroMode ? value : null;
            }
        }

        private string searchPhrase = string.Empty;
        public string SearchPhrase
        {
            get => searchPhrase;

            set
            {
                searchPhrase = value;
                Timeline.SearchText(value);
            }
        }

        /// <summary>
        /// Invoked when the tab's header text changes.
        /// </summary>
        public event EventHandler? HeaderChanged;


        private TimelineTab(bool isFromFile)
        {
            Timeline = new Timeline()
            {
                Margin = new Thickness(10),
                FontSize = 14,
                MaxTimelineWidth = 800
            };

            if (!isFromFile)
            {
                Timeline.Items.CollectionChanged += Items_CollectionChanged;
            }

            Content = Timeline;
            Header = "Untitled Timeline";
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
        public async static Task<TimelineTab> OpenDocument(string filePath)
        {
            string json = File.ReadAllText(filePath);

            if (await ValidateJson(json))
            {
                dynamic documentJson = JsonConvert.DeserializeObject<JObject>(json,
                    new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None })!;

                if (documentJson.version != 1)
                {
                    throw new InvalidFileException();
                }

                TimeZoneInfo? timeZone = TimeZoneInfo.GetSystemTimeZones()
                    .FirstOrDefault(tzi => tzi.Id == (string)documentJson.timeZone);

                if (timeZone == null)
                {
                    throw new InvalidFileException();
                }

                TimelineTab tab = new(true)
                {
                    FilePath = filePath,
                    Description = documentJson["description"],
                    TimeZone = timeZone
                };

                tab.HasUnsavedChanges = false; // Also sets the tab header

                foreach (dynamic itemJson in documentJson.items)
                {
                    try
                    {
                        TimelineItem item = new()
                        {
                            DateTime = DateTime.ParseExact((string)itemJson.time, "yyyy-MM-dd'T'HH:mm:ss", null),
                            Text = itemJson.text
                        };

                        tab.AddPropertyChangedHandler(item);
                        tab.Timeline.Items.Add(item);
                    }
                    catch (FormatException)
                    {
                        throw new InvalidFileException();
                    }
                }

                tab.AddCollectionChangedHandler();
                tab.Timeline.ResetZoom();
                return tab;
            }
            else
            {
                throw new InvalidFileException();
            }
        }

        private static async Task<bool> ValidateJson(string json)
        {
            string schemaJson;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "TimelineCreator.FileSchema.json")!)
            {
                using (StreamReader reader = new(stream))
                {
                    schemaJson = reader.ReadToEnd();
                }
            }

            JsonSchema schema = await JsonSchema.FromJsonAsync(schemaJson);
            ICollection<NJsonSchema.Validation.ValidationError> jsonErrors = schema.Validate(json);

            return jsonErrors.Count == 0;
        }

        /// <summary>
        /// Saves the timeline document to a specific/new location.
        /// </summary>
        public void SaveDocumentAs(string filePath)
        {
            FilePath = filePath;
            Header = Path.GetFileName(FilePath);
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
            documentJson.version = 1;
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

            JsonSerializerSettings settings = new()
            {
                Formatting = Formatting.Indented,
                DateFormatString = "yyyy-MM-dd'T'HH:mm:ss"
            };

            File.WriteAllText(FilePath, JsonConvert.SerializeObject(documentJson, settings));
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
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }

                // Refresh search to take account of new items
                Timeline.SearchText(SearchPhrase);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (TimelineItem item in e.OldItems!)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
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

            // Refresh search to take account of possibly changed text
            Timeline.SearchText(SearchPhrase);
        }
    }

    public class InvalidFileException : Exception
    {

    }
}
