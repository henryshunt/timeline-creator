using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TimelineCreator
{
    public partial class TimelineItem : INotifyPropertyChanged
    {
        private static readonly SolidColorBrush ELLIPSE_COLOUR = new(Colors.SlateGray);
        private static readonly SolidColorBrush HOVER_COLOUR = new(Colors.MidnightBlue);

        private DateTime dateTime = DateTime.Now;
        public DateTime DateTime
        {
            get { return dateTime; }
            set
            {
                dateTime = value;
                DisplayTimeValue();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DateTime)));
            }
        }

        private string text = string.Empty;
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                textTextBlock.Text = text;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }

        private bool isSelected = false;
        internal bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                markerEllipse.Fill = value ? HOVER_COLOUR : ELLIPSE_COLOUR;
            }
        }

        private DateTime? tZeroTime = null;

        /// <summary>
        /// Time to display item time as a countdown/up relative to. <see cref="null"/> to display the item time.
        /// </summary>
        internal DateTime? TZeroTime
        {
            get { return tZeroTime; }

            set
            {
                tZeroTime = value;
                DisplayTimeValue();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;


        public TimelineItem()
        {
            InitializeComponent();
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            markerEllipse.Fill = HOVER_COLOUR;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsSelected)
            {
                markerEllipse.Fill = ELLIPSE_COLOUR;
            }
        }

        /// <summary>
        /// Gets the position of the centre of the item's marker relative to the item's bounds.
        /// </summary>
        internal Point GetMarkerCenterPos()
        {
            return new Point(timeTextBlock.DesiredSize.Width + 10 + (markerEllipse.Width / 2),
                timeTextBlock.DesiredSize.Height / 2);
        }

        /// <summary>
        /// Displays the item time based on the value of <see cref="TZeroTime"/>.
        /// </summary>
        private void DisplayTimeValue()
        {
            if (TZeroTime == null)
            {
                timeTextBlock.Text = DateTime.ToString("HH:mm:ss");
            }
            else
            {
                TimeSpan relToTZero = DateTime - (DateTime)TZeroTime;

                if (relToTZero == TimeSpan.Zero)
                {
                    timeTextBlock.Text = relToTZero.ToString("hh\\:mm\\:ss");
                }
                else if (relToTZero > TimeSpan.Zero)
                {
                    timeTextBlock.Text = "+" + relToTZero.ToString("hh\\:mm\\:ss");
                }
                else if (relToTZero < TimeSpan.Zero)
                {
                    timeTextBlock.Text = "-" + relToTZero.ToString("hh\\:mm\\:ss");
                }
            }
        }
    }
}
