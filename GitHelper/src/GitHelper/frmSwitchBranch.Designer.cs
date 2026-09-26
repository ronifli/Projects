namespace GitHelper
{
    partial class frmSwitchBranch
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label UI_lbl_RepoCaption;
        private System.Windows.Forms.Label UI_lbl_Repo;
        private System.Windows.Forms.Label UI_lbl_FilterCaption;
        private System.Windows.Forms.TextBox UI_txt_Filter;
        private System.Windows.Forms.Button UI_cmd_Fetch;
        private System.Windows.Forms.ListBox UI_lst_Branches;
        private System.Windows.Forms.Label UI_lbl_Count;
        private System.Windows.Forms.Label UI_lbl_Dirty;
        private System.Windows.Forms.Button UI_cmd_Switch;
        private System.Windows.Forms.Button UI_cmd_Cancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.UI_lbl_RepoCaption = new System.Windows.Forms.Label();
            this.UI_lbl_Repo = new System.Windows.Forms.Label();
            this.UI_lbl_FilterCaption = new System.Windows.Forms.Label();
            this.UI_txt_Filter = new System.Windows.Forms.TextBox();
            this.UI_cmd_Fetch = new System.Windows.Forms.Button();
            this.UI_lst_Branches = new System.Windows.Forms.ListBox();
            this.UI_lbl_Count = new System.Windows.Forms.Label();
            this.UI_lbl_Dirty = new System.Windows.Forms.Label();
            this.UI_cmd_Switch = new System.Windows.Forms.Button();
            this.UI_cmd_Cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // UI_lbl_RepoCaption
            //
            this.UI_lbl_RepoCaption.Location = new System.Drawing.Point(12, 15);
            this.UI_lbl_RepoCaption.Name = "UI_lbl_RepoCaption";
            this.UI_lbl_RepoCaption.Size = new System.Drawing.Size(100, 20);
            this.UI_lbl_RepoCaption.TabIndex = 0;
            this.UI_lbl_RepoCaption.Text = "Repository";
            //
            // UI_lbl_Repo
            //
            this.UI_lbl_Repo.Location = new System.Drawing.Point(116, 15);
            this.UI_lbl_Repo.Name = "UI_lbl_Repo";
            this.UI_lbl_Repo.Size = new System.Drawing.Size(420, 20);
            this.UI_lbl_Repo.TabIndex = 1;
            //
            // UI_lbl_FilterCaption
            //
            this.UI_lbl_FilterCaption.Location = new System.Drawing.Point(12, 48);
            this.UI_lbl_FilterCaption.Name = "UI_lbl_FilterCaption";
            this.UI_lbl_FilterCaption.Size = new System.Drawing.Size(100, 20);
            this.UI_lbl_FilterCaption.TabIndex = 2;
            this.UI_lbl_FilterCaption.Text = "Search";
            //
            // UI_txt_Filter
            // focused on open: typing filters, Down moves into the list,
            // Enter switches to whatever is selected
            //
            this.UI_txt_Filter.Location = new System.Drawing.Point(116, 45);
            this.UI_txt_Filter.Name = "UI_txt_Filter";
            this.UI_txt_Filter.Size = new System.Drawing.Size(330, 24);
            this.UI_txt_Filter.TabIndex = 3;
            this.UI_txt_Filter.TextChanged += new System.EventHandler(this.UI_txt_Filter_TextChanged);
            this.UI_txt_Filter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.UI_txt_Filter_KeyDown);
            //
            // UI_cmd_Fetch
            //
            this.UI_cmd_Fetch.Location = new System.Drawing.Point(456, 44);
            this.UI_cmd_Fetch.Name = "UI_cmd_Fetch";
            this.UI_cmd_Fetch.Size = new System.Drawing.Size(80, 26);
            this.UI_cmd_Fetch.TabIndex = 4;
            this.UI_cmd_Fetch.Text = "&Fetch";
            this.UI_cmd_Fetch.UseVisualStyleBackColor = true;
            this.UI_cmd_Fetch.Click += new System.EventHandler(this.UI_cmd_Fetch_Click);
            //
            // UI_lst_Branches
            //
            this.UI_lst_Branches.IntegralHeight = false;
            this.UI_lst_Branches.ItemHeight = 17;
            this.UI_lst_Branches.Location = new System.Drawing.Point(116, 78);
            this.UI_lst_Branches.Name = "UI_lst_Branches";
            this.UI_lst_Branches.Size = new System.Drawing.Size(420, 260);
            this.UI_lst_Branches.TabIndex = 5;
            this.UI_lst_Branches.DoubleClick += new System.EventHandler(this.UI_lst_Branches_DoubleClick);
            this.UI_lst_Branches.KeyDown += new System.Windows.Forms.KeyEventHandler(this.UI_lst_Branches_KeyDown);
            //
            // UI_lbl_Count
            //
            this.UI_lbl_Count.ForeColor = System.Drawing.Color.DimGray;
            this.UI_lbl_Count.Location = new System.Drawing.Point(116, 344);
            this.UI_lbl_Count.Name = "UI_lbl_Count";
            this.UI_lbl_Count.Size = new System.Drawing.Size(420, 20);
            this.UI_lbl_Count.TabIndex = 6;
            //
            // UI_lbl_Dirty
            //
            this.UI_lbl_Dirty.ForeColor = System.Drawing.Color.Firebrick;
            this.UI_lbl_Dirty.Location = new System.Drawing.Point(12, 370);
            this.UI_lbl_Dirty.Name = "UI_lbl_Dirty";
            this.UI_lbl_Dirty.Size = new System.Drawing.Size(524, 36);
            this.UI_lbl_Dirty.TabIndex = 7;
            //
            // UI_cmd_Switch
            //
            this.UI_cmd_Switch.Location = new System.Drawing.Point(116, 412);
            this.UI_cmd_Switch.Name = "UI_cmd_Switch";
            this.UI_cmd_Switch.Size = new System.Drawing.Size(110, 30);
            this.UI_cmd_Switch.TabIndex = 8;
            this.UI_cmd_Switch.Text = "&Switch";
            this.UI_cmd_Switch.UseVisualStyleBackColor = true;
            this.UI_cmd_Switch.Click += new System.EventHandler(this.UI_cmd_Switch_Click);
            //
            // UI_cmd_Cancel
            //
            this.UI_cmd_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.UI_cmd_Cancel.Location = new System.Drawing.Point(234, 412);
            this.UI_cmd_Cancel.Name = "UI_cmd_Cancel";
            this.UI_cmd_Cancel.Size = new System.Drawing.Size(90, 30);
            this.UI_cmd_Cancel.TabIndex = 9;
            this.UI_cmd_Cancel.Text = "Cancel";
            this.UI_cmd_Cancel.UseVisualStyleBackColor = true;
            //
            // frmSwitchBranch
            //

            this.CancelButton = this.UI_cmd_Cancel;
            this.ClientSize = new System.Drawing.Size(548, 456);
            this.Controls.Add(this.UI_lbl_RepoCaption);
            this.Controls.Add(this.UI_lbl_Repo);
            this.Controls.Add(this.UI_lbl_FilterCaption);
            this.Controls.Add(this.UI_txt_Filter);
            this.Controls.Add(this.UI_cmd_Fetch);
            this.Controls.Add(this.UI_lst_Branches);
            this.Controls.Add(this.UI_lbl_Count);
            this.Controls.Add(this.UI_lbl_Dirty);
            this.Controls.Add(this.UI_cmd_Switch);
            this.Controls.Add(this.UI_cmd_Cancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSwitchBranch";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Switch Branch";
            this.Shown += new System.EventHandler(this.frmSwitchBranch_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
