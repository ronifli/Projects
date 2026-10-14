// MainForm - Telerik RadGridView of monday.com tasks with grouping, filtering, sorting, SW Duration sum,
// and double-click to open the task in monday.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls.Data;
using Telerik.WinControls.UI;

namespace MondayApp
{
    class MainForm : Form
    {
        static readonly string[] Columns = { "Task", "SW Eng", "SW TT", "SW Duration", "RD Status", "is TT?", "Team", "R&D Priority", "Customer", "Ready For Release", "R&D DD" };
        static readonly string[] NumericColumns = { "SW Duration", "R&D Priority" }; // sorted / summed as numbers
        static readonly string[] DateColumns = { "Ready For Release", "R&D DD" }; // sorted as dates, shown as yyyy-MM-dd
        static readonly string SettingsFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MondayApp", "window.txt");
        static readonly string TasksFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MondayApp", "tasks.json");
        static readonly System.Web.Script.Serialization.JavaScriptSerializer json =
            new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        static readonly string LayoutFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MondayApp", "grid.xml");

        readonly RadGridView grid = new RadGridView();
        readonly ToolStripStatusLabel status = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        readonly ToolStripButton refresh = new ToolStripButton("Refresh");
        readonly DataTable table = new DataTable();

        public MainForm()
        {
            Text = "Monday Reports";
            Size = new Size(1200, 750);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F);
            using (var s = typeof(MainForm).Assembly.GetManifestResourceStream("MondayApp.ico")) Icon = new Icon(s);
            LoadWindowSettings();
            FormClosing += (s, e) => { SaveWindowSettings(); SaveGridLayout(); };

            foreach (var c in Columns)
                table.Columns.Add(c, NumericColumns.Contains(c) ? typeof(double) : DateColumns.Contains(c) ? typeof(DateTime) : typeof(string));
            table.Columns.Add("Url");

            grid.Dock = DockStyle.Fill;
            grid.Font = Font;
            grid.ReadOnly = true;
            grid.AllowAddNewRow = false;
            grid.AllowDeleteRow = false;
            grid.ShowRowHeaderColumn = false;
            grid.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = GridViewSelectionMode.FullRowSelect;
            grid.EnableGrouping = true;          // drag a column header to the group panel
            grid.ShowGroupPanel = true;
            grid.EnableFiltering = true;          // filter row under the headers...
            grid.ShowHeaderCellButtons = true;    // ...and Excel-like filter popup on each header
            grid.EnableSorting = true;
            grid.MasterTemplate.ShowTotals = true;
            grid.MasterTemplate.ShowParentGroupSummaries = true;
            grid.DataSource = table;
            Load += (s, e) => SetupColumns(); // RadGridView creates its columns once the control exists

            grid.CellDoubleClick += Grid_CellDoubleClick;
            grid.FilterChanged += (s, e) => UpdateCount();
            grid.GroupSummaryEvaluate += (s, e) =>
            {
                // SW Duration is in hours; also show days (8 hours a day)
                if (e.SummaryItem.Name != "SW Duration" || !(e.Value is IConvertible)) return;
                double hours = Convert.ToDouble(e.Value);
                e.FormatString = "Σ " + hours.ToString("0.##") + " (" + (hours / 8).ToString("0.#") + " days)";
            };

            var statusStrip = new StatusStrip();
            statusStrip.Items.Add(status);
            statusStrip.Items.Add(refresh);
            refresh.Click += (s, e) => LoadData();

            Controls.Add(grid);
            Controls.Add(statusStrip);
            Shown += (s, e) => { ShowCachedTasks(); LoadData(); };
        }

        void SetupColumns()
        {
            grid.Columns["Url"].IsVisible = false;
            grid.Columns["Task"].Width = 400;
            foreach (var c in DateColumns) grid.Columns[c].FormatString = "{0:yyyy-MM-dd}";
            LoadGridLayout();

            // Sum of SW Duration at the bottom of the grid and of every group (follows filtering)
            grid.MasterTemplate.SummaryRowsBottom.Clear(); // the saved layout may carry its own copy
            var sum = new GridViewSummaryItem("SW Duration", "Σ {0:0.##}", GridAggregateFunction.Sum);
            grid.MasterTemplate.SummaryRowsBottom.Add(new GridViewSummaryRowItem(new[] { sum }));
            grid.MasterTemplate.BottomPinnedRowsMode = GridViewBottomPinnedRowsMode.Fixed;
            grid.MasterView.SummaryRows[0].PinPosition = PinnedRowPosition.Bottom; // total always visible
        }

