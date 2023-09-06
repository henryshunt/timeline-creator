# Timeline Creator
A desktop application for creating, rendering, saving and reopening timelines of data.

I created this as part of my close following of SpaceX's Starship program, in advance of the first Integrated Flight Test, so that I could track events on a timeline during testing campaigns to help with understanding the operation of the ground systems.

The application has a tabbed, document-based interface, and a timeline renderer that I created from scratch.

# Miscellaneous Information
- Click and drag to pan up or down the timeline. Scroll the mouse wheel to zoom in and out.
- Single-click an item to select it and double-click an item to edit it.
- Check "T-0 Mode" and enter a time, and the timeline items will show their time as a countdown/up relative to the T-0 time.
- When adding or editing an item with T-0 mode enabled, you can enter the time as a countdown/up to the T-0 time. Use the below checkbox to switch between T-0 -relative time and wall-clock time entry.
- Select an item, then select another item with the control key held. This will display the time difference between the two items.
- Zooming out is limited to a maximum of three days. This software is designed for precise, short-range timelines rather than long-range timelines.

# Shortcuts
- Ctrl-N -- Create a new timeline document
- Ctrl-O -- Open an existing timeline document
- Ctrl-S -- Save the open timeline document
- Ctrl-I/Double-click background -- Add a new item 
- Home -- Move timeline view to start at first item
- End -- Move timeline view to end at last item
- Mouse Wheel Scroll -- Zoom in/out
- Ctrl-0/Mouse Wheel Press -- Zoom to fit entire timeline
- Delete -- Delete selected item
- Esc -- Deselect selected item
- Ctrl-Tab -- Switch to next tab
- Ctrl-F -- Focus on timeline search box