using System;

namespace TimelineRenderer
{
    public partial class TimelineItem
    {
        public DateTime DateTime { get; set; }
        public bool IsApproximate { get; set; } = false;
        public string? Text { get; set; } = null;

        public TimelineItem(DateTime dateTime)
        {
            DateTime = dateTime;
        }

        public TimelineItem(DateTime dateTime, string? text)
        {
            DateTime = dateTime;
            Text = (text != null && text.Length > 0) ? text : null;
        }
    }
}
