using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace TimelineCreator.Controls
{
    public partial class TZeroTimeField : UserControl, INotifyPropertyChanged
    {
        private TimeSpan? value = null;
        public TimeSpan? Value
        {
            get => value;

            set
            {
                this.value = value;
                theTextBox.Background = new SolidColorBrush(Colors.White);

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                ValueChanged?.Invoke(this, new TZeroValueChangedEventArgs(value));
            }
        }

        public event EventHandler<TZeroValueChangedEventArgs>? ValueChanged;
        public event EventHandler? ValidationError;
        public event PropertyChangedEventHandler? PropertyChanged;


        public TZeroTimeField()
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

            theTextBox.Background = new SolidColorBrush(Colors.White);
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


    internal class TZeroValueConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((TimeSpan)value <= TimeSpan.Zero)
                {
                    return ((TimeSpan)value).ToString("'T-'hh':'mm':'ss");
                }
                else
                {
                    return ((TimeSpan)value).ToString("'T+'hh':'mm':'ss");
                }
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
                if (TimeSpan.TryParseExact((string)value, "'T-'hh':'mm':'ss", null, out TimeSpan tMinusTime))
                {
                    return tMinusTime.Negate();
                }
                else if (TimeSpan.TryParseExact((string)value, "'T+'hh':'mm':'ss", null, out TimeSpan tPlusTime))
                {
                    return tPlusTime;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }

    internal class TZeroValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value != null && (string)value != string.Empty)
            {
                if (TimeSpan.TryParseExact((string)value, "'T-'hh':'mm':'ss", null, out _) ||
                    TimeSpan.TryParseExact((string)value, "'T+'hh':'mm':'ss", null, out _))
                {
                    return new ValidationResult(true, null);
                }
                else
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

    public class TZeroValueChangedEventArgs : EventArgs
    {
        public TimeSpan? Value { get; set; }

        public TZeroValueChangedEventArgs(TimeSpan? value)
        {
            Value = value;
        }
    }
}
