using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GitHelper
{
    /// <summary>
    /// The dialog. Controls and their design-time positions live in
    /// frmMain.Designer.cs; everything below is behaviour, plus the runtime
    /// layout pass that keeps the button rows and text boxes right under DPI
    /// scaling and resizing.
    /// </summary>
    public partial class frmMain : Form
    {
        // The column the input controls start at. The application declares
        // itself DPI-unaware (see app.manifest), so Windows always reports 96
        // dpi and these design-time pixels are the pixels that get drawn -
        // Windows stretches the finished window for a scaled display.
        private const int InputLeft = 136;

        private readonly List<RepoInfo> _repos;

        // The two rows whose buttons are re-flowed at runtime; taken from the
        // designer so the two layouts cannot drift apart.
        private int _bodyRowTop;
        private int _actionRowTop;

        private bool _pushOnly;
        private bool _suspendPreview;

        // The commit-msg hook is checked once per repository per run, and only
        // once the window is up: the constructor's own SelectedIndex change
        // must not put a message box in front of a form that is not shown yet.
#if COMMIT_HOOK
        private readonly HashSet<string> _hookChecked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _shown;
#endif

        public frmMain(List<RepoInfo> repos)
        {
            InitializeComponent();
            AppIcon.Apply(this);
            UI_cmd_ShowLog.Image = ToolbarIcons.ShowLog;

            // Take the two re-flowed rows' positions from the designer BEFORE
            // anything can resize the form: RestoreWindow sets the saved size,
            // that raises Resize -> UpdateButtonRow, and with these still 0
            // both rows were moved to the top of the window (and then read
            // back from there as their "designer" position).
            _bodyRowTop = UI_cmd_FromVs.Top;
            _actionRowTop = UI_cmd_Copy.Top;
            RestoreWindow();

            _repos = repos;

            UI_cmb_Type.SelectedIndex = -1;
            UI_cmb_Risk.SelectedIndex = -1;

            foreach (var r in _repos) UI_cmb_Repo.Items.Add(r.Name);
            if (_repos.Count > 0) UI_cmb_Repo.SelectedIndex = 0;   // the repo the dialog was launched from

            UpdateStagedLabel();
            // Deliberately NOT ImportInitialBody() here: it reaches into
            // Visual Studio over UI Automation, and anything slow in the
            // constructor happens before Application.Run shows the window -
            // the dialog would be a process with no UI until VS let go.
            // frmMain_Shown does it once the form is on screen.
            UpdatePreview();
            UpdateButtonRow();
        }

        /// <summary>
        /// Alt+F12 opens the admin window (frmAdmin) - deliberately on no menu.
        /// ProcessCmdKey, not KeyDown, so it works whichever control has focus.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.F12))
            {
                var r = CurrentRepo;
                if (r == null) { ShowError("No repository selected."); return true; }
                new frmAdmin(r).Show(this);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private RepoInfo CurrentRepo
        {
            get
            {
                if (UI_cmb_Repo.SelectedIndex < 0 || UI_cmb_Repo.SelectedIndex >= _repos.Count) return null;
                return _repos[UI_cmb_Repo.SelectedIndex];
            }
        }

        // --- designer event handlers ---------------------------------------

        private void field_Changed(object sender, EventArgs e) { UpdatePreview(); }

        private void UI_chk_StageAll_CheckedChanged(object sender, EventArgs e) { UpdateStagedLabel(); }

        private void UI_cmb_Repo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStagedLabel();

            var r = CurrentRepo;
            if (r == null) return;

            Text = "Git Helper  -  " + r.Name + " [" + r.Branch + "]";

            // New files are the usual reason a commit quietly misses something,
            // so when this repository has any, they start included. Unticking
            // stays unticked - the default is only re-applied when another
            // repository is selected.
            UI_chk_StageAll.Checked = r.Untracked.Length > 0;
#if COMMIT_HOOK   // commit-msg hook: off for now, see CommitHook.cs
            if (_shown) CheckCommitHook();
#endif
        }

        private void UI_cmd_AddRepo_Click(object sender, EventArgs e) { AddRepository(); }

        private void UI_cmd_FromVs_Click(object sender, EventArgs e) { ImportVsBody(); }

        private void UI_cmd_FromClip_Click(object sender, EventArgs e) { ImportClipboardBody(); }

        private void UI_cmd_LastCommit_Click(object sender, EventArgs e) { ImportLastCommit(); }

        private void UI_cmd_ClearBody_Click(object sender, EventArgs e)
        {
            UI_txt_Body.Clear();
            UpdatePreview();
        }

        private void UI_cmd_AI_Click(object sender, EventArgs e) { GenerateBodyWithAi(); }

        private void UI_mnu_CopilotSetup_Click(object sender, EventArgs e) { CheckCopilot(true); }

        private void UI_cmd_Copy_Click(object sender, EventArgs e) { CopyToClipboard(); }

        private void UI_cmd_Commit_Click(object sender, EventArgs e) { InvokeCommit(false); }

        private void UI_cmd_CommitPush_Click(object sender, EventArgs e) { InvokeCommit(true); }

        private void UI_cmd_SoftReset_Click(object sender, EventArgs e) { UndoLocalCommits(); }

        /// <summary>
        /// Registering the External Tool writes into Visual Studio's private
        /// registry hive, which needs administrator rights - so this hands the
        /// job to an elevated copy of the exe instead of asking the commit
        /// dialog to run elevated.
        /// </summary>
        private void UI_mnu_InstallPlugin_Click(object sender, EventArgs e)
        {
            if (Install.Elevation.IsElevated())
            {
                using (var form = new frmInstall()) form.ShowDialog(this);
                return;
            }

            if (!Install.Elevation.RelaunchElevated())
            {
                ShowError("Administrator rights are needed to register the tool with Visual Studio.\n\n" +
                          "Nothing was changed.");
            }
        }

        private void UI_mnu_SwitchBranch_Click(object sender, EventArgs e) { SwitchBranch(); }

        private void UI_mnu_ResetLocal_Click(object sender, EventArgs e) { ResetLocal(); }

        /// <summary>The context-menu item is ticked when the entry is registered.</summary>
        private void UI_mnu_Tools_DropDownOpening(object sender, EventArgs e)
        {
            // "Remove" once both entries are there; an older single entry, or
            // one for an exe that has moved, offers "Add" again to refresh it.
            UI_mnu_ShellMenu.Checked = ShellIntegration.IsRegistered();
            UI_mnu_ShellMenu.Text = UI_mnu_ShellMenu.Checked
                ? "Remove Git Helper from &Context Menu"
                : "Add Git Helper to &Context Menu";
        }

        private void UI_mnu_ShellMenu_Click(object sender, EventArgs e) { ToggleShellMenu(); }

        /// <summary>
        /// Compares another branch against this one and lists what never made
        /// it across - the audit the structured commit message exists for.
        /// </summary>
        private void UI_mnu_CheckMerge_Click(object sender, EventArgs e)
        {
            var r = CurrentRepo;
            if (r == null) { ShowError("No repository selected."); return; }

            r.Refresh();
            using (var form = new frmCheckMerge(r)) form.ShowDialog(this);
        }

        /// <summary>History of the selected repository's checked-out branch.</summary>
        private void UI_mnu_ShowLog_Click(object sender, EventArgs e)
        {
            var r = CurrentRepo;
            if (r == null) { ShowError("No repository selected."); return; }

            r.Refresh();
            using (var form = new frmShowLog(r)) form.ShowDialog(this);
        }

        /// <summary>
        /// Clears the stored login for this repository's remote and takes a new
        /// one, optionally changing who commits are attributed to as well.
        /// </summary>
        private void UI_mnu_SwitchUser_Click(object sender, EventArgs e)
        {
            var r = CurrentRepo;
            if (r == null) { ShowError("No repository selected."); return; }

            using (var form = new frmSwitchUser(r)) form.ShowDialog(this);
        }

        private void frmMain_Resize(object sender, EventArgs e) { UpdateButtonRow(); }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            UpdateButtonRow();
            Activate();
            UI_cmb_Type.Focus();

#if COMMIT_HOOK   // commit-msg hook: off for now, see CommitHook.cs
            _shown = true;
            CheckCommitHook();
#endif

            // BeginInvoke so this runs after the form has painted: the lookup
            // into Visual Studio is capped at a few seconds, but the window
            // should be up and usable before it even starts.
            BeginInvoke(new Action(ImportInitialBody));

            // After the window is up: the AI button needs Copilot CLI, and
            // offering to set it up belongs here, once, not in the middle of
            // writing a commit.
            //BeginInvoke(new Action(() => CheckCopilot(false)));
        }

        // --- window position ---------------------------------------------------

        /// <summary>
        /// Puts the window back where it was last closed.
        ///
        /// The saved rectangle is only used when it still lands on a screen:
        /// a laptop undocked from a second monitor, or a smaller display than
        /// last time, would otherwise open the dialog somewhere invisible with
        /// no obvious way to get it back.
        /// </summary>
        private void RestoreWindow()
        {
            var saved = Settings.WindowBounds();
            if (saved == null) return;

            var bounds = saved.Value;

            // Never smaller than the form can usefully be, never bigger than
            // the desktop.
            bounds.Width = Math.Max(bounds.Width, MinimumSize.Width);
            bounds.Height = Math.Max(bounds.Height, MinimumSize.Height);

            var desktop = SystemInformation.VirtualScreen;
            bounds.Width = Math.Min(bounds.Width, desktop.Width);
            bounds.Height = Math.Min(bounds.Height, desktop.Height);

            if (!IsOnAScreen(bounds)) return;   // keep the designer's CenterScreen

            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;

            if (Settings.WindowMaximized()) WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// True when a decent part of the title bar is visible on some screen,
        /// so the window can still be moved by hand.
        /// </summary>
        private static bool IsOnAScreen(Rectangle bounds)
        {
            // A strip of the top edge is what matters: a window dragged mostly
            // off the bottom is still usable, one whose caption is off-screen
            // is not.
            var caption = new Rectangle(bounds.X, bounds.Y, bounds.Width, 32);

            foreach (var screen in Screen.AllScreens)
            {
                var overlap = Rectangle.Intersect(screen.WorkingArea, caption);
                if (overlap.Width >= 80 && overlap.Height >= 8) return true;
            }
            return false;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // RestoreBounds, not Bounds: while maximized or minimized, Bounds
            // is the maximized rectangle, and restoring that would leave the
            // window pinned to the screen edges once un-maximized.
            var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;

            // A minimized window has no meaningful restore size on some
            // versions; skip rather than store nonsense.
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            Settings.SaveWindow(bounds, WindowState == FormWindowState.Maximized);
        }

        // --- adding a repository ---------------------------------------------

        /// <summary>
        /// Pick a working copy the built-in list doesn't cover. Any folder
        /// inside one will do - git resolves it to the root - and the root is
        /// remembered in HKCU so it is offered again next time.
        /// </summary>
        private void AddRepository()
        {
            string picked;
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Pick a folder inside the repository to add.";
                dlg.ShowNewFolderButton = false;

                var current = CurrentRepo;
                if (current != null) dlg.SelectedPath = current.Path;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                picked = dlg.SelectedPath;
            }

            var root = Git.ResolveRoot(picked);
            if (root == null)
            {
                ShowError("Not a git repository:\n" + picked +
                          "\n\nPick a folder inside a working copy.");
                return;
            }

            // Already listed: just select it, and don't record it twice.
            for (var i = 0; i < _repos.Count; i++)
            {
                if (!string.Equals(_repos[i].Path, root, StringComparison.OrdinalIgnoreCase)) continue;
                UI_cmb_Repo.SelectedIndex = i;
                return;
            }

            var repo = new RepoInfo(root);
            repo.Refresh();
            _repos.Add(repo);
            UI_cmb_Repo.Items.Add(repo.Name);
            UI_cmb_Repo.SelectedIndex = UI_cmb_Repo.Items.Count - 1;   // fires the usual repo-changed handler

            Settings.Remember(root);
        }

        // --- runtime layout -------------------------------------------------

        /// <summary>
        /// Lay a row out left-to-right from the input column, widening any
        /// button whose caption no longer fits its box.
        /// </summary>
        private static void SetRowLeftAligned(int top, params Button[] buttons)
        {
            const int gap = 8;
            var left = InputLeft;

            foreach (var b in buttons)
            {
                var needed = TextRenderer.MeasureText(b.Text, b.Font).Width + 24;
                if (needed > b.Width) b.Width = needed;

                b.Left = left;
                b.Top = top;
                left = b.Left + b.Width + gap;
            }
        }

        /// <summary>
        /// The designer positions everything for the default font and size;
        /// this re-flows the parts that a longer caption or a wider window
        /// would otherwise break. Do not replace it with fixed coordinates:
        /// captions grow under DPI scaling and the outer buttons fall off.
        /// </summary>
        private void UpdateButtonRow()
        {
            if (_actionRowTop == 0) return;   // designer positions not captured yet

            // left to right in each row, and the stage-all checkbox sits on its
            // own line below the action buttons
            SetRowLeftAligned(_actionRowTop, UI_cmd_Copy, UI_cmd_Commit, UI_cmd_CommitPush, UI_cmd_Cancel);
            SetRowLeftAligned(_bodyRowTop, UI_cmd_FromVs, UI_cmd_FromClip, UI_cmd_LastCommit, UI_cmd_ClearBody, UI_cmd_AI);

            // progress for the AI button, right after it on the same row
            UI_prg_AI.Left = UI_cmd_AI.Right + 8;
            UI_prg_AI.Top = _bodyRowTop + (UI_cmd_AI.Height - UI_prg_AI.Height) / 2;
            UI_lbl_AIStatus.Left = UI_prg_AI.Right + 6;
            UI_lbl_AIStatus.Top = _bodyRowTop + (UI_cmd_AI.Height - UI_lbl_AIStatus.Height) / 2;

            UI_chk_StageAll.Left = InputLeft;
            UI_chk_StageAll.Top = _actionRowTop + UI_cmd_Cancel.Height + 12;

            // same row as the commit buttons but hard right, clear of them
            var needed = TextRenderer.MeasureText(UI_cmd_SoftReset.Text, UI_cmd_SoftReset.Font).Width + 24;
            if (needed > UI_cmd_SoftReset.Width) UI_cmd_SoftReset.Width = needed;
            UI_cmd_SoftReset.Top = _actionRowTop + (UI_cmd_Cancel.Height - UI_cmd_SoftReset.Height) / 2;
            UI_cmd_SoftReset.Left = ClientSize.Width - 12 - UI_cmd_SoftReset.Width;

            UI_lbl_SoftReset.Top = UI_cmd_SoftReset.Top + UI_cmd_SoftReset.Height + 4;
            UI_lbl_SoftReset.Left = ClientSize.Width - 12 - UI_lbl_SoftReset.Width;

            UI_chk_PullRequest.Left = UI_chk_StageAll.Left;
            UI_chk_PullRequest.Top = UI_chk_StageAll.Top + UI_chk_StageAll.Height + 2;

            // the "what will be committed" line belongs under the checkboxes it explains
            UI_lbl_Staged.Left = UI_chk_StageAll.Left;
            UI_lbl_Staged.Top = UI_chk_PullRequest.Top + UI_chk_PullRequest.Height + 4;

            // stretch the text boxes to the window width; anchors alone don't
            // do it, because the form is resized after the controls are placed
            var rightEdge = ClientSize.Width - 12;
            UI_txt_Body.Width = rightEdge - UI_txt_Body.Left;
            UI_txt_Preview.Width = rightEdge - UI_txt_Preview.Left;

            UI_lbl_Count.Left = rightEdge - UI_lbl_Count.Width;
            UI_txt_Subject.Width = UI_lbl_Count.Left - 8 - UI_txt_Subject.Left;

            // the selected repo's full path runs to the right edge, stopping
            // short of the Add repo button
            UI_lbl_RepoPath.Width = rightEdge - UI_lbl_RepoPath.Left;

            // the "what will be committed" line runs to the right edge
            // (Install VS Plugin, which used to sit at its end, is Tools > Install VS Plugin now)
            UI_lbl_Staged.Width = rightEdge - UI_lbl_Staged.Left;
        }

        // --- message ---------------------------------------------------------

        /// <summary>
        /// The area checkboxes, in the order they appear on the form. Their
        /// Text is the value written to the message, so the list lives in one
        /// place - the designer - and nothing here repeats it.
        /// </summary>
        private IEnumerable<CheckBox> AreaBoxes
        {
            get { return UI_panel_Area.Controls.OfType<CheckBox>(); }
        }

        private CommitFields ReadFields()
        {
            return new CommitFields
            {
                Subject = UI_txt_Subject.Text,
                Body = UI_txt_Body.Text,
                TT = UI_txt_TT.Text,
                Customer = UI_txt_Customer.Text,
                CommitType = UI_cmb_Type.SelectedItem as string ?? string.Empty,
                Risk = UI_cmb_Risk.SelectedItem as string ?? string.Empty,
                Areas = AreaBoxes.Where(c => c.Checked).Select(c => c.Text).ToArray(),
                OtherVersions = UI_chk_OtherVersions.Checked,
            };
        }

        private string BuildMessage()
        {
            return CommitMessage.Build(ReadFields());
        }

        private void UpdatePreview()
        {
            if (_suspendPreview) return;

            UI_txt_Preview.Text = BuildMessage();

            var headerLen = UI_txt_Preview.Text.Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Length;
            UI_lbl_Count.Text = headerLen + "/72";
            UI_lbl_Count.ForeColor = headerLen > 72 ? Color.Firebrick
                : headerLen > 50 ? Color.DarkGoldenrod
                : Color.DimGray;

            // commit type, risk level and at least one area are mandatory;
            // TT and customer are not
            var ready = UI_txt_Subject.Text.Trim().Length > 0
                        && UI_cmb_Type.SelectedItem != null
                        && UI_cmb_Risk.SelectedItem != null
                        && AreaBoxes.Any(c => c.Checked);
            UI_cmd_Commit.Enabled = ready;
            UI_cmd_Copy.Enabled = ready;

            // With nothing to commit but commits waiting to go out, the same
            // button becomes a plain Push - no message needed, so the field
            // rules don't apply.
            var r = CurrentRepo;
            var hasWork = r != null && r.HasWork(UI_chk_StageAll.Checked);
            _pushOnly = r != null && !hasWork && r.Unpushed > 0;

            UI_cmd_CommitPush.Enabled = ready || _pushOnly;
            var wanted = _pushOnly ? "&Push " + r.Unpushed + " commit(s)" : "Commit && &Push";
            if (UI_cmd_CommitPush.Text != wanted)
            {
                UI_cmd_CommitPush.Text = wanted;
                UpdateButtonRow();
            }
        }

        private void UpdateStagedLabel()
        {
            // Only the selected repository: 'git ls-files --others' and friends
            // take a noticeable moment on a large working copy, and refreshing
            // every repo in the dropdown meant paying that for all of them
            // before the window could appear. Changing the selection comes back
            // through here, so the one on screen is always current.
            var current = CurrentRepo;
            if (current != null) current.Refresh();

            var r = current;
            if (r == null)
            {
                UI_lbl_Staged.Text = "No repository selected";
                return;
            }

            UI_lbl_RepoPath.Text = r.Path;

            var parts = new List<string>();
            if (r.Staged.Length > 0) parts.Add(r.Staged.Length + " staged");
            if (r.Dirty.Length > 0) parts.Add(r.Dirty.Length + " modified");
            if (r.Untracked.Length > 0) parts.Add(r.Untracked.Length + " new");

            if (parts.Count > 0)
            {
                UI_lbl_Staged.Text = r.Untracked.Length > 0 && !UI_chk_StageAll.Checked
                    ? string.Join(", ", parts) + "   (new files excluded - tick the box above to include them)"
                    : "Will commit: " + string.Join(", ", parts);
                UI_lbl_Staged.ForeColor = Color.ForestGreen;
            }
            else
            {
                UI_lbl_Staged.Text = "Nothing to commit - no changes in " + r.Name;
                UI_lbl_Staged.ForeColor = Color.Firebrick;
            }

            UpdatePullRequestState();
            UpdateSoftResetLabel();
        }

        /// <summary>
        /// A pull request needs a branch other than the one it would target, so
        /// the checkbox is only meaningful off the default branch.
        /// </summary>
        private void UpdatePullRequestState()
        {
            var r = CurrentRepo;
            if (r == null) return;

            var baseBranch = Git.DefaultBranch(r.Path);
            if (r.Branch == baseBranch)
            {
                UI_chk_PullRequest.Enabled = false;
                UI_chk_PullRequest.Checked = false;
                UI_chk_PullRequest.Text = "Create pull request after push  (on '" + baseBranch +
                                      "' - nothing to open a PR against)";
            }
            else
            {
                UI_chk_PullRequest.Enabled = true;
                UI_chk_PullRequest.Text = "Create pull request after push  (" + r.Branch + " -> " + baseBranch + ")";
            }
        }

        private void UpdateSoftResetLabel()
        {
            var n = UnpushedCount();
            if (n > 0)
            {
                UI_lbl_SoftReset.Text = n + " commit(s), local files will stay unchanged";
                UI_cmd_SoftReset.Enabled = true;
            }
            else if (n == 0)
            {
                UI_lbl_SoftReset.Text = "nothing to undo - everything is pushed";
                UI_cmd_SoftReset.Enabled = false;
            }
            else
            {
                UI_lbl_SoftReset.Text = "no upstream - local files will stay unchanged";
                UI_cmd_SoftReset.Enabled = true;
            }

            // Loading a message changes nothing by itself, so this is offered
            // for the last commit whether or not it has been pushed - only the
            // repo having no commits at all disables it.
            var r = CurrentRepo;
            UI_cmd_LastCommit.Enabled = r != null && Git.HasCommit(r.Path);
        }

        private int UnpushedCount()
        {
            var r = CurrentRepo;
            return r == null ? 0 : Git.UnpushedCount(r.Path);   // -1: no upstream, everything here is local
        }

        // --- loading the body -------------------------------------------------

        private void SetBody(string text)
        {
            UI_txt_Body.Text = text;
            UpdatePreview();
        }

        private static string GetClipboardText()
        {
            try
            {
                if (Clipboard.ContainsText()) return Clipboard.GetText();
            }
            catch
            {
                // clipboard can be locked by another process; not worth failing over
            }
            return string.Empty;
        }

        private bool ImportClipboardBody()
        {
            var text = GetClipboardText().Trim();
            if (text.Length == 0) return false;
            SetBody(text);
            return true;
        }

        /// <param name="timeoutMs">
        /// How long to let the UI Automation lookup run. The startup pull uses
        /// the short default and quietly falls back to the clipboard; pressing
        /// "From VS" is a deliberate request, so it waits longer.
        /// </param>
        /// <summary>
        /// Asks Visual Studio for its commit text without ever waiting on the
        /// UI thread, and hands the answer (possibly empty) to
        /// <paramref name="onResult"/> back on the UI thread.
        ///
        /// Waiting here instead - even with a timeout - stops the message pump,
        /// and a form that is on screen with a stalled pump is the half-painted
        /// window you get when the dialog is launched from Visual Studio.
        /// </summary>
        private void BeginVsLookup(int timeoutMs, Action<string> onResult)
        {
            var worker = new Thread(() =>
            {
                var text = VisualStudio.GetCommitMessage(timeoutMs).Trim();
                if (IsDisposed) return;
                try { BeginInvoke(onResult, text); } catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }   // window closed mid-flight
            });
            worker.IsBackground = true;
            worker.Start();
        }

        /// <summary>The "From VS" button: a deliberate request, so it waits longer.</summary>
        private void ImportVsBody()
        {
            UI_cmd_FromVs.Enabled = false;
            Cursor = Cursors.AppStarting;

            BeginVsLookup(10000, text =>
            {
                UI_cmd_FromVs.Enabled = true;
                Cursor = Cursors.Default;
                if (text.Length > 0) SetBody(text);
            });
        }

        /// <summary>
        /// On startup prefer what is actually typed in Visual Studio; the
        /// clipboard is only a fallback, since it often holds something
        /// unrelated. Both arrive after the window is already usable.
        /// </summary>
        private void ImportInitialBody()
        {
            BeginVsLookup(VisualStudio.DefaultTimeoutMs, text =>
            {
                // Don't overwrite anything typed while the lookup was running.
                if (UI_txt_Body.Text.Trim().Length > 0) return;

                if (text.Length > 0) SetBody(text);
                //else ImportClipboardBody();
            });
        }

        private void ImportLastCommit()
        {
            var r = CurrentRepo;
            if (r == null) { ShowError("No repository selected."); return; }

            var result = Git.Run(r.Path, "log", "-1", "--format=%B");
            if (!result.Ok || result.Output.Trim().Length == 0)
            {
                ShowError("No commit to load.");
                return;
            }

            var f = CommitMessage.Parse(result.Output);

            _suspendPreview = true;
            try
            {
                UI_txt_Subject.Text = f.Subject;
                UI_txt_Body.Text = f.Body;
                UI_txt_TT.Text = f.TT;
                UI_txt_Customer.Text = f.Customer;

                // SelectedItem stays unset when the stored value isn't one of
                // the choices, so an unrecognised message still forces a
                // deliberate pick
                UI_cmb_Type.SelectedItem = UI_cmb_Type.Items.Contains(f.CommitType) ? f.CommitType : null;
                UI_cmb_Risk.SelectedItem = UI_cmb_Risk.Items.Contains(f.Risk) ? f.Risk : null;

                // Same rule as the combos: an area the message names but this
                // form does not offer is dropped rather than invented, so the
                // author has to make a deliberate choice.
                foreach (var box in AreaBoxes)
                    box.Checked = f.Areas.Any(a => string.Equals(a, box.Text, StringComparison.OrdinalIgnoreCase));

                UI_chk_OtherVersions.Checked = f.OtherVersions;
            }
            finally
            {
                _suspendPreview = false;
            }
            UpdatePreview();
        }

        private void CopyToClipboard()
        {
            var message = BuildMessage();

            if (!TrySetClipboardText(message))
            {
                ShowError("Could not put the message on the clipboard.\n\n" +
                          "Another program is holding it open - a clipboard manager, a remote " +
                          "desktop session, or Visual Studio itself. The message is still in the " +
                          "Preview box, so try again, or copy it from there.");
                return;
            }
        }

        /// <summary>
        /// The clipboard is a shared, single-owner resource: any other process
        /// can have it open at the moment we ask, and <c>Clipboard.SetText</c>
        /// then throws <c>ExternalException</c> (CLIPBRD_E_CANT_OPEN). That is
        /// common enough - clipboard managers, RDP, Visual Studio - that it
        /// must not be an unhandled exception in a dialog holding a commit
        /// message someone just typed.
        ///
        /// The SetDataObject overload retries internally, which clears the
        /// transient case; the catch covers the rest.
        /// </summary>
		/// 
        private static bool TrySetClipboardText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            try
            {
                Clipboard.SetText(text );   
                return true;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                return false;   // someone else owns it
            }
            catch (ArgumentException)
            {
                return false;   // empty or unserialisable
            }
        }

        // --- AI body --------------------------------------------------------------

        /// <summary>
        /// Writes the body from the pending changes with GitHub Copilot CLI
        /// (see <see cref="AiBody"/>). Runs in the background - it takes a
        /// while - and the window stays usable; the answer replaces the body
        /// (after asking, when there is one) and is cut to 30 lines by the box
        /// itself if the model ignored the limit.
        /// </summary>
        private void GenerateBodyWithAi()
        {
            var r = CurrentRepo;
            if (r == null) { ShowError("No repository selected."); return; }

            r.Refresh();
            var includeNew = UI_chk_StageAll.Checked;
            if (!r.HasWork(includeNew))
            {
                ShowError("Nothing to describe - no changes in " + r.Name + ".");
                return;
            }

            // No questions. The body is read first (it goes along as notes for
            // the AI) and then cleared at once, so the box is empty while the
            // message is being generated and gets only the AI's text.
            // An empty subject is written too - one line, short enough for the
            // 72-character "Subject: ..." line.
            var fields = ReadFields();
            var wantSubject = fields.Subject.Trim().Length == 0;
            UI_lbl_AIStatus.Text = wantSubject ? "Generating subject and message..." : "Generating message...";
            UI_txt_Body.Clear();
            UpdatePreview();
            ShowAiProgress(true);

            var worker = new Thread(() =>
            {
                var result = AiBody.Generate(r.Path, fields, includeNew);
                if (IsDisposed) return;
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        ShowAiProgress(false);

                        if (result.Ok)
                        {
                            Settings.CopilotSignedIn = true;   // it answered, so it is signed in

                            // only into a subject that is still empty - one typed
                            // while waiting wins
                            if (wantSubject && result.Subject.Length > 0 && UI_txt_Subject.Text.Trim().Length == 0)
                                UI_txt_Subject.Text = result.Subject;
                            if (result.Body.Length > 0) SetBody(result.Body);
                            UpdatePreview();
                        }
                        else if (!CopilotSetup.IsInstalled)
                        {
                            CheckCopilot(true);
                        }
                        else if (CopilotSetup.IsAuthError(result.Error))
                        {
                            Settings.CopilotSignedIn = false;
                            OfferCopilotSignIn(true, "Copilot answered that it is not signed in:\n\n" +
                                                     Shorten(result.Error) + "\n\n");
                        }
                        else ShowError(result.Error);
                    }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }   // window closed mid-flight
            });
            worker.IsBackground = true;
            worker.Start();
        }

        // --- Copilot setup ----------------------------------------------------------

        /// <summary>
        /// Installs GitHub Copilot CLI when it is missing and signs in when it
        /// looks signed out - asking first each time. At startup
        /// (<paramref name="fromMenu"/> false) a No is remembered and the
        /// question is not repeated; Tools > Set up Copilot asks regardless.
        /// </summary>
        private void CheckCopilot(bool fromMenu)
        {
            if (IsDisposed || (!fromMenu && Settings.CopilotSetupDeclined)) return;

            if (!CopilotSetup.IsInstalled)
            {
                var how = CopilotSetup.InstallDescription;
                if (how == null)
                {
                    if (fromMenu)
                        ShowError("GitHub Copilot CLI is not installed, and neither winget nor npm was found to install it.\n\n" +
                                  "Install it by hand (https://docs.github.com/copilot/how-tos/copilot-cli), then try again.");
                    return;
                }

                var answer = MessageBox.Show(this,
                    "The AI button writes commit bodies with GitHub Copilot CLI, which is not installed on this PC.\n\n" +
                    "Install it now?\n\n  " + how + "\n\n" +
                    "This accepts the package's license terms. It takes a minute or two; you can keep working." +
                    (fromMenu ? string.Empty : "\n\nIf you say No you will not be asked again - use Tools > Set up Copilot."),
                    "Git Helper", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    if (!fromMenu) Settings.CopilotSetupDeclined = true;
                    return;
                }

                SetAiBusy(true, "Installing GitHub Copilot CLI (" + how + ")...");
                var worker = new Thread(() =>
                {
                    var result = CopilotSetup.Install();
                    PostToUi(() =>
                    {
                        SetAiBusy(false, null);
                        if (!result.Ok || !CopilotSetup.IsInstalled)
                        {
                            ShowError("Installing GitHub Copilot CLI failed (exit " + result.ExitCode + "):\n\n" + Shorten(result.Output));
                            return;
                        }
                        OfferCopilotSignIn(fromMenu, "GitHub Copilot CLI is installed.\n\n");
                    });
                });
                worker.IsBackground = true;
                worker.Start();
                return;
            }

            if (!CopilotSetup.LooksSignedIn())
            {
                OfferCopilotSignIn(fromMenu, string.Empty);
                return;
            }

            if (fromMenu)
                MessageBox.Show(this, "GitHub Copilot CLI is installed and signed in - the AI button is ready.",
                    "Git Helper", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Offers <c>copilot login</c>. GitHub does not let a program sign in
        /// to Copilot with a password, so GitHelper never asks for one: the
        /// user name and password go into GitHub's own page in the browser.
        /// The user name Visual Studio / git already use is shown (and put on
        /// the clipboard), so the right account is picked.
        /// </summary>
        private void OfferCopilotSignIn(bool fromMenu, string preface)
        {
            var r = CurrentRepo;
            var user = CopilotSetup.GitHubUserName(r == null ? null : r.Path);
            var who = user.Length > 0
                ? "Your GitHub account on this PC (from Visual Studio / git): " + user + "\n\n"
                : string.Empty;

            var answer = MessageBox.Show(this,
                preface + "Sign in to GitHub Copilot now?\n\n" + who +
                "A console window opens and shows a one-time code, and your browser opens GitHub:\n" +
                "  1. sign in there" + (user.Length > 0 ? " as " + user + " (copied to the clipboard)" : string.Empty) +
                " with your password and 2FA,\n" +
                "  2. enter the code from the console window and approve,\n" +
                "  3. the console window closes by itself.\n\n" +
                "GitHub does not allow programs to sign in to Copilot with a password, so it is typed on GitHub's page, not here." +
                (fromMenu ? string.Empty : "\n\nIf you say No you will not be asked again - use Tools > Set up Copilot."),
                "Git Helper", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                if (!fromMenu) Settings.CopilotSetupDeclined = true;
                return;
            }

            if (user.Length > 0)
            {
                try { Clipboard.SetDataObject(user, true, 5, 100); } catch { /* clipboard busy - not important */ }
            }

            SetAiBusy(true, "Waiting for the GitHub Copilot sign-in window to finish...");
            var worker = new Thread(() =>
            {
                string error;
                var ok = CopilotSetup.Login(out error);
                PostToUi(() =>
                {
                    SetAiBusy(false, null);
                    if (ok)
                        MessageBox.Show(this, "Signed in to GitHub Copilot - the AI button is ready.",
                            "Git Helper", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        ShowError(error + "\n\nTry again from Tools > Set up Copilot.");
                });
            });
            worker.IsBackground = true;
            worker.Start();
        }

        /// <summary>The running bar and "Generating message..." beside the AI button.</summary>
        private void ShowAiProgress(bool running)
        {
            UI_cmd_AI.Enabled = !running;
            UI_mnu_CopilotSetup.Enabled = !running;
            UI_prg_AI.Visible = running;
            UI_lbl_AIStatus.Visible = running;
        }

        private void SetAiBusy(bool busy, string status)
        {
            UI_cmd_AI.Enabled = !busy;
            UI_mnu_CopilotSetup.Enabled = !busy;
            if (busy)
            {
                UI_lbl_Staged.Text = status;
                UI_lbl_Staged.ForeColor = Color.DimGray;
            }
            else
            {
                UpdateStagedLabel();
            }
        }

        private void PostToUi(Action action)
        {
            if (IsDisposed) return;
            try { BeginInvoke(action); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }   // window closed mid-flight
        }

        private static string Shorten(string text)
        {
            text = (text ?? string.Empty).Trim();
            return text.Length > 1500 ? text.Substring(0, 1500) + "..." : text;
        }

#if COMMIT_HOOK   // commit-msg hook: off for now, see CommitHook.cs
        // --- commit message hook -----------------------------------------------

        /// <summary>
        /// Makes sure the selected repository enforces the message policy for
        /// commits made anywhere, not just here: writes .githooks/commit-msg
        /// and the .gitattributes rule when they are missing, and sets
        /// core.hooksPath. Quiet when there is nothing to do or only the
        /// config was missing. When files were added they are committed on
        /// their own as "Git Hook Update" (not pushed), and the user is told.
        /// </summary>
        private void CheckCommitHook()
        {
            var r = CurrentRepo;
            if (r == null || !_hookChecked.Add(r.Path)) return;

            var result = CommitHook.Ensure(r.Path);
            switch (result.State)
            {
                case HookSetup.Installed:
                    // Commit the new files straight away, on their own, so
                    // they reach the team with the next push instead of riding
                    // along in someone's unrelated change.
                    var commit = CommitHook.CommitSetup(r.Path, result.Pending);
                    UpdateStagedLabel();
                    UpdatePreview();   // Push button: one more commit waiting to go out

                    if (commit.Ok)
                    {
                        //MessageBox.Show(
                        //    "Commit message check enabled for " + r.Name + ".\n\n" +
                        //    "Committed as '" + CommitHook.CommitSubject + "' on " + r.Branch + ":\n  " +
                        //    string.Join("\n  ", result.Pending) + "\n\n" +
                        //    "Not pushed yet - it goes out with your next push.",
                        //    "Git Helper", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        ShowError(
                            "Commit message check enabled for " + r.Name + ", but committing the new files failed " +
                            "(exit " + commit.ExitCode + "):\n\n" + commit.Output + "\n\n" +
                            "The files are in the working copy - commit them yourself:\n  " +
                            string.Join("\n  ", result.Pending));
                    }
                    break;

                case HookSetup.OtherHooksPath:
                    MessageBox.Show(r.Name + ": " + result.Detail,
                        "Git Helper", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;

                case HookSetup.Failed:
                    ShowError(result.Detail);
                    break;
            }

            // Hook is on, but something beside it needs attention (the LFS
            // hooks could not be written).
            var sideNote = result.State == HookSetup.Installed || result.State == HookSetup.Enabled
                           || result.State == HookSetup.AlreadyActive;
            if (sideNote && result.Detail.Length > 0)
                MessageBox.Show(r.Name + ": " + result.Detail, "Git Helper", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
#endif

        // --- committing --------------------------------------------------------

        private void SetBusy(bool busy, string what)
        {
            foreach (var b in new[] { UI_cmd_Copy, UI_cmd_Commit, UI_cmd_CommitPush, UI_cmd_Cancel, UI_cmd_SoftReset })
                b.Enabled = !busy;

            if (busy)
            {
                Cursor = Cursors.WaitCursor;
                UI_lbl_Staged.Text = what;
                UI_lbl_Staged.ForeColor = Color.DimGray;
            }
            else
            {
                Cursor = Cursors.Default;
                UpdateStagedLabel();
            }
            Refresh();
        }

        private void InvokeCommit(bool push)
        {
            var msg = BuildMessage();
            UpdateStagedLabel();
            var includeNew = UI_chk_StageAll.Checked;

            var r = CurrentRepo;
            if (r == null) { ShowError("No repository selected."); return; }

            // Push-only: commits already exist, there is just nothing new to add.
            var pushOnly = push && !r.HasWork(includeNew) && r.Unpushed > 0;

            if (!pushOnly && !r.HasWork(includeNew))
            {
                ShowError(r.Untracked.Length > 0
                    ? "Nothing to commit in " + r.Name + ".\n\nThe only changes are " + r.Untracked.Length +
                      " new file(s), which git does not track yet.\n\n" +
                      "Tick 'Include new (untracked) files' to add them."
                    : "Nothing to commit - no changes in " + r.Name + ".");
                return;
            }

            // Pushing publishes work, so settle the upstream question before committing.
            var setUpstream = false;
            if (push && Git.Upstream(r.Path).Length == 0)
            {
                var answer = MessageBox.Show(
                    r.Name + ": branch '" + r.Branch + "' has no upstream.\r\n\r\n" +
                    "Push it to origin and track it?\r\n\r\n  git push --set-upstream origin " + r.Branch,
                    "No upstream", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes) return;
                setUpstream = true;
            }

            SetBusy(true, pushOnly ? "Pushing " + r.Name + "..." : "Committing in " + r.Name + "...");
            string tmp = null;
            try
            {
                var report = new List<string>();

                if (pushOnly)
                {
                    report.Add(r.Name + ": " + r.Unpushed + " commit(s) to push, nothing new to commit.");
                }
                else
                {
                    // -F <tmpfile> avoids shell quoting problems with
                    // multi-line messages, quotes and '!'.
                    tmp = CommitMessage.WriteTempFile(msg);

                    // 'git add -A' first when new files are wanted, since -a
                    // never picks up anything untracked.
                    if (includeNew && r.Untracked.Length > 0)
                    {
                        var addOut = Git.Run(r.Path, "add", "-A");
                        if (!addOut.Ok)
                        {
                            ShowError("git add -A failed:\n\n" + addOut.Output);
                            return;
                        }
                    }

                    // Commit what is already staged; when nothing is staged, -a
                    // sweeps up every modified tracked file.
                    var args = new List<string> { "commit", "-F", tmp };
                    if (Git.StagedFiles(r.Path).Length == 0) args.Add("-a");

                    var commitOut = Git.Run(r.Path, args.ToArray());
                    if (!commitOut.Ok)
                    {
                        ShowError("git commit failed (exit " + commitOut.ExitCode + "):\n\n" + commitOut.Output);
                        return;
                    }
                    report.Add(r.Name + " - commit:");
                    report.Add(commitOut.Output);
                }

                if (push)
                {
                    SetBusy(true, "Pushing " + r.Name + "...");
                    var pushOut = setUpstream
                        ? Git.Run(r.Path, "push", "--set-upstream", "origin", r.Branch)
                        : Git.Run(r.Path, "push");

                    if (!pushOut.Ok)
                    {
                        var tail = pushOnly
                            ? "\n\nNothing was changed locally."
                            : "\n\nThe commit is safe locally - push it when the problem is resolved.";
                        ShowError((pushOnly ? "Push failed" : "Committed, but the push failed") +
                                  " (exit " + pushOut.ExitCode + "):\n\n" + pushOut.Output + tail);
                        return;
                    }
                    report.Add(string.Empty);
                    report.Add(r.Name + " - push:");
                    report.Add(pushOut.Output);

                    if (UI_chk_PullRequest.Checked)
                    {
                        SetBusy(true, "Opening pull request...");
                        OpenPullRequest(r, UI_txt_Subject.Text.Trim(), msg);
                    }
                }

                MessageBox.Show(
                    string.Join("\n", report),
                    pushOnly ? "Pushed" : push ? "Committed and pushed" : "Committed",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            finally
            {
                if (tmp != null)
                {
                    try { File.Delete(tmp); } catch { /* temp file; nothing to do */ }
                }
                if (!IsDisposed) SetBusy(false, string.Empty);
            }
        }

        /// <summary>
        /// No gh CLI and no stored token: open GitHub's compare page with the
        /// fields pre-filled and let the author press "Create pull request"
        /// there.
        /// </summary>
        private void OpenPullRequest(RepoInfo r, string subject, string body)
        {
            var slug = Git.GitHubSlug(r.Path);
            if (slug.Length == 0)
            {
                ShowError("Cannot work out the GitHub project for " + r.Name + " from its 'origin' remote.");
                return;
            }

            var baseBranch = Git.DefaultBranch(r.Path);
            if (r.Branch == baseBranch)
            {
                ShowError(r.Name + " is on '" + baseBranch + "', the branch a pull request would target.\n\n" +
                          "The commit has been pushed. Create a feature branch to open a PR.");
                return;
            }

            var compare = "https://github.com/" + slug + "/compare/" + baseBranch + "..." + r.Branch + "?expand=1";
            var url = compare + "&title=" + Uri.EscapeDataString(subject) + "&body=" + Uri.EscapeDataString(body);

            // GitHub rejects absurdly long query strings; drop the body if it is huge
            if (url.Length > 6000) url = compare + "&title=" + Uri.EscapeDataString(subject);

            Process.Start(url);
        }

        // --- Explorer context menu ---------------------------------------------

        /// <summary>
        /// Adds or removes the Explorer folder entry. It is written under
        /// HKCU\Software\Classes, so no elevation is involved and nothing
        /// changes for other users of the machine.
        /// </summary>
        private void ToggleShellMenu()
        {
            try
            {
                if (ShellIntegration.IsRegistered())
                {
                    ShellIntegration.Unregister();
                    MessageBox.Show("Git Helper was removed from the folder context menu.", "Git Helper",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShellIntegration.Register();

                MessageBox.Show(
                    "Added to the folder context menu.\r\n\r\n" +
                    "Right-click a folder - or the background of an open folder - and pick:\r\n" +
                    "  Open Git Helper  - the commit dialog\r\n" +
                    "  Git Helper Log   - only the log window\r\n\r\n" +
                    "The folder is passed as the repository, so a folder anywhere inside a\r\n" +
                    "working copy opens that repo.\r\n\r\n" +
                    ShellIntegration.CommandLine + "\r\n\r\n" +
                    "On Windows 11 it sits under \"Show more options\" (Shift+F10), which is " +
                    "where every classic entry goes.",
                    "Git Helper", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Could not update the folder context menu:\n\n" + ex.Message);
            }
        }

        // --- switch branch -----------------------------------------------------

        /// <summary>
        /// Check out a branch chosen from origin. An existing local branch is
        /// checked out as it stands - with whatever local commits it carries;
        /// one that only exists on the server is created tracking it.
        ///
        /// Nothing is discarded here: a checkout that would overwrite local
        /// edits is refused by git, and the message says so rather than
        /// deciding for the author.
        /// </summary>
        private void SwitchBranch()
        {
            var r = CurrentRepo;
            if (r == null) { ShowError("No repository selected."); return; }

            r.Refresh();

            string branch;
            bool isNew;
            using (var dlg = new frmSwitchBranch(r))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                branch = dlg.SelectedBranch;
                isNew = dlg.IsNewLocalBranch;
            }

            SetBusy(true, "Switching " + r.Name + " to " + branch + "...");
            try
            {
                var checkout = isNew
                    ? Git.Run(r.Path, "checkout", "-b", branch, "--track", "origin/" + branch)
                    : Git.Run(r.Path, "checkout", branch);

                if (!checkout.Ok)
                {
                    ShowError("git checkout " + branch + " failed:\n\n" + checkout.Output +
                              "\n\nLocal changes that would be overwritten are the usual cause - " +
                              "commit them, or discard them with Git > Reset Local, then switch again.");
                    return;
                }

                MessageBox.Show(r.Name + " is now on '" + branch + "'." +
                                (isNew ? "\n\nCreated from origin/" + branch + " and tracking it." : string.Empty) +
                                "\n\n" + checkout.Output,
                    "Switch Branch", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                if (!IsDisposed)
                {
                    SetBusy(false, string.Empty);
                    // The title carries the branch, and the staged counts are
                    // for a different branch now.
                    var current = CurrentRepo;
                    if (current != null) Text = "Git Helper  -  " + current.Name + " [" + current.Branch + "]";
                }
            }
        }

        // --- reset local -------------------------------------------------------

        /// <summary>
        /// Throw away local changes and bring the branch up to date:
        /// <c>git reset --hard</c>, optionally <c>git clean -fd</c>, then
        /// <c>git pull --ff-only</c>.
        ///
        /// This is the one action here that destroys work that was never
        /// committed, so it names every file count before doing anything and
        /// does not proceed without a plain Yes.
        /// </summary>
        private void ResetLocal()
        {
            var r = CurrentRepo;
            if (r == null) { ShowError("No repository selected."); return; }

            r.Refresh();

            var changed = r.Staged.Length + r.Dirty.Length;
            var upstream = Git.Upstream(r.Path);

            if (changed == 0 && r.Untracked.Length == 0 && upstream.Length == 0)
            {
                ShowError("Nothing to reset in " + r.Name + ", and the branch has no upstream to pull from.");
                return;
            }

            var what = new List<string>();
            if (r.Staged.Length > 0) what.Add(r.Staged.Length + " staged file(s)");
            if (r.Dirty.Length > 0) what.Add(r.Dirty.Length + " modified file(s)");

            var message = new StringBuilder();
            message.AppendLine(r.Name + " [" + r.Branch + "]");
            message.AppendLine();
            message.AppendLine(what.Count > 0
                ? "Discard " + string.Join(" and ", what) + ", then pull from origin?"
                : "No local edits to discard - just pull from origin?");
            message.AppendLine();
            message.AppendLine("  git reset --hard HEAD");
            message.AppendLine(upstream.Length > 0
                ? "  git pull --ff-only"
                : "  (no upstream - nothing will be pulled)");
            message.AppendLine();

            if (what.Count > 0)
                message.AppendLine("Discarded edits CANNOT be recovered - they were never committed.");

            if (r.Untracked.Length > 0)
                message.AppendLine(r.Untracked.Length + " new/untracked file(s) will be left alone; " +
                                   "you will be asked about them separately.");

            var unpushed = Git.UnpushedCount(r.Path);
            if (unpushed > 0)
                message.AppendLine(unpushed + " unpushed commit(s) are kept. Use 'Undo Local Commits' for those.");

            if (MessageBox.Show(message.ToString(), "Reset Local",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            // Untracked files are a separate question: 'reset --hard' never
            // touches them, and deleting them is the easiest way to lose work
            // by accident.
            var deleteUntracked = false;
            if (r.Untracked.Length > 0)
            {
                var answer = MessageBox.Show(
                    "Also delete the " + r.Untracked.Length + " new/untracked file(s) in " + r.Name + "?\r\n\r\n" +
                    "  git clean -fd\r\n\r\n" +
                    "These are files git has never seen, so there is no copy of them anywhere.\r\n\r\n" +
                    "Yes = delete them.  No = keep them and continue.  Cancel = do nothing.",
                    "Reset Local", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (answer == DialogResult.Cancel) return;
                deleteUntracked = answer == DialogResult.Yes;
            }

            SetBusy(true, "Resetting " + r.Name + "...");
            try
            {
                var report = new List<string>();

                var reset = Git.Run(r.Path, "reset", "--hard", "HEAD");
                if (!reset.Ok)
                {
                    ShowError("git reset --hard failed:\n\n" + reset.Output);
                    return;
                }
                report.Add("reset --hard: " + reset.Output);

                if (deleteUntracked)
                {
                    var clean = Git.Run(r.Path, "clean", "-fd");
                    if (!clean.Ok)
                    {
                        ShowError("Local changes were discarded, but git clean failed:\n\n" + clean.Output);
                        return;
                    }
                    report.Add(string.Empty);
                    report.Add("clean -fd: " + (clean.Output.Length > 0 ? clean.Output : "nothing to remove"));
                }

                if (upstream.Length > 0)
                {
                    SetBusy(true, "Pulling " + r.Name + "...");

                    // --ff-only: with unpushed commits a plain pull would merge
                    // or rebase, which is not what "reset to what is on the
                    // server" should quietly do.
                    var pull = Git.Run(r.Path, "pull", "--ff-only");
                    if (!pull.Ok)
                    {
                        ShowError("Local changes were discarded, but the pull failed:\n\n" + pull.Output +
                                  (unpushed > 0
                                      ? "\n\nThis branch has " + unpushed + " unpushed commit(s), so it cannot " +
                                        "fast-forward. Push them, or drop them with 'Undo Local Commits', then pull."
                                      : string.Empty));
                        return;
                    }
                    report.Add(string.Empty);
                    report.Add("pull --ff-only: " + pull.Output);
                }
                else
                {
                    report.Add(string.Empty);
                    report.Add("No upstream for '" + r.Branch + "' - nothing pulled.");
                }

                MessageBox.Show(string.Join("\n", report), "Reset Local",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                if (!IsDisposed) SetBusy(false, string.Empty);
            }
        }

        // --- undo local commits ------------------------------------------------

        private void UndoLocalCommits()
        {
            var repo = CurrentRepo;
            if (repo == null) { ShowError("No repository selected."); return; }

            var unpushed = UnpushedCount();
            var local = Git.Run(repo.Path, "log", "--oneline", "-20").Lines;
            if (local.Length == 0) { ShowError("No commits to reset."); return; }

            var limit = unpushed >= 0 ? unpushed : local.Length;
            if (limit <= 0)
            {
                ShowError("Nothing to reset.\n\nEvery local commit has already been pushed. " +
                          "Resetting those would rewrite published history, so this button won't do it.");
                return;
            }

            var listing = string.Join("\r\n", local.Take(Math.Min(limit, 10)));
            int n;

            if (unpushed >= 0)
            {
                // The branch tracks a remote, so "everything since the last
                // push" is a known number - just confirm it.
                n = limit;
                var confirm = MessageBox.Show(
                    repo.Name + ": undo all " + n + " commit(s) made since the last push?\r\n\r\n" +
                    listing + "\r\n\r\ngit reset --mixed HEAD~" + n + "\r\n\r\n" +
                    "No file changes are lost - they return as unstaged changes.",
                    "Soft Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
            }
            else
            {
                // No upstream: "unpushed" is meaningless and resetting
                // everything could wipe the whole history, so ask rather than
                // assume.
                var answer = Microsoft.VisualBasic.Interaction.InputBox(
                    "This branch has no upstream, so there is no last push to measure from.\r\n\r\n" +
                    "How many commits to undo?\r\n\r\n" + listing, "Soft Reset", "1");
                if (string.IsNullOrWhiteSpace(answer)) return;

                if (!int.TryParse(answer.Trim(), out n) || n < 1)
                {
                    ShowError("'" + answer + "' is not a positive whole number.");
                    return;
                }
                if (n > limit)
                {
                    ShowError("This branch only has " + limit + " commit(s).");
                    return;
                }
            }

            // HEAD~N needs N ancestors: resetting every commit on the branch
            // would point past the root commit and git refuses.
            int total;
            if (int.TryParse(Git.Line(repo.Path, "rev-list", "--count", "HEAD"), out total) && n >= total)
            {
                ShowError("Cannot undo " + n + " commit(s): the branch only has " + total +
                          ", and there would be no commit left to reset onto.\n\nUndo at most " + (total - 1) + ".");
                return;
            }

            // --mixed: drop the commits and unstage, leaving the work as plain edits
            var reset = Git.Run(repo.Path, "reset", "--mixed", "HEAD~" + n);
            if (!reset.Ok)
            {
                ShowError("git reset failed:\n\n" + reset.Output);
                return;
            }

            UpdateStagedLabel();

            // Files those commits ADDED are untracked again now, and -a never
            // picks up untracked files - so say so rather than letting them be
            // dropped quietly.
            var note = string.Empty;
            var after = CurrentRepo;
            if (after != null && after.Untracked.Length > 0)
            {
                note = "\r\n\r\n" + after.Untracked.Length + " file(s) are new/untracked again - " +
                       "tick 'Include new (untracked) files' to commit them.";
            }

            MessageBox.Show(
                repo.Name + ": undid " + n + " commit(s). The changes are back in your working tree " +
                "as unstaged changes." + note,
                "Undo Local Commits", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Git Helper", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
