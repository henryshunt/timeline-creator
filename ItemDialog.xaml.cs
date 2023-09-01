using System;
using System.Windows;
using TimelineCreator.Controls;

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
                DateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(UtcNowNoMillis(), timeZone.Id)
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
                theTextBox.Text = Item.Text;
            }

            theTimeField.Value = Item.DateTime;
            theTimeField.Focus();
        }

        private void TheTimeField_ValueChanged(object sender, DateTimeValueChangedEventArgs e)
        {
            submitButton.IsEnabled = e.Value != null;
        }

        private void TheTimeField_ValidationError(object sender, EventArgs e)
        {
            submitButton.IsEnabled = false;
        }

        private void NowButton_Click(object sender, RoutedEventArgs e)
        {
            theTimeField.Value = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(UtcNowNoMillis(), timeZone.Id);
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Item.DateTime = (DateTime)theTimeField.Value!;
            Item.Text = theTextBox.Text;
            DialogResult = true;
        }

        private static DateTime UtcNowNoMillis()
        {
            DateTime now = DateTime.UtcNow;
            return new DateTime(now.Year, now.Month, now.Day,
                now.Hour, now.Minute, now.Second, now.Kind);
        }
    }
}
