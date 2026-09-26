using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GitHelper
{
    /// <summary>
    /// Git ▸ Show Log: the history of the checked-out branch, with the
    /// fields parsed from messages that carry "Commit Type:" (Commit Type,
    /// Area, TT Number, Risk Level) as columns. Other commits are listed in
    /// grey with only their first line as the subject. Selecting a commit
    /// shows its full message and changed files.
    ///
    /// Unticked, "From origin" shows the local branch (HEAD) - including
    /// commits not pushed yet, excluding ones not pulled. Ticked, it fetches
    /// and shows the upstream branch (origin/&lt;branch&gt;) as the server has it.
    ///
    /// Both git calls run on background threads - a 2000-commit log or a large
    /// --stat must not freeze the window.
    /// </summary>
    public partial class frmShowLog : Form
    {
        private readonly RepoInfo _repo;
        private List<LogEntry> _entries = new List<LogEntry>();
        private int _loadGeneration;
        private string _detailsSha;
        /// <summary>The branch whose history is shown: "main", "RTM0" or "origin/RTM0".</summary>
        private string _branch;

        /// <summary>
        /// Where "From origin" goes for <see cref="_branch"/>: its upstream
        /// (origin/&lt;name&gt;); empty when it has none, or when _branch is
        /// already a remote branch.
        /// </summary>
        private string _upstream = string.Empty;

        private string[] _remoteBranches = new string[0];

        /// <summary>Every branch the combo can offer; the search shows a subset of it.</summary>
        private List<string> _allBranches = new List<string>();

        // Column sort. -1 = git's own order (newest first, as the history runs).
        // Clicking a header sorts by it ascending, again descending, a third
        // time back to git's order.
        // Column index = LogColumn value, in the designer's column order.
        private int _sortColumn = -1;
        private bool _sortDescending;
        private string[] _columnTitles;

        /// <summary>The filter box above each column, in column order.</summary>
        private readonly TextBox[] _columnFilters;

        /// <summary>What the list shows now, for the summary line.</summary>
        private string _sourceNote = string.Empty;

        /// <param name="standalone">
        /// Started on its own (folder context menu, "Git Helper Log") rather
        /// than from the dialog: it then needs a taskbar button and a place of
        /// its own on screen.
        /// </param>
        public frmShowLog(RepoInfo repo, bool standalone = false)
        {
            InitializeComponent();
            if (standalone)
            {
                ShowInTaskbar = true;
                StartPosition = FormStartPosition.CenterScreen;
            }
            AppIcon.Apply(this);
            WindowPlacement.RestoreWindow(this, PlacementName);

            _repo = repo;
            FillBranches();
            SelectBranch(repo.Branch);

            _columnFilters = new[]
            {
                UI_txt_FilterDate, UI_txt_FilterAuthor, UI_txt_FilterType, UI_txt_FilterArea,
                UI_txt_FilterTT, UI_txt_FilterRisk, UI_txt_FilterSubject, UI_txt_FilterCommit,
            };

            _columnTitles = UI_grd_Commits.Columns.Cast<ColumnHeader>().Select(c => c.Text).ToArray();

            UI_cmb_Count.SelectedIndexChanged -= UI_cmb_Count_SelectedIndexChanged;
            UI_cmb_Count.SelectedItem = "500";
            UI_cmb_Count.SelectedIndexChanged += UI_cmb_Count_SelectedIndexChanged;
        }

        // --- designer event handlers -----------------------------------------

        /// <summary>Registry prefix for this window's position, size and splitters.</summary>
        private const string PlacementName = "ShowLog";

        private void frmShowLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            WindowPlacement.Save(this, PlacementName, UI_pnl_Split, UI_pnl_DetailsSplit);
        }

        private void frmShowLog_Shown(object sender, EventArgs e)
        {
            // after the window has its final (restored, maybe maximized) size:
            // a splitter's allowed range depends on it
            WindowPlacement.RestoreSplitters(PlacementName, UI_pnl_Split, UI_pnl_DetailsSplit);

            LayoutColumnFilters();
            Reload();
        }

        private void UI_cmd_Refresh_Click(object sender, EventArgs e) { Reload(); }

        private void UI_cmb_Count_SelectedIndexChanged(object sender, EventArgs e) { Reload(); }

        private void UI_txt_Search_TextChanged(object sender, EventArgs e) { Populate(); }

        private void UI_chk_Origin_CheckedChanged(object sender, EventArgs e) { Reload(); }

        private void UI_cmb_Branch_SelectionChangeCommitted(object sender, EventArgs e)
        {
            var chosen = UI_cmb_Branch.SelectedItem as string;
            if (chosen == null) return;
            ShowBranchItems(_allBranches);
            if (chosen != _branch) { SelectBranch(chosen); Reload(); }
            else UI_cmb_Branch.Text = _branch;
        }

        /// <summary>
        /// Search: typing narrows the list to the branches that contain the
        /// text anywhere, any case - "rtm" finds "RTM0" and "origin/RTM0" - and
        /// keeps it open. The typed text and the caret are put back after the
        /// refill, because clearing a ComboBox's items clears its text too.
        /// </summary>
        private void UI_cmb_Branch_TextUpdate(object sender, EventArgs e)
        {
            var typed = UI_cmb_Branch.Text;
            var caret = UI_cmb_Branch.SelectionStart;
            var needle = typed.Trim();

            var matches = needle.Length == 0
                ? _allBranches
                : _allBranches.Where(b => b.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            ShowBranchItems(matches);
            UI_cmb_Branch.DroppedDown = matches.Count > 0;

            // dropping the list selects the text and hides the mouse pointer
            UI_cmb_Branch.Text = typed;
            UI_cmb_Branch.SelectionStart = Math.Min(caret, typed.Length);
            UI_cmb_Branch.SelectionLength = 0;
            Cursor.Current = Cursors.Default;
        }

        private void ShowBranchItems(IList<string> branches)
        {
            UI_cmb_Branch.BeginUpdate();
            UI_cmb_Branch.Items.Clear();
            foreach (var b in branches) UI_cmb_Branch.Items.Add(b);
            UI_cmb_Branch.EndUpdate();
        }

        /// <summary>Typed a name: Enter applies it, Escape puts the current one back.</summary>
        private void UI_cmb_Branch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { ApplyTypedBranch(); e.Handled = e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Escape && UI_cmb_Branch.Text != _branch)
            {
                UI_cmb_Branch.DroppedDown = false;
                ShowBranchItems(_allBranches);
                UI_cmb_Branch.Text = _branch;
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        private void UI_cmb_Branch_Leave(object sender, EventArgs e) { ApplyTypedBranch(); }

        /// <summary>Clicking the branch name opens the list, not just a caret.</summary>
        private void UI_cmb_Branch_MouseClick(object sender, MouseEventArgs e)
        {
            if (!UI_cmb_Branch.DroppedDown) UI_cmb_Branch.DroppedDown = true;
        }

        private void UI_txt_ColumnFilter_TextChanged(object sender, EventArgs e) { Populate(); }

        private void UI_grd_Commits_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e) { LayoutColumnFilters(); }

        private void UI_grd_Commits_HorizontalScrolled(object sender, EventArgs e) { LayoutColumnFilters(); }

        private void UI_grd_Commits_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column != _sortColumn)
            {
                _sortColumn = e.Column;
                _sortDescending = false;
            }
            else if (!_sortDescending)
            {
                _sortDescending = true;
            }
            else
            {
                _sortColumn = -1;   // third click: back to git's order
                _sortDescending = false;
            }

            ShowSortArrow();
            Populate();
        }

        private void UI_grd_Commits_SelectedIndexChanged(object sender, EventArgs e) { ShowDetails(); }

        private void UI_cmd_CopySha_Click(object sender, EventArgs e)
        {
            var entry = Selected;
            if (entry == null) return;
            try
            {
                Clipboard.SetDataObject(entry.Sha, true, 10, 100);
                UI_lbl_Summary.Text = "Copied " + entry.Sha;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not copy: " + ex.Message, "Show Log", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- loading -----------------------------------------------------------

        private int MaxCount
        {
            get
            {
                int n;
                return int.TryParse(UI_cmb_Count.SelectedItem as string, out n) ? n : 0;   // "All" -> 0
            }
        }

        private void Reload()
        {
            var generation = ++_loadGeneration;
            var max = MaxCount;
            // A remote branch is always read after a fetch; a local one from
            // its upstream only when "From origin" is ticked.
            var isRemote = IsRemote(_branch);
            var fromOrigin = isRemote || (UI_chk_Origin.Checked && _upstream.Length > 0);
            var rev = isRemote ? _branch : fromOrigin ? _upstream : _branch;
            var shown = rev;

            SetBusy(true);
            Text = "Log  -  " + _repo.Name + "  [" + shown + "]";
            UI_lbl_Summary.Text = fromOrigin
                ? "Fetching from origin..."
                : "Reading history of " + shown + "...";

            var worker = new Thread(() =>
            {
                try
                {
                    // Fetch first, or origin/<branch> is only as new as the
                    // last pull. A failed fetch still shows what was last seen.
                    var note = "local branch " + shown;
                    if (fromOrigin)
                    {
                        var fetch = CommitLog.Fetch(_repo.Path);
                        note = fetch.Ok
                            ? shown + ", fetched " + DateTime.Now.ToString("HH:mm")
                            : "origin - FETCH FAILED, showing the last fetched state";
                    }

                    var entries = CommitLog.Read(_repo.Path, max, rev);
                    Post(() =>
                    {
                        if (generation != _loadGeneration) return;   // a newer load was started
                        _entries = entries;
                        _sourceNote = note;
                        SetBusy(false);
                        Populate();
                    });
                }
                catch (Exception ex)
                {
                    Post(() =>
                    {
                        if (generation != _loadGeneration) return;
                        SetBusy(false);
                        UI_lbl_Summary.Text = string.Empty;
                        MessageBox.Show(ex.Message, "Show Log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void Populate()
        {
            var needle = UI_txt_Search.Text;
            var filters = _columnFilters.Select(t => t.Text).ToArray();
            var activeFilters = filters.Count(f => f.Trim().Length > 0);
            var shown = Sorted(_entries
                .Where(x => x.Matches(needle) && LogSort.PassesColumnFilters(x, filters))
                .ToList());
            var previous = Selected == null ? null : Selected.Sha;

            UI_grd_Commits.BeginUpdate();
            UI_grd_Commits.Items.Clear();
            foreach (var x in shown)
            {
                // CellText is also what the column filters match, so a row
                // always shows the text it was filtered on.
                var cells = Enumerable.Range(0, UI_grd_Commits.Columns.Count)
                    .Select(c => LogSort.CellText(x, (LogColumn)c)).ToArray();
                var row = new ListViewItem(cells) { Tag = x };

                if (!x.IsStructured) row.ForeColor = Color.Gray;
                UI_grd_Commits.Items.Add(row);
                if (x.Sha == previous) row.Selected = true;
            }
            UI_grd_Commits.EndUpdate();

            if (UI_grd_Commits.SelectedItems.Count == 0 && UI_grd_Commits.Items.Count > 0)
                UI_grd_Commits.Items[0].Selected = true;
            if (UI_grd_Commits.SelectedItems.Count > 0)
                UI_grd_Commits.SelectedItems[0].EnsureVisible();
            else
                ShowDetails();

            var structured = shown.Count(x => x.IsStructured);
            var limited = MaxCount > 0 && _entries.Count == MaxCount ? " (newest " + MaxCount + ")" : string.Empty;
            UI_lbl_Summary.Text = (needle.Trim().Length == 0 && activeFilters == 0
                    ? _entries.Count + " commit(s)" + limited
                    : shown.Count + " of " + _entries.Count + " commit(s) match")
                + " (" + _sourceNote + ")"
                + "  -  " + structured + " with Commit Type, " + (shown.Count - structured) + " other (grey)";
        }

        // --- column filters ------------------------------------------------------

        /// <summary>
        /// Puts each filter box exactly over its column: same left edge and
        /// width, shifted by the list's border and its horizontal scroll, so
        /// they follow resized columns and a list scrolled sideways. A column
        /// dragged narrower than a usable box hides its box.
        /// </summary>
        private void LayoutColumnFilters()
        {
            if (_columnFilters == null || !UI_grd_Commits.IsHandleCreated) return;

            // where the list's client area (column 0 at scroll 0) starts,
            // in the filter panel's coordinates
            var client = UI_grd_Commits.RectangleToScreen(UI_grd_Commits.ClientRectangle);
            var x = UI_panel_ColumnFilters.PointToClient(client.Location).X - UI_grd_Commits.HorizontalOffset;

            UI_panel_ColumnFilters.SuspendLayout();
            for (var i = 0; i < _columnFilters.Length && i < UI_grd_Commits.Columns.Count; i++)
            {
                var width = UI_grd_Commits.Columns[i].Width;
                var box = _columnFilters[i];
                box.Visible = width > 12;
                box.SetBounds(x, 1, Math.Max(0, width - 1), box.Height);
                x += width;
            }
            UI_panel_ColumnFilters.ResumeLayout();
        }

        // --- sorting -------------------------------------------------------------

        private List<LogEntry> Sorted(List<LogEntry> list)
        {
            return _sortColumn < 0 ? list : LogSort.Sort(list, (LogColumn)_sortColumn, _sortDescending);
        }

        /// <summary>▲ / ▼ after the sorted column's title; none in git's order.</summary>
        private void ShowSortArrow()
        {
            for (var i = 0; i < UI_grd_Commits.Columns.Count && i < _columnTitles.Length; i++)
            {
                UI_grd_Commits.Columns[i].Text = i == _sortColumn
                    ? _columnTitles[i] + (_sortDescending ? " \u25BC" : " \u25B2")
                    : _columnTitles[i];
            }
        }

        // --- details -------------------------------------------------------------

        private LogEntry Selected
        {
            get
            {
                return UI_grd_Commits.SelectedItems.Count == 0
                    ? null
                    : UI_grd_Commits.SelectedItems[0].Tag as LogEntry;
            }
        }

        /// <summary>
        /// The selected commit: header and full message in the text box, and
        /// every file it changed - status, path, +/- lines - in the list below
        /// it. The files are read on a background thread.
        /// </summary>
        private void ShowDetails()
        {
            var x = Selected;
            UI_cmd_CopySha.Enabled = x != null;
            if (x == null)
            {
                _detailsSha = null;
                UI_txt_Details.Clear();
                UI_grd_Files.Items.Clear();
                UI_col_FilePath.Text = "File";
                return;
            }
            if (x.Sha == _detailsSha) return;
            _detailsSha = x.Sha;

            var head = new StringBuilder();
            head.AppendLine("Commit:  " + x.Sha);
            head.AppendLine("Author:  " + x.Author);
            head.AppendLine("Date:    " + x.Date);
            head.AppendLine();
            head.Append(x.Message.Replace("\r\n", "\n").Replace("\n", "\r\n"));
            UI_txt_Details.Text = head.ToString();

            UI_grd_Files.Items.Clear();
            UI_col_FilePath.Text = "File  (reading...)";

            var sha = x.Sha;
            var worker = new Thread(() =>
            {
                List<ChangedFile> files;
                try { files = CommitLog.ChangedFiles(_repo.Path, sha); }
                catch { files = new List<ChangedFile>(); }

                Post(() =>
                {
                    if (sha != _detailsSha) return;   // selection moved on
                    ShowFiles(files);
                });
            });
            worker.IsBackground = true;
            worker.Start();
        }

        /// <summary>Double-click a file: compare its before/after in ExamDiff Pro.</summary>
        private void UI_grd_Files_DoubleClick(object sender, EventArgs e)
        {
            if (UI_grd_Files.SelectedItems.Count == 0) return;
            var file = UI_grd_Files.SelectedItems[0].Tag as ChangedFile;
            if (file == null) return;

            Cursor = Cursors.AppStarting;
            try
            {
                string error;
                if (!FileCompare.Open(_repo.Path, file, out error))
                    MessageBox.Show(this, error, "File compare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ShowFiles(List<ChangedFile> files)
        {
            UI_grd_Files.BeginUpdate();
            UI_grd_Files.Items.Clear();
            foreach (var f in files.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase))
            {
                var row = new ListViewItem(new[] { f.Status, f.DisplayPath, f.Lines }) { Tag = f };
                switch (f.Code)
                {
                    case 'A': row.ForeColor = Color.ForestGreen; break;
                    case 'D': row.ForeColor = Color.Firebrick; break;
                    case 'R':
                    case 'C': row.ForeColor = Color.RoyalBlue; break;
                }
                UI_grd_Files.Items.Add(row);
            }
            UI_grd_Files.EndUpdate();

            var added = files.Count(f => f.Code == 'A');
            var modified = files.Count(f => f.Code == 'M');
            var deleted = files.Count(f => f.Code == 'D');
            var other = files.Count - added - modified - deleted;
            UI_col_FilePath.Text = files.Count == 0
                ? "File  (none)"
                : "File  (" + files.Count + ": " + added + " added, " + modified + " modified, " + deleted + " deleted" +
                  (other > 0 ? ", " + other + " other" : string.Empty) + ")";
        }

        // --- branch ----------------------------------------------------------------

        private static bool IsRemote(string branch)
        {
            return branch != null && branch.StartsWith("origin/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Local branches (the checked-out one first) then origin/*. Nothing is
        /// fetched for this - it lists what the last fetch saw; ticking From
        /// origin fetches.
        /// </summary>
        private void FillBranches()
        {
            var local = Git.Run(_repo.Path, "branch", "--format=%(refname:short)").Lines
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith("(", StringComparison.Ordinal))   // "(HEAD detached at ...)"
                .OrderBy(l => l, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _remoteBranches = Git.RemoteBranches(_repo.Path);

            var current = _repo.Branch;
            local.RemoveAll(b => string.Equals(b, current, StringComparison.Ordinal));

            _allBranches = new List<string>();
            if (!string.IsNullOrEmpty(current)) _allBranches.Add(current);
            _allBranches.AddRange(local);
            _allBranches.AddRange(_remoteBranches);
            ShowBranchItems(_allBranches);
        }

        /// <summary>
        /// Makes <paramref name="branch"/> the one shown and works out what
        /// "From origin" means for it: its upstream, or origin/&lt;same name&gt;
        /// when no upstream is configured but such a branch exists. A remote
        /// branch is origin already - the box is ticked and greyed.
        /// </summary>
        private void SelectBranch(string branch)
        {
            var wasRemote = IsRemote(_branch);
            _branch = string.IsNullOrEmpty(branch) ? "HEAD" : branch;
            UI_cmb_Branch.Text = _branch;
            var index = UI_cmb_Branch.FindStringExact(_branch);
            if (index >= 0) UI_cmb_Branch.SelectedIndex = index;

            _upstream = string.Empty;
            if (!IsRemote(_branch))
            {
                _upstream = Git.Line(_repo.Path, "rev-parse", "--abbrev-ref", "--symbolic-full-name", _branch + "@{upstream}");
                if (_upstream.Length == 0 && _remoteBranches.Contains("origin/" + _branch, StringComparer.Ordinal))
                    _upstream = "origin/" + _branch;
            }

            UI_chk_Origin.CheckedChanged -= UI_chk_Origin_CheckedChanged;
            if (IsRemote(_branch))
            {
                UI_chk_Origin.Checked = true;
                UI_chk_Origin.Enabled = false;
                UI_chk_Origin.Text = "From origin";
            }
            else
            {
                // coming back from a remote branch starts from the local copy again
                if (_upstream.Length == 0 || wasRemote) UI_chk_Origin.Checked = false;
                UI_chk_Origin.Enabled = _upstream.Length > 0;
                UI_chk_Origin.Text = _upstream.Length > 0 ? "From &origin" : "From origin (none)";
            }
            UI_chk_Origin.CheckedChanged += UI_chk_Origin_CheckedChanged;
        }

        /// <summary>
        /// Enter / leaving the box: an exact name wins; otherwise the first
        /// branch the search still shows (so "rtm" + Enter opens the first
        /// match). Nothing matching: beep, and the current branch stays.
        /// </summary>
        private void ApplyTypedBranch()
        {
            var typed = UI_cmb_Branch.Text.Trim();
            UI_cmb_Branch.DroppedDown = false;

            var chosen = _allBranches.FirstOrDefault(b => string.Equals(b, typed, StringComparison.OrdinalIgnoreCase))
                         ?? (typed.Length == 0 ? null
                             : _allBranches.FirstOrDefault(b => b.IndexOf(typed, StringComparison.OrdinalIgnoreCase) >= 0));

            ShowBranchItems(_allBranches);
            if (chosen == null)
            {
                if (typed != _branch) System.Media.SystemSounds.Beep.Play();
                SelectBranch(_branch);
                return;
            }
            if (chosen == _branch) { SelectBranch(_branch); return; }

            SelectBranch(chosen);
            Reload();
        }

        // --- helpers -------------------------------------------------------------

        private void Post(Action action)
        {
            if (IsDisposed) return;
            try { BeginInvoke(action); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void SetBusy(bool busy)
        {
            UI_cmd_Refresh.Enabled = !busy;
            UI_cmb_Count.Enabled = !busy;
            UI_cmb_Branch.Enabled = !busy;
            UI_chk_Origin.Enabled = !busy && !IsRemote(_branch) && _upstream.Length > 0;
            Cursor = busy ? Cursors.AppStarting : Cursors.Default;
        }
    }
}
