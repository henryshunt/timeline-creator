using System;

namespace TimelineRenderer
{
    public class TimelineItemSelectedEventArgs : EventArgs
    {
        public TimelineItem Item { get; private set; }

        public TimelineItemSelectedEventArgs(TimelineItem item)
        {
            Item = item;
        }
    }
}
