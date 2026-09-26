namespace GitHelper
{
    partial class frmCheckMerge
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label UI_lbl_SourceCaption;
        private System.Windows.Forms.ComboBox UI_cmb_Source;
        private System.Windows.Forms.Label UI_lbl_TargetCaption;
        private System.Windows.Forms.Label UI_lbl_Target;
        private System.Windows.Forms.Button UI_cmd_Check;
        private System.Windows.Forms.Label UI_lbl_SearchCaption;
        private System.Windows.Forms.TextBox UI_txt_Search;
        private System.Windows.Forms.Label UI_lbl_MatchCount;
        private System.Windows.Forms.CheckBox UI_chk_MissingOnly;
        private System.Windows.Forms.ListView UI_grd_Results;
        private System.Windows.Forms.ColumnHeader UI_col_From;
        private System.Windows.Forms.ColumnHeader UI_col_Status;
        private System.Windows.Forms.ColumnHeader UI_col_Type;
        private System.Windows.Forms.ColumnHeader UI_col_TT;
        private System.Windows.Forms.ColumnHeader UI_col_Subject;
        private System.Windows.Forms.ColumnHeader UI_col_Date;
        private System.Windows.Forms.ColumnHeader UI_col_Author;
        private System.Windows.Forms.ColumnHeader UI_col_Sha;
        private System.Windows.Forms.Label UI_lbl_Summary;
        private System.Windows.Forms.Label UI_lbl_Caveat;
        private System.Windows.Forms.Button UI_cmd_Copy;
        private System.Windows.Forms.Button UI_cmd_Close;
        private System.Windows.Forms.GroupBox UI_pnl_AICheck;
        private System.Windows.Forms.Label UI_lbl_AISince;
        private System.Windows.Forms.DateTimePicker UI_dtp_AISince;
        private System.Windows.Forms.Button UI_cmd_AICheck;
        private System.Windows.Forms.Button UI_cmd_AILog;
        private System.Windows.Forms.ProgressBar UI_prg_AICheck;
        private System.Windows.Forms.Label UI_lbl_AIStatus;
        private System.Windows.Forms.ListView UI_grd_AIResults;
        private System.Windows.Forms.ColumnHeader UI_col_AiFrom;
        private System.Windows.Forms.ColumnHeader UI_col_AiVerdict;
        private System.Windows.Forms.ColumnHeader UI_col_AiCommit;
        private System.Windows.Forms.ColumnHeader UI_col_AiDate;
        private System.Windows.Forms.ColumnHeader UI_col_AiSubject;
        private System.Windows.Forms.ColumnHeader UI_col_AiEvidence;
        private System.Windows.Forms.ColumnHeader UI_col_AiReason;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.UI_lbl_SourceCaption = new System.Windows.Forms.Label();
            this.UI_cmb_Source = new System.Windows.Forms.ComboBox();
            this.UI_lbl_TargetCaption = new System.Windows.Forms.Label();
            this.UI_lbl_Target = new System.Windows.Forms.Label();
            this.UI_cmd_Check = new System.Windows.Forms.Button();
            this.UI_lbl_SearchCaption = new System.Windows.Forms.Label();
            this.UI_txt_Search = new System.Windows.Forms.TextBox();
            this.UI_lbl_MatchCount = new System.Windows.Forms.Label();
            this.UI_chk_MissingOnly = new System.Windows.Forms.CheckBox();
            this.UI_grd_Results = new System.Windows.Forms.ListView();
            this.UI_col_From = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Status = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Type = new System.Windows.Forms.ColumnHeader();
            this.UI_col_TT = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Subject = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Date = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Author = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Sha = new System.Windows.Forms.ColumnHeader();
            this.UI_lbl_Summary = new System.Windows.Forms.Label();
            this.UI_lbl_Caveat = new System.Windows.Forms.Label();
            this.UI_cmd_Copy = new System.Windows.Forms.Button();
            this.UI_cmd_Close = new System.Windows.Forms.Button();
            this.UI_pnl_AICheck = new System.Windows.Forms.GroupBox();
            this.UI_lbl_AISince = new System.Windows.Forms.Label();
            this.UI_dtp_AISince = new System.Windows.Forms.DateTimePicker();
            this.UI_cmd_AICheck = new System.Windows.Forms.Button();
            this.UI_cmd_AILog = new System.Windows.Forms.Button();
            this.UI_prg_AICheck = new System.Windows.Forms.ProgressBar();
            this.UI_lbl_AIStatus = new System.Windows.Forms.Label();
            this.UI_grd_AIResults = new System.Windows.Forms.ListView();
            this.UI_col_AiFrom = new System.Windows.Forms.ColumnHeader();
            this.UI_col_AiVerdict = new System.Windows.Forms.ColumnHeader();
            this.UI_col_AiCommit = new System.Windows.Forms.ColumnHeader();
            this.UI_col_AiDate = new System.Windows.Forms.ColumnHeader();
            this.UI_col_AiSubject = new System.Windows.Forms.ColumnHeader();
            this.UI_col_AiEvidence = new System.Windows.Forms.ColumnHeader();
            this.UI_col_AiReason = new System.Windows.Forms.ColumnHeader();
            this.UI_pnl_AICheck.SuspendLayout();
            this.SuspendLayout();
            //
            // UI_lbl_SourceCaption
            //
            this.UI_lbl_SourceCaption.Location = new System.Drawing.Point(12, 15);
            this.UI_lbl_SourceCaption.Name = "UI_lbl_SourceCaption";
            this.UI_lbl_SourceCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_SourceCaption.TabIndex = 0;
            this.UI_lbl_SourceCaption.Text = "Changes from";
            //
            // UI_cmb_Source
            // the branch whose commits should all be here, e.g. RTM0
            //
            this.UI_cmb_Source.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UI_cmb_Source.Location = new System.Drawing.Point(136, 12);
            this.UI_cmb_Source.Name = "UI_cmb_Source";
            this.UI_cmb_Source.Size = new System.Drawing.Size(280, 24);
            this.UI_cmb_Source.TabIndex = 1;
            //
            // UI_lbl_TargetCaption
            //
            this.UI_lbl_TargetCaption.Location = new System.Drawing.Point(432, 15);
            this.UI_lbl_TargetCaption.Name = "UI_lbl_TargetCaption";
            this.UI_lbl_TargetCaption.Size = new System.Drawing.Size(70, 20);
            this.UI_lbl_TargetCaption.TabIndex = 2;
            this.UI_lbl_TargetCaption.Text = "present in";
            //
            // UI_lbl_Target
            //
            this.UI_lbl_Target.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.UI_lbl_Target.Location = new System.Drawing.Point(508, 15);
            this.UI_lbl_Target.Name = "UI_lbl_Target";
            this.UI_lbl_Target.Size = new System.Drawing.Size(280, 20);
            this.UI_lbl_Target.TabIndex = 3;
            //
            // UI_cmd_Check
            //
            this.UI_cmd_Check.Location = new System.Drawing.Point(794, 10);
            this.UI_cmd_Check.Name = "UI_cmd_Check";
            this.UI_cmd_Check.Size = new System.Drawing.Size(100, 28);
            this.UI_cmd_Check.TabIndex = 4;
            this.UI_cmd_Check.Text = "&Check";
            this.UI_cmd_Check.UseVisualStyleBackColor = true;
            this.UI_cmd_Check.Click += new System.EventHandler(this.UI_cmd_Check_Click);
            //
            // UI_lbl_SearchCaption
            //
            this.UI_lbl_SearchCaption.Location = new System.Drawing.Point(12, 47);
            this.UI_lbl_SearchCaption.Name = "UI_lbl_SearchCaption";
            this.UI_lbl_SearchCaption.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_SearchCaption.TabIndex = 5;
            this.UI_lbl_SearchCaption.Text = "Search";
            //
            // UI_txt_Search
            // filters the branch list above; a release repo has too many
            // branches to scroll through
            //
            this.UI_txt_Search.Location = new System.Drawing.Point(136, 44);
            this.UI_txt_Search.Name = "UI_txt_Search";
            this.UI_txt_Search.Size = new System.Drawing.Size(280, 24);
            this.UI_txt_Search.TabIndex = 6;
            this.UI_txt_Search.TextChanged += new System.EventHandler(this.UI_txt_Search_TextChanged);
            //
            // UI_lbl_MatchCount
            //
            this.UI_lbl_MatchCount.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_MatchCount.Location = new System.Drawing.Point(432, 47);
            this.UI_lbl_MatchCount.Name = "UI_lbl_MatchCount";
            this.UI_lbl_MatchCount.Size = new System.Drawing.Size(180, 20);
            this.UI_lbl_MatchCount.TabIndex = 7;
            //
            // UI_chk_MissingOnly
            //
            this.UI_chk_MissingOnly.Location = new System.Drawing.Point(620, 44);
            this.UI_chk_MissingOnly.Name = "UI_chk_MissingOnly";
            this.UI_chk_MissingOnly.Size = new System.Drawing.Size(274, 24);
            this.UI_chk_MissingOnly.TabIndex = 5;
            this.UI_chk_MissingOnly.Text = "Show only what is missing";
            this.UI_chk_MissingOnly.UseVisualStyleBackColor = true;
            this.UI_chk_MissingOnly.CheckedChanged += new System.EventHandler(this.UI_chk_MissingOnly_CheckedChanged);
            //
            // UI_grd_Results
            //
            this.UI_grd_Results.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_grd_Results.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.UI_col_From, this.UI_col_Status, this.UI_col_Type, this.UI_col_TT, this.UI_col_Subject,
                this.UI_col_Date, this.UI_col_Author, this.UI_col_Sha });
            this.UI_grd_Results.FullRowSelect = true;
            this.UI_grd_Results.GridLines = true;
            this.UI_grd_Results.HideSelection = false;
            this.UI_grd_Results.Location = new System.Drawing.Point(12, 74);
            this.UI_grd_Results.Name = "UI_grd_Results";
            this.UI_grd_Results.Size = new System.Drawing.Size(882, 380);
            this.UI_grd_Results.TabIndex = 6;
            this.UI_grd_Results.UseCompatibleStateImageBehavior = false;
            this.UI_grd_Results.View = System.Windows.Forms.View.Details;
            this.UI_grd_Results.DoubleClick += new System.EventHandler(this.UI_grd_Results_DoubleClick);
            //
            // columns
            //
            this.UI_col_From.Text = "From";
            this.UI_col_From.Width = 140;
            this.UI_col_Status.Text = "Status";
            this.UI_col_Status.Width = 170;
            this.UI_col_Type.Text = "Type";
            this.UI_col_Type.Width = 90;
            this.UI_col_TT.Text = "TT";
            this.UI_col_TT.Width = 70;
            this.UI_col_Subject.Text = "Subject";
            this.UI_col_Subject.Width = 250;
            this.UI_col_Date.Text = "Date";
            this.UI_col_Date.Width = 90;
            this.UI_col_Author.Text = "Author";
            this.UI_col_Author.Width = 110;
            this.UI_col_Sha.Text = "Commit";
            this.UI_col_Sha.Width = 80;
            //
            // UI_lbl_Summary
            //
            this.UI_lbl_Summary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_lbl_Summary.Location = new System.Drawing.Point(12, 722);
            this.UI_lbl_Summary.Name = "UI_lbl_Summary";
            this.UI_lbl_Summary.Size = new System.Drawing.Size(882, 20);
            this.UI_lbl_Summary.TabIndex = 7;
            //
            // UI_lbl_Caveat
            //
            this.UI_lbl_Caveat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_lbl_Caveat.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_Caveat.Location = new System.Drawing.Point(12, 746);
            this.UI_lbl_Caveat.Name = "UI_lbl_Caveat";
            this.UI_lbl_Caveat.Size = new System.Drawing.Size(882, 36);
            this.UI_lbl_Caveat.TabIndex = 8;
            this.UI_lbl_Caveat.Text = "Matched by patch-id, then by cherry-pick trailer, then by Subject, then by TT. " +
                "\"probably present\" rows rely on the message alone - a Change-Id field would make this exact.";
            //
            // UI_cmd_Copy
            //
            this.UI_cmd_Copy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left)));
            this.UI_cmd_Copy.Location = new System.Drawing.Point(12, 790);
            this.UI_cmd_Copy.Name = "UI_cmd_Copy";
            this.UI_cmd_Copy.Size = new System.Drawing.Size(150, 30);
            this.UI_cmd_Copy.TabIndex = 9;
            this.UI_cmd_Copy.Text = "Copy as &TSV";
            this.UI_cmd_Copy.UseVisualStyleBackColor = true;
            this.UI_cmd_Copy.Click += new System.EventHandler(this.UI_cmd_Copy_Click);
            //
            // UI_cmd_Close
            //
            this.UI_cmd_Close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_cmd_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.UI_cmd_Close.Location = new System.Drawing.Point(810, 790);
            this.UI_cmd_Close.Name = "UI_cmd_Close";
            this.UI_cmd_Close.Size = new System.Drawing.Size(84, 30);
            this.UI_cmd_Close.TabIndex = 10;
            this.UI_cmd_Close.Text = "Close";
            this.UI_cmd_Close.UseVisualStyleBackColor = true;
            //
            // frmCheckMerge
            //

            this.CancelButton = this.UI_cmd_Close;
            //
            // UI_pnl_AICheck
            // AI check: the selected branch(es), commits since a date, judged
            // by content with GitHub Copilot - see MergeAi
            //
            this.UI_pnl_AICheck.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_pnl_AICheck.Controls.Add(this.UI_lbl_AISince);
            this.UI_pnl_AICheck.Controls.Add(this.UI_dtp_AISince);
            this.UI_pnl_AICheck.Controls.Add(this.UI_cmd_AICheck);
            this.UI_pnl_AICheck.Controls.Add(this.UI_cmd_AILog);
            this.UI_pnl_AICheck.Controls.Add(this.UI_prg_AICheck);
            this.UI_pnl_AICheck.Controls.Add(this.UI_lbl_AIStatus);
            this.UI_pnl_AICheck.Controls.Add(this.UI_grd_AIResults);
            this.UI_pnl_AICheck.Location = new System.Drawing.Point(12, 462);
            this.UI_pnl_AICheck.Name = "UI_pnl_AICheck";
            this.UI_pnl_AICheck.Size = new System.Drawing.Size(882, 250);
            this.UI_pnl_AICheck.TabIndex = 11;
            this.UI_pnl_AICheck.TabStop = false;
            this.UI_pnl_AICheck.Text = "AI check - which changes are missing, judged by the code (GitHub Copilot)";
            //
            // UI_lbl_AISince
            //
            this.UI_lbl_AISince.Location = new System.Drawing.Point(10, 25);
            this.UI_lbl_AISince.Name = "UI_lbl_AISince";
            this.UI_lbl_AISince.Size = new System.Drawing.Size(96, 20);
            this.UI_lbl_AISince.TabIndex = 0;
            this.UI_lbl_AISince.Text = "Commits &since";
            //
            // UI_dtp_AISince
            //
            this.UI_dtp_AISince.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.UI_dtp_AISince.Location = new System.Drawing.Point(108, 21);
            this.UI_dtp_AISince.Name = "UI_dtp_AISince";
            this.UI_dtp_AISince.Size = new System.Drawing.Size(120, 23);
            this.UI_dtp_AISince.TabIndex = 1;
            //
            // UI_cmd_AICheck
            //
            this.UI_cmd_AICheck.Location = new System.Drawing.Point(240, 19);
            this.UI_cmd_AICheck.Name = "UI_cmd_AICheck";
            this.UI_cmd_AICheck.Size = new System.Drawing.Size(120, 27);
            this.UI_cmd_AICheck.TabIndex = 2;
            this.UI_cmd_AICheck.Text = "Chec&k with AI";
            this.UI_cmd_AICheck.UseVisualStyleBackColor = true;
            this.UI_cmd_AICheck.Click += new System.EventHandler(this.UI_cmd_AICheck_Click);
            //
            // UI_cmd_AILog
            // the missing lines behind the verdicts - opens by itself after a check
            //
            this.UI_cmd_AILog.Enabled = false;
            this.UI_cmd_AILog.Location = new System.Drawing.Point(368, 19);
            this.UI_cmd_AILog.Name = "UI_cmd_AILog";
            this.UI_cmd_AILog.Size = new System.Drawing.Size(120, 27);
            this.UI_cmd_AILog.TabIndex = 3;
            this.UI_cmd_AILog.Text = "Missing &lines...";
            this.UI_cmd_AILog.UseVisualStyleBackColor = true;
            this.UI_cmd_AILog.Click += new System.EventHandler(this.UI_cmd_AILog_Click);
            //
            // UI_prg_AICheck
            //
            this.UI_prg_AICheck.Location = new System.Drawing.Point(496, 25);
            this.UI_prg_AICheck.MarqueeAnimationSpeed = 30;
            this.UI_prg_AICheck.Name = "UI_prg_AICheck";
            this.UI_prg_AICheck.Size = new System.Drawing.Size(90, 16);
            this.UI_prg_AICheck.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.UI_prg_AICheck.TabIndex = 3;
            this.UI_prg_AICheck.Visible = false;
            //
            // UI_lbl_AIStatus
            //
            this.UI_lbl_AIStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_lbl_AIStatus.AutoEllipsis = true;
            this.UI_lbl_AIStatus.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_AIStatus.Location = new System.Drawing.Point(594, 25);
            this.UI_lbl_AIStatus.Name = "UI_lbl_AIStatus";
            this.UI_lbl_AIStatus.Size = new System.Drawing.Size(278, 20);
            this.UI_lbl_AIStatus.TabIndex = 4;
            this.UI_lbl_AIStatus.Text = "Runs on the branch chosen in \"Changes from\" (ALL = every listed branch).";
            //
            // UI_grd_AIResults
            //
            this.UI_grd_AIResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_grd_AIResults.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.UI_col_AiFrom, this.UI_col_AiVerdict, this.UI_col_AiCommit, this.UI_col_AiDate,
                this.UI_col_AiSubject, this.UI_col_AiEvidence, this.UI_col_AiReason });
            this.UI_grd_AIResults.FullRowSelect = true;
            this.UI_grd_AIResults.GridLines = true;
            this.UI_grd_AIResults.HideSelection = false;
            this.UI_grd_AIResults.Location = new System.Drawing.Point(10, 52);
            this.UI_grd_AIResults.Name = "UI_grd_AIResults";
            this.UI_grd_AIResults.Size = new System.Drawing.Size(862, 188);
            this.UI_grd_AIResults.TabIndex = 5;
            this.UI_grd_AIResults.UseCompatibleStateImageBehavior = false;
            this.UI_grd_AIResults.View = System.Windows.Forms.View.Details;
            this.UI_grd_AIResults.DoubleClick += new System.EventHandler(this.UI_grd_AIResults_DoubleClick);
            this.UI_col_AiFrom.Text = "From";
            this.UI_col_AiFrom.Width = 130;
            this.UI_col_AiVerdict.Text = "Verdict";
            this.UI_col_AiVerdict.Width = 130;
            this.UI_col_AiCommit.Text = "Commit";
            this.UI_col_AiCommit.Width = 70;
            this.UI_col_AiDate.Text = "Date";
            this.UI_col_AiDate.Width = 80;
            this.UI_col_AiSubject.Text = "Subject";
            this.UI_col_AiSubject.Width = 200;
            this.UI_col_AiEvidence.Text = "In target";
            this.UI_col_AiEvidence.Width = 80;
            this.UI_col_AiReason.Text = "Reason";
            this.UI_col_AiReason.Width = 400;
            //
            this.ClientSize = new System.Drawing.Size(906, 832);
            this.Controls.Add(this.UI_lbl_SourceCaption);
            this.Controls.Add(this.UI_cmb_Source);
            this.Controls.Add(this.UI_lbl_TargetCaption);
            this.Controls.Add(this.UI_lbl_Target);
            this.Controls.Add(this.UI_cmd_Check);
            this.Controls.Add(this.UI_lbl_SearchCaption);
            this.Controls.Add(this.UI_txt_Search);
            this.Controls.Add(this.UI_lbl_MatchCount);
            this.Controls.Add(this.UI_chk_MissingOnly);
            this.Controls.Add(this.UI_grd_Results);
            this.Controls.Add(this.UI_lbl_Summary);
            this.Controls.Add(this.UI_lbl_Caveat);
            this.Controls.Add(this.UI_cmd_Copy);
            this.Controls.Add(this.UI_cmd_Close);
            this.Controls.Add(this.UI_pnl_AICheck);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(740, 740);
            this.Name = "frmCheckMerge";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Check Merge";
            this.UI_pnl_AICheck.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
