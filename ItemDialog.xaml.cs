using System;
using System.Windows;
using System.Windows.Controls;
using TimelineCreator.Controls;

namespace TimelineCreator
{
    public partial class ItemDialog : Window
    {
        private readonly TimeZoneInfo timeZone;
        public TimelineItem Item { get; private set; }
        public DateTime? TZeroTime { get; set; } = null;
        public bool IsTZeroMode { get; set; } = false;

        public bool WasDeleted { get; private set; } = false;

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
                deleteButton.Visibility = Visibility.Visible;
                theTextBox.Text = Item.Text;
            }

            theTimeField.Value = Item.DateTime;
            importantCheckBox.IsChecked = Item.IsImportant;

            if (TZeroTime != null)
            {
                tZeroCheckBox.IsEnabled = true;

                if (IsTZeroMode)
                {
                    // TZeroCheckBox_CheckedChanged() will take care of the rest
                    tZeroCheckBox.IsChecked = true;
                }
            }
            else
            {
                theTimeField.Focus();
            }
        }

        private void TheTimeField_ValueChanged(object sender, DateTimeValueChangedEventArgs e)
        {
            submitButton.IsEnabled = e.Value != null;
        }

        private void TheTZeroField_ValueChanged(object sender, TZeroValueChangedEventArgs e)
        {
            submitButton.IsEnabled = e.Value != null;
        }

        private void Field_ValidationError(object sender, EventArgs e)
        {
            submitButton.IsEnabled = false;
        }

        private void NowButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(UtcNowNoMillis(), timeZone.Id);

            if (tZeroCheckBox.IsChecked == false)
            {
                theTimeField.Value = now;
            }
            else
            {
                theTZeroField.Value = now - TZeroTime;
            }
        }

        private void TZeroCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked == true)
            {
                theTZeroField.Value = theTimeField.Value - TZeroTime;

                theTimeField.Visibility = Visibility.Collapsed;
                theTZeroField.Visibility = Visibility.Visible;
                theTZeroField.Focus();
            }
            else
            {
                theTimeField.Value = TZeroTime + theTZeroField.Value;

                theTZeroField.Visibility = Visibility.Collapsed;
                theTimeField.Visibility = Visibility.Visible;
                theTimeField.Focus();
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (tZeroCheckBox.IsChecked == true)
            {
                Item.DateTime = (DateTime)(TZeroTime + theTZeroField.Value)!;
            }
            else
            {
                Item.DateTime = (DateTime)theTimeField.Value!;
            }

            Item.Text = theTextBox.Text;
            Item.IsImportant = importantCheckBox.IsChecked == true;
            DialogResult = true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            WasDeleted = true;
            DialogResult = true;
        }

        /// <summary>
        /// Gets the current UTC time with the milliseconds zeroed out.
        /// </summary>
        private static DateTime UtcNowNoMillis()
        {
            DateTime now = DateTime.UtcNow;
            return new DateTime(now.Year, now.Month, now.Day,
                now.Hour, now.Minute, now.Second, now.Kind);
        }
    }
}
