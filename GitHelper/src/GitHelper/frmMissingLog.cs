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
    /// The log behind the AI check: for every commit the AI called missing,
    /// partial or unsure, the lines that are not in the target, and the ones
    /// that are there but CHANGED - per file, with the source branch, the
    /// commit and line numbers on both sides (see <see cref="MissingLines"/>).
    /// Left: the commits; right: the selected one's report - missing added
    /// lines red, changed lines blue with the target's version under them,
    /// removed lines the target still has amber. Runs in the background.
    /// </summary>
    public partial class frmMissingLog : Form
    {
        private const string PlacementName = "MissingLog";

        private readonly string _repo;
        private readonly string _target;
        private readonly List<AiMergeItem> _items;
        private readonly Dictionary<string, string> _reports = new Dictionary<string, string>();

        /// <summary>Commit to open on (double-click in Check Merge); null = the first.</summary>
        private readonly string _selectSha;

        public frmMissingLog(string repo, string target, IEnumerable<AiMergeItem> items, string selectSha = null)
        {
            _selectSha = selectSha;
            InitializeComponent();
            AppIcon.Apply(this);
            WindowPlacement.RestoreWindow(this, PlacementName);

            _repo = repo;
            _target = target;
            _items = items.ToList();
            Text = "AI check - missing and changed lines against " + target;
        }

        private void frmMissingLog_Shown(object sender, EventArgs e)
        {
            WindowPlacement.RestoreSplitters(PlacementName, UI_pnl_Split);

            var worker = new Thread(() =>
            {
                var rows = new List<Tuple<AiMergeItem, int, string>>();
                foreach (var item in _items)
                {
                    string report;
                    int missing;
                    try
                    {
                        var files = MissingLines.Analyze(_repo, item.Sha, _target);
                        missing = files.Sum(f => f.MissingAdded + f.StillRemoved + f.ChangedLines);
                        report = MissingLines.Report(item, _target, files);
                    }
                    catch (Exception ex)
                    {
                        missing = -1;
                        report = "Could not compare " + item.ShortSha + ": " + ex.Message;
                    }
                    rows.Add(Tuple.Create(item, missing, report));
                }
                Post(() => Fill(rows));
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void Fill(List<Tuple<AiMergeItem, int, string>> rows)
        {
            UI_prg_Load.Visible = false;
            UI_grd_Commits.BeginUpdate();
            foreach (var r in rows)
            {
                _reports[r.Item1.Sha] = r.Item3;
                var row = new ListViewItem(new[]
                {
                    r.Item1.Source, r.Item1.VerdictText, r.Item1.ShortSha,
                    r.Item2 < 0 ? "?" : r.Item2.ToString(), r.Item1.Subject,
                }) { Tag = r.Item1 };
                row.ForeColor = r.Item2 > 0 ? Color.Firebrick : r.Item2 == 0 ? Color.ForestGreen : Color.Gray;
                UI_grd_Commits.Items.Add(row);
            }
            UI_grd_Commits.EndUpdate();
            var open = UI_grd_Commits.Items.Cast<ListViewItem>()
                .FirstOrDefault(i => _selectSha != null && ((AiMergeItem)i.Tag).Sha == _selectSha);
            if (open == null && UI_grd_Commits.Items.Count > 0) open = UI_grd_Commits.Items[0];
            if (open != null)
            {
                open.Selected = true;
                open.EnsureVisible();
            }

            var withGaps = rows.Count(r => r.Item2 > 0);
            UI_lbl_Summary.Text = rows.Count + " commit(s) checked line by line against " + _target + ": " +
                                  withGaps + " with missing or changed lines, " + rows.Count(r => r.Item2 == 0) +
                                  " whose lines are all there as they are. Red = missing, blue = changed (target version below it), amber = removed line still there.";
            UI_cmd_CopyAll.Enabled = rows.Count > 0;
        }

        private void UI_grd_Commits_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (UI_grd_Commits.SelectedItems.Count == 0) return;
            var item = (AiMergeItem)UI_grd_Commits.SelectedItems[0].Tag;
            string report;
            ShowReport(_reports.TryGetValue(item.Sha, out report) ? report : string.Empty);
        }

        /// <summary>The report text, coloured by line: headers bold, gaps red / amber.</summary>
        private void ShowReport(string report)
        {
            UI_txt_Log.SuspendLayout();
            UI_txt_Log.Clear();
            var bold = new Font(UI_txt_Log.Font, FontStyle.Bold);
            foreach (var line in report.Replace("\r\n", "\n").Split('\n'))
            {
                var color = line.StartsWith("  + ", StringComparison.Ordinal) ? Color.Firebrick
                          : line.StartsWith("  - ", StringComparison.Ordinal) ? Color.DarkGoldenrod
                          : line.StartsWith("  ~ ", StringComparison.Ordinal) ? Color.RoyalBlue
                          : line.StartsWith("    T", StringComparison.Ordinal) ? Color.SlateGray
                          : line.Contains("** FILE NOT IN TARGET **") ? Color.Firebrick
                          : SystemColors.WindowText;
                UI_txt_Log.SelectionStart = UI_txt_Log.TextLength;
                UI_txt_Log.SelectionColor = color;
                UI_txt_Log.SelectionFont = line.StartsWith("== ", StringComparison.Ordinal) || line.StartsWith("Commit ", StringComparison.Ordinal)
                    ? bold : UI_txt_Log.Font;
                UI_txt_Log.AppendText(line + "\n");
            }
            UI_txt_Log.SelectionStart = 0;
            UI_txt_Log.ScrollToCaret();
            UI_txt_Log.ResumeLayout();
        }

        private void UI_cmd_CopyAll_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            foreach (ListViewItem row in UI_grd_Commits.Items)
            {
                string report;
                if (_reports.TryGetValue(((AiMergeItem)row.Tag).Sha, out report))
                    sb.AppendLine(report).AppendLine(new string('-', 80));
            }
            try { Clipboard.SetDataObject(sb.ToString(), true, 10, 100); UI_lbl_Summary.Text = "Copied the whole log to the clipboard."; }
            catch (Exception ex) { MessageBox.Show(this, "Could not copy: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void UI_cmd_Close_Click(object sender, EventArgs e) { Close(); }

        private void frmMissingLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            WindowPlacement.Save(this, PlacementName, UI_pnl_Split);
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
