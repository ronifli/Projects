using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Automation;

namespace GitHelper
{
    /// <summary>
    /// Reads Visual Studio's Git Changes commit box over UI Automation. The
    /// control is a WPF TextBox with AutomationId 'commentTextBox'.
    ///
    /// A UI Automation request is a cross-process call that has to be served by
    /// Visual Studio's own UI thread, and a descendants search covers its whole
    /// element tree. When VS is busy - which it is right after launching an
    /// external tool - that call can block for a very long time, so it is never
    /// made on this application's UI thread and never without a deadline.
    /// </summary>
    public static class VisualStudio
    {
        private const string CommitBoxAutomationId = "commentTextBox";

        /// <summary>How long to wait before giving up and using the clipboard.</summary>
        public const int DefaultTimeoutMs = 3000;

        /// <summary>
        /// The text typed in VS's commit box, or empty when VS isn't running,
        /// the pane is closed, the box is empty, the automation id has changed,
        /// or the lookup took longer than <paramref name="timeoutMs"/> -
        /// callers fall back to the clipboard.
        /// </summary>
        public static string GetCommitMessage(int timeoutMs = DefaultTimeoutMs)
        {
            // Nothing to ask: skip the thread and the wait entirely.
            var running = Process.GetProcessesByName("devenv");
            foreach (var p in running) p.Dispose();
            if (running.Length == 0) return string.Empty;

            var result = string.Empty;

            // UI Automation clients are supposed to run off the UI thread, and
            // an MTA thread keeps the call from being pumped into this STA
            // message loop - which is what made the dialog invisible until VS
            // exited.
            var worker = new Thread(() => { result = Read(); });
            worker.IsBackground = true;   // a stuck lookup must not hold the process open
            worker.SetApartmentState(ApartmentState.MTA);
            worker.Start();

            return worker.Join(timeoutMs) ? result : string.Empty;
        }

        private static string Read()
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("devenv"))
                {
                    using (proc)
                    {
                        if (proc.MainWindowHandle == IntPtr.Zero) continue;

                        var root = AutomationElement.FromHandle(proc.MainWindowHandle);
                        if (root == null) continue;

                        var box = root.FindFirst(
                            TreeScope.Descendants,
                            new PropertyCondition(AutomationElement.AutomationIdProperty, CommitBoxAutomationId));
                        if (box == null) continue;

                        var vp = box.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                        if (vp != null)
                        {
                            var value = vp.Current.Value;
                            if (!string.IsNullOrEmpty(value)) return value;
                        }
                    }
                }
            }
            catch
            {
                // a pane that is closed or mid-layout can throw; treat as "no text"
            }
            return string.Empty;
        }
    }
}
