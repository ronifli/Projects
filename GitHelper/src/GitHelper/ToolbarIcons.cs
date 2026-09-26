using System.Drawing;
using System.Drawing.Drawing2D;

namespace GitHelper
{
    /// <summary>
    /// Toolbar images, drawn in code so there is no image file to ship.
    /// 16x16 - the application is DPI-unaware, so this is what is drawn.
    /// </summary>
    public static class ToolbarIcons
    {
        private static Bitmap _showLog;

        /// <summary>
        /// "Show Log": a commit history - a vertical branch line with three
        /// commits on it, each with a line of text beside it, like a log list.
        /// Indigo like the application icon, the newest commit amber like its
        /// branch tip.
        /// </summary>
        public static Bitmap ShowLog
        {
            get
            {
                if (_showLog != null) return _showLog;

                var bmp = new Bitmap(16, 16);
                using (var g = Graphics.FromImage(bmp))
                using (var line = new Pen(Color.FromArgb(120, 130, 190), 1.4f))
                using (var indigo = new SolidBrush(Color.FromArgb(63, 81, 181)))
                using (var amber = new SolidBrush(Color.FromArgb(255, 160, 0)))
                using (var text = new Pen(Color.FromArgb(90, 90, 90), 1.6f))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    // the branch
                    g.DrawLine(line, 4f, 2.5f, 4f, 13.5f);

                    // three commits, newest on top, each with its "log line"
                    var ys = new[] { 3f, 8f, 13f };
                    for (var i = 0; i < ys.Length; i++)
                    {
                        g.FillEllipse(i == 0 ? amber : indigo, 1.8f, ys[i] - 2.2f, 4.4f, 4.4f);
                        g.DrawLine(text, 8.5f, ys[i], i == 1 ? 12.5f : 15f, ys[i]);
                    }
                }
                _showLog = bmp;
                return bmp;
            }
        }
    }
}
