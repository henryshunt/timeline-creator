using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TimelineCreator
{
    public partial class ItemDialog : Window
    {
        private readonly TimeZoneInfo timeZone;
        public TimelineItem Item { get; private set; }

        private readonly bool isEditing = false;


        public ItemDialog(TimeZoneInfo timeZone)
        {
            this.timeZone = timeZone;
            Item = new TimelineItem()
            {
                DateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeZone.Id)
            };

            InitializeComponent();
        }

        public ItemDialog(TimeZoneInfo timeZone, TimelineItem item)
        {
            isEditing = true;
            this.timeZone = timeZone;
            Item = item;

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (isEditing)
            {
                Title = "Edit Timeline Item";
                submitButton.Content = "Save Item";
            }

            timeTextBox.Text = Item.DateTime.ToString("dd/MM/yyyy HH:mm:ss");

            itemTextField.Text = Item.Text;
            itemTextField.Focus();
        }

        private void TimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DateTime.ParseExact(timeTextBox.Text, "dd/MM/yyyy HH:mm:ss", null);
                submitButton.IsEnabled = true;
            }
            catch (FormatException)
            {
                submitButton.IsEnabled = false;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && submitButton.IsEnabled)
            {
                SubmitButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void NowButton_Click(object sender, RoutedEventArgs e)
        {
            timeTextBox.Text = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeZone.Id)
                .ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Item.DateTime = DateTime.ParseExact(timeTextBox.Text, "dd/MM/yyyy HH:mm:ss", null);
            Item.Text = itemTextField.Text;

            DialogResult = true;
        }

        private void tZeroButton_Click(object sender, RoutedEventArgs e)
        {
            //TimeSpan span = TimeSpan.Parse(addTimeTextBox.Text);
            //DateTime t0 = new(2023, 4, 20, 8, 28, 0);

            //DateTime time = t0 - span;

            //// Add item in correct place by DateTime
            //int i = 0;
            //for (; i < selectedTab.Timeline.Items.Count; i++)
            //{
            //    if (selectedTab.Timeline.Items[i].DateTime > time)
            //        break;
            //}

            //selectedTab.Timeline.Items.Insert(i, new TimelineItem() { DateTime = time, Text = addTextTextBox.Text });

            //addTimeTextBox.Clear();
            //addTextTextBox.Clear();
        }
    }
}
