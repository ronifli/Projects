namespace GitHelper
{
    partial class frmMain
    {
        private System.ComponentModel.IContainer components = null;

        // --- controls, in the order they appear on the form ----------------
        private System.Windows.Forms.MenuStrip UI_pnl_MainMenu;
        private System.Windows.Forms.ToolStrip UI_pnl_Toolbar;
        private System.Windows.Forms.ToolStripButton UI_cmd_ShowLog;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_Git;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_SwitchBranch;
        private System.Windows.Forms.ToolStripSeparator UI_mnu_GitSeparator;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_ResetLocal;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_Tools;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_CheckMerge;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_ShowLog;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_SwitchUser;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_CopilotSetup;
        private System.Windows.Forms.ToolStripSeparator UI_mnu_ToolsSeparator2;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_InstallPlugin;
        private System.Windows.Forms.ToolStripSeparator UI_mnu_ToolsSeparator;
        private System.Windows.Forms.ToolStripMenuItem UI_mnu_ShellMenu;
        private System.Windows.Forms.Label UI_lbl_Repo;
        private System.Windows.Forms.ComboBox UI_cmb_Repo;
        private System.Windows.Forms.Button UI_cmd_AddRepo;
        private System.Windows.Forms.Label UI_lbl_RepoPath;
        private System.Windows.Forms.Label UI_lbl_Type;
        private System.Windows.Forms.ComboBox UI_cmb_Type;
        private System.Windows.Forms.Label UI_lbl_Risk;
        private System.Windows.Forms.ComboBox UI_cmb_Risk;
        private System.Windows.Forms.Label UI_lbl_RiskHelp;
        private System.Windows.Forms.Label UI_lbl_Area;
        private System.Windows.Forms.FlowLayoutPanel UI_panel_Area;
        private System.Windows.Forms.CheckBox UI_chk_AreaAOI;
        private System.Windows.Forms.CheckBox UI_chk_AreaAVI;
        private System.Windows.Forms.CheckBox UI_chk_AreaCDB;
        private System.Windows.Forms.CheckBox UI_chk_AreaSpark;
        private System.Windows.Forms.CheckBox UI_chk_AreaAlgo;
        private System.Windows.Forms.CheckBox UI_chk_AreaMachine;
        private System.Windows.Forms.CheckBox UI_chk_AreaSprint;
        private System.Windows.Forms.CheckBox UI_chk_AreaOrcal;
        private System.Windows.Forms.CheckBox UI_chk_AreaOther;
        private System.Windows.Forms.Label UI_lbl_AreaHelp;
        private System.Windows.Forms.Label UI_lbl_TT;
        private System.Windows.Forms.TextBox UI_txt_TT;
        private System.Windows.Forms.Label UI_lbl_TTHelp;
        private System.Windows.Forms.Label UI_lbl_Customer;
        private System.Windows.Forms.TextBox UI_txt_Customer;
        private System.Windows.Forms.Label UI_lbl_CustomerHelp;
        private System.Windows.Forms.CheckBox UI_chk_OtherVersions;
        private System.Windows.Forms.Label UI_lbl_Subject;
        private System.Windows.Forms.TextBox UI_txt_Subject;
        private System.Windows.Forms.Label UI_lbl_Count;
        private System.Windows.Forms.Label UI_lbl_Body;
        private GitHelper.LineLimitedTextBox UI_txt_Body;
        private System.Windows.Forms.Button UI_cmd_FromVs;
        private System.Windows.Forms.Button UI_cmd_FromClip;
        private System.Windows.Forms.Button UI_cmd_LastCommit;
        private System.Windows.Forms.Button UI_cmd_ClearBody;
        private System.Windows.Forms.Button UI_cmd_AI;
        private System.Windows.Forms.ProgressBar UI_prg_AI;
        private System.Windows.Forms.Label UI_lbl_AIStatus;
        private System.Windows.Forms.Label UI_lbl_PreviewCaption;
        private System.Windows.Forms.TextBox UI_txt_Preview;
        private System.Windows.Forms.CheckBox UI_chk_StageAll;
        private System.Windows.Forms.CheckBox UI_chk_PullRequest;
        private System.Windows.Forms.Label UI_lbl_Staged;
        private System.Windows.Forms.Button UI_cmd_SoftReset;
        private System.Windows.Forms.Label UI_lbl_SoftReset;
        private System.Windows.Forms.Button UI_cmd_Copy;
        private System.Windows.Forms.Button UI_cmd_Commit;
        private System.Windows.Forms.Button UI_cmd_CommitPush;
        private System.Windows.Forms.Button UI_cmd_Cancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Designer-generated layout. The rows step down in 34px increments
        /// from y=12, labels at x=12, inputs at x=136, help text at x=326 -
        /// adding a field means following that pattern and bumping the form
        /// height by ~34px.
        ///
        /// The button rows and the text box widths are adjusted again at
        /// runtime in <see cref="UpdateButtonRow"/>: captions grow under DPI
        /// scaling and would otherwise push the outer buttons off the form.
        /// </summary>
        private void InitializeComponent()
        {
            this.UI_pnl_MainMenu = new System.Windows.Forms.MenuStrip();
            this.UI_pnl_Toolbar = new System.Windows.Forms.ToolStrip();
            this.UI_cmd_ShowLog = new System.Windows.Forms.ToolStripButton();
            this.UI_mnu_Git = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_mnu_SwitchBranch = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_mnu_GitSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.UI_mnu_ResetLocal = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_mnu_Tools = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_mnu_CheckMerge = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_mnu_ShowLog = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_mnu_SwitchUser = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_mnu_CopilotSetup = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_mnu_ToolsSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.UI_mnu_InstallPlugin = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_mnu_ToolsSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.UI_mnu_ShellMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.UI_lbl_Repo = new System.Windows.Forms.Label();
            this.UI_cmb_Repo = new System.Windows.Forms.ComboBox();
            this.UI_cmd_AddRepo = new System.Windows.Forms.Button();
            this.UI_lbl_RepoPath = new System.Windows.Forms.Label();
            this.UI_lbl_Type = new System.Windows.Forms.Label();
            this.UI_cmb_Type = new System.Windows.Forms.ComboBox();
            this.UI_lbl_Risk = new System.Windows.Forms.Label();
            this.UI_cmb_Risk = new System.Windows.Forms.ComboBox();
            this.UI_lbl_RiskHelp = new System.Windows.Forms.Label();
            this.UI_lbl_Area = new System.Windows.Forms.Label();
            this.UI_panel_Area = new System.Windows.Forms.FlowLayoutPanel();
            this.UI_chk_AreaAOI = new System.Windows.Forms.CheckBox();
            this.UI_chk_AreaAVI = new System.Windows.Forms.CheckBox();
            this.UI_chk_AreaCDB = new System.Windows.Forms.CheckBox();
            this.UI_chk_AreaSpark = new System.Windows.Forms.CheckBox();
            this.UI_chk_AreaAlgo = new System.Windows.Forms.CheckBox();
            this.UI_chk_AreaMachine = new System.Windows.Forms.CheckBox();
            this.UI_chk_AreaSprint = new System.Windows.Forms.CheckBox();
            this.UI_chk_AreaOrcal = new System.Windows.Forms.CheckBox();
            this.UI_chk_AreaOther = new System.Windows.Forms.CheckBox();
            this.UI_lbl_AreaHelp = new System.Windows.Forms.Label();
            this.UI_lbl_TT = new System.Windows.Forms.Label();
            this.UI_txt_TT = new System.Windows.Forms.TextBox();
            this.UI_lbl_TTHelp = new System.Windows.Forms.Label();
            this.UI_lbl_Customer = new System.Windows.Forms.Label();
            this.UI_txt_Customer = new System.Windows.Forms.TextBox();
            this.UI_lbl_CustomerHelp = new System.Windows.Forms.Label();
            this.UI_chk_OtherVersions = new System.Windows.Forms.CheckBox();
            this.UI_lbl_Subject = new System.Windows.Forms.Label();
            this.UI_txt_Subject = new System.Windows.Forms.TextBox();
            this.UI_lbl_Count = new System.Windows.Forms.Label();
            this.UI_lbl_Body = new System.Windows.Forms.Label();
            this.UI_txt_Body = new GitHelper.LineLimitedTextBox();
            this.UI_cmd_FromVs = new System.Windows.Forms.Button();
            this.UI_cmd_FromClip = new System.Windows.Forms.Button();
            this.UI_cmd_LastCommit = new System.Windows.Forms.Button();
            this.UI_cmd_ClearBody = new System.Windows.Forms.Button();
            this.UI_cmd_AI = new System.Windows.Forms.Button();
            this.UI_prg_AI = new System.Windows.Forms.ProgressBar();
            this.UI_lbl_AIStatus = new System.Windows.Forms.Label();
            this.UI_lbl_PreviewCaption = new System.Windows.Forms.Label();
            this.UI_txt_Preview = new System.Windows.Forms.TextBox();
            this.UI_chk_StageAll = new System.Windows.Forms.CheckBox();
            this.UI_chk_PullRequest = new System.Windows.Forms.CheckBox();
            this.UI_lbl_Staged = new System.Windows.Forms.Label();
            this.UI_cmd_SoftReset = new System.Windows.Forms.Button();
            this.UI_lbl_SoftReset = new System.Windows.Forms.Label();
            this.UI_cmd_Copy = new System.Windows.Forms.Button();
            this.UI_cmd_Commit = new System.Windows.Forms.Button();
            this.UI_cmd_CommitPush = new System.Windows.Forms.Button();
            this.UI_cmd_Cancel = new System.Windows.Forms.Button();
            this.UI_pnl_MainMenu.SuspendLayout();
            this.UI_pnl_Toolbar.SuspendLayout();
            this.SuspendLayout();
            //
            // UI_pnl_MainMenu
            //
            this.UI_pnl_MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.UI_mnu_Git,
                this.UI_mnu_Tools });
            this.UI_pnl_MainMenu.Location = new System.Drawing.Point(0, 0);
            this.UI_pnl_MainMenu.Name = "UI_pnl_MainMenu";
            this.UI_pnl_MainMenu.Size = new System.Drawing.Size(844, 28);
            this.UI_pnl_MainMenu.TabIndex = 0;
            //
            // UI_pnl_Toolbar
            // under the menu; every control below was moved down by its 25px
            // height, like the menu before it (controls are placed absolutely)
            //
            this.UI_pnl_Toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.UI_pnl_Toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.UI_cmd_ShowLog });
            this.UI_pnl_Toolbar.Location = new System.Drawing.Point(0, 28);
            this.UI_pnl_Toolbar.Name = "UI_pnl_Toolbar";
            this.UI_pnl_Toolbar.Size = new System.Drawing.Size(844, 25);
            this.UI_pnl_Toolbar.TabIndex = 1;
            //
            // UI_cmd_ShowLog
            // same as Git > Show Log; the image is drawn in code (ToolbarIcons)
            //
            this.UI_cmd_ShowLog.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            this.UI_cmd_ShowLog.Name = "UI_cmd_ShowLog";
            this.UI_cmd_ShowLog.Size = new System.Drawing.Size(84, 22);
            this.UI_cmd_ShowLog.Text = "Show Log";
            this.UI_cmd_ShowLog.ToolTipText = "Commit history of the selected repository";
            this.UI_cmd_ShowLog.Click += new System.EventHandler(this.UI_mnu_ShowLog_Click);
            //
            // UI_mnu_Git
            // things done to the selected repository
            //
            this.UI_mnu_Git.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.UI_mnu_SwitchBranch,
                this.UI_mnu_ShowLog,
                this.UI_mnu_GitSeparator,
                this.UI_mnu_ResetLocal });
            this.UI_mnu_Git.Name = "UI_mnu_Git";
            this.UI_mnu_Git.Size = new System.Drawing.Size(42, 24);
            this.UI_mnu_Git.Text = "&Git";
            //
            // UI_mnu_SwitchBranch
            //
            this.UI_mnu_SwitchBranch.Name = "UI_mnu_SwitchBranch";
            this.UI_mnu_SwitchBranch.Size = new System.Drawing.Size(240, 26);
            this.UI_mnu_SwitchBranch.Text = "&Switch...";
            this.UI_mnu_SwitchBranch.ToolTipText = "Check out a branch from origin";
            this.UI_mnu_SwitchBranch.Click += new System.EventHandler(this.UI_mnu_SwitchBranch_Click);
            //
            // UI_mnu_GitSeparator
            //
            this.UI_mnu_GitSeparator.Name = "UI_mnu_GitSeparator";
            this.UI_mnu_GitSeparator.Size = new System.Drawing.Size(237, 6);
            //
            // UI_mnu_ResetLocal
            // throws away local changes and pulls - destructive, hence the
            // ellipsis: it always asks first
            //
            this.UI_mnu_ResetLocal.Name = "UI_mnu_ResetLocal";
            this.UI_mnu_ResetLocal.Size = new System.Drawing.Size(240, 26);
            this.UI_mnu_ResetLocal.Text = "&Reset Local...";
            this.UI_mnu_ResetLocal.ToolTipText = "Discard local changes and pull the latest from origin";
            this.UI_mnu_ResetLocal.Click += new System.EventHandler(this.UI_mnu_ResetLocal_Click);
            //
            // UI_mnu_Tools
            // things done to this machine rather than to a repository
            //
            this.UI_mnu_Tools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.UI_mnu_CheckMerge,
                this.UI_mnu_SwitchUser,
                this.UI_mnu_ToolsSeparator2,
                this.UI_mnu_InstallPlugin,
                this.UI_mnu_CopilotSetup,
                this.UI_mnu_ToolsSeparator,
                this.UI_mnu_ShellMenu });
            this.UI_mnu_Tools.Name = "UI_mnu_Tools";
            this.UI_mnu_Tools.Size = new System.Drawing.Size(54, 24);
            this.UI_mnu_Tools.Text = "&Tools";
            this.UI_mnu_Tools.DropDownOpening += new System.EventHandler(this.UI_mnu_Tools_DropDownOpening);
            //
            // UI_mnu_CheckMerge
            // does every change on that branch also exist on this one
            //
            this.UI_mnu_CheckMerge.Name = "UI_mnu_CheckMerge";
            this.UI_mnu_CheckMerge.Size = new System.Drawing.Size(240, 26);
            this.UI_mnu_CheckMerge.Text = "Check &Merge...";
            this.UI_mnu_CheckMerge.ToolTipText = "Find changes on another branch that are not on this one";
            this.UI_mnu_CheckMerge.Click += new System.EventHandler(this.UI_mnu_CheckMerge_Click);
            //
            // UI_mnu_ShowLog
            // history of the checked-out branch, structured fields as columns
            //
            this.UI_mnu_ShowLog.Name = "UI_mnu_ShowLog";
            this.UI_mnu_ShowLog.Size = new System.Drawing.Size(240, 26);
            this.UI_mnu_ShowLog.Text = "Show &Log...";
            this.UI_mnu_ShowLog.ToolTipText = "Commit history of the current branch";
            this.UI_mnu_ShowLog.Click += new System.EventHandler(this.UI_mnu_ShowLog_Click);
            //
            // UI_mnu_CopilotSetup
            // install GitHub Copilot CLI and sign in, for the AI button
            //
            this.UI_mnu_CopilotSetup.Name = "UI_mnu_CopilotSetup";
            this.UI_mnu_CopilotSetup.Size = new System.Drawing.Size(240, 26);
            this.UI_mnu_CopilotSetup.Text = "Set up Co&pilot (AI)...";
            this.UI_mnu_CopilotSetup.ToolTipText = "Install GitHub Copilot CLI and sign in, for the AI button";
            this.UI_mnu_CopilotSetup.Click += new System.EventHandler(this.UI_mnu_CopilotSetup_Click);
            //
            // UI_mnu_SwitchUser
            // clears the stored login for the remote and takes a new one
            //
            this.UI_mnu_SwitchUser.Name = "UI_mnu_SwitchUser";
            this.UI_mnu_SwitchUser.Size = new System.Drawing.Size(240, 26);
            this.UI_mnu_SwitchUser.Text = "Switch Git &User...";
            this.UI_mnu_SwitchUser.ToolTipText = "Clear the stored credentials and sign in as someone else";
            this.UI_mnu_SwitchUser.Click += new System.EventHandler(this.UI_mnu_SwitchUser_Click);
            //
            // UI_mnu_ToolsSeparator2
            //
            this.UI_mnu_ToolsSeparator2.Name = "UI_mnu_ToolsSeparator2";
            this.UI_mnu_ToolsSeparator2.Size = new System.Drawing.Size(237, 6);
            //
            // UI_mnu_InstallPlugin
            //
            this.UI_mnu_InstallPlugin.Name = "UI_mnu_InstallPlugin";
            this.UI_mnu_InstallPlugin.Size = new System.Drawing.Size(240, 26);
            this.UI_mnu_InstallPlugin.Text = "&Install VS Plugin...";
            this.UI_mnu_InstallPlugin.Click += new System.EventHandler(this.UI_mnu_InstallPlugin_Click);
            //
            // UI_mnu_ToolsSeparator
            //
            this.UI_mnu_ToolsSeparator.Name = "UI_mnu_ToolsSeparator";
            this.UI_mnu_ToolsSeparator.Size = new System.Drawing.Size(237, 6);
            //
            // UI_mnu_ShellMenu
            // a checked item: ticked when the Explorer entry is registered,
            // and clicking it toggles
            //
            this.UI_mnu_ShellMenu.CheckOnClick = false;
            this.UI_mnu_ShellMenu.Name = "UI_mnu_ShellMenu";
            this.UI_mnu_ShellMenu.Size = new System.Drawing.Size(240, 26);
            this.UI_mnu_ShellMenu.Text = "Add Git Helper to &Context Menu";
            this.UI_mnu_ShellMenu.ToolTipText = "Right-click a folder in Explorer to open Git Helper there";
            this.UI_mnu_ShellMenu.Click += new System.EventHandler(this.UI_mnu_ShellMenu_Click);
            //
            // UI_lbl_Repo
            //
            this.UI_lbl_Repo.Location = new System.Drawing.Point(12, 68);
            this.UI_lbl_Repo.Name = "UI_lbl_Repo";
            this.UI_lbl_Repo.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Repo.TabIndex = 0;
            this.UI_lbl_Repo.Text = "Repository *";
            //
            // UI_cmb_Repo
            // every git action in this dialog targets the repository picked here
            //
            this.UI_cmb_Repo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UI_cmb_Repo.Location = new System.Drawing.Point(136, 65);
            this.UI_cmb_Repo.Name = "UI_cmb_Repo";
            this.UI_cmb_Repo.Size = new System.Drawing.Size(320, 24);
            this.UI_cmb_Repo.TabIndex = 1;
            this.UI_cmb_Repo.SelectedIndexChanged += new System.EventHandler(this.UI_cmb_Repo_SelectedIndexChanged);
            //
            // UI_cmd_AddRepo
            // adds a working copy the built-in list doesn't cover; remembered
            // in HKCU so it is offered again next time
            //
            this.UI_cmd_AddRepo.Location = new System.Drawing.Point(462, 65);
            this.UI_cmd_AddRepo.Name = "UI_cmd_AddRepo";
            this.UI_cmd_AddRepo.Size = new System.Drawing.Size(100, 24);
            this.UI_cmd_AddRepo.TabIndex = 2;
            this.UI_cmd_AddRepo.Text = "&Add repo...";
            this.UI_cmd_AddRepo.UseVisualStyleBackColor = true;
            this.UI_cmd_AddRepo.Click += new System.EventHandler(this.UI_cmd_AddRepo_Click);
            //
            // UI_lbl_RepoPath
            //
            this.UI_lbl_RepoPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_lbl_RepoPath.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_RepoPath.Location = new System.Drawing.Point(570, 68);
            this.UI_lbl_RepoPath.Name = "UI_lbl_RepoPath";
            this.UI_lbl_RepoPath.Size = new System.Drawing.Size(262, 20);
            this.UI_lbl_RepoPath.TabIndex = 3;
            //
            // UI_lbl_Type
            //
            this.UI_lbl_Type.Location = new System.Drawing.Point(12, 102);
            this.UI_lbl_Type.Name = "UI_lbl_Type";
            this.UI_lbl_Type.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Type.TabIndex = 3;
            this.UI_lbl_Type.Text = "Commit Type *";
            //
            // UI_cmb_Type
            // deliberately starts unselected so the author has to choose
            //
            this.UI_cmb_Type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UI_cmb_Type.Items.AddRange(new object[] { "Bug Fix", "Feature", "Development" });
            this.UI_cmb_Type.Location = new System.Drawing.Point(136, 99);
            this.UI_cmb_Type.Name = "UI_cmb_Type";
            this.UI_cmb_Type.Size = new System.Drawing.Size(180, 24);
            this.UI_cmb_Type.TabIndex = 4;
            this.UI_cmb_Type.SelectedIndexChanged += new System.EventHandler(this.field_Changed);
            //
            // UI_lbl_Risk
            //
            this.UI_lbl_Risk.Location = new System.Drawing.Point(12, 136);
            this.UI_lbl_Risk.Name = "UI_lbl_Risk";
            this.UI_lbl_Risk.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Risk.TabIndex = 5;
            this.UI_lbl_Risk.Text = "Risk Level *";
            //
            // UI_cmb_Risk
            // also starts unselected
            //
            this.UI_cmb_Risk.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UI_cmb_Risk.Items.AddRange(new object[] { "Low", "Medium", "High" });
            this.UI_cmb_Risk.Location = new System.Drawing.Point(136, 133);
            this.UI_cmb_Risk.Name = "UI_cmb_Risk";
            this.UI_cmb_Risk.Size = new System.Drawing.Size(180, 24);
            this.UI_cmb_Risk.TabIndex = 6;
            this.UI_cmb_Risk.SelectedIndexChanged += new System.EventHandler(this.field_Changed);
            //
            // UI_lbl_RiskHelp
            //
            this.UI_lbl_RiskHelp.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_RiskHelp.Location = new System.Drawing.Point(326, 136);
            this.UI_lbl_RiskHelp.Name = "UI_lbl_RiskHelp";
            this.UI_lbl_RiskHelp.Size = new System.Drawing.Size(320, 20);
            this.UI_lbl_RiskHelp.TabIndex = 7;
            this.UI_lbl_RiskHelp.Text = "required";
            //
            // UI_lbl_Area
            //
            this.UI_lbl_Area.Location = new System.Drawing.Point(12, 170);
            this.UI_lbl_Area.Name = "UI_lbl_Area";
            this.UI_lbl_Area.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Area.TabIndex = 8;
            this.UI_lbl_Area.Text = "Area *";
            //
            // UI_panel_Area
            // one checkbox per area, laid out left to right and wrapping onto a
            // second row. The Text of each box IS the value written to the
            // message, so the two cannot drift apart.
            //
            this.UI_panel_Area.Controls.Add(this.UI_chk_AreaAOI);
            this.UI_panel_Area.Controls.Add(this.UI_chk_AreaAVI);
            this.UI_panel_Area.Controls.Add(this.UI_chk_AreaCDB);
            this.UI_panel_Area.Controls.Add(this.UI_chk_AreaSpark);
            this.UI_panel_Area.Controls.Add(this.UI_chk_AreaAlgo);
            this.UI_panel_Area.Controls.Add(this.UI_chk_AreaMachine);
            this.UI_panel_Area.Controls.Add(this.UI_chk_AreaSprint);
            this.UI_panel_Area.Controls.Add(this.UI_chk_AreaOrcal);
            this.UI_panel_Area.Controls.Add(this.UI_chk_AreaOther);
            this.UI_panel_Area.Location = new System.Drawing.Point(136, 167);
            this.UI_panel_Area.Name = "UI_panel_Area";
            this.UI_panel_Area.Size = new System.Drawing.Size(566, 52);
            this.UI_panel_Area.TabIndex = 9;
            this.UI_panel_Area.WrapContents = true;
            //
            // the area checkboxes
            //
            this.UI_chk_AreaAOI.Name = "UI_chk_AreaAOI";
            this.UI_chk_AreaAOI.Size = new System.Drawing.Size(92, 24);
            this.UI_chk_AreaAOI.TabIndex = 0;
            this.UI_chk_AreaAOI.Text = "AOI";
            this.UI_chk_AreaAOI.UseVisualStyleBackColor = true;
            this.UI_chk_AreaAOI.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            this.UI_chk_AreaAVI.Name = "UI_chk_AreaAVI";
            this.UI_chk_AreaAVI.Size = new System.Drawing.Size(92, 24);
            this.UI_chk_AreaAVI.TabIndex = 1;
            this.UI_chk_AreaAVI.Text = "AVI";
            this.UI_chk_AreaAVI.UseVisualStyleBackColor = true;
            this.UI_chk_AreaAVI.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            this.UI_chk_AreaCDB.Name = "UI_chk_AreaCDB";
            this.UI_chk_AreaCDB.Size = new System.Drawing.Size(92, 24);
            this.UI_chk_AreaCDB.TabIndex = 2;
            this.UI_chk_AreaCDB.Text = "CDB";
            this.UI_chk_AreaCDB.UseVisualStyleBackColor = true;
            this.UI_chk_AreaCDB.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            this.UI_chk_AreaSpark.Name = "UI_chk_AreaSpark";
            this.UI_chk_AreaSpark.Size = new System.Drawing.Size(92, 24);
            this.UI_chk_AreaSpark.TabIndex = 3;
            this.UI_chk_AreaSpark.Text = "Spark";
            this.UI_chk_AreaSpark.UseVisualStyleBackColor = true;
            this.UI_chk_AreaSpark.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            this.UI_chk_AreaAlgo.Name = "UI_chk_AreaAlgo";
            this.UI_chk_AreaAlgo.Size = new System.Drawing.Size(92, 24);
            this.UI_chk_AreaAlgo.TabIndex = 4;
            this.UI_chk_AreaAlgo.Text = "Algo";
            this.UI_chk_AreaAlgo.UseVisualStyleBackColor = true;
            this.UI_chk_AreaAlgo.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            this.UI_chk_AreaMachine.Name = "UI_chk_AreaMachine";
            this.UI_chk_AreaMachine.Size = new System.Drawing.Size(92, 24);
            this.UI_chk_AreaMachine.TabIndex = 5;
            this.UI_chk_AreaMachine.Text = "Machine";
            this.UI_chk_AreaMachine.UseVisualStyleBackColor = true;
            this.UI_chk_AreaMachine.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            this.UI_chk_AreaSprint.Name = "UI_chk_AreaSprint";
            this.UI_chk_AreaSprint.Size = new System.Drawing.Size(92, 24);
            this.UI_chk_AreaSprint.TabIndex = 6;
            this.UI_chk_AreaSprint.Text = "Sprint";
            this.UI_chk_AreaSprint.UseVisualStyleBackColor = true;
            this.UI_chk_AreaSprint.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            this.UI_chk_AreaOrcal.Name = "UI_chk_AreaOrcal";
            this.UI_chk_AreaOrcal.Size = new System.Drawing.Size(92, 24);
            this.UI_chk_AreaOrcal.TabIndex = 7;
            this.UI_chk_AreaOrcal.Text = "Orcal";
            this.UI_chk_AreaOrcal.UseVisualStyleBackColor = true;
            this.UI_chk_AreaOrcal.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            this.UI_chk_AreaOther.Name = "UI_chk_AreaOther";
            this.UI_chk_AreaOther.Size = new System.Drawing.Size(92, 24);
            this.UI_chk_AreaOther.TabIndex = 8;
            this.UI_chk_AreaOther.Text = "Other";
            this.UI_chk_AreaOther.UseVisualStyleBackColor = true;
            this.UI_chk_AreaOther.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            // UI_lbl_AreaHelp
            //
            this.UI_lbl_AreaHelp.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_AreaHelp.Location = new System.Drawing.Point(710, 170);
            this.UI_lbl_AreaHelp.Name = "UI_lbl_AreaHelp";
            this.UI_lbl_AreaHelp.Size = new System.Drawing.Size(124, 40);
            this.UI_lbl_AreaHelp.TabIndex = 10;
            this.UI_lbl_AreaHelp.Text = "required; pick one or more";
            //
            // UI_lbl_TT
            //
            this.UI_lbl_TT.Location = new System.Drawing.Point(12, 230);
            this.UI_lbl_TT.Name = "UI_lbl_TT";
            this.UI_lbl_TT.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_TT.TabIndex = 8;
            this.UI_lbl_TT.Text = "TT Number";
            //
            // UI_txt_TT
            //
            this.UI_txt_TT.Location = new System.Drawing.Point(136, 227);
            this.UI_txt_TT.Name = "UI_txt_TT";
            this.UI_txt_TT.Size = new System.Drawing.Size(180, 24);
            this.UI_txt_TT.TabIndex = 9;
            this.UI_txt_TT.TextChanged += new System.EventHandler(this.field_Changed);
            //
            // UI_lbl_TTHelp
            //
            this.UI_lbl_TTHelp.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_TTHelp.Location = new System.Drawing.Point(326, 230);
            this.UI_lbl_TTHelp.Name = "UI_lbl_TTHelp";
            this.UI_lbl_TTHelp.Size = new System.Drawing.Size(320, 20);
            this.UI_lbl_TTHelp.TabIndex = 10;
            this.UI_lbl_TTHelp.Text = "optional; adds a \"TT: ...\" line when filled";
            //
            // UI_lbl_Customer
            //
            this.UI_lbl_Customer.Location = new System.Drawing.Point(12, 264);
            this.UI_lbl_Customer.Name = "UI_lbl_Customer";
            this.UI_lbl_Customer.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Customer.TabIndex = 11;
            this.UI_lbl_Customer.Text = "Customer";
            //
            // UI_txt_Customer
            //
            this.UI_txt_Customer.Location = new System.Drawing.Point(136, 261);
            this.UI_txt_Customer.Name = "UI_txt_Customer";
            this.UI_txt_Customer.Size = new System.Drawing.Size(180, 24);
            this.UI_txt_Customer.TabIndex = 12;
            this.UI_txt_Customer.TextChanged += new System.EventHandler(this.field_Changed);
            //
            // UI_lbl_CustomerHelp
            //
            this.UI_lbl_CustomerHelp.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_CustomerHelp.Location = new System.Drawing.Point(326, 264);
            this.UI_lbl_CustomerHelp.Name = "UI_lbl_CustomerHelp";
            this.UI_lbl_CustomerHelp.Size = new System.Drawing.Size(320, 20);
            this.UI_lbl_CustomerHelp.TabIndex = 13;
            this.UI_lbl_CustomerHelp.Text = "optional; adds a \"Customer: ...\" line when filled";
            //
            // UI_chk_OtherVersions
            //
            this.UI_chk_OtherVersions.Location = new System.Drawing.Point(136, 295);
            this.UI_chk_OtherVersions.Name = "UI_chk_OtherVersions";
            this.UI_chk_OtherVersions.Size = new System.Drawing.Size(280, 24);
            this.UI_chk_OtherVersions.TabIndex = 14;
            this.UI_chk_OtherVersions.Text = "Should Be Added To Other Versions";
            this.UI_chk_OtherVersions.UseVisualStyleBackColor = true;
            this.UI_chk_OtherVersions.CheckedChanged += new System.EventHandler(this.field_Changed);
            //
            // UI_lbl_Subject
            //
            this.UI_lbl_Subject.Location = new System.Drawing.Point(12, 332);
            this.UI_lbl_Subject.Name = "UI_lbl_Subject";
            this.UI_lbl_Subject.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Subject.TabIndex = 15;
            this.UI_lbl_Subject.Text = "Subject *";
            //
            // UI_txt_Subject
            //
            this.UI_txt_Subject.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_txt_Subject.Location = new System.Drawing.Point(136, 329);
            this.UI_txt_Subject.Name = "UI_txt_Subject";
            this.UI_txt_Subject.Size = new System.Drawing.Size(490, 24);
            this.UI_txt_Subject.TabIndex = 16;
            this.UI_txt_Subject.TextChanged += new System.EventHandler(this.field_Changed);
            //
            // UI_lbl_Count
            //
            this.UI_lbl_Count.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_lbl_Count.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_Count.Location = new System.Drawing.Point(634, 332);
            this.UI_lbl_Count.Name = "UI_lbl_Count";
            this.UI_lbl_Count.Size = new System.Drawing.Size(70, 20);
            this.UI_lbl_Count.TabIndex = 17;
            //
            // UI_lbl_Body
            //
            this.UI_lbl_Body.Location = new System.Drawing.Point(12, 366);
            this.UI_lbl_Body.Name = "UI_lbl_Body";
            this.UI_lbl_Body.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Body.TabIndex = 18;
            this.UI_lbl_Body.Text = "Body";
            //
            // UI_txt_Body
            //
            this.UI_txt_Body.AcceptsReturn = true;
            this.UI_txt_Body.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_txt_Body.Location = new System.Drawing.Point(136, 363);
            this.UI_txt_Body.MaxLines = 30;
            this.UI_txt_Body.Multiline = true;
            this.UI_txt_Body.Name = "UI_txt_Body";
            this.UI_txt_Body.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.UI_txt_Body.Size = new System.Drawing.Size(566, 140);
            this.UI_txt_Body.TabIndex = 19;
            this.UI_txt_Body.TextChanged += new System.EventHandler(this.field_Changed);
            //
            // UI_cmd_FromVs
            //
            this.UI_cmd_FromVs.Location = new System.Drawing.Point(136, 505);
            this.UI_cmd_FromVs.Name = "UI_cmd_FromVs";
            this.UI_cmd_FromVs.Size = new System.Drawing.Size(90, 24);
            this.UI_cmd_FromVs.TabIndex = 20;
            this.UI_cmd_FromVs.Text = "From &VS";
            this.UI_cmd_FromVs.UseVisualStyleBackColor = true;
            this.UI_cmd_FromVs.Click += new System.EventHandler(this.UI_cmd_FromVs_Click);
            //
            // UI_cmd_FromClip
            //
            this.UI_cmd_FromClip.Location = new System.Drawing.Point(234, 505);
            this.UI_cmd_FromClip.Name = "UI_cmd_FromClip";
            this.UI_cmd_FromClip.Size = new System.Drawing.Size(110, 24);
            this.UI_cmd_FromClip.TabIndex = 21;
            this.UI_cmd_FromClip.Text = "From clip&board";
            this.UI_cmd_FromClip.UseVisualStyleBackColor = true;
            this.UI_cmd_FromClip.Click += new System.EventHandler(this.UI_cmd_FromClip_Click);
            //
            // UI_cmd_LastCommit
            //
            this.UI_cmd_LastCommit.Location = new System.Drawing.Point(352, 505);
            this.UI_cmd_LastCommit.Name = "UI_cmd_LastCommit";
            this.UI_cmd_LastCommit.Size = new System.Drawing.Size(100, 24);
            this.UI_cmd_LastCommit.TabIndex = 22;
            this.UI_cmd_LastCommit.Text = "Last commi&t";
            this.UI_cmd_LastCommit.UseVisualStyleBackColor = true;
            this.UI_cmd_LastCommit.Click += new System.EventHandler(this.UI_cmd_LastCommit_Click);
            //
            // UI_cmd_ClearBody
            //
            this.UI_cmd_ClearBody.Location = new System.Drawing.Point(460, 505);
            this.UI_cmd_ClearBody.Name = "UI_cmd_ClearBody";
            this.UI_cmd_ClearBody.Size = new System.Drawing.Size(80, 24);
            this.UI_cmd_ClearBody.TabIndex = 23;
            this.UI_cmd_ClearBody.Text = "C&lear";
            this.UI_cmd_ClearBody.UseVisualStyleBackColor = true;
            this.UI_cmd_ClearBody.Click += new System.EventHandler(this.UI_cmd_ClearBody_Click);
            //
            // UI_cmd_AI
            // writes the body from the diff with GitHub Copilot CLI (Claude model)
            //
            this.UI_cmd_AI.Location = new System.Drawing.Point(548, 505);
            this.UI_cmd_AI.Name = "UI_cmd_AI";
            this.UI_cmd_AI.Size = new System.Drawing.Size(60, 24);
            this.UI_cmd_AI.TabIndex = 24;
            this.UI_cmd_AI.Text = "A&I";
            this.UI_cmd_AI.UseVisualStyleBackColor = true;
            this.UI_cmd_AI.Click += new System.EventHandler(this.UI_cmd_AI_Click);
            //
            // UI_prg_AI
            // running bar while the AI writes the body; hidden otherwise
            //
            this.UI_prg_AI.Location = new System.Drawing.Point(616, 509);
            this.UI_prg_AI.MarqueeAnimationSpeed = 30;
            this.UI_prg_AI.Name = "UI_prg_AI";
            this.UI_prg_AI.Size = new System.Drawing.Size(120, 16);
            this.UI_prg_AI.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.UI_prg_AI.TabIndex = 25;
            this.UI_prg_AI.Visible = false;
            //
            // UI_lbl_AIStatus
            //
            this.UI_lbl_AIStatus.AutoSize = true;
            this.UI_lbl_AIStatus.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_AIStatus.Location = new System.Drawing.Point(742, 509);
            this.UI_lbl_AIStatus.Name = "UI_lbl_AIStatus";
            this.UI_lbl_AIStatus.TabIndex = 26;
            this.UI_lbl_AIStatus.Text = "Generating message...";
            this.UI_lbl_AIStatus.Visible = false;
            //
            // UI_lbl_PreviewCaption
            //
            this.UI_lbl_PreviewCaption.Location = new System.Drawing.Point(12, 542);
            this.UI_lbl_PreviewCaption.Name = "UI_lbl_PreviewCaption";
            this.UI_lbl_PreviewCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_PreviewCaption.TabIndex = 24;
            this.UI_lbl_PreviewCaption.Text = "Preview";
            //
            // UI_txt_Preview
            //
            this.UI_txt_Preview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_txt_Preview.BackColor = System.Drawing.Color.WhiteSmoke;
            this.UI_txt_Preview.Font = new System.Drawing.Font("Consolas", 9F);
            this.UI_txt_Preview.Location = new System.Drawing.Point(136, 539);
            this.UI_txt_Preview.Multiline = true;
            this.UI_txt_Preview.Name = "UI_txt_Preview";
            this.UI_txt_Preview.ReadOnly = true;
            this.UI_txt_Preview.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.UI_txt_Preview.Size = new System.Drawing.Size(566, 130);
            this.UI_txt_Preview.TabIndex = 25;
            //
            // UI_cmd_Copy
            //
            this.UI_cmd_Copy.Location = new System.Drawing.Point(136, 703);
            this.UI_cmd_Copy.Name = "UI_cmd_Copy";
            this.UI_cmd_Copy.Size = new System.Drawing.Size(130, 30);
            this.UI_cmd_Copy.TabIndex = 26;
            this.UI_cmd_Copy.Text = "&Copy to clipboard";
            this.UI_cmd_Copy.UseVisualStyleBackColor = true;
            this.UI_cmd_Copy.Click += new System.EventHandler(this.UI_cmd_Copy_Click);
            //
            // UI_cmd_Commit
            //
            this.UI_cmd_Commit.Location = new System.Drawing.Point(274, 703);
            this.UI_cmd_Commit.Name = "UI_cmd_Commit";
            this.UI_cmd_Commit.Size = new System.Drawing.Size(90, 30);
            this.UI_cmd_Commit.TabIndex = 27;
            this.UI_cmd_Commit.Text = "C&ommit";
            this.UI_cmd_Commit.UseVisualStyleBackColor = true;
            this.UI_cmd_Commit.Click += new System.EventHandler(this.UI_cmd_Commit_Click);
            //
            // UI_cmd_CommitPush
            //
            this.UI_cmd_CommitPush.Location = new System.Drawing.Point(372, 703);
            this.UI_cmd_CommitPush.Name = "UI_cmd_CommitPush";
            this.UI_cmd_CommitPush.Size = new System.Drawing.Size(130, 30);
            this.UI_cmd_CommitPush.TabIndex = 28;
            this.UI_cmd_CommitPush.Text = "Commit && &Push";
            this.UI_cmd_CommitPush.UseVisualStyleBackColor = true;
            this.UI_cmd_CommitPush.Click += new System.EventHandler(this.UI_cmd_CommitPush_Click);
            //
            // UI_cmd_Cancel
            //
            this.UI_cmd_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.UI_cmd_Cancel.Location = new System.Drawing.Point(510, 703);
            this.UI_cmd_Cancel.Name = "UI_cmd_Cancel";
            this.UI_cmd_Cancel.Size = new System.Drawing.Size(84, 30);
            this.UI_cmd_Cancel.TabIndex = 29;
            this.UI_cmd_Cancel.Text = "Cancel";
            this.UI_cmd_Cancel.UseVisualStyleBackColor = true;
            //
            // UI_cmd_SoftReset
            // kept away from the commit buttons because it rewrites history
            //
            this.UI_cmd_SoftReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_cmd_SoftReset.Location = new System.Drawing.Point(712, 705);
            this.UI_cmd_SoftReset.Name = "UI_cmd_SoftReset";
            this.UI_cmd_SoftReset.Size = new System.Drawing.Size(110, 26);
            this.UI_cmd_SoftReset.TabIndex = 30;
            this.UI_cmd_SoftReset.Text = "Undo Local Commits";
            this.UI_cmd_SoftReset.UseVisualStyleBackColor = true;
            this.UI_cmd_SoftReset.Click += new System.EventHandler(this.UI_cmd_SoftReset_Click);
            //
            // UI_lbl_SoftReset
            //
            this.UI_lbl_SoftReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_lbl_SoftReset.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_SoftReset.Location = new System.Drawing.Point(542, 735);
            this.UI_lbl_SoftReset.Name = "UI_lbl_SoftReset";
            this.UI_lbl_SoftReset.Size = new System.Drawing.Size(280, 18);
            this.UI_lbl_SoftReset.TabIndex = 31;
            this.UI_lbl_SoftReset.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // UI_chk_StageAll
            //
            this.UI_chk_StageAll.Location = new System.Drawing.Point(136, 745);
            this.UI_chk_StageAll.Name = "UI_chk_StageAll";
            this.UI_chk_StageAll.Size = new System.Drawing.Size(320, 24);
            this.UI_chk_StageAll.TabIndex = 32;
            this.UI_chk_StageAll.Text = "Include new (untracked) files";
            this.UI_chk_StageAll.UseVisualStyleBackColor = true;
            this.UI_chk_StageAll.CheckedChanged += new System.EventHandler(this.UI_chk_StageAll_CheckedChanged);
            //
            // UI_chk_PullRequest
            //
            this.UI_chk_PullRequest.Location = new System.Drawing.Point(136, 771);
            this.UI_chk_PullRequest.Name = "UI_chk_PullRequest";
            this.UI_chk_PullRequest.Size = new System.Drawing.Size(520, 24);
            this.UI_chk_PullRequest.TabIndex = 33;
            this.UI_chk_PullRequest.Text = "Create pull request after push";
            this.UI_chk_PullRequest.UseVisualStyleBackColor = true;
            //
            // UI_lbl_Staged
            //
            this.UI_lbl_Staged.Location = new System.Drawing.Point(136, 799);
            this.UI_lbl_Staged.Name = "UI_lbl_Staged";
            this.UI_lbl_Staged.Size = new System.Drawing.Size(560, 20);
            this.UI_lbl_Staged.TabIndex = 34;
            //
            // frmMain
            //

            this.CancelButton = this.UI_cmd_Cancel;
            this.ClientSize = new System.Drawing.Size(844, 912);
            this.Controls.Add(this.UI_lbl_Repo);
            this.Controls.Add(this.UI_cmb_Repo);
            this.Controls.Add(this.UI_cmd_AddRepo);
            this.Controls.Add(this.UI_lbl_RepoPath);
            this.Controls.Add(this.UI_lbl_Type);
            this.Controls.Add(this.UI_cmb_Type);
            this.Controls.Add(this.UI_lbl_Risk);
            this.Controls.Add(this.UI_cmb_Risk);
            this.Controls.Add(this.UI_lbl_RiskHelp);
            this.Controls.Add(this.UI_lbl_Area);
            this.Controls.Add(this.UI_panel_Area);
            this.Controls.Add(this.UI_lbl_AreaHelp);
            this.Controls.Add(this.UI_lbl_TT);
            this.Controls.Add(this.UI_txt_TT);
            this.Controls.Add(this.UI_lbl_TTHelp);
            this.Controls.Add(this.UI_lbl_Customer);
            this.Controls.Add(this.UI_txt_Customer);
            this.Controls.Add(this.UI_lbl_CustomerHelp);
            this.Controls.Add(this.UI_chk_OtherVersions);
            this.Controls.Add(this.UI_lbl_Subject);
            this.Controls.Add(this.UI_txt_Subject);
            this.Controls.Add(this.UI_lbl_Count);
            this.Controls.Add(this.UI_lbl_Body);
            this.Controls.Add(this.UI_txt_Body);
            this.Controls.Add(this.UI_cmd_FromVs);
            this.Controls.Add(this.UI_cmd_FromClip);
            this.Controls.Add(this.UI_cmd_LastCommit);
            this.Controls.Add(this.UI_cmd_ClearBody);
            this.Controls.Add(this.UI_cmd_AI);
            this.Controls.Add(this.UI_prg_AI);
            this.Controls.Add(this.UI_lbl_AIStatus);
            this.Controls.Add(this.UI_lbl_PreviewCaption);
            this.Controls.Add(this.UI_txt_Preview);
            this.Controls.Add(this.UI_cmd_Copy);
            this.Controls.Add(this.UI_cmd_Commit);
            this.Controls.Add(this.UI_cmd_CommitPush);
            this.Controls.Add(this.UI_cmd_Cancel);
            this.Controls.Add(this.UI_cmd_SoftReset);
            this.Controls.Add(this.UI_lbl_SoftReset);
            this.Controls.Add(this.UI_chk_StageAll);
            this.Controls.Add(this.UI_chk_PullRequest);
            this.Controls.Add(this.UI_lbl_Staged);
            this.Controls.Add(this.UI_pnl_Toolbar);
            this.Controls.Add(this.UI_pnl_MainMenu);
            this.MainMenuStrip = this.UI_pnl_MainMenu;
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(780, 891);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Git Helper";
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            this.Resize += new System.EventHandler(this.frmMain_Resize);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.UI_pnl_MainMenu.ResumeLayout(false);
            this.UI_pnl_Toolbar.ResumeLayout(false);
            this.UI_pnl_Toolbar.PerformLayout();
            this.UI_pnl_MainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
