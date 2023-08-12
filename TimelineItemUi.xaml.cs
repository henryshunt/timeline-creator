using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TimelineRenderer
{
    public partial class TimelineItemUi : UserControl
    {
        private static readonly SolidColorBrush ELLIPSE_COLOUR = new(Colors.SlateGray);
        private static readonly SolidColorBrush HOVER_COLOUR = new(Colors.Coral);

        public DateTime DateTime { get; set; }
        public bool IsApproximate { get; set; } = false;
        public string Text { get; set; } = string.Empty;

        private bool isSelected = false;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                theEllipse.Fill = value ? HOVER_COLOUR : ELLIPSE_COLOUR;
            }
        }

        public TimelineItemUi()
        {
            InitializeComponent();
            DataContext = this;
        }

        internal double GetMarkerCenterXPos()
        {
            return tildeTextBlock.DesiredSize.Width + timeTextBlock.DesiredSize.Width + 5;

            var transform = theEllipse.TransformToVisual(theStackPanel as FrameworkElement);
            return transform.Transform(new Point(0, 0)).X;

            return theEllipse.TransformToAncestor(theStackPanel).Transform(new Point(0, 0)).X;

            return theEllipse.TranslatePoint(new Point(0, 0), theStackPanel).X;
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            theEllipse.Fill = HOVER_COLOUR;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsSelected)
                theEllipse.Fill = ELLIPSE_COLOUR;
        }
    }
}
