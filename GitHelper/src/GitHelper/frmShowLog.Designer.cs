namespace GitHelper
{
    partial class frmShowLog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label UI_lbl_Branch;
        private System.Windows.Forms.ComboBox UI_cmb_Branch;
        private System.Windows.Forms.Label UI_lbl_Search;
        private System.Windows.Forms.TextBox UI_txt_Search;
        private System.Windows.Forms.Label UI_lbl_Count;
        private System.Windows.Forms.ComboBox UI_cmb_Count;
        private System.Windows.Forms.Button UI_cmd_Refresh;
        private System.Windows.Forms.CheckBox UI_chk_Origin;
        private System.Windows.Forms.SplitContainer UI_pnl_Split;
        private GitHelper.ScrollNotifyListView UI_grd_Commits;
        private System.Windows.Forms.Panel UI_panel_ColumnFilters;
        private System.Windows.Forms.TextBox UI_txt_FilterDate;
        private System.Windows.Forms.TextBox UI_txt_FilterAuthor;
        private System.Windows.Forms.TextBox UI_txt_FilterType;
        private System.Windows.Forms.TextBox UI_txt_FilterArea;
        private System.Windows.Forms.TextBox UI_txt_FilterTT;
        private System.Windows.Forms.TextBox UI_txt_FilterRisk;
        private System.Windows.Forms.TextBox UI_txt_FilterSubject;
        private System.Windows.Forms.TextBox UI_txt_FilterCommit;
        private System.Windows.Forms.ColumnHeader UI_col_Date;
        private System.Windows.Forms.ColumnHeader UI_col_Author;
        private System.Windows.Forms.ColumnHeader UI_col_Type;
        private System.Windows.Forms.ColumnHeader UI_col_Risk;
        private System.Windows.Forms.ColumnHeader UI_col_Area;
        private System.Windows.Forms.ColumnHeader UI_col_TT;
        private System.Windows.Forms.ColumnHeader UI_col_Subject;
        private System.Windows.Forms.ColumnHeader UI_col_Sha;
        private System.Windows.Forms.TextBox UI_txt_Details;
        private System.Windows.Forms.SplitContainer UI_pnl_DetailsSplit;
        private System.Windows.Forms.ListView UI_grd_Files;
        private System.Windows.Forms.ColumnHeader UI_col_FileStatus;
        private System.Windows.Forms.ColumnHeader UI_col_FilePath;
        private System.Windows.Forms.ColumnHeader UI_col_FileLines;
        private System.Windows.Forms.Label UI_lbl_Summary;
        private System.Windows.Forms.Button UI_cmd_CopySha;
        private System.Windows.Forms.Button UI_cmd_Close;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.UI_lbl_Branch = new System.Windows.Forms.Label();
            this.UI_cmb_Branch = new System.Windows.Forms.ComboBox();
            this.UI_lbl_Search = new System.Windows.Forms.Label();
            this.UI_txt_Search = new System.Windows.Forms.TextBox();
            this.UI_lbl_Count = new System.Windows.Forms.Label();
            this.UI_cmb_Count = new System.Windows.Forms.ComboBox();
            this.UI_cmd_Refresh = new System.Windows.Forms.Button();
            this.UI_chk_Origin = new System.Windows.Forms.CheckBox();
            this.UI_pnl_Split = new System.Windows.Forms.SplitContainer();
            this.UI_grd_Commits = new GitHelper.ScrollNotifyListView();
            this.UI_panel_ColumnFilters = new System.Windows.Forms.Panel();
            this.UI_txt_FilterDate = new System.Windows.Forms.TextBox();
            this.UI_txt_FilterAuthor = new System.Windows.Forms.TextBox();
            this.UI_txt_FilterType = new System.Windows.Forms.TextBox();
            this.UI_txt_FilterArea = new System.Windows.Forms.TextBox();
            this.UI_txt_FilterTT = new System.Windows.Forms.TextBox();
            this.UI_txt_FilterRisk = new System.Windows.Forms.TextBox();
            this.UI_txt_FilterSubject = new System.Windows.Forms.TextBox();
            this.UI_txt_FilterCommit = new System.Windows.Forms.TextBox();
            this.UI_col_Date = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Author = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Type = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Risk = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Area = new System.Windows.Forms.ColumnHeader();
            this.UI_col_TT = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Subject = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Sha = new System.Windows.Forms.ColumnHeader();
            this.UI_txt_Details = new System.Windows.Forms.TextBox();
            this.UI_pnl_DetailsSplit = new System.Windows.Forms.SplitContainer();
            this.UI_grd_Files = new System.Windows.Forms.ListView();
            this.UI_col_FileStatus = new System.Windows.Forms.ColumnHeader();
            this.UI_col_FilePath = new System.Windows.Forms.ColumnHeader();
            this.UI_col_FileLines = new System.Windows.Forms.ColumnHeader();
            this.UI_lbl_Summary = new System.Windows.Forms.Label();
            this.UI_cmd_CopySha = new System.Windows.Forms.Button();
            this.UI_cmd_Close = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.UI_pnl_Split)).BeginInit();
            this.UI_pnl_Split.Panel1.SuspendLayout();
            this.UI_pnl_Split.Panel2.SuspendLayout();
            this.UI_pnl_Split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.UI_pnl_DetailsSplit)).BeginInit();
            this.UI_pnl_DetailsSplit.Panel1.SuspendLayout();
            this.UI_pnl_DetailsSplit.Panel2.SuspendLayout();
            this.UI_pnl_DetailsSplit.SuspendLayout();
            this.SuspendLayout();
            //
            // UI_lbl_Branch
            //
            this.UI_lbl_Branch.Location = new System.Drawing.Point(12, 15);
            this.UI_lbl_Branch.Name = "UI_lbl_Branch";
            this.UI_lbl_Branch.Size = new System.Drawing.Size(60, 20);
            this.UI_lbl_Branch.TabIndex = 0;
            this.UI_lbl_Branch.Text = "Branch";
            //
            // UI_cmb_Branch
            // the branch whose history is shown: local branches first (the
            // checked-out one on top), then origin/*. Click to pick, or type -
            // the list narrows to the branches containing what was typed.
            //
            this.UI_cmb_Branch.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.UI_cmb_Branch.Location = new System.Drawing.Point(72, 12);
            this.UI_cmb_Branch.MaxDropDownItems = 25;
            this.UI_cmb_Branch.Name = "UI_cmb_Branch";
            this.UI_cmb_Branch.Size = new System.Drawing.Size(200, 24);
            this.UI_cmb_Branch.TabIndex = 1;
            this.UI_cmb_Branch.SelectionChangeCommitted += new System.EventHandler(this.UI_cmb_Branch_SelectionChangeCommitted);
            this.UI_cmb_Branch.TextUpdate += new System.EventHandler(this.UI_cmb_Branch_TextUpdate);
            this.UI_cmb_Branch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.UI_cmb_Branch_KeyDown);
            this.UI_cmb_Branch.Leave += new System.EventHandler(this.UI_cmb_Branch_Leave);
            this.UI_cmb_Branch.MouseClick += new System.Windows.Forms.MouseEventHandler(this.UI_cmb_Branch_MouseClick);
            //
            // UI_lbl_Search
            //
            this.UI_lbl_Search.Location = new System.Drawing.Point(284, 15);
            this.UI_lbl_Search.Name = "UI_lbl_Search";
            this.UI_lbl_Search.Size = new System.Drawing.Size(52, 20);
            this.UI_lbl_Search.TabIndex = 2;
            this.UI_lbl_Search.Text = "&Search";
            //
            // UI_txt_Search
            // filters the loaded commits: subject, body, TT, author, type, area, SHA
            //
            this.UI_txt_Search.Location = new System.Drawing.Point(338, 12);
            this.UI_txt_Search.Name = "UI_txt_Search";
            this.UI_txt_Search.Size = new System.Drawing.Size(260, 23);
            this.UI_txt_Search.TabIndex = 3;
            this.UI_txt_Search.TextChanged += new System.EventHandler(this.UI_txt_Search_TextChanged);
            //
            // UI_lbl_Count
            //
            this.UI_lbl_Count.Location = new System.Drawing.Point(614, 15);
            this.UI_lbl_Count.Name = "UI_lbl_Count";
            this.UI_lbl_Count.Size = new System.Drawing.Size(40, 20);
            this.UI_lbl_Count.TabIndex = 4;
            this.UI_lbl_Count.Text = "Show";
            //
            // UI_cmb_Count
            //
            this.UI_cmb_Count.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UI_cmb_Count.Items.AddRange(new object[] { "100", "500", "2000", "All" });
            this.UI_cmb_Count.Location = new System.Drawing.Point(656, 12);
            this.UI_cmb_Count.Name = "UI_cmb_Count";
            this.UI_cmb_Count.Size = new System.Drawing.Size(70, 24);
            this.UI_cmb_Count.TabIndex = 5;
            this.UI_cmb_Count.SelectedIndexChanged += new System.EventHandler(this.UI_cmb_Count_SelectedIndexChanged);
            //
            // UI_cmd_Refresh
            //
            this.UI_cmd_Refresh.Location = new System.Drawing.Point(738, 11);
            this.UI_cmd_Refresh.Name = "UI_cmd_Refresh";
            this.UI_cmd_Refresh.Size = new System.Drawing.Size(90, 27);
            this.UI_cmd_Refresh.TabIndex = 6;
            this.UI_cmd_Refresh.Text = "&Refresh";
            this.UI_cmd_Refresh.Click += new System.EventHandler(this.UI_cmd_Refresh_Click);
            //
            // UI_chk_Origin
            // unticked: the local branch (HEAD); ticked: fetch, then origin/<branch>
            //
            this.UI_chk_Origin.Location = new System.Drawing.Point(842, 14);
            this.UI_chk_Origin.Name = "UI_chk_Origin";
            this.UI_chk_Origin.Size = new System.Drawing.Size(146, 22);
            this.UI_chk_Origin.TabIndex = 7;
            this.UI_chk_Origin.Text = "From &origin";
            this.UI_chk_Origin.CheckedChanged += new System.EventHandler(this.UI_chk_Origin_CheckedChanged);
            //
            // UI_pnl_Split
            // commit list on top, the selected commit's message and files below
            //
            this.UI_pnl_Split.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_pnl_Split.Location = new System.Drawing.Point(12, 46);
            this.UI_pnl_Split.Name = "UI_pnl_Split";
            this.UI_pnl_Split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.UI_pnl_Split.Panel1.Controls.Add(this.UI_grd_Commits);
            this.UI_pnl_Split.Panel1.Controls.Add(this.UI_panel_ColumnFilters);
            this.UI_pnl_Split.Panel2.Controls.Add(this.UI_pnl_DetailsSplit);
            this.UI_pnl_Split.Size = new System.Drawing.Size(976, 610);
            this.UI_pnl_Split.SplitterDistance = 300;
            this.UI_pnl_Split.TabIndex = 8;
            //
            // UI_grd_Commits
            //
            this.UI_grd_Commits.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.UI_col_Date, this.UI_col_Author, this.UI_col_Type, this.UI_col_Area,
                this.UI_col_TT, this.UI_col_Risk, this.UI_col_Subject, this.UI_col_Sha });
            this.UI_grd_Commits.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UI_grd_Commits.FullRowSelect = true;
            this.UI_grd_Commits.GridLines = true;
            this.UI_grd_Commits.HideSelection = false;
            this.UI_grd_Commits.MultiSelect = false;
            this.UI_grd_Commits.Name = "UI_grd_Commits";
            this.UI_grd_Commits.TabIndex = 1;
            this.UI_grd_Commits.UseCompatibleStateImageBehavior = false;
            this.UI_grd_Commits.View = System.Windows.Forms.View.Details;
            this.UI_grd_Commits.SelectedIndexChanged += new System.EventHandler(this.UI_grd_Commits_SelectedIndexChanged);
            this.UI_grd_Commits.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.UI_grd_Commits_ColumnClick);
            this.UI_grd_Commits.ColumnWidthChanged += new System.Windows.Forms.ColumnWidthChangedEventHandler(this.UI_grd_Commits_ColumnWidthChanged);
            this.UI_grd_Commits.HorizontalScrolled += new System.EventHandler(this.UI_grd_Commits_HorizontalScrolled);
            this.UI_grd_Commits.Resize += new System.EventHandler(this.UI_grd_Commits_HorizontalScrolled);
            //
            // UI_panel_ColumnFilters
            // one filter box above each column; frmShowLog.LayoutColumnFilters
            // lines them up with the columns at runtime (widths, scrolling)
            //
            this.UI_panel_ColumnFilters.Controls.Add(this.UI_txt_FilterDate);
            this.UI_panel_ColumnFilters.Controls.Add(this.UI_txt_FilterAuthor);
            this.UI_panel_ColumnFilters.Controls.Add(this.UI_txt_FilterType);
            this.UI_panel_ColumnFilters.Controls.Add(this.UI_txt_FilterArea);
            this.UI_panel_ColumnFilters.Controls.Add(this.UI_txt_FilterTT);
            this.UI_panel_ColumnFilters.Controls.Add(this.UI_txt_FilterRisk);
            this.UI_panel_ColumnFilters.Controls.Add(this.UI_txt_FilterSubject);
            this.UI_panel_ColumnFilters.Controls.Add(this.UI_txt_FilterCommit);
            this.UI_panel_ColumnFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.UI_panel_ColumnFilters.Name = "UI_panel_ColumnFilters";
            this.UI_panel_ColumnFilters.Size = new System.Drawing.Size(976, 26);
            this.UI_panel_ColumnFilters.TabIndex = 0;
            //
            // UI_txt_FilterDate
            //
            this.UI_txt_FilterDate.Location = new System.Drawing.Point(2, 1);
            this.UI_txt_FilterDate.Name = "UI_txt_FilterDate";
            this.UI_txt_FilterDate.Size = new System.Drawing.Size(120, 23);
            this.UI_txt_FilterDate.TabIndex = 0;
            this.UI_txt_FilterDate.TextChanged += new System.EventHandler(this.UI_txt_ColumnFilter_TextChanged);
            //
            // UI_txt_FilterAuthor
            //
            this.UI_txt_FilterAuthor.Location = new System.Drawing.Point(122, 1);
            this.UI_txt_FilterAuthor.Name = "UI_txt_FilterAuthor";
            this.UI_txt_FilterAuthor.Size = new System.Drawing.Size(110, 23);
            this.UI_txt_FilterAuthor.TabIndex = 1;
            this.UI_txt_FilterAuthor.TextChanged += new System.EventHandler(this.UI_txt_ColumnFilter_TextChanged);
            //
            // UI_txt_FilterType
            //
            this.UI_txt_FilterType.Location = new System.Drawing.Point(232, 1);
            this.UI_txt_FilterType.Name = "UI_txt_FilterType";
            this.UI_txt_FilterType.Size = new System.Drawing.Size(95, 23);
            this.UI_txt_FilterType.TabIndex = 2;
            this.UI_txt_FilterType.TextChanged += new System.EventHandler(this.UI_txt_ColumnFilter_TextChanged);
            //
            // UI_txt_FilterArea
            //
            this.UI_txt_FilterArea.Location = new System.Drawing.Point(327, 1);
            this.UI_txt_FilterArea.Name = "UI_txt_FilterArea";
            this.UI_txt_FilterArea.Size = new System.Drawing.Size(100, 23);
            this.UI_txt_FilterArea.TabIndex = 3;
            this.UI_txt_FilterArea.TextChanged += new System.EventHandler(this.UI_txt_ColumnFilter_TextChanged);
            //
            // UI_txt_FilterTT
            //
            this.UI_txt_FilterTT.Location = new System.Drawing.Point(427, 1);
            this.UI_txt_FilterTT.Name = "UI_txt_FilterTT";
            this.UI_txt_FilterTT.Size = new System.Drawing.Size(75, 23);
            this.UI_txt_FilterTT.TabIndex = 4;
            this.UI_txt_FilterTT.TextChanged += new System.EventHandler(this.UI_txt_ColumnFilter_TextChanged);
            //
            // UI_txt_FilterRisk
            //
            this.UI_txt_FilterRisk.Location = new System.Drawing.Point(502, 1);
            this.UI_txt_FilterRisk.Name = "UI_txt_FilterRisk";
            this.UI_txt_FilterRisk.Size = new System.Drawing.Size(70, 23);
            this.UI_txt_FilterRisk.TabIndex = 5;
            this.UI_txt_FilterRisk.TextChanged += new System.EventHandler(this.UI_txt_ColumnFilter_TextChanged);
            //
            // UI_txt_FilterSubject
            //
            this.UI_txt_FilterSubject.Location = new System.Drawing.Point(572, 1);
            this.UI_txt_FilterSubject.Name = "UI_txt_FilterSubject";
            this.UI_txt_FilterSubject.Size = new System.Drawing.Size(310, 23);
            this.UI_txt_FilterSubject.TabIndex = 6;
            this.UI_txt_FilterSubject.TextChanged += new System.EventHandler(this.UI_txt_ColumnFilter_TextChanged);
            //
            // UI_txt_FilterCommit
            //
            this.UI_txt_FilterCommit.Location = new System.Drawing.Point(882, 1);
            this.UI_txt_FilterCommit.Name = "UI_txt_FilterCommit";
            this.UI_txt_FilterCommit.Size = new System.Drawing.Size(70, 23);
            this.UI_txt_FilterCommit.TabIndex = 7;
            this.UI_txt_FilterCommit.TextChanged += new System.EventHandler(this.UI_txt_ColumnFilter_TextChanged);
            //
            // columns
            //
            this.UI_col_Date.Text = "Date";
            this.UI_col_Date.Width = 120;
            this.UI_col_Author.Text = "Author";
            this.UI_col_Author.Width = 110;
            this.UI_col_Type.Text = "Commit Type";
            this.UI_col_Type.Width = 95;
            this.UI_col_Area.Text = "Area";
            this.UI_col_Area.Width = 100;
            this.UI_col_TT.Text = "TT Number";
            this.UI_col_TT.Width = 75;
            this.UI_col_Risk.Text = "Risk Level";
            this.UI_col_Risk.Width = 70;
            this.UI_col_Subject.Text = "Subject";
            this.UI_col_Subject.Width = 310;
            this.UI_col_Sha.Text = "Commit";
            this.UI_col_Sha.Width = 70;
            //
            // UI_pnl_DetailsSplit
            // the selected commit: its message on top, the files it changed below
            //
            this.UI_pnl_DetailsSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UI_pnl_DetailsSplit.Name = "UI_pnl_DetailsSplit";
            this.UI_pnl_DetailsSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.UI_pnl_DetailsSplit.Panel1.Controls.Add(this.UI_txt_Details);
            this.UI_pnl_DetailsSplit.Panel2.Controls.Add(this.UI_grd_Files);
            this.UI_pnl_DetailsSplit.SplitterDistance = 130;
            this.UI_pnl_DetailsSplit.TabIndex = 0;
            //
            // UI_grd_Files
            // every file the selected commit changed
            //
            this.UI_grd_Files.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.UI_col_FileStatus, this.UI_col_FilePath, this.UI_col_FileLines });
            this.UI_grd_Files.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UI_grd_Files.FullRowSelect = true;
            this.UI_grd_Files.GridLines = true;
            this.UI_grd_Files.HideSelection = false;
            this.UI_grd_Files.Name = "UI_grd_Files";
            this.UI_grd_Files.TabIndex = 0;
            this.UI_grd_Files.UseCompatibleStateImageBehavior = false;
            this.UI_grd_Files.View = System.Windows.Forms.View.Details;
            this.UI_grd_Files.DoubleClick += new System.EventHandler(this.UI_grd_Files_DoubleClick);
            this.UI_col_FileStatus.Text = "Status";
            this.UI_col_FileStatus.Width = 95;
            this.UI_col_FilePath.Text = "File";
            this.UI_col_FilePath.Width = 720;
            this.UI_col_FileLines.Text = "Lines";
            this.UI_col_FileLines.Width = 100;
            //
            // UI_txt_Details
            //
            this.UI_txt_Details.BackColor = System.Drawing.SystemColors.Window;
            this.UI_txt_Details.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UI_txt_Details.Font = new System.Drawing.Font("Consolas", 9F);
            this.UI_txt_Details.Multiline = true;
            this.UI_txt_Details.Name = "UI_txt_Details";
            this.UI_txt_Details.ReadOnly = true;
            this.UI_txt_Details.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.UI_txt_Details.TabIndex = 0;
            this.UI_txt_Details.WordWrap = false;
            //
            // UI_lbl_Summary
            //
            this.UI_lbl_Summary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_lbl_Summary.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_Summary.Location = new System.Drawing.Point(12, 668);
            this.UI_lbl_Summary.Name = "UI_lbl_Summary";
            this.UI_lbl_Summary.Size = new System.Drawing.Size(660, 20);
            this.UI_lbl_Summary.TabIndex = 9;
            //
            // UI_cmd_CopySha
            //
            this.UI_cmd_CopySha.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_cmd_CopySha.Enabled = false;
            this.UI_cmd_CopySha.Location = new System.Drawing.Point(784, 664);
            this.UI_cmd_CopySha.Name = "UI_cmd_CopySha";
            this.UI_cmd_CopySha.Size = new System.Drawing.Size(110, 27);
            this.UI_cmd_CopySha.TabIndex = 10;
            this.UI_cmd_CopySha.Text = "Copy &SHA";
            this.UI_cmd_CopySha.Click += new System.EventHandler(this.UI_cmd_CopySha_Click);
            //
            // UI_cmd_Close
            //
            this.UI_cmd_Close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_cmd_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.UI_cmd_Close.Location = new System.Drawing.Point(898, 664);
            this.UI_cmd_Close.Name = "UI_cmd_Close";
            this.UI_cmd_Close.Size = new System.Drawing.Size(90, 27);
            this.UI_cmd_Close.TabIndex = 11;
            this.UI_cmd_Close.Text = "Close";
            //
            // frmShowLog
            //
            this.CancelButton = this.UI_cmd_Close;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this.UI_lbl_Branch);
            this.Controls.Add(this.UI_cmb_Branch);
            this.Controls.Add(this.UI_lbl_Search);
            this.Controls.Add(this.UI_txt_Search);
            this.Controls.Add(this.UI_lbl_Count);
            this.Controls.Add(this.UI_cmb_Count);
            this.Controls.Add(this.UI_cmd_Refresh);
            this.Controls.Add(this.UI_chk_Origin);
            this.Controls.Add(this.UI_pnl_Split);
            this.Controls.Add(this.UI_lbl_Summary);
            this.Controls.Add(this.UI_cmd_CopySha);
            this.Controls.Add(this.UI_cmd_Close);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1000, 560);
            this.Name = "frmShowLog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Log";
            this.Shown += new System.EventHandler(this.frmShowLog_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmShowLog_FormClosing);
            this.UI_pnl_DetailsSplit.Panel1.ResumeLayout(false);
            this.UI_pnl_DetailsSplit.Panel1.PerformLayout();
            this.UI_pnl_DetailsSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.UI_pnl_DetailsSplit)).EndInit();
            this.UI_pnl_DetailsSplit.ResumeLayout(false);
            this.UI_pnl_Split.Panel1.ResumeLayout(false);
            this.UI_pnl_Split.Panel2.ResumeLayout(false);
            this.UI_pnl_Split.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.UI_pnl_Split)).EndInit();
            this.UI_pnl_Split.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
