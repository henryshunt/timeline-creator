using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace TimelineRenderer
{
    internal class TimelineDocument
    {
        public string Title { get; set; } = "Untitled Document";
        public string Description { get; set; } = "";
        public readonly ObservableCollection<TimelineItem> Items = new();

        public bool HasUnsavedChanges { get; private set; } = true;


        public TimelineDocument(string title)
        {
            Title = title;
            Items.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach(TimelineItem item in e.NewItems)
            {
                
            }
        }

        public static void Open(string filePath)
        {

        }

        public void Save(string filePath)
        {
            dynamic documentJson = new JObject();
            documentJson.title = Title;
            documentJson.description = Description;
            documentJson.items = new JArray();

            foreach (TimelineItem item in Items)
            {
                dynamic itemJson = new JObject();
                itemJson.dateTime = item.DateTime;
                itemJson.isApproximate = item.IsApproximate;
                itemJson.text = item.Text;

                documentJson.items.Add(itemJson);
            }

            string jsonString = JsonConvert.SerializeObject(documentJson, Formatting.Indented);
            File.WriteAllText(filePath, jsonString);
        }
    }
}
