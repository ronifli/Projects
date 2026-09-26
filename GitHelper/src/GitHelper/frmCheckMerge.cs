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
    /// "Is everything that was done on that branch also here?" - the check the
    /// structured commit message exists for.
    ///
    /// See <see cref="MergeCheck"/> for how a commit is matched across branches
    /// once a cherry-pick has given it a new SHA.
    /// </summary>
    public partial class frmCheckMerge : Form
    {
        private readonly RepoInfo _repo;
        /// <summary>One result per source branch checked - one, or every one for ALL.</summary>
        private List<MergeCheckResult> _results;

        /// <summary>Branches that could not be compared (no common ancestor...), with why.</summary>
        private List<string> _errors = new List<string>();

        /// <summary>
        /// What the source branches are compared against: the ORIGIN copy of
        /// the checked-out branch (its upstream, e.g. origin/main), never the
        /// local one - the audit is about what is on the server, not about
        /// commits someone has not pushed or pulled. Empty when the branch has
        /// no origin counterpart.
        /// </summary>
        private string _target = string.Empty;

        /// <summary>When origin was last fetched successfully, for the summary; null if it failed.</summary>
        private string _fetchNote = string.Empty;

        /// <summary>First entry of "Changes from": check every branch the list shows.</summary>
        private const string AllItem = "ALL";

        /// <summary>Every candidate branch; the combo shows whatever matches the search.</summary>
        private string[] _allBranches;

        // Which branches are offered: MergeCheck.ReleaseBranchKeywords ("/rtm" AND "/root").

        public frmCheckMerge(RepoInfo repo)
        {
            InitializeComponent();
            AppIcon.Apply(this);

            _repo = repo;
            Text = "Check Merge  -  " + repo.Name;

            _target = Git.Upstream(repo.Path);
            if (_target.Length == 0 &&
                Git.RemoteBranches(repo.Path).Contains("origin/" + repo.Branch, StringComparer.Ordinal))
                _target = "origin/" + repo.Branch;
            UI_lbl_Target.Text = _target.Length > 0 ? _target : repo.Branch + "  (not on origin)";

            FillSources();
            // the date used last time (registry CheckMerge.AISince), else 30 days back
            UI_dtp_AISince.Value = ClampToPicker(UI_dtp_AISince, Settings.GetDate("CheckMerge.AISince", DateTime.Today.AddDays(-30)));
            UI_dtp_AISince.ValueChanged += (sender, e) => Settings.SetDate("CheckMerge.AISince", UI_dtp_AISince.Value.Date);

            // not from here: the fetch posts back with BeginInvoke, which needs
            // the window's handle - it exists once the form is shown
            Shown += (sender, e) => FetchThenRefill();
        }

        /// <summary>
        /// Brings origin/* up to date in the background when the window opens,
        /// then refills the list - a release branch created this morning should
        /// be there. The list from the last fetch is usable meanwhile.
        /// </summary>
        private void FetchThenRefill()
        {
            SetBusy(true);
            UI_lbl_Summary.Text = "Fetching from origin...";
            var worker = new Thread(() =>
            {
                var fetch = CommitLog.Fetch(_repo.Path);
                Post(() =>
                {
                    SetBusy(false);
                    _fetchNote = fetch.Ok ? "origin fetched " + DateTime.Now.ToString("HH:mm") : null;
                    FillSources();
                    UI_lbl_Summary.Text = fetch.Ok
                        ? "Branches as on origin (fetched " + DateTime.Now.ToString("HH:mm") + ")."
                        : "FETCH FAILED - the branches are as of the last successful fetch.";
                    if (_target.Length == 0)
                        UI_lbl_Summary.Text = "'" + _repo.Branch + "' is not on origin - push it first; " +
                                          "the check compares origin branches only.";
                });
            });
            worker.IsBackground = true;
            worker.Start();
        }

        /// <summary>
        /// Branches worth comparing against: those under root with an RTM
        /// segment, on origin and locally (<see cref="MergeCheck.ReleaseBranchKeywords"/>), minus the
        /// one we are on - a branch cannot be missing from itself.
        /// </summary>
        private void FillSources()
        {
            // origin only - local branches may hold unpushed or stale work
            _allBranches = Git.RemoteBranches(_repo.Path)
                .Where(b => b.Length > 0)
                .Where(MergeCheck.IsReleaseBranch)
                .Where(b => !string.Equals(b, _target, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            ApplySearch();

            // The maintained release is what people check against most often.
            var preferred = _allBranches.FirstOrDefault(
                b => b.IndexOf("RTM", StringComparison.OrdinalIgnoreCase) >= 0);
            if (preferred != null) UI_cmb_Source.SelectedItem = preferred;
            else if (UI_cmb_Source.Items.Count > 0) UI_cmb_Source.SelectedIndex = 0;
        }

        private void UI_txt_Search_TextChanged(object sender, EventArgs e) { ApplySearch(); }

        /// <summary>
        /// Refills the branch list with whatever matches, keeping the current
        /// choice when it still does - typing a narrower search must not
        /// silently select a different branch.
        /// </summary>
        private void ApplySearch()
        {
            var needle = UI_txt_Search.Text.Trim();
            var previous = UI_cmb_Source.SelectedItem as string;

            var shown = _allBranches
                .Where(b => needle.Length == 0 ||
                            b.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            UI_cmb_Source.BeginUpdate();
            UI_cmb_Source.Items.Clear();
            if (shown.Length > 1) UI_cmb_Source.Items.Add(AllItem);   // one branch: ALL would be the same thing
            foreach (var b in shown) UI_cmb_Source.Items.Add(b);
            UI_cmb_Source.EndUpdate();

            if (previous != null && UI_cmb_Source.Items.Contains(previous))
                UI_cmb_Source.SelectedItem = previous;
            else if (shown.Length > 0)
                UI_cmb_Source.SelectedItem = shown[0];

            UI_lbl_MatchCount.Text = needle.Length == 0
                ? _allBranches.Length + " /root + /rtm branch(es)"
                : shown.Length + " of " + _allBranches.Length + " match";

            UI_cmd_Check.Enabled = UI_cmb_Source.Items.Count > 0;
        }

        /// <summary>The branches the list currently offers, without ALL.</summary>
        private string[] ListedBranches
        {
            get { return UI_cmb_Source.Items.Cast<string>().Where(b => b != AllItem).ToArray(); }
        }

        private void UI_cmd_Check_Click(object sender, EventArgs e)
        {
            var chosen = UI_cmb_Source.SelectedItem as string;
            if (chosen == null) return;

            // ALL = every branch the list shows right now, so a search narrows it
            var sources = chosen == AllItem ? ListedBranches : new[] { chosen };
            if (sources.Length == 0) return;
            if (_target.Length == 0)
            {
                MessageBox.Show("'" + _repo.Branch + "' has no branch on origin to compare against.\n\n" +
                                "The check compares origin with origin - push the branch first.",
                                "Check Merge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var target = _target;

            SetBusy(true);
            UI_grd_Results.Items.Clear();
            UI_lbl_Summary.Text = "Comparing " + (sources.Length == 1 ? sources[0] : sources.Length + " branches") +
                              " against " + target + "...";

            // git log over a release branch is not instant, and a frozen window
            // is what this codebase keeps getting wrong.
            var worker = new Thread(() =>
            {
                // both sides are origin refs: bring them up to date first
                Post(() => UI_lbl_Summary.Text = "Fetching from origin...");
                var fetch = CommitLog.Fetch(_repo.Path);
                Post(() => _fetchNote = fetch.Ok ? "origin fetched " + DateTime.Now.ToString("HH:mm") : null);

                var results = new List<MergeCheckResult>();
                var errors = new List<string>();
                for (var i = 0; i < sources.Length; i++)
                {
                    var source = sources[i];
                    if (sources.Length > 1)
                    {
                        var n = i + 1;
                        Post(() => UI_lbl_Summary.Text = "Comparing " + n + " of " + sources.Length + ": " + source + "...");
                    }

                    // one branch that cannot be compared must not stop the others
                    try { results.Add(MergeCheck.Run(_repo.Path, source, target)); }
                    catch (Exception ex) { errors.Add(source + ": " + ex.Message); }
                }
                Post(() => Show(results, errors));
            });
            worker.IsBackground = true;
            worker.Start();
        }

        /// <summary>A stored date the picker cannot show (out of its range) falls back to its nearest end.</summary>
        private static DateTime ClampToPicker(DateTimePicker picker, DateTime value)
        {
            if (value < picker.MinDate) return picker.MinDate;
            if (value > picker.MaxDate) return picker.MaxDate;
            return value;
        }

        private void Post(Action action)
        {
            if (IsDisposed) return;
            try { BeginInvoke(action); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void Show(List<MergeCheckResult> results, List<string> errors)
        {
            _results = results;
            _errors = errors;
            SetBusy(false);
            Populate();

            if (results.Count == 0 && errors.Count > 0)
                MessageBox.Show(string.Join("\n\n", errors), "Check Merge", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void UI_chk_MissingOnly_CheckedChanged(object sender, EventArgs e) { Populate(); }

        private void Populate()
        {
            if (_results == null) return;

            UI_grd_Results.BeginUpdate();
            UI_grd_Results.Items.Clear();
            foreach (var result in _results)
            {
                var shown = UI_chk_MissingOnly.Checked
                    ? result.Items.Where(i => !i.IsPresent).ToList()
                    : result.Items;

                foreach (var item in shown)
                {
                    var row = new ListViewItem(new[]
                    {
                        result.Source,
                        item.StatusText,
                        item.CommitType,
                        item.TT,
                        item.Subject,
                        item.Date,
                        item.Author,
                        item.ShortSha,
                    }) { Tag = Tuple.Create(result, item) };

                    // Missing is the answer the report exists to produce; the
                    // message-only matches are flagged as worth a second look.
                    if (!item.IsPresent) row.ForeColor = Color.Firebrick;
                    else if (item.Status == MergeStatus.SameSubject || item.Status == MergeStatus.SameTT)
                        row.ForeColor = Color.DarkGoldenrod;
                    else row.ForeColor = Color.ForestGreen;

                    UI_grd_Results.Items.Add(row);
                }
            }
            UI_grd_Results.EndUpdate();

            UI_lbl_Summary.Text = (_results.Count == 1 && _errors.Count == 0
                ? SummaryFor(_results[0])
                : SummaryForMany())
                + (_fetchNote == null ? "  (FETCH FAILED - origin as of the last fetch.)" : string.Empty);
        }

        /// <summary>One branch - the wording the report always had.</summary>
        private string SummaryFor(MergeCheckResult r)
        {
            // Always say how many were left out for having no structured
            // message: the audit only covers what the dialog wrote, and a big
            // number here means it covers less than the result suggests.
            var skipped = r.SkippedUnstructured > 0
                ? "  " + r.SkippedUnstructured + " commit(s) skipped - no structured message."
                : string.Empty;

            if (r.Items.Count == 0)
            {
                return (r.SkippedUnstructured > 0
                    ? "No structured commit on " + r.Source + " is missing from " + r.Target + "."
                    : "Nothing to check: every commit on " + r.Source +
                      " is already reachable from " + r.Target + " - the branch has been merged in.")
                    + skipped;
            }

            return string.Format(
                "{0} structured commit(s) on {1} are not in {2}'s history: {3} look ported, {4} MISSING.{5}{6}",
                r.Items.Count, r.Source, r.Target, r.PresentCount, r.MissingCount,
                UI_chk_MissingOnly.Checked ? "  Showing the missing ones." : string.Empty,
                skipped);
        }

        /// <summary>ALL: totals, which branches have something missing, and any that failed.</summary>
        private string SummaryForMany()
        {
            var missing = _results.Sum(r => r.MissingCount);
            var ported = _results.Sum(r => r.PresentCount);
            var skipped = _results.Sum(r => r.SkippedUnstructured);
            var withMissing = _results.Where(r => r.MissingCount > 0)
                .Select(r => r.Source + " (" + r.MissingCount + ")").ToList();

            var text = _results.Count + " branch(es) against " + _target + ": " +
                       ported + " look ported, " + missing + " MISSING" +
                       (withMissing.Count > 0 ? " - in " + string.Join(", ", withMissing) : " - nothing missing anywhere") + ".";
            if (UI_chk_MissingOnly.Checked) text += "  Showing the missing ones.";
            if (skipped > 0) text += "  " + skipped + " commit(s) skipped - no structured message.";
            if (_errors.Count > 0) text += "  Could not compare: " + string.Join("; ", _errors);
            return text;
        }

        private void UI_cmd_Copy_Click(object sender, EventArgs e)
        {
            if (_results == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("From\tStatus\tType\tTT\tSubject\tDate\tAuthor\tCommit");
            foreach (ListViewItem row in UI_grd_Results.Items)
                sb.AppendLine(string.Join("\t", row.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text)));

            try
            {
                // Same retry as the commit dialog: another process can own the
                // clipboard when we ask.
                Clipboard.SetDataObject(sb.ToString(), true, 10, 100);
                UI_cmd_Copy.Text = "Copied!";
                Refresh();
                Thread.Sleep(350);
                UI_cmd_Copy.Text = "Copy as &TSV";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not copy: " + ex.Message, "Check Merge",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- AI check ---------------------------------------------------------------

        /// <summary>
        /// The AI check: for the branch(es) chosen in "Changes from", every
        /// commit since the chosen date that is not in the target's history,
        /// judged by Copilot from the code itself (see <see cref="MergeAi"/>).
        /// Origin is fetched first, like the regular check. One request per
        /// branch, one after another in the background; a branch that fails
        /// does not stop the others.
        /// </summary>
        private void UI_cmd_AICheck_Click(object sender, EventArgs e)
        {
            var chosen = UI_cmb_Source.SelectedItem as string;
            if (chosen == null) return;
            if (_target.Length == 0)
            {
                MessageBox.Show("'" + _repo.Branch + "' has no branch on origin to compare against - push it first.",
                    "Check Merge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sources = chosen == AllItem ? ListedBranches : new[] { chosen };
            var since = UI_dtp_AISince.Value.Date;
            var target = _target;

            SetBusy(true);
            UI_prg_AICheck.Visible = true;
            UI_grd_AIResults.Items.Clear();
            UI_lbl_AIStatus.Text = "Fetching from origin...";

            var worker = new Thread(() =>
            {
                CommitLog.Fetch(_repo.Path);

                var results = new List<AiMergeResult>();
                var errors = new List<string>();
                for (var i = 0; i < sources.Length; i++)
                {
                    var source = sources[i];
                    var n = i + 1;
                    Post(() => UI_lbl_AIStatus.Text = "Asking AI" +
                        (sources.Length > 1 ? " (" + n + " of " + sources.Length + ")" : string.Empty) +
                        ": " + source + " - can take a few minutes...");

                    try { results.Add(MergeAi.Check(_repo.Path, source, target, since)); }
                    catch (Exception ex) { errors.Add(source + ": " + ex.Message); }
                }
                Post(() => ShowAi(results, errors, since));
            });
            worker.IsBackground = true;
            worker.Start();
        }

        /// <summary>Last AI check's results and target, for the missing-lines log.</summary>
        private List<AiMergeResult> _aiResults;
        private string _aiTarget;

        /// <summary>The commits worth a line-by-line look: everything not proven present.</summary>
        private List<AiMergeItem> AiGaps
        {
            get
            {
                return _aiResults == null ? new List<AiMergeItem>()
                    : _aiResults.SelectMany(r => r.Items)
                        .Where(i => i.Verdict == AiVerdict.Missing || i.Verdict == AiVerdict.Partial || i.Verdict == AiVerdict.Unsure)
                        .OrderBy(i => VerdictOrder(i.Verdict))
                        .ToList();
            }
        }

        private void UI_cmd_AILog_Click(object sender, EventArgs e) { OpenMissingLog(); }

        /// <summary>
        /// Double-click an AI result: the log opens on that commit's lines. The
        /// other missing / partial / unsure commits are listed too, so the log
        /// can be walked from there.
        /// </summary>
        private void UI_grd_AIResults_DoubleClick(object sender, EventArgs e)
        {
            if (UI_grd_AIResults.SelectedItems.Count == 0) return;
            var item = UI_grd_AIResults.SelectedItems[0].Tag as AiMergeItem;
            if (item == null) return;

            var items = AiGaps;
            if (!items.Contains(item)) items.Insert(0, item);   // a "present" row: show it anyway
            new frmMissingLog(_repo.Path, _aiTarget, items, item.Sha).Show(this);
        }

        /// <summary>
        /// Double-click a result of the regular check: the same log, for that
        /// one commit - its missing and changed lines against the target.
        /// </summary>
        private void UI_grd_Results_DoubleClick(object sender, EventArgs e)
        {
            if (UI_grd_Results.SelectedItems.Count == 0) return;
            var tag = UI_grd_Results.SelectedItems[0].Tag as Tuple<MergeCheckResult, MergeCheckItem>;
            if (tag == null) return;

            var r = tag.Item1;
            var c = tag.Item2;
            var item = new AiMergeItem
            {
                Source = r.Source,
                Sha = c.Sha,
                Date = c.Date,
                Author = c.Author,
                Subject = c.Subject,
                Verdict = c.IsPresent ? AiVerdict.Present : AiVerdict.Missing,
                Reason = "Check Merge: " + c.StatusText,
            };
            new frmMissingLog(_repo.Path, r.Target, new[] { item }, item.Sha).Show(this);
        }

        /// <summary>
        /// Opens the log of actual missing lines (see <see cref="frmMissingLog"/>)
        /// for the last AI check. Not modal, so it can stay open beside this window.
        /// </summary>
        private void OpenMissingLog()
        {
            var gaps = AiGaps;
            if (gaps.Count == 0)
            {
                MessageBox.Show(this, "The last AI check found nothing missing, partial or unsure.",
                    "Check Merge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            new frmMissingLog(_repo.Path, _aiTarget, gaps).Show(this);
        }

        private void ShowAi(List<AiMergeResult> results, List<string> errors, DateTime since)
        {
            _aiResults = results;
            _aiTarget = results.Count > 0 ? results[0].Target : _target;
            UI_prg_AICheck.Visible = false;
            SetBusy(false);

            UI_grd_AIResults.BeginUpdate();
            UI_grd_AIResults.Items.Clear();
            foreach (var r in results)
            {
                // the answer the panel exists for first: missing, then partial, unsure, present
                foreach (var item in r.Items.OrderBy(i => VerdictOrder(i.Verdict)))
                {
                    var row = new ListViewItem(new[]
                    {
                        r.Source, item.VerdictText, item.ShortSha, item.Date, item.Subject,
                        item.Evidence ?? string.Empty, item.Reason ?? string.Empty,
                    }) { Tag = item };
                    switch (item.Verdict)
                    {
                        case AiVerdict.Missing: row.ForeColor = Color.Firebrick; break;
                        case AiVerdict.Partial: row.ForeColor = Color.DarkGoldenrod; break;
                        case AiVerdict.Unsure: row.ForeColor = Color.Gray; break;
                        default: row.ForeColor = Color.ForestGreen; break;
                    }
                    UI_grd_AIResults.Items.Add(row);
                }
            }
            UI_grd_AIResults.EndUpdate();

            var all = results.SelectMany(r => r.Items).ToList();
            var text = all.Count == 0
                ? "No commits since " + since.ToString("yyyy-MM-dd") + " that are not already in " + _target + "."
                : all.Count + " commit(s) since " + since.ToString("yyyy-MM-dd") + ": " +
                  all.Count(i => i.Verdict == AiVerdict.Missing) + " MISSING, " +
                  all.Count(i => i.Verdict == AiVerdict.Partial) + " partial, " +
                  all.Count(i => i.Verdict == AiVerdict.Unsure) + " unsure, " +
                  all.Count(i => i.Verdict == AiVerdict.Present || i.Verdict == AiVerdict.SamePatch) + " present. " +
                  "AI suggestions - check before acting.";
            var notes = results.Where(r => !string.IsNullOrEmpty(r.Note) && r.Items.Count > 0).Select(r => r.Source + ": " + r.Note).ToList();
            if (notes.Count > 0) text += "  " + string.Join("; ", notes);
            if (errors.Count > 0) text += "  Failed: " + string.Join("; ", errors);
            UI_lbl_AIStatus.Text = text;

            UI_cmd_AILog.Enabled = AiGaps.Count > 0;

            // the answer is in the lines: open the log straight away when there is anything to show
            if (AiGaps.Count > 0) OpenMissingLog();

            if (results.Count == 0 && errors.Count > 0)
            {
                var msg = string.Join("\n\n", errors);
                if (CopilotSetup.IsAuthError(msg) || !CopilotSetup.IsInstalled)
                    msg += "\n\nSet up Copilot from the main window: Tools > Set up Copilot (AI).";
                MessageBox.Show(msg, "Check Merge - AI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static int VerdictOrder(AiVerdict v)
        {
            switch (v)
            {
                case AiVerdict.Missing: return 0;
                case AiVerdict.Partial: return 1;
                case AiVerdict.Unsure: return 2;
                case AiVerdict.Present: return 3;
                default: return 4;
            }
        }

        private void SetBusy(bool busy)
        {
            UI_cmd_AICheck.Enabled = !busy && UI_cmb_Source.Items.Count > 0;
            UI_cmd_AILog.Enabled = !busy && AiGaps.Count > 0;
            UI_dtp_AISince.Enabled = !busy;
            UI_cmd_Check.Enabled = !busy && UI_cmb_Source.Items.Count > 0;
            UI_cmb_Source.Enabled = !busy;
            UI_txt_Search.Enabled = !busy;
            UI_chk_MissingOnly.Enabled = !busy;
            UI_cmd_Copy.Enabled = !busy && _results != null;
            Cursor = busy ? Cursors.AppStarting : Cursors.Default;
        }
    }
}
