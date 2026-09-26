using System;
using System.Windows.Forms;

namespace GitHelper
{
    /// <summary>
    /// A ListView that says when it scrolls sideways. ListView has no Scroll
    /// event; the column filter boxes above Show Log's list need one to stay
    /// lined up with their columns when the list is scrolled horizontally.
    /// </summary>
    public class ScrollNotifyListView : ListView
    {
        private const int WM_HSCROLL = 0x0114;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int SB_HORZ = 0;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);

        /// <summary>Raised after any horizontal scroll (bar, wheel, keyboard).</summary>
        public event EventHandler HorizontalScrolled;

        /// <summary>Pixels the list is scrolled to the right.</summary>
        public int HorizontalOffset
        {
            get { return IsHandleCreated ? GetScrollPos(Handle, SB_HORZ) : 0; }
        }

        private int _lastOffset;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // WM_HSCROLL/WM_MOUSEHWHEEL cover the bar and tilt wheel; the
            // offset check also catches keyboard moves and EnsureVisible.
            if (m.Msg == WM_HSCROLL || m.Msg == WM_MOUSEHWHEEL || HorizontalOffset != _lastOffset)
            {
                var now = HorizontalOffset;
                if (now == _lastOffset && m.Msg != WM_HSCROLL && m.Msg != WM_MOUSEHWHEEL) return;
                _lastOffset = now;
                var handler = HorizontalScrolled;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }
    }
}
