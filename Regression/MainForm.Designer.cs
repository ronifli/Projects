namespace Regression
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.mnuActions = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuBuild = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuClean = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuRestore = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.mnuTestEmail = new System.Windows.Forms.ToolStripMenuItem();
            this.bar = new System.Windows.Forms.FlowLayoutPanel();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnLog = new System.Windows.Forms.Button();
            this.chkDevenv = new System.Windows.Forms.CheckBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.logBox = new System.Windows.Forms.RichTextBox();
            this.split = new System.Windows.Forms.SplitContainer();
            this.stateBox = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.bar.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // menuStrip
            //
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.mnuActions });
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.TabIndex = 3;
            //
            // mnuActions
            //
            this.mnuActions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuBuild, this.mnuClean, this.mnuRestore, this.mnuSeparator, this.mnuTestEmail });
            this.mnuActions.Name = "mnuActions";
            this.mnuActions.Text = "&Actions";
            //
            // mnuBuild
            //
            this.mnuBuild.Name = "mnuBuild";
            this.mnuBuild.Text = "&Build Only";
            this.mnuBuild.Click += new System.EventHandler(this.mnuBuild_Click);
            //
            // mnuClean
            //
            this.mnuClean.Name = "mnuClean";
            this.mnuClean.Text = "&Clean Only";
            this.mnuClean.Click += new System.EventHandler(this.mnuClean_Click);
            //
            // mnuRestore
            //
            this.mnuRestore.Name = "mnuRestore";
            this.mnuRestore.Text = "&Git Restore Only";
            this.mnuRestore.Click += new System.EventHandler(this.mnuRestore_Click);
            //
            // mnuTestEmail
            //
            this.mnuTestEmail.Name = "mnuTestEmail";
            this.mnuTestEmail.Text = "Send &Test Email";
            this.mnuTestEmail.Click += new System.EventHandler(this.mnuTestEmail_Click);
            //
            // bar
            //
            this.bar.Controls.Add(this.btnStart);
            this.bar.Controls.Add(this.btnStop);
            this.bar.Controls.Add(this.btnLog);
            this.bar.Controls.Add(this.chkDevenv);
            this.bar.Dock = System.Windows.Forms.DockStyle.Top;
            this.bar.Name = "bar";
            this.bar.Padding = new System.Windows.Forms.Padding(4);
            this.bar.Size = new System.Drawing.Size(984, 40);
            this.bar.TabIndex = 0;
            //
            // btnStart
            //
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(120, 28);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "Start Regression";
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            //
            // btnStop
            //
            this.btnStop.Enabled = false;
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(80, 28);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop";
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            //
            // btnLog
            //
            this.btnLog.Name = "btnLog";
            this.btnLog.Size = new System.Drawing.Size(80, 28);
            this.btnLog.TabIndex = 2;
            this.btnLog.Text = "Show Log";
            this.btnLog.Click += new System.EventHandler(this.btnLog_Click);
            //
            // chkDevenv
            //
            this.chkDevenv.AutoSize = true;
            this.chkDevenv.Checked = true;
            this.chkDevenv.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDevenv.Name = "chkDevenv";
            this.chkDevenv.Padding = new System.Windows.Forms.Padding(10, 4, 0, 0);
            this.chkDevenv.TabIndex = 3;
            this.chkDevenv.Text = "Use devenv (else MSBuild)";
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.lblStatus });
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.TabIndex = 2;
            //
            // lblStatus
            //
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "Idle";
            //
            // logBox
            //
            this.logBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.logBox.TabIndex = 1;
            this.logBox.WordWrap = false;
            //
            // stateBox
            //
            this.stateBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stateBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.stateBox.Name = "stateBox";
            this.stateBox.ReadOnly = true;
            this.stateBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.stateBox.TabIndex = 0;
            this.stateBox.WordWrap = false;
            //
            // split
            //
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Name = "split";
            this.split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.split.Panel1.Controls.Add(this.logBox);
            this.split.Panel2.Controls.Add(this.stateBox);
            this.split.Size = new System.Drawing.Size(984, 520);
            this.split.SplitterDistance = 360;
            this.split.TabIndex = 1;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 611);
            this.Controls.Add(this.split);
            this.Controls.Add(this.bar);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.statusStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.Text = "Regression";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.bar.ResumeLayout(false);
            this.bar.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem mnuActions;
        private System.Windows.Forms.ToolStripMenuItem mnuBuild;
        private System.Windows.Forms.ToolStripMenuItem mnuClean;
        private System.Windows.Forms.ToolStripMenuItem mnuRestore;
        private System.Windows.Forms.ToolStripSeparator mnuSeparator;
        private System.Windows.Forms.ToolStripMenuItem mnuTestEmail;
        private System.Windows.Forms.FlowLayoutPanel bar;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnLog;
        private System.Windows.Forms.CheckBox chkDevenv;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.RichTextBox logBox;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.RichTextBox stateBox;
    }
}
