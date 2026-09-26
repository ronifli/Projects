namespace GitHelper
{
    partial class frmMissingLog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label UI_lbl_Summary;
        private System.Windows.Forms.SplitContainer UI_pnl_Split;
        private System.Windows.Forms.ListView UI_grd_Commits;
        private System.Windows.Forms.ColumnHeader UI_col_From;
        private System.Windows.Forms.ColumnHeader UI_col_Verdict;
        private System.Windows.Forms.ColumnHeader UI_col_Commit;
        private System.Windows.Forms.ColumnHeader UI_col_Missing;
        private System.Windows.Forms.ColumnHeader UI_col_Subject;
        private System.Windows.Forms.RichTextBox UI_txt_Log;
        private System.Windows.Forms.ProgressBar UI_prg_Load;
        private System.Windows.Forms.Button UI_cmd_CopyAll;
        private System.Windows.Forms.Button UI_cmd_Close;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.UI_lbl_Summary = new System.Windows.Forms.Label();
            this.UI_pnl_Split = new System.Windows.Forms.SplitContainer();
            this.UI_grd_Commits = new System.Windows.Forms.ListView();
            this.UI_col_From = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Verdict = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Commit = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Missing = new System.Windows.Forms.ColumnHeader();
            this.UI_col_Subject = new System.Windows.Forms.ColumnHeader();
            this.UI_txt_Log = new System.Windows.Forms.RichTextBox();
            this.UI_prg_Load = new System.Windows.Forms.ProgressBar();
            this.UI_cmd_CopyAll = new System.Windows.Forms.Button();
            this.UI_cmd_Close = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.UI_pnl_Split)).BeginInit();
            this.UI_pnl_Split.Panel1.SuspendLayout();
            this.UI_pnl_Split.Panel2.SuspendLayout();
            this.UI_pnl_Split.SuspendLayout();
            this.SuspendLayout();
            //
            // UI_lbl_Summary
            //
            this.UI_lbl_Summary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_lbl_Summary.AutoEllipsis = true;
            this.UI_lbl_Summary.Location = new System.Drawing.Point(12, 12);
            this.UI_lbl_Summary.Name = "UI_lbl_Summary";
            this.UI_lbl_Summary.Size = new System.Drawing.Size(976, 20);
            this.UI_lbl_Summary.TabIndex = 0;
            this.UI_lbl_Summary.Text = "Comparing the changed lines with the target...";
            //
            // UI_pnl_Split
            // commits on the left, the selected one's missing lines on the right
            //
            this.UI_pnl_Split.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_pnl_Split.Location = new System.Drawing.Point(12, 36);
            this.UI_pnl_Split.Name = "UI_pnl_Split";
            this.UI_pnl_Split.Panel1.Controls.Add(this.UI_grd_Commits);
            this.UI_pnl_Split.Panel2.Controls.Add(this.UI_txt_Log);
            this.UI_pnl_Split.Size = new System.Drawing.Size(976, 562);
            this.UI_pnl_Split.SplitterDistance = 400;
            this.UI_pnl_Split.TabIndex = 1;
            //
            // UI_grd_Commits
            //
            this.UI_grd_Commits.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.UI_col_From, this.UI_col_Verdict, this.UI_col_Commit, this.UI_col_Missing, this.UI_col_Subject });
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
            this.UI_col_From.Text = "From";
            this.UI_col_From.Width = 110;
            this.UI_col_Verdict.Text = "Verdict";
            this.UI_col_Verdict.Width = 95;
            this.UI_col_Commit.Text = "Commit";
            this.UI_col_Commit.Width = 70;
            this.UI_col_Missing.Text = "Missing / changed";
            this.UI_col_Missing.Width = 110;
            this.UI_col_Subject.Text = "Subject";
            this.UI_col_Subject.Width = 260;
            //
            // UI_txt_Log
            //
            this.UI_txt_Log.BackColor = System.Drawing.SystemColors.Window;
            this.UI_txt_Log.DetectUrls = false;
            this.UI_txt_Log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UI_txt_Log.Font = new System.Drawing.Font("Consolas", 9F);
            this.UI_txt_Log.Name = "UI_txt_Log";
            this.UI_txt_Log.ReadOnly = true;
            this.UI_txt_Log.TabIndex = 0;
            this.UI_txt_Log.WordWrap = false;
            //
            // UI_prg_Load
            //
            this.UI_prg_Load.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left)));
            this.UI_prg_Load.Location = new System.Drawing.Point(12, 612);
            this.UI_prg_Load.MarqueeAnimationSpeed = 30;
            this.UI_prg_Load.Name = "UI_prg_Load";
            this.UI_prg_Load.Size = new System.Drawing.Size(160, 16);
            this.UI_prg_Load.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.UI_prg_Load.TabIndex = 2;
            //
            // UI_cmd_CopyAll
            //
            this.UI_cmd_CopyAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_cmd_CopyAll.Enabled = false;
            this.UI_cmd_CopyAll.Location = new System.Drawing.Point(778, 606);
            this.UI_cmd_CopyAll.Name = "UI_cmd_CopyAll";
            this.UI_cmd_CopyAll.Size = new System.Drawing.Size(110, 27);
            this.UI_cmd_CopyAll.TabIndex = 3;
            this.UI_cmd_CopyAll.Text = "&Copy all";
            this.UI_cmd_CopyAll.UseVisualStyleBackColor = true;
            this.UI_cmd_CopyAll.Click += new System.EventHandler(this.UI_cmd_CopyAll_Click);
            //
            // UI_cmd_Close
            //
            this.UI_cmd_Close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_cmd_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.UI_cmd_Close.Location = new System.Drawing.Point(894, 606);
            this.UI_cmd_Close.Name = "UI_cmd_Close";
            this.UI_cmd_Close.Size = new System.Drawing.Size(94, 27);
            this.UI_cmd_Close.TabIndex = 4;
            this.UI_cmd_Close.Text = "Close";
            this.UI_cmd_Close.UseVisualStyleBackColor = true;
            this.UI_cmd_Close.Click += new System.EventHandler(this.UI_cmd_Close_Click);
            //
            // frmMissingLog
            //
            this.CancelButton = this.UI_cmd_Close;
            this.ClientSize = new System.Drawing.Size(1000, 645);
            this.Controls.Add(this.UI_lbl_Summary);
            this.Controls.Add(this.UI_pnl_Split);
            this.Controls.Add(this.UI_prg_Load);
            this.Controls.Add(this.UI_cmd_CopyAll);
            this.Controls.Add(this.UI_cmd_Close);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(760, 420);
            this.Name = "frmMissingLog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "AI check - missing and changed lines";
            this.Shown += new System.EventHandler(this.frmMissingLog_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMissingLog_FormClosing);
            this.UI_pnl_Split.Panel1.ResumeLayout(false);
            this.UI_pnl_Split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.UI_pnl_Split)).EndInit();
            this.UI_pnl_Split.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
