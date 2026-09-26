using System;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Windows.Forms;

namespace GitHelper
{
    /// <summary>
    /// A multi-line TextBox that never holds more than <see cref="MaxLines"/>
    /// lines, however the text arrives:
    ///
    /// - typing: Enter (and Ctrl+Enter) is refused on the last allowed line;
    /// - pasting (Ctrl+V, Shift+Ins, the context menu - all WM_PASTE): only as
    ///   many lines of the clipboard as still fit are inserted, so the text
    ///   already in the box is never pushed out;
    /// - setting Text from code (From VS, From clipboard, Last commit): the
    ///   surplus lines at the end are dropped.
    ///
    /// Each time something is refused or cut, it beeps and shows a short tip
    /// under the box. Lines are counted as the user sees them on separate
    /// rows only when separated by a newline - word-wrapped rows do not count,
    /// which matches what git sees.
    /// </summary>
    public class LineLimitedTextBox : TextBox
    {
        private const int WM_PASTE = 0x0302;

        private readonly ToolTip _tip = new ToolTip();
        private bool _trimming;

        /// <summary>0 = no limit.</summary>
        public int MaxLines { get; set; }

        /// <summary>Lines in <paramref name="text"/>; an empty box has none.</summary>
        public static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return text.Count(c => c == '\n') + 1;
        }

        /// <summary>The first <paramref name="max"/> lines of <paramref name="text"/>.</summary>
        public static string FirstLines(string text, int max)
        {
            if (max <= 0 || string.IsNullOrEmpty(text)) return string.Empty;
            var lines = text.Replace("\r\n", "\n").Split('\n');
            return lines.Length <= max ? text : string.Join("\r\n", lines.Take(max));
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            // '\r' is Enter, '\n' is Ctrl+Enter
            if (MaxLines > 0 && (e.KeyChar == '\r' || e.KeyChar == '\n')
                && CountLines(TextWithSelectionReplacedBy("\r\n")) > MaxLines)
            {
                e.Handled = true;
                Refuse("The body is limited to " + MaxLines + " lines.");
                return;
            }
            base.OnKeyPress(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_PASTE && MaxLines > 0 && !ReadOnly)
            {
                PasteWithinLimit();
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            // Typing and pasting are already kept within the limit; this
            // catches Text set from code.
            if (MaxLines > 0 && !_trimming && CountLines(Text) > MaxLines)
            {
                var dropped = CountLines(Text) - MaxLines;
                _trimming = true;
                try
                {
                    Text = FirstLines(Text, MaxLines);   // raises OnTextChanged again, once
                    SelectionStart = Text.Length;
                }
                finally
                {
                    _trimming = false;
                }
                Refuse("The body is limited to " + MaxLines + " lines - the last " + dropped + " were left out.");
                return;
            }
            base.OnTextChanged(e);
        }

        private void PasteWithinLimit()
        {
            string clip;
            try
            {
                if (!Clipboard.ContainsText()) return;
                clip = Clipboard.GetText().Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                return;   // clipboard held by another process
            }

            // The pasted text joins the line the caret is on, so its first
            // line is free; each further line costs one.
            var without = TextWithSelectionReplacedBy(string.Empty);
            var room = MaxLines - Math.Max(1, CountLines(without));
            var fits = FirstLines(clip, room + 1);

            if (fits.Length > 0) SelectedText = fits;   // keeps Ctrl+Z working
            if (fits.Length < clip.Length)
            {
                Refuse(room <= 0
                    ? "The body already has " + MaxLines + " lines - nothing more can be pasted."
                    : "Only the first " + (room + 1) + " line(s) of the clipboard fit - the body is limited to " +
                      MaxLines + " lines.");
            }
        }

        private string TextWithSelectionReplacedBy(string insert)
        {
            return Text.Remove(SelectionStart, SelectionLength).Insert(SelectionStart, insert);
        }

        private void Refuse(string why)
        {
            SystemSounds.Beep.Play();
            if (IsHandleCreated && Visible)
                _tip.Show(why, this, new Point(0, Height + 2), 3000);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tip.Dispose();
            base.Dispose(disposing);
        }
    }
}
