namespace GitHelper
{
    partial class frmInstall
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label UI_lbl_Vs;
        private System.Windows.Forms.ComboBox UI_cmb_Vs;
        private System.Windows.Forms.Label UI_lbl_Detected;
        private System.Windows.Forms.Label UI_lbl_Exe;
        private System.Windows.Forms.TextBox UI_txt_Exe;
        private System.Windows.Forms.Label UI_lbl_Shortcut;
        private System.Windows.Forms.TextBox UI_txt_Shortcut;
        private System.Windows.Forms.CheckBox UI_chk_Shortcut;
        private System.Windows.Forms.CheckBox UI_chk_Toolbar;
        private System.Windows.Forms.Label UI_lbl_Warning;
        private System.Windows.Forms.TextBox UI_txt_Log;
        private System.Windows.Forms.Button UI_cmd_Install;
        private System.Windows.Forms.Button UI_cmd_Close;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.UI_lbl_Vs = new System.Windows.Forms.Label();
            this.UI_cmb_Vs = new System.Windows.Forms.ComboBox();
            this.UI_lbl_Detected = new System.Windows.Forms.Label();
            this.UI_lbl_Exe = new System.Windows.Forms.Label();
            this.UI_txt_Exe = new System.Windows.Forms.TextBox();
            this.UI_lbl_Shortcut = new System.Windows.Forms.Label();
            this.UI_txt_Shortcut = new System.Windows.Forms.TextBox();
            this.UI_chk_Shortcut = new System.Windows.Forms.CheckBox();
            this.UI_chk_Toolbar = new System.Windows.Forms.CheckBox();
            this.UI_lbl_Warning = new System.Windows.Forms.Label();
            this.UI_txt_Log = new System.Windows.Forms.TextBox();
            this.UI_cmd_Install = new System.Windows.Forms.Button();
            this.UI_cmd_Close = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // UI_lbl_Vs
            //
            this.UI_lbl_Vs.Location = new System.Drawing.Point(12, 15);
            this.UI_lbl_Vs.Name = "UI_lbl_Vs";
            this.UI_lbl_Vs.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Vs.TabIndex = 0;
            this.UI_lbl_Vs.Text = "Visual Studio *";
            //
            // UI_cmb_Vs
            // detected installations, newest first; 2026 preselected when present
            //
            this.UI_cmb_Vs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UI_cmb_Vs.Location = new System.Drawing.Point(136, 12);
            this.UI_cmb_Vs.Name = "UI_cmb_Vs";
            this.UI_cmb_Vs.Size = new System.Drawing.Size(380, 24);
            this.UI_cmb_Vs.TabIndex = 1;
            //
            // UI_lbl_Detected
            //
            this.UI_lbl_Detected.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_Detected.Location = new System.Drawing.Point(136, 39);
            this.UI_lbl_Detected.Name = "UI_lbl_Detected";
            this.UI_lbl_Detected.Size = new System.Drawing.Size(500, 20);
            this.UI_lbl_Detected.TabIndex = 2;
            //
            // UI_lbl_Exe
            //
            this.UI_lbl_Exe.Location = new System.Drawing.Point(12, 70);
            this.UI_lbl_Exe.Name = "UI_lbl_Exe";
            this.UI_lbl_Exe.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Exe.TabIndex = 3;
            this.UI_lbl_Exe.Text = "Command";
            //
            // UI_txt_Exe
            //
            this.UI_txt_Exe.Location = new System.Drawing.Point(136, 67);
            this.UI_txt_Exe.Name = "UI_txt_Exe";
            this.UI_txt_Exe.ReadOnly = true;
            this.UI_txt_Exe.Size = new System.Drawing.Size(500, 24);
            this.UI_txt_Exe.TabIndex = 4;
            //
            // UI_lbl_Shortcut
            //
            this.UI_lbl_Shortcut.Location = new System.Drawing.Point(12, 104);
            this.UI_lbl_Shortcut.Name = "UI_lbl_Shortcut";
            this.UI_lbl_Shortcut.Size = new System.Drawing.Size(120, 20);
            this.UI_lbl_Shortcut.TabIndex = 5;
            this.UI_lbl_Shortcut.Text = "Shortcut";
            //
            // UI_txt_Shortcut
            // in the spelling VS itself writes into a .vssettings file
            //
            this.UI_txt_Shortcut.Location = new System.Drawing.Point(136, 101);
            this.UI_txt_Shortcut.Name = "UI_txt_Shortcut";
            this.UI_txt_Shortcut.Size = new System.Drawing.Size(140, 24);
            this.UI_txt_Shortcut.TabIndex = 6;
            this.UI_txt_Shortcut.Text = "Alt+Num -";
            //
            // UI_chk_Shortcut
            //
            this.UI_chk_Shortcut.Checked = true;
            this.UI_chk_Shortcut.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UI_chk_Shortcut.Location = new System.Drawing.Point(136, 131);
            this.UI_chk_Shortcut.Name = "UI_chk_Shortcut";
            this.UI_chk_Shortcut.Size = new System.Drawing.Size(500, 24);
            this.UI_chk_Shortcut.TabIndex = 7;
            this.UI_chk_Shortcut.Text = "Bind the keyboard shortcut  (starts Visual Studio twice, ~1-2 minutes)";
            this.UI_chk_Shortcut.UseVisualStyleBackColor = true;
            //
            // UI_chk_Toolbar
            //
            this.UI_chk_Toolbar.Checked = true;
            this.UI_chk_Toolbar.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UI_chk_Toolbar.Location = new System.Drawing.Point(136, 157);
            this.UI_chk_Toolbar.Name = "UI_chk_Toolbar";
            this.UI_chk_Toolbar.Size = new System.Drawing.Size(500, 24);
            this.UI_chk_Toolbar.TabIndex = 8;
            this.UI_chk_Toolbar.Text = "Add a button to the Standard toolbar";
            this.UI_chk_Toolbar.UseVisualStyleBackColor = true;
            //
            // UI_lbl_Warning
            //
            this.UI_lbl_Warning.ForeColor = System.Drawing.Color.Firebrick;
            this.UI_lbl_Warning.Location = new System.Drawing.Point(12, 187);
            this.UI_lbl_Warning.Name = "UI_lbl_Warning";
            this.UI_lbl_Warning.Size = new System.Drawing.Size(624, 20);
            this.UI_lbl_Warning.TabIndex = 9;
            this.UI_lbl_Warning.Text = "Visual Studio must be closed before installing.";
            //
            // UI_txt_Log
            //
            this.UI_txt_Log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_txt_Log.BackColor = System.Drawing.Color.WhiteSmoke;
            this.UI_txt_Log.Font = new System.Drawing.Font("Consolas", 9F);
            this.UI_txt_Log.Location = new System.Drawing.Point(12, 211);
            this.UI_txt_Log.Multiline = true;
            this.UI_txt_Log.Name = "UI_txt_Log";
            this.UI_txt_Log.ReadOnly = true;
            this.UI_txt_Log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.UI_txt_Log.Size = new System.Drawing.Size(624, 240);
            this.UI_txt_Log.TabIndex = 10;
            this.UI_txt_Log.WordWrap = false;
            //
            // UI_cmd_Install
            //
            this.UI_cmd_Install.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left)));
            this.UI_cmd_Install.Location = new System.Drawing.Point(12, 461);
            this.UI_cmd_Install.Name = "UI_cmd_Install";
            this.UI_cmd_Install.Size = new System.Drawing.Size(130, 30);
            this.UI_cmd_Install.TabIndex = 11;
            this.UI_cmd_Install.Text = "&Install";
            this.UI_cmd_Install.UseVisualStyleBackColor = true;
            this.UI_cmd_Install.Click += new System.EventHandler(this.UI_cmd_Install_Click);
            //
            // UI_cmd_Close
            //
            this.UI_cmd_Close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right)));
            this.UI_cmd_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.UI_cmd_Close.Location = new System.Drawing.Point(552, 461);
            this.UI_cmd_Close.Name = "UI_cmd_Close";
            this.UI_cmd_Close.Size = new System.Drawing.Size(84, 30);
            this.UI_cmd_Close.TabIndex = 12;
            this.UI_cmd_Close.Text = "Close";
            this.UI_cmd_Close.UseVisualStyleBackColor = true;
            //
            // frmInstall
            //

            this.CancelButton = this.UI_cmd_Close;
            this.ClientSize = new System.Drawing.Size(648, 503);
            this.Controls.Add(this.UI_lbl_Vs);
            this.Controls.Add(this.UI_cmb_Vs);
            this.Controls.Add(this.UI_lbl_Detected);
            this.Controls.Add(this.UI_lbl_Exe);
            this.Controls.Add(this.UI_txt_Exe);
            this.Controls.Add(this.UI_lbl_Shortcut);
            this.Controls.Add(this.UI_txt_Shortcut);
            this.Controls.Add(this.UI_chk_Shortcut);
            this.Controls.Add(this.UI_chk_Toolbar);
            this.Controls.Add(this.UI_lbl_Warning);
            this.Controls.Add(this.UI_txt_Log);
            this.Controls.Add(this.UI_cmd_Install);
            this.Controls.Add(this.UI_cmd_Close);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(600, 420);
            this.Name = "frmInstall";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Install VS Plugin";
            this.Shown += new System.EventHandler(this.frmInstall_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