        // Empty or non-numeric text -> DBNull
        static object Number(string s)
        {
            double v;
            return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)
                ? (object)v : DBNull.Value;
        }

        // monday date text, e.g. "Oct 01, 2026" or "2026-10-01" -> DateTime; otherwise DBNull
        static object Date(string s)
        {
            DateTime d;
            return DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out d)
                ? (object)d : DBNull.Value;
        }

        void UpdateCount()
        {
            int shown = grid.MasterTemplate.DataView.ItemCount;
            status.Text = shown + " of " + table.Rows.Count + " tasks   (double-click a row to open it in monday)";
        }

        // grid.xml: Telerik layout - sorting, filters, grouping, column order/widths
        void LoadGridLayout()
        {
            if (!System.IO.File.Exists(LayoutFile)) return;
            try { grid.LoadLayout(LayoutFile); }
            catch (Exception) { try { System.IO.File.Delete(LayoutFile); } catch (Exception) { } } // incompatible layout: start fresh
        }

        void SaveGridLayout()
        {
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LayoutFile));
                grid.SaveLayout(LayoutFile);
            }
            catch (Exception) { }
        }

        // window.txt: Left,Top,Width,Height,Maximized
        void LoadWindowSettings()
        {
            try
            {
                var p = System.IO.File.ReadAllText(SettingsFile).Split(',').Select(int.Parse).ToArray();
                var bounds = new Rectangle(p[0], p[1], p[2], p[3]);
                if (!Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(bounds))) return; // monitor gone
                StartPosition = FormStartPosition.Manual;
                Bounds = bounds;
                if (p[4] == 1) WindowState = FormWindowState.Maximized;
            }
            catch (Exception) { /* first run or bad file: keep defaults */ }
        }

        void SaveWindowSettings()
        {
            try
            {
                var b = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SettingsFile));
                System.IO.File.WriteAllText(SettingsFile, string.Join(",", b.Left, b.Top, b.Width, b.Height,
                    WindowState == FormWindowState.Maximized ? 1 : 0));
            }
            catch (Exception) { }
        }

        // Shows the tasks saved by the last successful load, so the grid is usable while monday is queried
        void ShowCachedTasks()
        {
            try
            {
                var tasks = json.Deserialize<List<MondayTask>>(System.IO.File.ReadAllText(TasksFile));
                FillTable(tasks);
                status.Text = table.Rows.Count + " tasks from " + System.IO.File.GetLastWriteTime(TasksFile).ToString("yyyy-MM-dd HH:mm")
                              + " (cached) - updating from monday...";
            }
            catch (Exception) { /* no cache yet */ }
        }

        void FillTable(List<MondayTask> tasks)
        {
            grid.BeginUpdate();
            table.BeginLoadData();
            table.Rows.Clear();
            foreach (var t in tasks)
                table.Rows.Add(t.Task, t.Eng, t.TT, Number(t.Duration), t.Status, t.IsTT, t.Team, Number(t.Priority),
                               t.Customer, Date(t.ReadyForRelease), Date(t.RdDD), t.Url);
            table.EndLoadData();
            grid.EndUpdate();
        }

        async void LoadData()
        {
            refresh.Enabled = false;
            bool haveCache = table.Rows.Count > 0;
            if (!haveCache) status.Text = "Loading from monday...";
            try
            {
                var tasks = await Task.Run(() => new MondayClient().FetchTasks(msg => BeginInvoke((Action)(() => { if (!haveCache) status.Text = msg; }))));
                FillTable(tasks);
                UpdateCount();
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TasksFile));
                    System.IO.File.WriteAllText(TasksFile, json.Serialize(tasks));
                }
                catch (Exception) { }
            }
            catch (Exception ex)
            {
                status.Text = "Error: " + ex.Message + (haveCache ? "   (showing cached tasks)" : "");
                if (!haveCache) MessageBox.Show(this, ex.Message, "Monday", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { refresh.Enabled = true; }
        }

        void Grid_CellDoubleClick(object sender, GridViewCellEventArgs e)
        {
            var row = e.Row as GridViewDataRowInfo; // ignore header, filter, group and summary rows
            if (row == null) return;
            var url = row.Cells["Url"].Value as string;
            if (!string.IsNullOrEmpty(url)) Process.Start(url);
        }
    }
}
