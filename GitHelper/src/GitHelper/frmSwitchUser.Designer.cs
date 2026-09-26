namespace GitHelper
{
    partial class frmSwitchUser
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label UI_lbl_RepoCaption;
        private System.Windows.Forms.Label UI_lbl_Repo;
        private System.Windows.Forms.Label UI_lbl_RemoteCaption;
        private System.Windows.Forms.Label UI_lbl_Remote;
        private System.Windows.Forms.Label UI_lbl_CurrentCaption;
        private System.Windows.Forms.Label UI_lbl_Current;
        private System.Windows.Forms.Label UI_lbl_StoredCaption;
        private System.Windows.Forms.Label UI_lbl_Stored;
        private System.Windows.Forms.Button UI_cmd_Clear;
        private System.Windows.Forms.Label UI_lbl_WindowsCaption;
        private System.Windows.Forms.ListBox UI_lst_Windows;
        private System.Windows.Forms.CheckBox UI_chk_ForgetIdentity;
        private System.Windows.Forms.Label UI_lbl_SignInCaption;
        private System.Windows.Forms.Label UI_lbl_UserCaption;
        private System.Windows.Forms.TextBox UI_txt_User;
        private System.Windows.Forms.Label UI_lbl_PasswordCaption;
        private System.Windows.Forms.TextBox UI_txt_Password;
        private System.Windows.Forms.Label UI_lbl_PasswordHelp;
        private System.Windows.Forms.CheckBox UI_chk_Identity;
        private System.Windows.Forms.Label UI_lbl_NameCaption;
        private System.Windows.Forms.TextBox UI_txt_Name;
        private System.Windows.Forms.Label UI_lbl_EmailCaption;
        private System.Windows.Forms.TextBox UI_txt_Email;
        private System.Windows.Forms.Label UI_lbl_IdentityScope;
        private System.Windows.Forms.Label UI_lbl_ScopeCaption;
        private System.Windows.Forms.ComboBox UI_cmb_Scope;
        private System.Windows.Forms.Label UI_lbl_Status;
        private System.Windows.Forms.Button UI_cmd_SignIn;
        private System.Windows.Forms.Button UI_cmd_Close;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.UI_lbl_RepoCaption = new System.Windows.Forms.Label();
            this.UI_lbl_Repo = new System.Windows.Forms.Label();
            this.UI_lbl_RemoteCaption = new System.Windows.Forms.Label();
            this.UI_lbl_Remote = new System.Windows.Forms.Label();
            this.UI_lbl_CurrentCaption = new System.Windows.Forms.Label();
            this.UI_lbl_Current = new System.Windows.Forms.Label();
            this.UI_lbl_StoredCaption = new System.Windows.Forms.Label();
            this.UI_lbl_Stored = new System.Windows.Forms.Label();
            this.UI_cmd_Clear = new System.Windows.Forms.Button();
            this.UI_lbl_WindowsCaption = new System.Windows.Forms.Label();
            this.UI_lst_Windows = new System.Windows.Forms.ListBox();
            this.UI_chk_ForgetIdentity = new System.Windows.Forms.CheckBox();
            this.UI_lbl_SignInCaption = new System.Windows.Forms.Label();
            this.UI_lbl_UserCaption = new System.Windows.Forms.Label();
            this.UI_txt_User = new System.Windows.Forms.TextBox();
            this.UI_lbl_PasswordCaption = new System.Windows.Forms.Label();
            this.UI_txt_Password = new System.Windows.Forms.TextBox();
            this.UI_lbl_PasswordHelp = new System.Windows.Forms.Label();
            this.UI_chk_Identity = new System.Windows.Forms.CheckBox();
            this.UI_lbl_NameCaption = new System.Windows.Forms.Label();
            this.UI_txt_Name = new System.Windows.Forms.TextBox();
            this.UI_lbl_EmailCaption = new System.Windows.Forms.Label();
            this.UI_txt_Email = new System.Windows.Forms.TextBox();
            this.UI_lbl_IdentityScope = new System.Windows.Forms.Label();
            this.UI_lbl_ScopeCaption = new System.Windows.Forms.Label();
            this.UI_cmb_Scope = new System.Windows.Forms.ComboBox();
            this.UI_lbl_Status = new System.Windows.Forms.Label();
            this.UI_cmd_SignIn = new System.Windows.Forms.Button();
            this.UI_cmd_Close = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // UI_lbl_RepoCaption
            //
            this.UI_lbl_RepoCaption.Location = new System.Drawing.Point(12, 15);
            this.UI_lbl_RepoCaption.Name = "UI_lbl_RepoCaption";
            this.UI_lbl_RepoCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_RepoCaption.TabIndex = 0;
            this.UI_lbl_RepoCaption.Text = "Repository";
            //
            // UI_lbl_Repo
            //
            this.UI_lbl_Repo.Location = new System.Drawing.Point(136, 15);
            this.UI_lbl_Repo.Name = "UI_lbl_Repo";
            this.UI_lbl_Repo.Size = new System.Drawing.Size(440, 20);
            this.UI_lbl_Repo.TabIndex = 1;
            //
            // UI_lbl_RemoteCaption
            //
            this.UI_lbl_RemoteCaption.Location = new System.Drawing.Point(12, 41);
            this.UI_lbl_RemoteCaption.Name = "UI_lbl_RemoteCaption";
            this.UI_lbl_RemoteCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_RemoteCaption.TabIndex = 2;
            this.UI_lbl_RemoteCaption.Text = "Signs in to";
            //
            // UI_lbl_Remote
            //
            this.UI_lbl_Remote.Location = new System.Drawing.Point(136, 41);
            this.UI_lbl_Remote.Name = "UI_lbl_Remote";
            this.UI_lbl_Remote.Size = new System.Drawing.Size(440, 20);
            this.UI_lbl_Remote.TabIndex = 3;
            //
            // UI_lbl_CurrentCaption
            //
            this.UI_lbl_CurrentCaption.Location = new System.Drawing.Point(12, 67);
            this.UI_lbl_CurrentCaption.Name = "UI_lbl_CurrentCaption";
            this.UI_lbl_CurrentCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_CurrentCaption.TabIndex = 4;
            this.UI_lbl_CurrentCaption.Text = "Commits as";
            //
            // UI_lbl_Current
            //
            this.UI_lbl_Current.Location = new System.Drawing.Point(136, 67);
            this.UI_lbl_Current.Name = "UI_lbl_Current";
            this.UI_lbl_Current.Size = new System.Drawing.Size(440, 20);
            this.UI_lbl_Current.TabIndex = 5;
            //
            // UI_lbl_StoredCaption
            //
            this.UI_lbl_StoredCaption.Location = new System.Drawing.Point(12, 93);
            this.UI_lbl_StoredCaption.Name = "UI_lbl_StoredCaption";
            this.UI_lbl_StoredCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_StoredCaption.TabIndex = 6;
            this.UI_lbl_StoredCaption.Text = "Stored login";
            //
            // UI_lbl_Stored
            //
            this.UI_lbl_Stored.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_Stored.Location = new System.Drawing.Point(136, 93);
            this.UI_lbl_Stored.Name = "UI_lbl_Stored";
            this.UI_lbl_Stored.Size = new System.Drawing.Size(300, 20);
            this.UI_lbl_Stored.TabIndex = 7;
            //
            // UI_cmd_Clear
            // forgets what the credential helper has for this host, so the next
            // fetch or push has to ask again
            //
            this.UI_cmd_Clear.Location = new System.Drawing.Point(440, 89);
            this.UI_cmd_Clear.Name = "UI_cmd_Clear";
            this.UI_cmd_Clear.Size = new System.Drawing.Size(136, 28);
            this.UI_cmd_Clear.TabIndex = 8;
            this.UI_cmd_Clear.Text = "C&lear credentials";
            this.UI_cmd_Clear.UseVisualStyleBackColor = true;
            this.UI_cmd_Clear.Click += new System.EventHandler(this.UI_cmd_Clear_Click);
            //
            // UI_lbl_WindowsCaption
            //
            this.UI_lbl_WindowsCaption.Location = new System.Drawing.Point(12, 126);
            this.UI_lbl_WindowsCaption.Name = "UI_lbl_WindowsCaption";
            this.UI_lbl_WindowsCaption.Size = new System.Drawing.Size(120, 34);
            this.UI_lbl_WindowsCaption.TabIndex = 24;
            this.UI_lbl_WindowsCaption.Text = "Windows Credential Manager";
            //
            // UI_lst_Windows
            // every entry in Windows Credential Manager that mentions this
            // host, including ones git never wrote - those are what survives a
            // 'git credential reject' and makes a clear look like it did nothing
            //
            this.UI_lst_Windows.IntegralHeight = false;
            this.UI_lst_Windows.Location = new System.Drawing.Point(136, 126);
            this.UI_lst_Windows.Name = "UI_lst_Windows";
            this.UI_lst_Windows.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.UI_lst_Windows.Size = new System.Drawing.Size(440, 86);
            this.UI_lst_Windows.TabIndex = 25;
            //
            // UI_chk_ForgetIdentity
            // clearing a login leaves user.name behind, so the next person's
            // commits carry the previous author's name unless this goes too
            //
            // Ticked by default: this dialog exists to hand the machine to
            // someone else, and leaving the previous author's name behind is
            // the mistake it is meant to prevent. The confirmation names the
            // config file it would be removed from before anything happens.
            this.UI_chk_ForgetIdentity.Checked = true;
            this.UI_chk_ForgetIdentity.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UI_chk_ForgetIdentity.Location = new System.Drawing.Point(136, 218);
            this.UI_chk_ForgetIdentity.Name = "UI_chk_ForgetIdentity";
            this.UI_chk_ForgetIdentity.Size = new System.Drawing.Size(440, 24);
            this.UI_chk_ForgetIdentity.TabIndex = 28;
            this.UI_chk_ForgetIdentity.Text = "Also forget the commit identity (user.name / user.email)";
            this.UI_chk_ForgetIdentity.UseVisualStyleBackColor = true;
            //
            // UI_lbl_SignInCaption
            //
            this.UI_lbl_SignInCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.UI_lbl_SignInCaption.Location = new System.Drawing.Point(12, 256);
            this.UI_lbl_SignInCaption.Name = "UI_lbl_SignInCaption";
            this.UI_lbl_SignInCaption.Size = new System.Drawing.Size(300, 20);
            this.UI_lbl_SignInCaption.TabIndex = 9;
            this.UI_lbl_SignInCaption.Text = "Sign in as someone else";
            //
            // UI_lbl_UserCaption
            //
            this.UI_lbl_UserCaption.Location = new System.Drawing.Point(12, 285);
            this.UI_lbl_UserCaption.Name = "UI_lbl_UserCaption";
            this.UI_lbl_UserCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_UserCaption.TabIndex = 10;
            this.UI_lbl_UserCaption.Text = "User name";
            //
            // UI_txt_User
            //
            this.UI_txt_User.Location = new System.Drawing.Point(136, 282);
            this.UI_txt_User.Name = "UI_txt_User";
            this.UI_txt_User.Size = new System.Drawing.Size(240, 24);
            this.UI_txt_User.TabIndex = 11;
            this.UI_txt_User.TextChanged += new System.EventHandler(this.credentials_Changed);
            //
            // UI_lbl_PasswordCaption
            //
            this.UI_lbl_PasswordCaption.Location = new System.Drawing.Point(12, 315);
            this.UI_lbl_PasswordCaption.Name = "UI_lbl_PasswordCaption";
            this.UI_lbl_PasswordCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_PasswordCaption.TabIndex = 12;
            this.UI_lbl_PasswordCaption.Text = "Password / token";
            //
            // UI_txt_Password
            //
            this.UI_txt_Password.Location = new System.Drawing.Point(136, 312);
            this.UI_txt_Password.Name = "UI_txt_Password";
            this.UI_txt_Password.Size = new System.Drawing.Size(240, 24);
            this.UI_txt_Password.TabIndex = 13;
            this.UI_txt_Password.UseSystemPasswordChar = true;
            this.UI_txt_Password.TextChanged += new System.EventHandler(this.credentials_Changed);
            //
            // UI_lbl_PasswordHelp
            //
            this.UI_lbl_PasswordHelp.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_PasswordHelp.Location = new System.Drawing.Point(386, 282);
            this.UI_lbl_PasswordHelp.Name = "UI_lbl_PasswordHelp";
            this.UI_lbl_PasswordHelp.Size = new System.Drawing.Size(190, 54);
            this.UI_lbl_PasswordHelp.TabIndex = 14;
            this.UI_lbl_PasswordHelp.Text = "Handed straight to git's credential helper. Nothing is stored by Git Helper.";
            //
            // UI_chk_Identity
            //
            this.UI_chk_Identity.Location = new System.Drawing.Point(136, 346);
            this.UI_chk_Identity.Name = "UI_chk_Identity";
            this.UI_chk_Identity.Size = new System.Drawing.Size(440, 24);
            this.UI_chk_Identity.TabIndex = 15;
            this.UI_chk_Identity.Text = "Also change who commits are attributed to";
            this.UI_chk_Identity.UseVisualStyleBackColor = true;
            this.UI_chk_Identity.CheckedChanged += new System.EventHandler(this.UI_chk_Identity_CheckedChanged);
            //
            // UI_lbl_NameCaption
            //
            this.UI_lbl_NameCaption.Location = new System.Drawing.Point(12, 379);
            this.UI_lbl_NameCaption.Name = "UI_lbl_NameCaption";
            this.UI_lbl_NameCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_NameCaption.TabIndex = 16;
            this.UI_lbl_NameCaption.Text = "user.name";
            //
            // UI_txt_Name
            //
            this.UI_txt_Name.Location = new System.Drawing.Point(136, 376);
            this.UI_txt_Name.Name = "UI_txt_Name";
            this.UI_txt_Name.Size = new System.Drawing.Size(240, 24);
            this.UI_txt_Name.TabIndex = 17;
            //
            // UI_lbl_EmailCaption
            //
            this.UI_lbl_EmailCaption.Location = new System.Drawing.Point(12, 409);
            this.UI_lbl_EmailCaption.Name = "UI_lbl_EmailCaption";
            this.UI_lbl_EmailCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_EmailCaption.TabIndex = 18;
            this.UI_lbl_EmailCaption.Text = "user.email";
            //
            // UI_txt_Email
            //
            this.UI_txt_Email.Location = new System.Drawing.Point(136, 406);
            this.UI_txt_Email.Name = "UI_txt_Email";
            this.UI_txt_Email.Size = new System.Drawing.Size(240, 24);
            this.UI_txt_Email.TabIndex = 19;
            //
            // UI_lbl_IdentityScope
            //
            this.UI_lbl_IdentityScope.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_IdentityScope.Location = new System.Drawing.Point(386, 376);
            this.UI_lbl_IdentityScope.Name = "UI_lbl_IdentityScope";
            this.UI_lbl_IdentityScope.Size = new System.Drawing.Size(190, 54);
            this.UI_lbl_IdentityScope.TabIndex = 20;
            this.UI_lbl_IdentityScope.Text = "Set for this repository only, not globally.";
            //
            // UI_lbl_ScopeCaption
            //
            this.UI_lbl_ScopeCaption.Location = new System.Drawing.Point(12, 439);
            this.UI_lbl_ScopeCaption.Name = "UI_lbl_ScopeCaption";
            this.UI_lbl_ScopeCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_ScopeCaption.TabIndex = 26;
            this.UI_lbl_ScopeCaption.Text = "Apply identity to";
            //
            // UI_cmb_Scope
            // the identity usually lives in global config, so a repository-only
            // change leaves every other working copy committing as the old user
            //
            this.UI_cmb_Scope.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UI_cmb_Scope.Location = new System.Drawing.Point(136, 436);
            this.UI_cmb_Scope.Name = "UI_cmb_Scope";
            this.UI_cmb_Scope.Size = new System.Drawing.Size(240, 24);
            this.UI_cmb_Scope.TabIndex = 27;
            //
            // UI_lbl_Status
            //
            this.UI_lbl_Status.Location = new System.Drawing.Point(12, 484);
            this.UI_lbl_Status.Name = "UI_lbl_Status";
            this.UI_lbl_Status.Size = new System.Drawing.Size(564, 40);
            this.UI_lbl_Status.TabIndex = 21;
            //
            // UI_cmd_SignIn
            //
            this.UI_cmd_SignIn.Location = new System.Drawing.Point(136, 532);
            this.UI_cmd_SignIn.Name = "UI_cmd_SignIn";
            this.UI_cmd_SignIn.Size = new System.Drawing.Size(120, 30);
            this.UI_cmd_SignIn.TabIndex = 22;
            this.UI_cmd_SignIn.Text = "&Sign in";
            this.UI_cmd_SignIn.UseVisualStyleBackColor = true;
            this.UI_cmd_SignIn.Click += new System.EventHandler(this.UI_cmd_SignIn_Click);
            //
            // UI_cmd_Close
            //
            this.UI_cmd_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.UI_cmd_Close.Location = new System.Drawing.Point(264, 532);
            this.UI_cmd_Close.Name = "UI_cmd_Close";
            this.UI_cmd_Close.Size = new System.Drawing.Size(90, 30);
            this.UI_cmd_Close.TabIndex = 23;
            this.UI_cmd_Close.Text = "Close";
            this.UI_cmd_Close.UseVisualStyleBackColor = true;
            //
            // frmSwitchUser
            //
            this.CancelButton = this.UI_cmd_Close;
            this.ClientSize = new System.Drawing.Size(588, 576);
            this.Controls.Add(this.UI_lbl_RepoCaption);
            this.Controls.Add(this.UI_lbl_Repo);
            this.Controls.Add(this.UI_lbl_RemoteCaption);
            this.Controls.Add(this.UI_lbl_Remote);
            this.Controls.Add(this.UI_lbl_CurrentCaption);
            this.Controls.Add(this.UI_lbl_Current);
            this.Controls.Add(this.UI_lbl_StoredCaption);
            this.Controls.Add(this.UI_lbl_Stored);
            this.Controls.Add(this.UI_cmd_Clear);
            this.Controls.Add(this.UI_lbl_WindowsCaption);
            this.Controls.Add(this.UI_lst_Windows);
            this.Controls.Add(this.UI_chk_ForgetIdentity);
            this.Controls.Add(this.UI_lbl_SignInCaption);
            this.Controls.Add(this.UI_lbl_UserCaption);
            this.Controls.Add(this.UI_txt_User);
            this.Controls.Add(this.UI_lbl_PasswordCaption);
            this.Controls.Add(this.UI_txt_Password);
            this.Controls.Add(this.UI_lbl_PasswordHelp);
            this.Controls.Add(this.UI_chk_Identity);
            this.Controls.Add(this.UI_lbl_NameCaption);
            this.Controls.Add(this.UI_txt_Name);
            this.Controls.Add(this.UI_lbl_EmailCaption);
            this.Controls.Add(this.UI_txt_Email);
            this.Controls.Add(this.UI_lbl_IdentityScope);
            this.Controls.Add(this.UI_lbl_ScopeCaption);
            this.Controls.Add(this.UI_cmb_Scope);
            this.Controls.Add(this.UI_lbl_Status);
            this.Controls.Add(this.UI_cmd_SignIn);
            this.Controls.Add(this.UI_cmd_Close);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSwitchUser";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Switch Git User";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
