using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using GitHelper.Install;

namespace GitHelper
{
    /// <summary>
    /// Registers GitHelper.exe with Visual Studio: the External Tools entry, the
    /// keyboard shortcut and the toolbar button. This is the PowerShell
    /// installer's job, moved into the application.
    ///
    /// It must run elevated (loading a registry hive does), which is why
    /// <see cref="Elevation"/> re-launches the exe rather than doing the work
    /// in the commit dialog's own process.
    /// </summary>
    public partial class frmInstall : Form
    {
        /// <summary>
        /// The External Tools entry, which is what the Tools menu item and the
        /// toolbar button are labelled with. '&amp;' marks the accelerator key.
        /// </summary>
        private const string ToolTitle = "Git &Helper";

        /// <summary>
        /// Titles this tool has been registered under before. A machine that
        /// still has one keeps that slot, renamed in place: otherwise the old
        /// entry stays behind pointing at an exe that no longer exists, and
        /// since `Tools.ExternalCommandN` is a slot number rather than a name,
        /// a stale entry also repoints the shortcut and the toolbar button.
        /// </summary>
        private static readonly string[] LegacyToolTitles = { "Commit &Message" };

        private readonly List<VsInstall> _installs;

        public frmInstall()
        {
            InitializeComponent();
            AppIcon.Apply(this);

            UI_txt_Exe.Text = Assembly.GetExecutingAssembly().Location;

            _installs = VsInstall.Discover();
            foreach (var vs in _installs) UI_cmb_Vs.Items.Add(vs.ToString());

            var preferred = VsInstall.Preferred(_installs);
            if (preferred != null)
            {
                UI_cmb_Vs.SelectedIndex = _installs.IndexOf(preferred);
                UI_lbl_Detected.Text = _installs.Count == 1
                    ? "One installation found."
                    : _installs.Count + " installations found - " + preferred.ProductName + " is the newest.";
            }
            else
            {
                UI_lbl_Detected.Text = "No Visual Studio installation found.";
                UI_cmd_Install.Enabled = false;
            }
        }

        private VsInstall Selected
        {
            get
            {
                if (UI_cmb_Vs.SelectedIndex < 0 || UI_cmb_Vs.SelectedIndex >= _installs.Count) return null;
                return _installs[UI_cmb_Vs.SelectedIndex];
            }
        }

        private void frmInstall_Shown(object sender, EventArgs e)
        {
            if (!Elevation.IsElevated())
            {
                UI_lbl_Warning.Text = "Not running as administrator - loading the registry hive will fail.";
                UI_cmd_Install.Enabled = false;
                return;
            }

            foreach (var vs in _installs) Log("Found " + vs);
            if (_installs.Count > 0) Log(string.Empty);
        }

        private void UI_cmd_Install_Click(object sender, EventArgs e)
        {
            var vs = Selected;
            if (vs == null) return;

            if (Process.GetProcessesByName("devenv").Length > 0)
            {
                MessageBox.Show(
                    "Visual Studio is running. Close it completely and try again - otherwise VS " +
                    "overwrites the change when it exits.",
                    "Install VS Plugin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var exePath = UI_txt_Exe.Text;
            var shortcut = UI_txt_Shortcut.Text.Trim();
            var bindShortcut = UI_chk_Shortcut.Checked;
            var addToolbar = UI_chk_Toolbar.Checked;

            SetBusy(true);
            UI_txt_Log.Clear();
            Log("Installing into " + vs + Environment.NewLine);

            // The settings half starts and closes Visual Studio twice, so this
            // runs off the UI thread and logs back through Invoke.
            var worker = new Thread(() => Install(vs, exePath, shortcut, bindShortcut, addToolbar));
            worker.IsBackground = true;
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
        }

        private void Install(VsInstall vs, string exePath, string shortcut, bool bindShortcut, bool addToolbar)
        {
            try
            {
                var commandName = ExternalToolInstaller.Install(
                    vs, exePath, ToolTitle, LegacyToolTitles, Log);

                if (bindShortcut || addToolbar)
                {
                    Log(string.Empty);
                    var original = VsSettingsPatcher.Apply(
                        vs, commandName, shortcut, bindShortcut, addToolbar, Log);
                    Log("Unmodified export kept at: " + original);
                }
                else
                {
                    Log(string.Empty);
                    Log("Skipped the shortcut and the toolbar button.");
                }

                Log(string.Empty);
                Log("Done. Start Visual Studio and check Tools > External Tools.");
                Finish(true, "Installed into " + vs.ProductName + ".");
            }
            catch (Exception ex)
            {
                Log(string.Empty);
                Log("FAILED: " + ex.Message);
                Finish(false, ex.Message);
            }
        }

        /// <summary>Appends one line to the log, from any thread.</summary>
        private void Log(string line)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(Log), line); } catch (ObjectDisposedException) { }
                return;
            }

            UI_txt_Log.AppendText(line + Environment.NewLine);
        }

        private void Finish(bool ok, string message)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<bool, string>(Finish), ok, message); }
                catch (ObjectDisposedException) { }
                return;
            }

            SetBusy(false);
            MessageBox.Show(message, "Install VS Plugin", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private void SetBusy(bool busy)
        {
            UI_cmd_Install.Enabled = !busy;
            UI_cmb_Vs.Enabled = !busy;
            UI_chk_Shortcut.Enabled = !busy;
            UI_chk_Toolbar.Enabled = !busy;
            UI_txt_Shortcut.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }
    }
}
