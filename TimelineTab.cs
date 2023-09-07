using Newtonsoft.Json;
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
        /// <summary>
        /// Gets the tab's header text.
        /// </summary>
        public new string Header => ((TextBlock)base.Header).Text;

        private bool hasUnsavedChanges = false;

        /// <summary>
        /// Gets whether any changes have been made to the document since it was created or last saved.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => hasUnsavedChanges;

            private set
            {
                hasUnsavedChanges = value;
                UpdateTabHeader();
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

        /// <summary>
        /// Gets or sets the maximum width of the timeline itself within the tab.
        /// </summary>
        public int TimelineWidth
        {
            get => Timeline.MaxTimelineWidth;
            set => Timeline.MaxTimelineWidth = value;
        }

        private bool isTZeroModeEnabled = false;

        /// <summary>
        /// Gets or sets whether the timeline should show times relative to <see cref="TZeroTime"/> if it it set.
        /// </summary>
        public bool IsTZeroModeEnabled
        {
            get => isTZeroModeEnabled;

            set
            {
                isTZeroModeEnabled = value;
                Timeline.TZeroTime = isTZeroModeEnabled ? TZeroTime : null;
            }
        }

        private DateTime? tZeroTime = null;

        /// <summary>
        /// Gets or sets the T-0 time to use when <see cref="IsTZeroModeEnabled"/> is set to <see cref="true"/>.
        /// </summary>
        public DateTime? TZeroTime
        {
            get => tZeroTime;

            set
            {
                tZeroTime = value;
                Timeline.TZeroTime = IsTZeroModeEnabled ? tZeroTime : null;
            }
        }

        private string searchPhrase = string.Empty;

        /// <summary>
        /// Gets or sets the phrase that is being searched for in the timeline. When set, a search is performed.
        /// </summary>
        public string SearchPhrase
        {
            get => searchPhrase;

            set
            {
                searchPhrase = value;
                Timeline.SearchText(searchPhrase);
            }
        }

        /// <summary>
        /// Invoked when the tab's header text changes.
        /// </summary>
        public event EventHandler<HeaderChangedEventArgs>? HeaderChanged;


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
            UpdateTabHeader();
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

        /// <summary>
        /// Determines whether the JSON contents of a timeline file are valid.
        /// </summary>
        private static async Task<bool> ValidateJson(string json)
        {
            try
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
            catch (JsonReaderException)
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the timeline document to a specific/new location.
        /// </summary>
        public void SaveDocumentAs(string filePath)
        {
            FilePath = filePath;
            UpdateTabHeader();
            SaveDocument();
        }

        /// <summary>
        /// Saves the timeline document to its currently set file path.
        /// </summary>
        public void SaveDocument()
        {
            if (FilePath == string.Empty)
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

        /// <summary>
        /// Adds the handler to the CollectionChanged event of the tab's timeline items list. Necessary as part of the
        /// static <see cref="OpenDocument(string)"/> method.
        /// </summary>
        private void AddCollectionChangedHandler()
        {
            Timeline.Items.CollectionChanged += Items_CollectionChanged;
        }

        /// <summary>
        /// Invoked whenever the tab's timeline item list changes.
        /// </summary>
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

        /// <summary>
        /// Adds the the handler to the PropertyChanged event of a timeline item. Necessary as part of the static
        /// <see cref="OpenDocument(string)"/> method.
        /// </summary>
        private void AddPropertyChangedHandler(TimelineItem item)
        {
            item.PropertyChanged += Item_PropertyChanged;
        }

        /// <summary>
        /// Invoked whenever the DateTime or Text properties of a timeline item change.
        /// </summary>
        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            HasUnsavedChanges = true;

            // Refresh search to take account of possibly changed text
            Timeline.SearchText(SearchPhrase);
        }

        /// <summary>
        /// Sets the tab's header text based on the file path and whether there are any unsaved changes.
        /// </summary>
        private void UpdateTabHeader()
        {
            string header = hasUnsavedChanges ? "* " : "";

            if (FilePath != string.Empty)
            {
                string fileName = Path.GetFileNameWithoutExtension(FilePath);
                header += fileName != string.Empty ? fileName : "Untitled Timeline";
            }
            else
            {
                header += "Untitled Timeline";
            }

            TextBlock headerTextBlock = new() { Text = header };

            if (FilePath != string.Empty)
            {
                headerTextBlock.ToolTip = FilePath;
            }

            base.Header = headerTextBlock;
            HeaderChanged?.Invoke(this, new HeaderChangedEventArgs(header));
        }
    }

    public class HeaderChangedEventArgs : Exception
    {
        public string Header { get; private set; }

        public HeaderChangedEventArgs(string header)
        {
            Header = header;
        }
    }

    public class InvalidFileException : Exception { }
}
