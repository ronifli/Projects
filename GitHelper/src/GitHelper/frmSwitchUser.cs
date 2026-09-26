using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GitHelper
{
    /// <summary>
    /// Signs this machine in to the remote as somebody else: forget what the
    /// credential helper has for the host, then hand it a new user name and
    /// password or token.
    ///
    /// Git Helper stores nothing itself. The password goes to
    /// <c>git credential approve</c> on standard input, and from there to
    /// whatever helper git is configured with - on these machines the Git
    /// Credential Manager, which keeps it in Windows Credential Manager. It is
    /// never written to a file, the registry, or a command line.
    /// </summary>
    public partial class frmSwitchUser : Form
    {
        private readonly RepoInfo _repo;
        private readonly CredentialTarget _target;

        /// <summary>What Windows Credential Manager holds for the host.</summary>
        private List<StoredCredential> _windows = new List<StoredCredential>();

        private const string ScopeRepository = "this repository only";
        private const string ScopeGlobal = "all repositories (global)";

        public frmSwitchUser(RepoInfo repo)
        {
            InitializeComponent();
            AppIcon.Apply(this);

            _repo = repo;
            _target = GitCredentials.TargetFor(repo.Path);

            UI_lbl_Repo.Text = repo.Name + "   [" + repo.Branch + "]";

            UI_txt_Name.Enabled = false;
            UI_txt_Email.Enabled = false;
            UI_cmb_Scope.Enabled = false;

            UI_cmb_Scope.Items.Add(ScopeRepository);
            UI_cmb_Scope.Items.Add(ScopeGlobal);
            UI_cmb_Scope.SelectedIndex = 0;

            ShowIdentity();

            DescribeTarget();
            UpdateEnabled();
        }

        /// <summary>
        /// Says what the credential would apply to, and disables the whole
        /// thing when there is nothing a user name and password could change.
        /// </summary>
        private void DescribeTarget()
        {
            if (_target == null)
            {
                UI_lbl_Remote.Text = "no 'origin' remote";
                UI_lbl_Stored.Text = string.Empty;
                SetStatus("This repository has no 'origin', so there is no login to change.", true);
                UI_cmd_Clear.Enabled = false;
                UI_txt_User.Enabled = false;
                UI_txt_Password.Enabled = false;
                return;
            }

            UI_lbl_Remote.Text = _target.RemoteUrl;

            if (_target.IsSsh)
            {
                UI_lbl_Stored.Text = "SSH key";
                SetStatus("'origin' is an SSH remote: it authenticates with a key from your SSH agent, " +
                          "not a stored user name and password. Change the key, or switch the remote to https.",
                          true);
                UI_cmd_Clear.Enabled = false;
                UI_txt_User.Enabled = false;
                UI_txt_Password.Enabled = false;
                return;
            }

            ShowStoredUser();
            LoadWindowsCredentials();
        }

        /// <summary>
        /// Shows who commits are attributed to **and which config file says
        /// so**. Without the origin, "still the old user" looks like the tool
        /// failing, when it is usually the machine's global .gitconfig holding
        /// a name this dialog was only ever changing per-repository.
        /// </summary>
        private void ShowIdentity()
        {
            var name = GitCredentials.CurrentName(_repo.Path);
            var email = GitCredentials.CurrentEmail(_repo.Path);
            var origin = GitCredentials.IdentityOrigin(_repo.Path);

            UI_lbl_Current.Text = name.Length > 0 || email.Length > 0
                ? name + (email.Length > 0 ? "  <" + email + ">" : string.Empty) +
                  (origin.Length > 0 ? "   (from " + origin + ")" : string.Empty)
                : "(user.name and user.email are not set)";

            // Prefilled so the fields show what is being replaced, not a blank
            // that silently means "leave it alone".
            UI_txt_Name.Text = name;
            UI_txt_Email.Text = email;

            UI_lbl_IdentityScope.Text = origin.StartsWith("global", StringComparison.OrdinalIgnoreCase)
                ? "The current name comes from global config: a repository-only change fixes " +
                  "this clone and leaves the others."
                : "Set for the selected scope.";
        }

        /// <summary>
        /// Names the account git would authenticate as. Without this the dialog
        /// only said *where* a credential applied, never *who* it was - so
        /// there was no way to tell whether a sign-in had taken effect.
        /// </summary>
        private void ShowStoredUser()
        {
            if (_target == null) return;

            var user = GitCredentials.StoredUser(_repo.Path, _target);

            UI_lbl_Stored.Text = user.Length > 0
                ? user + "   at " + _target
                : "(nothing stored for " + _target + ")";
            UI_lbl_Stored.ForeColor = user.Length > 0 ? Color.Black : Color.DimGray;
        }

        /// <summary>
        /// Lists what Windows Credential Manager actually holds for this host.
        ///
        /// This is the answer to "I cleared it and it is still there":
        /// <c>git credential reject</c> only reaches the helper git is
        /// configured with. Entries written by something else - Visual Studio's
        /// GitHub extension keeps its own - survive it and stay visible under
        /// Windows Credentials.
        /// </summary>
        private void LoadWindowsCredentials()
        {
            UI_lst_Windows.Items.Clear();
            if (_target == null) return;

            _windows = WindowsCredentials.ForHost(_target.Host);

            foreach (var c in _windows) UI_lst_Windows.Items.Add(c);

            if (_windows.Count == 0)
            {
                UI_lst_Windows.Items.Add("(nothing stored for " + _target.Host + ")");
                UI_lst_Windows.Enabled = false;
                return;
            }

            UI_lst_Windows.Enabled = true;
            for (var i = 0; i < _windows.Count; i++) UI_lst_Windows.SetSelected(i, true);
        }

        private void credentials_Changed(object sender, EventArgs e) { UpdateEnabled(); }

        private void UI_chk_Identity_CheckedChanged(object sender, EventArgs e)
        {
            UI_txt_Name.Enabled = UI_chk_Identity.Checked;
            UI_txt_Email.Enabled = UI_chk_Identity.Checked;
            UI_cmb_Scope.Enabled = UI_chk_Identity.Checked;
            UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            var haveCredentials = _target != null && !_target.IsSsh &&
                                  UI_txt_User.Text.Trim().Length > 0 && UI_txt_Password.Text.Length > 0;

            // The identity half can be used on its own - changing who commits
            // are attributed to does not require signing in again.
            var haveIdentity = UI_chk_Identity.Checked &&
                               (UI_txt_Name.Text.Trim().Length > 0 || UI_txt_Email.Text.Trim().Length > 0);

            UI_cmd_SignIn.Enabled = haveCredentials || haveIdentity;
            UI_cmd_SignIn.Text = haveCredentials ? "&Sign in" : "&Apply";
        }

        private void UI_cmd_Clear_Click(object sender, EventArgs e)
        {
            if (_target == null) return;

            var selected = UI_lst_Windows.SelectedItems.OfType<StoredCredential>().ToList();

            var question = new StringBuilder();
            question.AppendLine("Forget the stored login for " + _target + "?");
            question.AppendLine();
            question.AppendLine("  git credential reject   (the helper git is configured with)");

            if (selected.Count > 0)
            {
                question.AppendLine();
                question.AppendLine("and delete these entries from Windows Credential Manager:");
                foreach (var c in selected) question.AppendLine("  " + c.Target);

                // Deleting someone else's entry has a consequence worth saying
                // out loud before it happens, not afterwards.
                if (selected.Any(c => !c.WrittenByGit))
                {
                    question.AppendLine();
                    question.AppendLine("Some of those were written by Visual Studio's GitHub extension, " +
                                        "not by git. Deleting them signs Visual Studio out of GitHub too - " +
                                        "which is usually the point when switching user.");
                }
            }

            if (UI_chk_ForgetIdentity.Checked)
            {
                question.AppendLine();
                question.AppendLine("and forget the commit identity, in " +
                                    GitCredentials.IdentityOrigin(_repo.Path) + ":");
                question.AppendLine("  " + GitCredentials.CurrentName(_repo.Path) +
                                    "  <" + GitCredentials.CurrentEmail(_repo.Path) + ">");
                question.AppendLine();
                question.AppendLine("git then refuses to commit until someone sets a name and email. " +
                                    "That is the point: the next person cannot commit under the last " +
                                    "person's name by accident.");
            }

            question.AppendLine();
            question.AppendLine("The next fetch or push asks for credentials again. No work is lost.");

            if (MessageBox.Show(question.ToString(), "Switch Git User",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            var report = new List<string>();

            var result = GitCredentials.Clear(_repo.Path, _target);
            report.Add(result.Ok
                ? "git helper cleared"
                : "git helper: " + result.Output);

            var deleted = 0;
            var failed = 0;
            foreach (var c in selected)
            {
                string error;
                if (WindowsCredentials.Delete(c, out error)) deleted++;
                else { failed++; report.Add(c.Target + ": " + error); }
            }

            if (selected.Count > 0) report.Insert(1, deleted + " Windows entr(ies) deleted");

            var identityFailed = false;
            if (UI_chk_ForgetIdentity.Checked)
            {
                GitResult forget;
                var scope = GitCredentials.ForgetIdentity(_repo.Path, out forget);

                identityFailed = !forget.Ok;
                report.Add(forget.Ok
                    ? "commit identity cleared (" + scope + ")"
                    : "commit identity: " + forget.Output);
            }

            ShowStoredUser();
            ShowIdentity();
            LoadWindowsCredentials();
            SetStatus(string.Join("; ", report), !result.Ok || failed > 0 || identityFailed);
        }

        private void UI_cmd_SignIn_Click(object sender, EventArgs e)
        {
            var didSomething = false;

            if (UI_chk_Identity.Checked)
            {
                if (!ApplyIdentity()) return;
                didSomething = true;
            }

            var user = UI_txt_User.Text.Trim();
            var password = UI_txt_Password.Text;

            if (_target != null && !_target.IsSsh && user.Length > 0 && password.Length > 0)
            {
                // Replace rather than add: a helper asked to approve a second
                // credential for the same host can otherwise keep handing back
                // the old one.
                GitCredentials.Clear(_repo.Path, _target);

                var stored = GitCredentials.Store(_repo.Path, _target, user, password);

                // Whatever happened, the password does not stay on screen or in
                // this control's buffer.
                UI_txt_Password.Clear();

                if (!stored.Ok)
                {
                    SetStatus("Could not store the credential: " + stored.Output, true);
                    return;
                }

                didSomething = true;

                // Read back what the helper now hands out, rather than
                // assuming the approve took: if some other helper earlier in
                // the list still answers first, this is where it shows.
                ShowStoredUser();
                LoadWindowsCredentials();

                var nowStored = GitCredentials.StoredUser(_repo.Path, _target);
                var message = new StringBuilder();

                if (nowStored.Length > 0 &&
                    !string.Equals(nowStored, user, StringComparison.OrdinalIgnoreCase))
                {
                    message.Append("Stored, but git still answers with '").Append(nowStored)
                           .Append("' - another credential helper earlier in the list is winning. ")
                           .Append("Check 'git config --show-origin --get-all credential.helper'.");
                    SetStatus(message.ToString(), true);
                    return;
                }

                message.Append("Signed in to ").Append(_target).Append(" as '").Append(user)
                       .Append("'. The next fetch or push will use it.");

                // The commonest surprise: the login changed, the name on the
                // commits did not.
                var identityName = GitCredentials.CurrentName(_repo.Path);
                if (UI_chk_Identity.Checked) message.Append("  Commit identity updated.");
                else if (identityName.Length > 0)
                    message.Append("  Commits are still attributed to ").Append(identityName)
                           .Append(" - tick the box to change that too.");

                SetStatus(message.ToString(), false);
            }
            else if (didSomething)
            {
                SetStatus("Commit identity updated for " + _repo.Name + ".", false);
            }
        }

        /// <summary>
        /// Writes user.name and user.email at the chosen scope.
        ///
        /// Repository-local is the default because it cannot surprise anyone
        /// else, but global has to be offered: that is where the identity
        /// actually lives on these machines, so a local-only change leaves
        /// every other clone on the PC committing as the previous user. The
        /// global case is confirmed first, since it changes every repository.
        /// </summary>
        private bool ApplyIdentity()
        {
            var name = UI_txt_Name.Text.Trim();
            var email = UI_txt_Email.Text.Trim();
            var global = (UI_cmb_Scope.SelectedItem as string) == ScopeGlobal;

            if (global)
            {
                var confirm = MessageBox.Show(
                    "Set user.name and user.email for EVERY repository on this machine?\r\n\r\n" +
                    "  " + name + (email.Length > 0 ? "  <" + email + ">" : string.Empty) + "\r\n\r\n" +
                    "This writes the global .gitconfig. Repositories with their own user.name " +
                    "keep it - a local setting still wins.",
                    "Switch Git User", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (confirm != DialogResult.Yes) return false;
            }

            var scope = global ? "--global" : "--local";

            if (name.Length > 0)
            {
                var r = Git.Run(_repo.Path, "config", scope, "user.name", name);
                if (!r.Ok) { SetStatus("git config user.name failed: " + r.Output, true); return false; }
            }
            if (email.Length > 0)
            {
                var r = Git.Run(_repo.Path, "config", scope, "user.email", email);
                if (!r.Ok) { SetStatus("git config user.email failed: " + r.Output, true); return false; }
            }

            ShowIdentity();
            return true;
        }

        private void SetStatus(string text, bool isProblem)
        {
            UI_lbl_Status.Text = text;
            UI_lbl_Status.ForeColor = isProblem ? Color.Firebrick : Color.ForestGreen;
        }
    }
}
