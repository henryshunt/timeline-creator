using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace TimelineCreator
{
    public partial class NumericField : UserControl, INotifyPropertyChanged
    {
        private int value = 0;
        public int Value
        {
            get => value;

            set
            {
                this.value = value;
                theTextBox.Background = null;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                ValueChanged?.Invoke(this, new NumericValueChangedEventArgs(value));
            }
        }

        public event EventHandler<NumericValueChangedEventArgs>? ValueChanged;
        public event EventHandler? ValidationError;
        public event PropertyChangedEventHandler? PropertyChanged;


        public NumericField()
        {
            DataContext = this;
            InitializeComponent();
        }

        public new void Focus()
        {
            theTextBox.Focus();
            theTextBox.CaretIndex = theTextBox.Text.Length;
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
    }


    internal class NumericValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((int)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.Parse((string)value);
        }
    }

    internal class NumericValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                int.Parse((string)value);
                return new ValidationResult(true, null);
            }
            catch (FormatException)
            {
                return new ValidationResult(false, string.Empty);
            }
            catch (OverflowException)
            {
                return new ValidationResult(false, string.Empty);
            }
        }
    }

    public class NumericValueChangedEventArgs : EventArgs
    {
        public int Value { get; set; }

        public NumericValueChangedEventArgs(int value)
        {
            Value = value;
        }
    }
}
