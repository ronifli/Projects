using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace GitHelper
{
    /// <summary>
    /// Remembers a window's position, size, maximized state and splitter
    /// positions in HKCU\Software\Camtek\GitHelper, under a name per window:
    ///
    ///   &lt;name&gt;.Window   "x,y,width,height,maximized"
    ///   &lt;name&gt;.Split.&lt;splitter name&gt;   distance in pixels
    ///
    /// Same rules as the main window: a saved rectangle is only used when a
    /// screen still shows its title bar (a monitor may be gone), sizes are
    /// clamped between MinimumSize and the desktop, and RestoreBounds - not
    /// Bounds - is saved while maximized. A splitter distance that no longer
    /// fits the window is clamped rather than refused.
    ///
    /// Call <see cref="RestoreWindow"/> from the constructor,
    /// <see cref="RestoreSplitters"/> once the form is shown (the splitters'
    /// limits depend on the final size) and <see cref="Save"/> from FormClosing.
    /// </summary>
    public static class WindowPlacement
    {
        public static void RestoreWindow(Form form, string name)
        {
            var parts = (Settings.GetString(name + ".Window") ?? string.Empty).Split(',');
            if (parts.Length != 5) return;

            int x, y, w, h, max;
            if (!int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y) ||
                !int.TryParse(parts[2], out w) || !int.TryParse(parts[3], out h) ||
                !int.TryParse(parts[4], out max) || w <= 0 || h <= 0)
                return;

            var desktop = SystemInformation.VirtualScreen;
            w = Math.Min(Math.Max(w, form.MinimumSize.Width), desktop.Width);
            h = Math.Min(Math.Max(h, form.MinimumSize.Height), desktop.Height);
            var bounds = new Rectangle(x, y, w, h);

            // at least 80x8 px of the title bar on some screen, or it cannot be dragged back
            var titleBar = new Rectangle(bounds.X, bounds.Y, bounds.Width, 30);
            var visible = Screen.AllScreens.Any(s =>
            {
                var i = Rectangle.Intersect(s.WorkingArea, titleBar);
                return i.Width >= 80 && i.Height >= 8;
            });
            if (!visible) return;   // keep the designer's start position

            form.StartPosition = FormStartPosition.Manual;
            form.Bounds = bounds;
            if (max != 0) form.WindowState = FormWindowState.Maximized;
        }

        public static void RestoreSplitters(string name, params SplitContainer[] splitters)
        {
            foreach (var split in splitters)
            {
                int d;
                var text = Settings.GetString(name + ".Split." + split.Name);
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out d)) continue;

                var size = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
                var max = size - split.Panel2MinSize - split.SplitterWidth;
                if (max < split.Panel1MinSize) continue;   // too small to honour anything

                try { split.SplitterDistance = Math.Max(split.Panel1MinSize, Math.Min(d, max)); }
                catch (InvalidOperationException) { /* layout not ready - keep the default */ }
            }
        }

        public static void Save(Form form, string name, params SplitContainer[] splitters)
        {
            var bounds = form.WindowState == FormWindowState.Normal ? form.Bounds : form.RestoreBounds;
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                Settings.SetString(name + ".Window", string.Join(",", new[]
                {
                    bounds.X, bounds.Y, bounds.Width, bounds.Height,
                    form.WindowState == FormWindowState.Maximized ? 1 : 0,
                }.Select(n => n.ToString(CultureInfo.InvariantCulture))));
            }

            // a minimized window has collapsed splitters - not worth keeping
            if (form.WindowState == FormWindowState.Minimized) return;
            foreach (var split in splitters)
                Settings.SetString(name + ".Split." + split.Name,
                    split.SplitterDistance.ToString(CultureInfo.InvariantCulture));
        }
    }
}
