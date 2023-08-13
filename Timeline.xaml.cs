using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TimelineCreator
{
    public partial class Timeline : UserControl
    {
        private const double TIME_LINE_THICKNESS = 2;
        private readonly SolidColorBrush TIME_LINE_COLOUR = new(Colors.Black);
        private readonly SolidColorBrush TICK_LINE_COLOUR = new(Colors.LightGray);
        private const double LEFT_PADDING = 130;
        private const double START_END_PADDING = 15;


        private int timelineWidth = 100;

        /// <summary>
        /// Percentage of the width of the control that the timeline itself takes up.
        /// </summary>
        public int TimelineWidth
        {
            get { return timelineWidth; }

            set
            {
                timelineWidth = value;
                theGrid.MaxWidth = CalcTimelineWidthFromPct();
                Render(false);
            }
        }

        public readonly ObservableCollection<TimelineItem> Items = new();

        private TimelineItem? selectedItem = null;
        public TimelineItem? SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (value != selectedItem)
                {
                    if (selectedItem != null)
                    {
                        selectedItem.IsSelected = false;
                    }

                    TimelineItem? oldItem = selectedItem;
                    selectedItem = value;

                    if (selectedItem == null)
                    {
                        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(Selector.SelectionChangedEvent,
                            new List<TimelineItem>() { oldItem! }, new List<TimelineItem>() { }));
                    }
                    else
                    {
                        List<TimelineItem> removedItems = (oldItem != null) ? new() { oldItem } : new() { };
                        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(Selector.SelectionChangedEvent,
                            removedItems, new List<TimelineItem>() { selectedItem }));

                        selectedItem.IsSelected = true;
                    }
                }
            }
        }

        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;


        private DateTime viewStartTime = DateTime.UtcNow;
        private DateTime viewEndTime = DateTime.UtcNow;
        private double secondsPerPixel = 0;

        private bool isMouseDown = false;
        private Point dragStartPos = new(0, 0);
        private DateTime dragViewStartTime = DateTime.UtcNow;
        private DateTime dragViewEndTime = DateTime.UtcNow;
        private bool hasDragged = false;


        public Timeline()
        {
            InitializeComponent();
            Items.CollectionChanged += Items_CollectionChanged;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            theGrid.MaxWidth = CalcTimelineWidthFromPct();
            //Render(true);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Render(false);
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Zoom(e.Delta > 0 ? 15 : -15, e.GetPosition(theGrid).Y);
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (secondsPerPixel > 0)
                {
                    TimelineItem item = (TimelineItem)e.NewItems![0]!;

                    if (item.DateTime >= viewStartTime && item.DateTime <= viewEndTime)
                        Render(false);
                }
                else Render(true);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (SelectedItem != null && e.OldItems!.Contains(SelectedItem))
                {
                    if (SelectedItem.DateTime >= viewStartTime && SelectedItem.DateTime <= viewEndTime)
                        Render(false);

                    SelectedItem = null;
                }
            }
        }

        #region Rendering
        /// <summary>
        /// Renders the entire timeline after clearing everything from the container.
        /// </summary>
        private void Render(bool zoomToFit)
        {
            theGrid.Children.Clear();

            RenderTimelineLine();

            if (Items.Count > 0)
            {
                if (secondsPerPixel == 0 || zoomToFit)
                {
                    viewStartTime = Items.Min(x => x.DateTime);
                    viewEndTime = Items.Max(x => x.DateTime);
                }

                // Determine render scaling factor
                double viewHeight = theGrid.ActualHeight - ((TIME_LINE_THICKNESS + START_END_PADDING) * 2);
                double viewSeconds = (viewEndTime - viewStartTime).TotalSeconds;

                secondsPerPixel = 0;
                if (viewHeight > 0 && viewSeconds > 0)
                    secondsPerPixel = viewSeconds / viewHeight;

                if (!DesignerProperties.GetIsInDesignMode(this))
                {
                    RenderTickLines();
                    RenderItems();
                }
            }
        }

        /// <summary>
        /// Renders the actual line of the timeline.
        /// </summary>
        private void RenderTimelineLine()
        {
            double xPosition;
            if (theGrid.ActualWidth <= CalcTimelineWidthFromPct())
                xPosition = LEFT_PADDING;
            else xPosition = (theGrid.ActualWidth / 2) - (CalcTimelineWidthFromPct() / 2) + LEFT_PADDING;

            // T at start of timeline line
            theGrid.Children.Add(new Line()
            {
                X1 = xPosition - 15,
                X2 = xPosition + 15,
                Y1 = TIME_LINE_THICKNESS / 2,
                Y2 = TIME_LINE_THICKNESS / 2,
                Stroke = TIME_LINE_COLOUR,
                StrokeThickness = TIME_LINE_THICKNESS
            });

            // Timeline line
            theGrid.Children.Add(new Line()
            {
                X1 = xPosition,
                Y1 = 0,
                X2 = xPosition,
                Y2 = theGrid.ActualHeight,
                Stroke = TIME_LINE_COLOUR,
                StrokeThickness = TIME_LINE_THICKNESS
            });

            // T at end of timeline line
            theGrid.Children.Add(new Line()
            {
                X1 = xPosition - 15,
                X2 = xPosition + 15,
                Y1 = theGrid.ActualHeight - TIME_LINE_THICKNESS / 2,
                Y2 = theGrid.ActualHeight - TIME_LINE_THICKNESS / 2,
                Stroke = TIME_LINE_COLOUR,
                StrokeThickness = TIME_LINE_THICKNESS
            });
        }

        /// <summary>
        /// Renders the labelled tick lines onto the timeline.
        /// </summary>
        private void RenderTickLines()
        {
            if (secondsPerPixel > 0)
            {
                TimeSpan tickSpacing = CalcTickSpacing(viewEndTime - viewStartTime);
                DateTime firstLineTime = RoundTimeUp(viewStartTime, tickSpacing);

                if (firstLineTime <= viewEndTime)
                {
                    double yPosition = TIME_LINE_THICKNESS + START_END_PADDING +
                        ((firstLineTime - viewStartTime).TotalSeconds / secondsPerPixel);

                    DateTime? previousTime = null;

                    for (DateTime lineTime = firstLineTime;
                         lineTime <= viewEndTime;
                         lineTime += tickSpacing)
                    {
                        if (lineTime != firstLineTime) // If not first iteration
                            yPosition += (lineTime - previousTime!).Value.TotalSeconds / secondsPerPixel;

                        RenderTickLine(yPosition, lineTime);
                        previousTime = lineTime;
                    }
                }
            }
        }

        private void RenderTickLine(double yPosition, DateTime dateTime)
        {
            double xPosition = 0;
            if (theGrid.ActualWidth > CalcTimelineWidthFromPct())
            {
                xPosition = (theGrid.ActualWidth / 2) - (CalcTimelineWidthFromPct() / 2);
            }

            theGrid.Children.Add(new Border()
            {
                BorderBrush = TICK_LINE_COLOUR,
                BorderThickness = new Thickness(0, 1, 0, 1),
                Margin = new Thickness(0, yPosition - 1, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            });

            TextBlock tickLabel = new()
            {
                Text = dateTime.ToString("HH:mm"),
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            theGrid.Children.Add(tickLabel);
            tickLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            tickLabel.Margin = new Thickness(xPosition + 5, yPosition - tickLabel.DesiredSize.Height - 3, 0, 0);
        }

        /// <summary>
        /// Renders the items onto the timeline.
        /// </summary>
        private void RenderItems()
        {
            double lastFullItemYPos = 0;

            if (Items.Count > 1)
            {
                foreach (TimelineItem item in Items)
                {
                    if (secondsPerPixel == 0)
                    {
                        if (item.DateTime >= viewStartTime && item.DateTime <= viewEndTime)
                        {
                            double yPosition = TIME_LINE_THICKNESS + START_END_PADDING;
                            RenderItem(item, yPosition, ref lastFullItemYPos);
                        }
                    }
                    else
                    {
                        if (item.DateTime >= viewStartTime && item.DateTime <= viewEndTime)
                        {
                            double yPosition = TIME_LINE_THICKNESS + START_END_PADDING +
                                ((item.DateTime - viewStartTime).TotalSeconds / secondsPerPixel);

                            RenderItem(item, yPosition, ref lastFullItemYPos);
                        }
                    }
                }
            }
            else
            {
                TimelineItem item = Items[0];

                if (item.DateTime >= viewStartTime && item.DateTime <= viewEndTime)
                    RenderItem(item, TIME_LINE_THICKNESS + START_END_PADDING, ref lastFullItemYPos);
            }
        }

        private void RenderItem(TimelineItem item, double yPosition, ref double lastFullItemYPos)
        {
            double xPosition;
            if (theGrid.ActualWidth <= CalcTimelineWidthFromPct())
                xPosition = LEFT_PADDING;
            else xPosition = (theGrid.ActualWidth / 2) - (CalcTimelineWidthFromPct() / 2) + LEFT_PADDING;

            theGrid.Children.Add(item);
            item.Margin = new Thickness(xPosition - item.GetMarkerCenterPos().X, yPosition - item.GetMarkerCenterPos().Y, 0, 0);
            item.HorizontalAlignment = HorizontalAlignment.Left;

            lastFullItemYPos = yPosition;

            // TODO: Do these need to be removed at the start of a render?
            item.MouseLeftButtonUp += TimelineItem_MouseLeftButtonUp;
            item.MouseLeftButtonDown += TimelineItem_MouseLeftButtonDown;
        }

        private void TimelineItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SelectedItem = (TimelineItem)sender;
            ((TimelineItem)sender).Focus();
            e.Handled = true;
        }

        private void TimelineItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
        #endregion

        #region Pan & Zoom
        public void ResetZoom()
        {
            Render(true);
        }

        public void Zoom(int percent, double centerYPos)
        {
            double totalChangePx = (Math.Abs(percent) / 100f) * theGrid.ActualHeight;

            double percentAbove = (centerYPos / theGrid.ActualHeight) * 100f;
            double abovePx = (totalChangePx / 100f) * percentAbove;
            TimeSpan timeAbove = TimeSpan.FromSeconds(abovePx * secondsPerPixel);
            double belowPx = totalChangePx - abovePx;
            TimeSpan timeBelow = TimeSpan.FromSeconds(belowPx * secondsPerPixel);

            if (percent > 0)
            {
                viewStartTime += timeAbove;
                viewEndTime -= timeBelow;
            }
            else
            {
                viewStartTime -= timeAbove;
                viewEndTime += timeBelow;
            }

            Render(false);
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isMouseDown = true;
                dragStartPos = e.GetPosition(theGrid);
                dragViewStartTime = viewStartTime;
                dragViewEndTime = viewEndTime;
                hasDragged = false;

                Mouse.Capture(theGrid);
            }
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                hasDragged = true;

                double posDelta = e.GetPosition(theGrid).Y - dragStartPos.Y;
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

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isMouseDown = false;
                theGrid.ReleaseMouseCapture();
                e.Handled = true;

                if (!hasDragged && e.OriginalSource == theGrid)
                    SelectedItem = null;
            }
            else if (e.ChangedButton == MouseButton.Middle)
                ResetZoom();
        }
        #endregion

        #region Helpers
        private double CalcTimelineWidthFromPct()
        {
            return (TimelineWidth * ActualWidth) / 100;
        }

        /// <summary>
        /// Determines the time between tick lines based on the total time covered by the view.
        /// </summary>
        private static TimeSpan CalcTickSpacing(TimeSpan timeSpan)
        {
            TimeSpan spacing;

            if (timeSpan <= TimeSpan.FromMinutes(10))
                spacing = TimeSpan.FromMinutes(1);
            else if (timeSpan <= TimeSpan.FromMinutes(30))
                spacing = TimeSpan.FromMinutes(5);
            else if (timeSpan <= TimeSpan.FromHours(3))
                spacing = TimeSpan.FromMinutes(15);
            else if (timeSpan <= TimeSpan.FromHours(6))
                spacing = TimeSpan.FromMinutes(30);
            else if (timeSpan <= TimeSpan.FromDays(1))
                spacing = TimeSpan.FromHours(1);
            else if (timeSpan <= TimeSpan.FromDays(3))
                spacing = TimeSpan.FromHours(3);
            else spacing = TimeSpan.FromHours(6);

            return spacing;
        }

        /// <summary>
        /// Rounds a time up to the nearest e.g. 5 minutes, 1 hour, 3 hours, etc.
        /// </summary>
        private static DateTime RoundTimeUp(DateTime time, TimeSpan nearest)
        {
            return new DateTime((time.Ticks + nearest.Ticks - 1) / nearest.Ticks * nearest.Ticks, time.Kind);
        }
        #endregion
    }
}
