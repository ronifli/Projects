using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GitHelper
{
    /// <summary>
    /// Picks a branch from origin and checks it out. The list is every
    /// <c>origin/*</c> ref the repository knows about, filtered as you type,
    /// because a repository with a few hundred branches is unusable otherwise.
    ///
    /// The checkout itself is the caller's job - this form only chooses.
    /// </summary>
    public partial class frmSwitchBranch : Form
    {
        private readonly RepoInfo _repo;
        private string[] _branches;

        public frmSwitchBranch(RepoInfo repo)
        {
            InitializeComponent();
            AppIcon.Apply(this);

            _repo = repo;
            UI_lbl_Repo.Text = repo.Name + "   [" + repo.Branch + "]";

            // Switching with local edits: git carries them over when it can and
            // refuses when the files differ between branches. Say so up front
            // rather than letting the checkout fail with a wall of paths.
            var dirty = repo.Staged.Length + repo.Dirty.Length;
            UI_lbl_Dirty.Text = dirty > 0
                ? dirty + " local change(s) are not committed. Git will carry them across when it can, " +
                  "and refuse the switch when the same files differ on the other branch - " +
                  "use Git > Reset Local first to discard them."
                : string.Empty;

            LoadBranches();
        }

        /// <summary>The chosen branch, without the 'origin/' prefix.</summary>
        public string SelectedBranch { get; private set; }

        /// <summary>True when no local branch of that name exists yet.</summary>
        public bool IsNewLocalBranch { get; private set; }

        private void frmSwitchBranch_Shown(object sender, EventArgs e)
        {
            UI_txt_Filter.Focus();
        }

        private void LoadBranches()
        {
            _branches = Git.RemoteBranches(_repo.Path);
            ApplyFilter();
        }

        /// <summary>
        /// Case-insensitive substring match on the full <c>origin/name</c>, so
        /// both "rtm" and "origin/rtm" find the same thing.
        /// </summary>
        private void ApplyFilter()
        {
            var needle = UI_txt_Filter.Text.Trim();

            var shown = _branches
                .Where(b => needle.Length == 0 ||
                            b.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            UI_lst_Branches.BeginUpdate();
            UI_lst_Branches.Items.Clear();
            foreach (var b in shown) UI_lst_Branches.Items.Add(Describe(b));
            UI_lst_Branches.EndUpdate();

            if (UI_lst_Branches.Items.Count > 0) UI_lst_Branches.SelectedIndex = 0;

            UI_lbl_Count.Text = needle.Length == 0
                ? _branches.Length + " branch(es) on origin"
                : shown.Length + " of " + _branches.Length + " match";

            UI_cmd_Switch.Enabled = UI_lst_Branches.Items.Count > 0;
        }

        /// <summary>
        /// Marks the branch already checked out, and the ones that exist
        /// locally - switching to those is a plain checkout with whatever local
        /// commits they already carry, not a fresh copy of the server's.
        /// </summary>
        private string Describe(string remoteBranch)
        {
            var name = Git.ShortBranchName(remoteBranch);

            if (string.Equals(name, _repo.Branch, StringComparison.Ordinal))
                return remoteBranch + "   (current)";

            return Git.LocalBranchExists(_repo.Path, name)
                ? remoteBranch + "   (local copy exists)"
                : remoteBranch;
        }

        private static string StripLabel(string item)
        {
            var cut = item.IndexOf("   (", StringComparison.Ordinal);
            return cut > 0 ? item.Substring(0, cut) : item;
        }

        private void UI_txt_Filter_TextChanged(object sender, EventArgs e) { ApplyFilter(); }

        /// <summary>Down moves into the list, Enter takes the selection.</summary>
        private void UI_txt_Filter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && UI_lst_Branches.Items.Count > 0)
            {
                UI_lst_Branches.Focus();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Enter)
            {
                Accept();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void UI_lst_Branches_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            Accept();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void UI_lst_Branches_DoubleClick(object sender, EventArgs e) { Accept(); }

        private void UI_cmd_Switch_Click(object sender, EventArgs e) { Accept(); }

        private void UI_cmd_Fetch_Click(object sender, EventArgs e)
        {
            // Without this the list only ever shows what was on the server the
            // last time anyone fetched - a branch made this morning would be
            // missing. --prune drops the ones that are gone.
            UI_cmd_Fetch.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                var result = Git.Run(_repo.Path, "fetch", "--prune", "origin");
                if (!result.Ok)
                {
                    MessageBox.Show("git fetch failed:\n\n" + result.Output, "Switch Branch",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                LoadBranches();
            }
            finally
            {
                Cursor = Cursors.Default;
                UI_cmd_Fetch.Enabled = true;
            }
        }

        private void Accept()
        {
            var item = UI_lst_Branches.SelectedItem as string;
            if (item == null) return;

            var name = Git.ShortBranchName(StripLabel(item));

            if (string.Equals(name, _repo.Branch, StringComparison.Ordinal))
            {
                MessageBox.Show(_repo.Name + " is already on '" + name + "'.", "Switch Branch",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectedBranch = name;
            IsNewLocalBranch = !Git.LocalBranchExists(_repo.Path, name);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
