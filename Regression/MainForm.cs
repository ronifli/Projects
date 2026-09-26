using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Regression
{
    public partial class MainForm : Form
    {
        readonly Runner runner = new Runner();
        CancellationTokenSource cts;

        public MainForm()
        {
            InitializeComponent();
            RestoreWindow();
            runner.Proc.Log += line => BeginInvoke((Action)(() => AppendLog(line)));
            runner.StateChanged += line => BeginInvoke((Action)(() => AppendLog(stateBox, line)));
        }

        void AppendLog(string text) => AppendLog(logBox, text);

        const int MaxLogChars = 2_000_000;
        Font boldFont;

        void AppendLog(RichTextBox box, string text)
        {
            if (box.IsDisposed || IsDisposed) return;
            if (boldFont == null) boldFont = new Font(box.Font, FontStyle.Bold);
            if (box.TextLength > MaxLogChars)
            {
                box.Select(0, box.TextLength - MaxLogChars / 2);
                box.SelectedText = "";
            }
            foreach (var line in text.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                bool isError = ProcessRunner.ErrorLine.IsMatch(line)
                    || line.IndexOf("FAILED", StringComparison.Ordinal) >= 0
                    || line.IndexOf("ERROR", StringComparison.Ordinal) >= 0;
                box.SelectionStart = box.TextLength;
                box.SelectionLength = 0;
                box.SelectionColor = isError ? Color.Red : box.ForeColor;
                box.SelectionFont = isError ? boldFont : box.Font;
                box.AppendText(line + Environment.NewLine);
            }
            // ScrollToCaret intermittently throws AccessViolationException inside RichEdit; scroll via WM_VSCROLL instead.
            SendMessage(box.Handle, WM_VSCROLL, (IntPtr)SB_BOTTOM, IntPtr.Zero);
        }

        const int WM_VSCROLL = 0x115, SB_BOTTOM = 7;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private void btnStart_Click(object sender, EventArgs e) => RunTask("Regression running", ct => runner.RegressionLoop(ct));
        private void mnuBuild_Click(object sender, EventArgs e) => RunTask("Building", ct => runner.DevEnv.Build(ct, out _, stopOnError: true));
        private void mnuClean_Click(object sender, EventArgs e) => RunTask("Cleaning", ct => runner.DevEnv.Clean(ct, out _));
        private void mnuRestore_Click(object sender, EventArgs e) => RunTask("Git restore", ct => runner.Git.RevertAndPull(ct));

        private void mnuTestEmail_Click(object sender, EventArgs e) => RunTask("Sending test email", ct => runner.SendMail("Regression: test email", "This is a test email from the Regression tool."));

        private void btnStop_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
            lblStatus.Text = "Stopping...";
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            if (File.Exists(runner.Proc.LogFile)) Process.Start("notepad.exe", $"\"{runner.Proc.LogFile}\"");
            else MessageBox.Show("No log file yet.");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            cts?.Cancel();
            SaveWindow();
        }

        static readonly string WindowFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Regression", "window.txt");

        void SaveWindow()
        {
            try
            {
                var b = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                Directory.CreateDirectory(Path.GetDirectoryName(WindowFile));
                File.WriteAllText(WindowFile, $"{b.X},{b.Y},{b.Width},{b.Height},{WindowState == FormWindowState.Maximized}");
            }
            catch { }
        }

        void RestoreWindow()
        {
            try
            {
                var p = File.ReadAllText(WindowFile).Split(',');
                var b = new Rectangle(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
                if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(b))) return;
                StartPosition = FormStartPosition.Manual;
                Bounds = b;
                if (bool.Parse(p[4])) WindowState = FormWindowState.Maximized;
            }
            catch { }
        }

        async void RunTask(string name, Action<CancellationToken> work)
        {
            cts = new CancellationTokenSource();
            runner.DevEnv.UseDevenv = chkDevenv.Checked;
            SetBusy(true, name);
            try
            {
                await Task.Run(() => work(cts.Token));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                AppendLog("ERROR: " + ex.Message);
            }
            finally
            {
                SetBusy(false, "Idle");
            }
        }

        void SetBusy(bool busy, string text)
        {
            lblStatus.Text = text;
            btnStop.Enabled = busy;
            chkDevenv.Enabled = btnStart.Enabled = mnuBuild.Enabled = mnuClean.Enabled = mnuRestore.Enabled = mnuTestEmail.Enabled = !busy;
        }
    }
}
