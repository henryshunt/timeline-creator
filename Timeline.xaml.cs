using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TimelineRenderer
{
    public partial class Timeline : UserControl
    {
        private const double TIME_LINE_THICKNESS = 2;
        private readonly SolidColorBrush TIME_LINE_COLOUR = new(Colors.Black);
        private readonly SolidColorBrush TICK_LINE_COLOUR = new(Colors.LightGray);
        private const double LEFT_PADDING = 120;
        private const double START_END_PADDING = 15;
        private readonly SolidColorBrush MARKER_COLOUR = new(Colors.SlateGray);
        private readonly SolidColorBrush MARKER_HOVER_COLOUR = new(Colors.Coral);

        public readonly ObservableCollection<TimelineItem> Items = new();
        public TimelineItemUi? SelectedItem { get; private set; } = null;

        public event EventHandler<TimelineItemSelectedEventArgs>? ItemSelected;


        private DateTime viewStartTime = DateTime.UtcNow;
        private DateTime viewEndTime = DateTime.UtcNow;
        private double secondsPerPixel = 0;

        private bool isMouseDown = false;
        private Point dragStartPos = new(0, 0);
        private DateTime dragViewStartTime = DateTime.UtcNow;
        private DateTime dragViewEndTime = DateTime.UtcNow;


        public Timeline()
        {
            InitializeComponent();

            //Items.CollectionChanged += Items_CollectionChanged;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Render(true);
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Render(true);
        }

        public void Render(bool zoomToFit)
        {
            theCanvas.Children.Clear();

            // Sort items list by time ascending
            List<TimelineItem> sorted = Items.OrderBy(x => x.DateTime).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                Items.Move(Items.IndexOf(sorted[i]), i);
            }

            RenderTimelineLine();

            if (Items.Count > 0)
            {
                if (secondsPerPixel == 0 || zoomToFit)
                {
                    viewStartTime = Items.Min(x => x.DateTime);
                    viewEndTime = Items.Max(x => x.DateTime);
                }

                // Determine render scaling factor
                double viewHeight = theCanvas.ActualHeight - ((TIME_LINE_THICKNESS + START_END_PADDING) * 2);
                double viewSeconds = (viewEndTime - viewStartTime).TotalSeconds;

                secondsPerPixel = 0;
                if (viewHeight > 0 && viewSeconds > 0)
                {
                    secondsPerPixel = viewSeconds / viewHeight;
                }

                if (!DesignerProperties.GetIsInDesignMode(this))
                {
                    RenderTickLines();
                    RenderItems();
                }
            }
        }

        private void RenderTimelineLine()
        {
            const double xPosition = LEFT_PADDING;

            // T at start of timeline line
            theCanvas.Children.Add(new Line()
            {
                X1 = xPosition - 15,
                X2 = xPosition + 15,
                Y1 = TIME_LINE_THICKNESS / 2,
                Y2 = TIME_LINE_THICKNESS / 2,
                Stroke = TIME_LINE_COLOUR,
                StrokeThickness = TIME_LINE_THICKNESS
            });

            // Timeline line
            theCanvas.Children.Add(new Line()
            {
                X1 = xPosition,
                Y1 = 0,
                X2 = xPosition,
                Y2 = theCanvas.ActualHeight,
                Stroke = TIME_LINE_COLOUR,
                StrokeThickness = TIME_LINE_THICKNESS
            });

            // T at end of timeline line
            theCanvas.Children.Add(new Line()
            {
                X1 = xPosition - 15,
                X2 = xPosition + 15,
                Y1 = theCanvas.ActualHeight - TIME_LINE_THICKNESS / 2,
                Y2 = theCanvas.ActualHeight - TIME_LINE_THICKNESS / 2,
                Stroke = TIME_LINE_COLOUR,
                StrokeThickness = TIME_LINE_THICKNESS
            });
        }

        private void RenderTickLines()
        {
            // Round start time up to start of next hour
            DateTime firstLineTime = viewStartTime;
            if (firstLineTime.Minute != 0)
            {
                firstLineTime += TimeSpan.FromHours(1);
            }

            firstLineTime = new DateTime(firstLineTime.Year, firstLineTime.Month,
                firstLineTime.Day, firstLineTime.Hour, 0, 0);


            if (firstLineTime <= viewEndTime)
            {
                double yPosition = TIME_LINE_THICKNESS + START_END_PADDING +
                    ((firstLineTime - viewStartTime).TotalSeconds / secondsPerPixel);

                DateTime? previousTime = null;

                for (DateTime lineTime = firstLineTime;
                     lineTime <= viewEndTime;
                     lineTime += TimeSpan.FromHours(1))
                {
                    if (lineTime != firstLineTime) // If not first iteration
                    {
                        yPosition += (lineTime - previousTime!).Value.TotalSeconds / secondsPerPixel;
                    }

                    theCanvas.Children.Add(new Line()
                    {
                        X2 = theCanvas.ActualWidth,
                        Y1 = yPosition,
                        Y2 = yPosition,
                        Stroke = TICK_LINE_COLOUR,
                        StrokeThickness = 2
                    });

                    TextBlock tickLabel = new()
                    {
                        Text = lineTime.ToString("HH:mm"),
                        FontSize = 12,
                        FontStyle = FontStyles.Italic
                    };

                    theCanvas.Children.Add(tickLabel);
                    tickLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(tickLabel, 4);
                    Canvas.SetTop(tickLabel, yPosition - tickLabel.DesiredSize.Height - 3);

                    previousTime = lineTime;
                }
            }
        }

        private void RenderItems()
        {
            double lastFullItemYPos = 0;

            if (Items.Count > 1)
            {
                foreach (TimelineItem item in Items)
                {
                    if (item.DateTime >= viewStartTime && item.DateTime <= viewEndTime)
                    {
                        double yPosition = TIME_LINE_THICKNESS + START_END_PADDING +
                            ((item.DateTime - viewStartTime).TotalSeconds / secondsPerPixel);

                        RenderItem(item, yPosition, ref lastFullItemYPos);
                    }
                }
            }
            else
            {
                TimelineItem item = Items[0];

                if (item.DateTime >= viewStartTime && item.DateTime <= viewEndTime)
                {
                    RenderItem(item, TIME_LINE_THICKNESS + START_END_PADDING, ref lastFullItemYPos);
                }
            }
        }

        private void RenderItem(TimelineItem item, double yPosition, ref double lastFullItemYPos)
        {
            TimelineItemUi itemUi = new()
            {
                DateTime = item.DateTime,
                Text = item.Text
            };

            theCanvas.Children.Add(itemUi);
            itemUi.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double x = itemUi.GetMarkerCenterXPos();
            Canvas.SetLeft(itemUi, LEFT_PADDING - x);
            Canvas.SetTop(itemUi, yPosition - (itemUi.DesiredSize.Height / 2));

            lastFullItemYPos = yPosition;

            itemUi.MouseLeftButtonDown += (sender, e) => e.Handled = true;
            itemUi.MouseLeftButtonUp += (sender, e) =>
            {
                if (SelectedItem != null)
                    SelectedItem.IsSelected = false;

                itemUi.IsSelected = true;
                SelectedItem = itemUi;

                ItemSelected?.Invoke(this, new TimelineItemSelectedEventArgs(item));
            };
        }

        public void ResetZoom()
        {
            Render(true);
        }

        public void Zoom(int percent, double centerYPos)
        {
            int percent2 = percent;
            if (percent < 0)
            {
                percent2 = Math.Abs(percent);
            }

            double fraction = (theCanvas.ActualHeight / percent2);
            TimeSpan timeDelta = TimeSpan.FromSeconds(fraction * secondsPerPixel);

            //double cpfrac = 

            Debug.WriteLine(timeDelta);

            if (percent > 0)
            {
                viewStartTime += timeDelta;
                viewEndTime -= timeDelta;
            }
            else
            {
                viewStartTime -= timeDelta;
                viewEndTime += timeDelta;
            }

            Render(false);
        }

        private void TheCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            dragStartPos = e.GetPosition(theCanvas);
            dragViewStartTime = viewStartTime;
            dragViewEndTime = viewEndTime;

            Mouse.Capture(theCanvas);
        }

        private void TheCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                double posDelta = e.GetPosition(theCanvas).Y - dragStartPos.Y;
                TimeSpan timeDelta = TimeSpan.FromSeconds(posDelta * secondsPerPixel);

                if (posDelta != 0)
                {
                    viewStartTime = dragViewStartTime - timeDelta;
                    viewEndTime = dragViewEndTime - timeDelta;
                }
                else
                {
                    viewStartTime = dragViewStartTime;
                    viewEndTime = dragViewEndTime;
                }

                Render(false);
            }
        }

        private void TheCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            theCanvas.ReleaseMouseCapture();
            e.Handled = true;

        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Render(false);
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                Zoom(10, e.GetPosition(theCanvas).Y);
            }
            else
            {
                Zoom(-10, e.GetPosition(theCanvas).Y);
            }
        }
    }
}
