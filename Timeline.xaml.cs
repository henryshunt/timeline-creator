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
        private const double LEFT_PADDING = 150;
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
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException();

                timelineWidth = value;

                if (IsLoaded)
                {
                    theGrid.MaxWidth = CalcTimelineWidthFromPct();
                    Render();
                }
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

        private DateTime? tZeroTime = null;

        /// <summary>
        /// Time to display items as a countdown/up relative to. <see cref="null"/> to display the item time.
        /// </summary>
        public DateTime? TZeroTime
        {
            get { return tZeroTime; }

            set
            {
                tZeroTime = value;

                foreach (TimelineItem item in Items)
                {
                    item.TZeroTime = tZeroTime;
                }

                Render();
            }
        }

        /// <summary>
        /// Invoked when the selected timeline item changes.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;


        private DateTime viewStartTime = DateTime.UtcNow;
        private DateTime viewEndTime = DateTime.UtcNow;
        private double secondsPerPixel = 0;

        private bool isMouseDown = false;
        private TimelineItem? mouseDownItem = null;
        private Point dragStartPos = new(0, 0);
        private DateTime dragViewStartTime = DateTime.UtcNow;
        private DateTime dragViewEndTime = DateTime.UtcNow;
        private bool hasDragged = false;


        public Timeline()
        {
            InitializeComponent();

            // Default view range is start of current day to start of next day
            DateTime now = DateTime.Now;
            viewStartTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            viewEndTime = viewStartTime + TimeSpan.FromDays(1);

            Items.CollectionChanged += Items_CollectionChanged;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            theGrid.MaxWidth = CalcTimelineWidthFromPct();
            Render();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            theGrid.MaxWidth = CalcTimelineWidthFromPct();
            Render();
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Zoom(e.Delta > 0 ? 15 : -15, e.GetPosition(theGrid).Y);
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // TODO: Deal with multiple items per add/remove event

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                TimelineItem item = (TimelineItem)e.NewItems![0]!;
                item.TZeroTime = TZeroTime;
                item.PropertyChanged += Item_PropertyChanged;

                if (item.DateTime >= viewStartTime && item.DateTime <= viewEndTime)
                {
                    Render();
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                TimelineItem item = (TimelineItem)e.OldItems![0]!;
                item.PropertyChanged -= Item_PropertyChanged;

                if (item.DateTime >= viewStartTime && item.DateTime <= viewEndTime)
                {
                    Render();
                }

                if (SelectedItem == item)
                {
                    SelectedItem = null;
                }
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Render();
        }

        #region Rendering
        /// <summary>
        /// Performs a render of the timeline, clearing everything from the view first.
        /// </summary>
        private void Render()
        {
            if (IsLoaded)
            {
                theGrid.Children.Clear();

                RenderTimelineLine();

                // Determine render scaling factor
                double viewHeight = theGrid.ActualHeight - ((TIME_LINE_THICKNESS + START_END_PADDING) * 2);
                double viewSeconds = (viewEndTime - viewStartTime).TotalSeconds;

                secondsPerPixel = 0;
                if (viewHeight > 0 && viewSeconds > 0)
                {
                    secondsPerPixel = viewSeconds / viewHeight;
                }

                if (secondsPerPixel > 0 && !DesignerProperties.GetIsInDesignMode(this))
                {
                    RenderTickLines();
                    RenderItems();
                }
            }
        }

        private void RenderTimelineLine()
        {
            // T at top of timeline line
            theGrid.Children.Add(new Line()
            {
                X1 = LEFT_PADDING - 15,
                X2 = LEFT_PADDING + 15,
                Y1 = TIME_LINE_THICKNESS / 2,
                Y2 = TIME_LINE_THICKNESS / 2,
                Stroke = TIME_LINE_COLOUR,
                StrokeThickness = TIME_LINE_THICKNESS
            });

            // Timeline line
            theGrid.Children.Add(new Line()
            {
                X1 = LEFT_PADDING,
                Y1 = 0,
                X2 = LEFT_PADDING,
                Y2 = theGrid.ActualHeight,
                Stroke = TIME_LINE_COLOUR,
                StrokeThickness = TIME_LINE_THICKNESS
            });

            // T at bottom of timeline line
            theGrid.Children.Add(new Line()
            {
                X1 = LEFT_PADDING - 15,
                X2 = LEFT_PADDING + 15,
                Y1 = theGrid.ActualHeight - TIME_LINE_THICKNESS / 2,
                Y2 = theGrid.ActualHeight - TIME_LINE_THICKNESS / 2,
                Stroke = TIME_LINE_COLOUR,
                StrokeThickness = TIME_LINE_THICKNESS
            });
        }

        private void RenderTickLines()
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
                    {
                        yPosition += (lineTime - previousTime!).Value.TotalSeconds / secondsPerPixel;
                    }

                    RenderTickLine(yPosition, lineTime);
                    previousTime = lineTime;
                }
            }
        }

        private void RenderTickLine(double yPosition, DateTime dateTime)
        {
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
            tickLabel.Margin = new Thickness(5, yPosition - tickLabel.DesiredSize.Height - 3, 0, 0);
        }

        private void RenderItems()
        {
            foreach (TimelineItem item in Items)
            {
                if (item.DateTime >= viewStartTime && item.DateTime <= viewEndTime)
                {
                    double yPosition = TIME_LINE_THICKNESS + START_END_PADDING +
                        ((item.DateTime - viewStartTime).TotalSeconds / secondsPerPixel);

                    RenderItem(item, yPosition);
                }
            }
        }

        private void RenderItem(TimelineItem item, double yPosition)
        {
            theGrid.Children.Add(item);
            item.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            item.Margin = new Thickness(LEFT_PADDING - item.GetMarkerCenterPos().X,
                yPosition - item.GetMarkerCenterPos().Y, 0, 0);
            item.HorizontalAlignment = HorizontalAlignment.Left;

            // TODO: Does this need to be removed at the start of a render?
            item.MouseDown += TimelineItem_MouseDown;
        }
        #endregion

        #region Pan & Zoom
        /// <summary>
        /// Sets the view range to a specific start and end time.
        /// </summary>
        public void GoToViewRange(DateTime viewStart, DateTime viewEnd)
        {
            viewStartTime = viewStart;
            viewEndTime = viewEnd;
            Render();
        }

        /// <summary>
        /// Gets the start and end time that defines the current view range.
        /// </summary>
        public (DateTime, DateTime) GetViewRange()
        {
            return (viewStartTime, viewEndTime);
        }

        private void Zoom(int percent, double centerYPos)
        {
            double totalChangePx = (Math.Abs(percent) / 100d) * theGrid.ActualHeight;

            double percentAbove = (centerYPos / theGrid.ActualHeight) * 100f;
            double abovePx = (totalChangePx / 100d) * percentAbove;
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

            Render();
        }

        /// <summary>
        /// Resets the view range to show all items on the timeline.
        /// </summary>
        public void ResetZoom()
        {
            if (Items.Count == 0)
            {
                // Default view range is start of current day to start of next day
                DateTime now = DateTime.Now;
                viewStartTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                viewEndTime = viewStartTime + TimeSpan.FromDays(1);
            }
            else if (Items.Count == 1)
            {
                viewStartTime = Items[0].DateTime - TimeSpan.FromMinutes(30);
                viewEndTime = Items[0].DateTime + TimeSpan.FromMinutes(30);
            }
            else
            {
                viewStartTime = Items.Min(x => x.DateTime);
                viewEndTime = Items.Max(x => x.DateTime);
            }

            Render();
        }

        private void TimelineItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && mouseDownItem == null)
            {
                mouseDownItem = (TimelineItem)sender;
            }
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
                double posDelta = e.GetPosition(theGrid).Y - dragStartPos.Y;
                TimeSpan timeDelta = TimeSpan.FromSeconds(posDelta * secondsPerPixel);

                if (posDelta != 0)
                {
                    viewStartTime = dragViewStartTime - timeDelta;
                    viewEndTime = dragViewEndTime - timeDelta;
                    hasDragged = true;
                }
                else
                {
                    viewStartTime = dragViewStartTime;
                    viewEndTime = dragViewEndTime;
                    hasDragged = false;
                }

                Render();
            }
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (!hasDragged)
                {
                    // Select clicked item or deselect item if background clicked
                    SelectedItem = mouseDownItem != null ? mouseDownItem : null;
                }

                isMouseDown = false;
                theGrid.ReleaseMouseCapture();
                mouseDownItem = null;
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                ResetZoom();
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Calculates the actual width that the timeline itself should take up within the control. Determined by 
        /// <see cref="TimelineWidth"/>.
        /// </summary>
        private double CalcTimelineWidthFromPct()
        {
            return (TimelineWidth * ActualWidth) / 100;
        }

        /// <summary>
        /// Determines the appropriate time between tick lines based on the total time covered by the view.
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
