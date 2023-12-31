﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace TimelineCreator.Controls
{
    public partial class TimelineItem : INotifyPropertyChanged
    {
        private static readonly SolidColorBrush ELLIPSE_COLOUR = new(Colors.SlateGray);
        private static readonly SolidColorBrush HOVER_COLOUR = new(Colors.MidnightBlue);

        private DateTime dateTime = DateTime.Now;
        public DateTime DateTime
        {
            get => dateTime;

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
            get => text;

            set
            {
                text = value;
                textTextBlock.Text = text;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }

        private bool isImportant = false;
        public bool IsImportant
        {
            get => isImportant;

            set
            {
                isImportant = value;
                importantTextBlock.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                textTextBlock.Text = text;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsImportant)));
            }
        }

        private bool isSelected = false;
        public bool IsSelected
        {
            get => isSelected;

            set
            {
                isSelected = value;
                markerEllipse.Fill = isSelected ? HOVER_COLOUR : ELLIPSE_COLOUR;
            }
        }

        private DateTime? tZeroTime = null;

        /// <summary>
        /// Time to display item time as a countdown/up relative to. <see cref="null"/> to display the item time.
        /// </summary>
        public DateTime? TZeroTime
        {
            get => tZeroTime;

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
        public Point GetMarkerCenterPos()
        {
            // TODO: Should be doing this by getting absolute position of marker ellipse relative to
            // the control but can't get that to work

            double xPosition = timeTextBlock.DesiredSize.Width +
                               theGrid.ColumnDefinitions[2].Width.Value +
                               (markerEllipse.Width / 2);

            if (importantTextBlock.Visibility != Visibility.Collapsed)
            {
                xPosition += importantTextBlock.DesiredSize.Width;
            }

            return new Point(xPosition, timeTextBlock.DesiredSize.Height / 2);
        }

        /// <summary>
        /// Highlights all occurrences of a search phrase within the item's text. <see cref="string.Empty"/> to clear
        /// search.
        /// </summary>
        public int SearchText(string phrase)
        {
            if (Text.Length > 0 && phrase.Length > 0)
            {
                List<int> matches = AllIndexesOf(Text.ToLower(), phrase.ToLower());

                if (matches.Count != 0)
                {
                    textTextBlock.Inlines.Clear();

                    // Create a run for each highlighted and non-highlighted section of text
                    int index = 0;
                    foreach (int match in matches)
                    {
                        if (match > index)
                        {
                            textTextBlock.Inlines.Add(new Run(Text[index..match]));
                            index += (match - index);
                        }

                        textTextBlock.Inlines.Add(new Run(text.Substring(match, phrase.Length))
                        {
                            Background = new SolidColorBrush(Color.FromArgb(127, 255, 200, 0))
                        });
                        index += phrase.Length;
                    }

                    if (Text.Length > index)
                    {
                        textTextBlock.Inlines.Add(new Run(Text[index..]));
                    }

                    return matches.Count;
                }
                else
                {
                    textTextBlock.Inlines.Clear();
                    textTextBlock.Text = Text;
                }
            }
            else
            {
                textTextBlock.Inlines.Clear();
                textTextBlock.Text = Text;
            }

            return 0;
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
                if (relToTZero > TimeSpan.Zero)
                {
                    timeTextBlock.Text = "+" + relToTZero.ToString("hh\\:mm\\:ss");
                }
                else if (relToTZero < TimeSpan.Zero)
                {
                    timeTextBlock.Text = "-" + relToTZero.ToString("hh\\:mm\\:ss");
                }
                else
                {
                    timeTextBlock.Text = relToTZero.ToString("hh\\:mm\\:ss");
                }
            }
        }

        /// <summary>
        /// Gets the start indexes of all occurrences of a string in another string.
        /// </summary>
        private static List<int> AllIndexesOf(string haystack, string needle)
        {
            List<int> indexes = new();

            int index = 0;
            while (index != -1)
            {
                index = haystack.IndexOf(needle, index);

                if (index != -1)
                {
                    indexes.Add(index);
                    index += needle.Length;
                }
            }

            return indexes;
        }
    }
}
