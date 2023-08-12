using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TimelineCreator
{
    public partial class TimelineItem : INotifyPropertyChanged
    {
        private DateTime dateTime = DateTime.Now;
        public DateTime DateTime
        {
            get { return dateTime; }
            set
            {
                dateTime = value;
                timeTextBlock.Text = value.ToString("HH:mm:ss");

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

        private static readonly SolidColorBrush ELLIPSE_COLOUR = new(Colors.SlateGray);
        private static readonly SolidColorBrush HOVER_COLOUR = new(Colors.MidnightBlue);

        private bool isSelected = false;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                markerEllipse.Fill = value ? HOVER_COLOUR : ELLIPSE_COLOUR;
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

        internal Point GetMarkerCenterPos()
        {
            return new Point(timeTextBlock.DesiredSize.Width + 10 + (markerEllipse.Width / 2),
                timeTextBlock.DesiredSize.Height / 2);
        }
    }
}
