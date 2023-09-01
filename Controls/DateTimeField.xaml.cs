using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace TimelineCreator.Controls
{
    public partial class DateTimeField : UserControl, INotifyPropertyChanged
    {
        private DateTime? value = null;
        public DateTime? Value
        {
            get => value;

            set
            {
                this.value = value;
                theTextBox.Background = null;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                ValueChanged?.Invoke(this, new DateTimeValueChangedEventArgs(value));
            }
        }

        public event EventHandler<DateTimeValueChangedEventArgs>? ValueChanged;
        public event EventHandler? ValidationError;
        public event PropertyChangedEventHandler? PropertyChanged;


        public DateTimeField()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetPlaceholderVisibility();
        }

        public new void Focus()
        {
            theTextBox.Focus();
            theTextBox.CaretIndex = theTextBox.Text.Length;
        }

        private void TheTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                SetPlaceholderVisibility();
            }
        }

        private void TheTextBox_Error(object sender, ValidationErrorEventArgs e)
        {
            theTextBox.Background = new SolidColorBrush(Color.FromRgb(255, 179, 186));
            ValidationError?.Invoke(this, new EventArgs());
        }

        private void TheTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Load the currently committed (valid) value back into textbox on focus loss
            ((TextBox)sender).GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            theTextBox.Background = null;
        }

        private void SetPlaceholderVisibility()
        {
            if (theTextBox.Text == string.Empty || theTextBox.Text == null)
            {
                placeholderText.Visibility = Visibility.Visible;
            }
            else
            {
                placeholderText.Visibility = Visibility.Collapsed;
            }
        }
    }


    internal class DateTimeValueConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                return string.Empty;
            }
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (string)value != string.Empty)
            {
                return DateTime.ParseExact((string)value, "yyyy-MM-dd HH:mm:ss", null);
            }
            else
            {
                return null;
            }
        }
    }

    internal class DateTimeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value != null && (string)value != string.Empty)
            {
                try
                {
                    DateTime.ParseExact((string)value, "yyyy-MM-dd HH:mm:ss", null);
                    return new ValidationResult(true, null);
                }
                catch (FormatException)
                {
                    return new ValidationResult(false, string.Empty);
                }
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }

    public class DateTimeValueChangedEventArgs : EventArgs
    {
        public DateTime? Value { get; set; }

        public DateTimeValueChangedEventArgs(DateTime? value)
        {
            Value = value;
        }
    }
}
