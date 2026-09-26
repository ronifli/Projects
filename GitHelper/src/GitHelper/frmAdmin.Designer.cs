namespace GitHelper
{
    partial class frmAdmin
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label UI_lbl_BranchCaption;
        private System.Windows.Forms.ComboBox UI_cmb_Branch;
        private System.Windows.Forms.Label UI_lbl_FromDate;
        private System.Windows.Forms.DateTimePicker UI_dtp_FromDate;
        private System.Windows.Forms.CheckBox UI_chk_IncludeMerges;
        private System.Windows.Forms.Button UI_cmd_CheckMessages;
        private System.Windows.Forms.ProgressBar UI_prg_Check;
        private System.Windows.Forms.SplitContainer UI_pnl_Split;
        private System.Windows.Forms.ListView UI_grd_Commits;
        private System.Windows.Forms.ColumnHeader UI_col_Branch;
        private System.Windows.Forms.ColumnHeader UI_col_Date;
        private System.Windows.Forms.ColumnHeader UI_col_Author;
        private System.Windows.Forms.ColumnHeader UI_col_Commit;
        private System.Windows.Forms.ColumnHeader UI_col_Problems;
        private System.Windows.Forms.ColumnHeader UI_col_FirstLine;
        private System.Windows.Forms.TextBox UI_txt_Message;
        private System.Windows.Forms.Label UI_lbl_Summary;
        private System.Windows.Forms.Button UI_cmd_CopyTsv;
        private System.Windows.Forms.Button UI_cmd_Close;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.UI_lbl_BranchCaption = new System.Windows.Forms.Label();
            this.UI_cmb_Branch = new System.Windows.Forms.ComboBox();
            this.UI_lbl_FromDate = new System.Windows.Forms.Label();
            this.UI_dtp_FromDate = new System.Windows.Forms.DateTimePicker();
            this.UI_chk_IncludeMerges = new System.Windows.Forms.CheckBox();
            this.UI_cmd_CheckMessages = new System.Windows.Forms.Button();
            this.UI_prg_Check = new System.Windows.Forms.ProgressBar();
            this.UI_pnl_Split = new System.Windows.Forms.SplitContainer();
            this.UI_grd_Commits = new System.Windows.Forms.ListView();
            this.UI_col_Branch = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Date = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Author = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Commit = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Problems = new System.Windows.Forms.ColumnHeader();
            this.UI_col_FirstLine = new System.Windows.Forms.ColumnHeader();
            this.UI_txt_Message = new System.Windows.Forms.TextBox();
            this.UI_lbl_Summary = new System.Windows.Forms.Label();
            this.UI_cmd_CopyTsv = new System.Windows.Forms.Button();
            this.UI_cmd_Close = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.UI_pnl_Split)).BeginInit();
            this.UI_pnl_Split.Panel1.SuspendLayout();
            this.UI_pnl_Split.Panel2.SuspendLayout();
            this.UI_pnl_Split.SuspendLayout();
            this.SuspendLayout();
            //
            // UI_lbl_BranchCaption
            //
            this.UI_lbl_BranchCaption.Location = new System.Drawing.Point(12, 15);
            this.UI_lbl_BranchCaption.Name = "UI_lbl_BranchCaption";
            this.UI_lbl_BranchCaption.Size = new System.Drawing.Size(80, 20);
            this.UI_lbl_BranchCaption.TabIndex = 0;
            this.UI_lbl_BranchCaption.Text = "Branch";
            //
            // UI_cmb_Branch
            // ALL, main (origin), then the release branches (/rtm and /root)
            //
            this.UI_cmb_Branch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UI_cmb_Branch.Location = new System.Drawing.Point(92, 11);
            this.UI_cmb_Branch.MaxDropDownItems = 25;
            this.UI_cmb_Branch.Name = "UI_cmb_Branch";
            this.UI_cmb_Branch.Size = new System.Drawing.Size(180, 24);
            this.UI_cmb_Branch.TabIndex = 1;
            //
            // UI_lbl_FromDate
            //
            this.UI_lbl_FromDate.Location = new System.Drawing.Point(280, 15);
            this.UI_lbl_FromDate.Name = "UI_lbl_FromDate";
            this.UI_lbl_FromDate.Size = new System.Drawing.Size(76, 20);
            this.UI_lbl_FromDate.TabIndex = 2;
            this.UI_lbl_FromDate.Text = "&From Date:";
            //
            // UI_dtp_FromDate
            //
            this.UI_dtp_FromDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.UI_dtp_FromDate.Location = new System.Drawing.Point(358, 11);
            this.UI_dtp_FromDate.Name = "UI_dtp_FromDate";
            this.UI_dtp_FromDate.Size = new System.Drawing.Size(120, 23);
            this.UI_dtp_FromDate.TabIndex = 3;
            //
            // UI_chk_IncludeMerges
            //
            this.UI_chk_IncludeMerges.Location = new System.Drawing.Point(492, 13);
            this.UI_chk_IncludeMerges.Name = "UI_chk_IncludeMerges";
            this.UI_chk_IncludeMerges.Size = new System.Drawing.Size(170, 22);
            this.UI_chk_IncludeMerges.TabIndex = 4;
            this.UI_chk_IncludeMerges.Text = "Include &merge commits";
            //
            // UI_cmd_CheckMessages
            //
            this.UI_cmd_CheckMessages.Location = new System.Drawing.Point(670, 10);
            this.UI_cmd_CheckMessages.Name = "UI_cmd_CheckMessages";
            this.UI_cmd_CheckMessages.Size = new System.Drawing.Size(200, 27);
            this.UI_cmd_CheckMessages.TabIndex = 5;
            this.UI_cmd_CheckMessages.Text = "&Check commit messages";
            this.UI_cmd_CheckMessages.UseVisualStyleBackColor = true;
            this.UI_cmd_CheckMessages.Click += new System.EventHandler(this.UI_cmd_CheckMessages_Click);
            //
            // UI_prg_Check
            //
            this.UI_prg_Check.Location = new System.Drawing.Point(880, 16);
            this.UI_prg_Check.MarqueeAnimationSpeed = 30;
            this.UI_prg_Check.Name = "UI_prg_Check";
            this.UI_prg_Check.Size = new System.Drawing.Size(100, 16);
            this.UI_prg_Check.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.UI_prg_Check.TabIndex = 6;
            this.UI_prg_Check.Visible = false;
            //
            // UI_pnl_Split
            // offending commits on top, the selected one's full message below
            //
            this.UI_pnl_Split.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_pnl_Split.Location = new System.Drawing.Point(12, 46);
            this.UI_pnl_Split.Name = "UI_pnl_Split";
            this.UI_pnl_Split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.UI_pnl_Split.Panel1.Controls.Add(this.UI_grd_Commits);
            this.UI_pnl_Split.Panel2.Controls.Add(this.UI_txt_Message);
            this.UI_pnl_Split.Size = new System.Drawing.Size(976, 520);
            this.UI_pnl_Split.SplitterDistance = 340;
            this.UI_pnl_Split.TabIndex = 7;
            //
            // UI_grd_Commits
            //
            this.UI_grd_Commits.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.UI_col_Branch, this.UI_col_Date, this.UI_col_Author, this.UI_col_Commit, this.UI_col_Problems, this.UI_col_FirstLine });
            this.UI_grd_Commits.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UI_grd_Commits.FullRowSelect = true;
            this.UI_grd_Commits.GridLines = true;
            this.UI_grd_Commits.HideSelection = false;
            this.UI_grd_Commits.MultiSelect = false;
            this.UI_grd_Commits.Name = "UI_grd_Commits";
            this.UI_grd_Commits.TabIndex = 0;
            this.UI_grd_Commits.UseCompatibleStateImageBehavior = false;
            this.UI_grd_Commits.View = System.Windows.Forms.View.Details;
            this.UI_grd_Commits.SelectedIndexChanged += new System.EventHandler(this.UI_grd_Commits_SelectedIndexChanged);
            this.UI_col_Branch.Text = "Branch";
            this.UI_col_Branch.Width = 150;
            this.UI_col_Date.Text = "Date";
            this.UI_col_Date.Width = 120;
            this.UI_col_Author.Text = "Author";
            this.UI_col_Author.Width = 120;
            this.UI_col_Commit.Text = "Commit";
            this.UI_col_Commit.Width = 75;
            this.UI_col_Problems.Text = "Problems";
            this.UI_col_Problems.Width = 260;
            this.UI_col_FirstLine.Text = "First line";
            this.UI_col_FirstLine.Width = 260;
            //
            // UI_txt_Message
            //
            this.UI_txt_Message.BackColor = System.Drawing.SystemColors.Window;
            this.UI_txt_Message.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UI_txt_Message.Font = new System.Drawing.Font("Consolas", 9F);
            this.UI_txt_Message.Multiline = true;
            this.UI_txt_Message.Name = "UI_txt_Message";
            this.UI_txt_Message.ReadOnly = true;
            this.UI_txt_Message.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.UI_txt_Message.TabIndex = 0;
            this.UI_txt_Message.WordWrap = false;
            //
            // UI_lbl_Summary
            //
            this.UI_lbl_Summary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_lbl_Summary.AutoEllipsis = true;
            this.UI_lbl_Summary.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_Summary.Location = new System.Drawing.Point(12, 580);
            this.UI_lbl_Summary.Name = "UI_lbl_Summary";
            this.UI_lbl_Summary.Size = new System.Drawing.Size(740, 20);
            this.UI_lbl_Summary.TabIndex = 8;
            this.UI_lbl_Summary.Text = "Pick a date and press Check.";
            //
            // UI_cmd_CopyTsv
            //
            this.UI_cmd_CopyTsv.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_cmd_CopyTsv.Enabled = false;
            this.UI_cmd_CopyTsv.Location = new System.Drawing.Point(764, 576);
            this.UI_cmd_CopyTsv.Name = "UI_cmd_CopyTsv";
            this.UI_cmd_CopyTsv.Size = new System.Drawing.Size(120, 27);
            this.UI_cmd_CopyTsv.TabIndex = 9;
            this.UI_cmd_CopyTsv.Text = "Copy as &TSV";
            this.UI_cmd_CopyTsv.UseVisualStyleBackColor = true;
            this.UI_cmd_CopyTsv.Click += new System.EventHandler(this.UI_cmd_CopyTsv_Click);
            //
            // UI_cmd_Close
            //
            this.UI_cmd_Close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_cmd_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.UI_cmd_Close.Location = new System.Drawing.Point(894, 576);
            this.UI_cmd_Close.Name = "UI_cmd_Close";
            this.UI_cmd_Close.Size = new System.Drawing.Size(94, 27);
            this.UI_cmd_Close.TabIndex = 10;
            this.UI_cmd_Close.Text = "Close";
            this.UI_cmd_Close.UseVisualStyleBackColor = true;
            this.UI_cmd_Close.Click += new System.EventHandler(this.UI_cmd_Close_Click);
            //
            // frmAdmin
            //
            this.CancelButton = this.UI_cmd_Close;
            this.ClientSize = new System.Drawing.Size(1000, 615);
            this.Controls.Add(this.UI_lbl_BranchCaption);
            this.Controls.Add(this.UI_cmb_Branch);
            this.Controls.Add(this.UI_lbl_FromDate);
            this.Controls.Add(this.UI_dtp_FromDate);
            this.Controls.Add(this.UI_chk_IncludeMerges);
            this.Controls.Add(this.UI_cmd_CheckMessages);
            this.Controls.Add(this.UI_prg_Check);
            this.Controls.Add(this.UI_pnl_Split);
            this.Controls.Add(this.UI_lbl_Summary);
            this.Controls.Add(this.UI_cmd_CopyTsv);
            this.Controls.Add(this.UI_cmd_Close);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1016, 400);
            this.Name = "frmAdmin";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Git Helper - Admin";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmAdmin_FormClosing);
            this.Shown += new System.EventHandler(this.frmAdmin_Shown);
            this.UI_pnl_Split.Panel1.ResumeLayout(false);
            this.UI_pnl_Split.Panel2.ResumeLayout(false);
            this.UI_pnl_Split.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.UI_pnl_Split)).EndInit();
            this.UI_pnl_Split.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
