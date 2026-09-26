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
    /// Admin window - not on any menu; Alt+F12 in the main window opens it.
    ///
    /// Check commit messages: every commit on the chosen branch - main
    /// (origin's default branch), one release branch, or ALL of them - since
    /// the From Date whose message does not follow the structured format - see <see cref="MessageAudit"/> for
    /// what is required. Each offender is listed with what is wrong, its full
    /// message below the list, and the summary counts them per author.
    /// </summary>
    public partial class frmAdmin : Form
    {
        private const string PlacementName = "Admin";

        private readonly RepoInfo _repo;
        private AuditResult _result;

        /// <summary>First entry of the branch list: check every branch in it.</summary>
        private const string AllItem = "ALL";

        public frmAdmin(RepoInfo repo)
        {
            InitializeComponent();
            AppIcon.Apply(this);
            WindowPlacement.RestoreWindow(this, PlacementName);

            _repo = repo;
            Text = "Git Helper - Admin  -  " + repo.Name;
            FillBranches(AllItem);
            // the date used last time (registry Admin.FromDate), else 30 days back
            UI_dtp_FromDate.Value = ClampToPicker(UI_dtp_FromDate, Settings.GetDate("Admin.FromDate", DateTime.Today.AddDays(-30)));
            UI_dtp_FromDate.ValueChanged += (sender, e) => Settings.SetDate("Admin.FromDate", UI_dtp_FromDate.Value.Date);
        }

        private void frmAdmin_Shown(object sender, EventArgs e)
        {
            WindowPlacement.RestoreSplitters(PlacementName, UI_pnl_Split);
        }

        private void frmAdmin_FormClosing(object sender, FormClosingEventArgs e)
        {
            WindowPlacement.Save(this, PlacementName, UI_pnl_Split);
        }

        /// <summary>
        /// ALL, then main (origin's default branch), then the release branches
        /// - the same rule as Check Merge (<see cref="MergeCheck.ReleaseBranchKeywords"/>).
        /// Keeps the current choice when it is still there.
        /// </summary>
        private void FillBranches(string select)
        {
            var revs = MessageAudit.AuditRevs(_repo.Path);
            UI_cmb_Branch.BeginUpdate();
            UI_cmb_Branch.Items.Clear();
            if (revs.Count > 1) UI_cmb_Branch.Items.Add(AllItem);
            foreach (var r in revs) UI_cmb_Branch.Items.Add(r);
            UI_cmb_Branch.EndUpdate();

            if (select != null && UI_cmb_Branch.Items.Contains(select)) UI_cmb_Branch.SelectedItem = select;
            else if (UI_cmb_Branch.Items.Count > 0) UI_cmb_Branch.SelectedIndex = 0;
        }

        private void UI_cmd_CheckMessages_Click(object sender, EventArgs e)
        {
            var chosen = UI_cmb_Branch.SelectedItem as string ?? AllItem;
            var since = UI_dtp_FromDate.Value.Date;
            var merges = UI_chk_IncludeMerges.Checked;

            SetBusy(true);
            UI_grd_Commits.Items.Clear();
            UI_txt_Message.Clear();
            UI_lbl_Summary.Text = "Fetching from origin...";

            var worker = new Thread(() =>
            {
                var fetch = CommitLog.Fetch(_repo.Path);

                // after the fetch: a release branch created today is in the list now
                var all = MessageAudit.AuditRevs(_repo.Path);
                var revs = chosen == AllItem ? all : new List<string> { chosen };
                Post(() =>
                {
                    FillBranches(chosen);
                    UI_lbl_Summary.Text = "Reading " + (revs.Count == 1 ? revs[0] : revs.Count + " branches") + "...";
                });

                try
                {
                    var result = MessageAudit.CheckAll(_repo.Path, revs, since, merges);
                    Post(() => Show(result, since, fetch.Ok));
                }
                catch (Exception ex)
                {
                    Post(() =>
                    {
                        SetBusy(false);
                        UI_lbl_Summary.Text = string.Empty;
                        MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void Show(AuditResult result, DateTime since, bool fetched)
        {
            _result = result;
            SetBusy(false);

            UI_grd_Commits.BeginUpdate();
            foreach (var item in result.Items)
            {
                UI_grd_Commits.Items.Add(new ListViewItem(new[]
                {
                    item.BranchText, item.Date, item.Author, item.ShortSha, string.Join("; ", item.Problems), item.FirstLine,
                }) { Tag = item, ForeColor = Color.Firebrick });
            }
            UI_grd_Commits.EndUpdate();
            if (UI_grd_Commits.Items.Count > 0) UI_grd_Commits.Items[0].Selected = true;

            var byAuthor = result.Items.GroupBy(i => i.Author)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key + " " + g.Count());
            UI_lbl_Summary.Text = result.Items.Count == 0
                ? "All " + result.Checked + " commit(s) on " + result.Rev + " since " + since.ToString("yyyy-MM-dd") + " follow the format."
                : result.Items.Count + " of " + result.Checked + " commit(s) on " + result.Rev + " since " +
                  since.ToString("yyyy-MM-dd") + " do not follow the format - " + string.Join(", ", byAuthor) + ".";
            if (!fetched) UI_lbl_Summary.Text += "  (FETCH FAILED - origin as of the last fetch.)";
            UI_cmd_CopyTsv.Enabled = result.Items.Count > 0;
        }

        private void UI_grd_Commits_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (UI_grd_Commits.SelectedItems.Count == 0) return;
            var item = (AuditItem)UI_grd_Commits.SelectedItems[0].Tag;
            UI_txt_Message.Text =
                "Commit:   " + item.Sha + "\r\n" +
                "Branch:   " + item.BranchText + "\r\n" +
                "Author:   " + item.Author + "\r\n" +
                "Date:     " + item.Date + "\r\n" +
                "Problems: " + string.Join("; ", item.Problems) + "\r\n\r\n" +
                item.Message.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        private void UI_cmd_CopyTsv_Click(object sender, EventArgs e)
        {
            if (_result == null) return;
            var sb = new StringBuilder();
            sb.AppendLine("Branch\tDate\tAuthor\tCommit\tProblems\tFirst line");
            foreach (var i in _result.Items)
                sb.AppendLine(i.BranchText + "\t" + i.Date + "\t" + i.Author + "\t" + i.Sha + "\t" + string.Join("; ", i.Problems) + "\t" + i.FirstLine.Replace("\t", " "));
            try
            {
                Clipboard.SetDataObject(sb.ToString(), true, 10, 100);
                UI_lbl_Summary.Text = "Copied " + _result.Items.Count + " row(s) to the clipboard.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not copy: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UI_cmd_Close_Click(object sender, EventArgs e) { Close(); }

        private void SetBusy(bool busy)
        {
            UI_cmd_CheckMessages.Enabled = !busy;
            UI_cmb_Branch.Enabled = !busy;
            UI_dtp_FromDate.Enabled = !busy;
            UI_chk_IncludeMerges.Enabled = !busy;
            UI_prg_Check.Visible = busy;
            Cursor = busy ? Cursors.AppStarting : Cursors.Default;
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
    }
}
