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
        static readonly string[] DateColumns = { "Ready For Release", "R&D DD" }; // sorted as dates, shown as dd-MM-yyyy
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

        // Extra monday columns chosen by the user (column ids); grid column name is ExtraPrefix + id
        const string ExtraPrefix = "m_";
        const string DateFormat = "{0:dd-MM-yyyy}";
        static readonly string ExtraColumnsFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MondayApp", "extra_columns.txt");
        List<string> extraIds = new List<string>();

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
            try { extraIds = System.IO.File.ReadAllLines(ExtraColumnsFile).Where(l => l.Trim() != "").ToList(); } catch (Exception) { }
            foreach (var id in extraIds) table.Columns.Add(ExtraPrefix + id, typeof(object)); // text, or DateTime when the column holds dates

            grid.Dock = DockStyle.Fill;
            grid.Font = Font;
            grid.ReadOnly = true;
            grid.AllowAddNewRow = false;
            grid.AllowDeleteRow = false;
            grid.ShowRowHeaderColumn = false;
            grid.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.None; // columns keep their own width (fit to content)
            grid.HorizontalScrollState = ScrollState.AlwaysShow;
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
            grid.ColumnChooserCreated += (s, e) => AddMondayColumnsButton(e.ColumnChooser);
            grid.ContextMenuOpening += Grid_ContextMenuOpening;
            // Extra monday columns are typed object, and RadGridView ignores FormatString there - so dates are formatted here
            grid.CellFormatting += (s, e) =>
            {
                if (e.CellElement.Value is DateTime) e.CellElement.Text = ((DateTime)e.CellElement.Value).ToString("dd-MM-yyyy");
            };
            grid.FilterChanged += (s, e) => UpdateCount();
            grid.GroupSummaryEvaluate += (s, e) =>
            {
                // Group header (its summary item has no aggregate, unlike the SW Duration total row):
                // "<value>  -  12 tasks, SW Duration 34 days"
                if (e.Group != null && (e.SummaryItem == null || e.SummaryItem.Aggregate == GridAggregateFunction.None))
                {
                    double sum = GroupDuration(e.Group);
                    e.FormatString = string.Format("{0}   -   {1} task{2}, SW Duration {3:0.##} days",
                        e.Value == null || e.Value is DBNull || e.Value.ToString().Trim() == "" ? "(empty)" : e.Value, e.Group.ItemCount, e.Group.ItemCount == 1 ? "" : "s", sum).Replace("{", "{{").Replace("}", "}}");
                }
            };

            var statusStrip = new StatusStrip();
            statusStrip.Items.Add(status);
            statusStrip.Items.Add(refresh);
            refresh.Click += (s, e) => LoadData();

            Controls.Add(grid);
            Controls.Add(statusStrip);
            Shown += (s, e) => { ShowCachedTasks(); LoadData(); };
        }

        void SetupColumns() { SetupColumns(LoadGridLayout); }

        // loadLayout restores the user's sorting/filters/grouping/widths; afterwards the grid's columns are
        // matched to the table's, since the layout may predate added or removed monday columns
        void SetupColumns(Action loadLayout)
        {
            grid.Columns["Url"].IsVisible = false;
            grid.Columns["Task"].Width = 400;
            loadLayout();
            grid.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.None; // layouts saved before fit-to-content had Fill
            grid.HorizontalScrollState = ScrollState.AlwaysShow;

            var seen = new HashSet<string>();
            foreach (var col in grid.Columns.ToList()) // drop columns of removed fields, and duplicates of a field
                if (!table.Columns.Contains(col.FieldName ?? col.Name) || !seen.Add(col.FieldName ?? col.Name))
                    grid.Columns.Remove(col);
            foreach (DataColumn dc in table.Columns)
                if (!grid.Columns.Any(c => (c.FieldName ?? c.Name) == dc.ColumnName))
                    grid.Columns.Add(new GridViewTextBoxColumn(dc.ColumnName, dc.ColumnName));
            // After the layout, which may carry an older format. Extra columns: only their DateTime values are affected.
            foreach (var c in DateColumns.Concat(extraIds.Select(id => ExtraPrefix + id)))
                if (grid.Columns.Contains(c)) grid.Columns[c].FormatString = DateFormat;
            SetExtraHeaders();
            grid.Columns["Url"].IsVisible = false;
            grid.Columns["Url"].VisibleInColumnChooser = false; // internal: used by double-click only

            // Sum of SW Duration at the bottom of the grid and of every group (follows filtering)
            grid.MasterTemplate.SummaryRowsBottom.Clear(); // the saved layout may carry its own copy
            var sum = new GridViewSummaryItem("SW Duration", "Σ {0:0.##}", GridAggregateFunction.Sum);
            grid.MasterTemplate.SummaryRowsBottom.Add(new GridViewSummaryRowItem(new[] { sum }));
            grid.MasterTemplate.BottomPinnedRowsMode = GridViewBottomPinnedRowsMode.Fixed;
            grid.MasterView.SummaryRows[0].PinPosition = PinnedRowPosition.Bottom; // total always visible
        }

        // Extra columns are named by id; show the monday title as header
        void SetExtraHeaders()
        {
            var titles = MondayClient.LoadBoardColumns().ToDictionary(c => c.Id, c => c.Title);
            foreach (var id in extraIds)
            {
                string title;
                if (grid.Columns.Contains(ExtraPrefix + id) && titles.TryGetValue(id, out title))
                    grid.Columns[ExtraPrefix + id].HeaderText = title;
            }
        }

        // Header right-click: "Freeze column" pins the column to the left so it stays while scrolling sideways.
        // The pinned state is part of the saved grid layout, so it is remembered.
        void Grid_ContextMenuOpening(object sender, ContextMenuOpeningEventArgs e)
        {
            var header = e.ContextMenuProvider as GridHeaderCellElement;
            if (header == null || header.ColumnInfo == null) return;
            var col = header.ColumnInfo;

            var item = new RadMenuItem(col.IsPinned ? "Unfreeze column" : "Freeze column");
            item.Click += (s, a) =>
            {
                if (col.IsPinned) col.IsPinned = false;
                else col.PinPosition = PinnedColumnPosition.Left;
            };
            e.ContextMenu.Items.Insert(0, new RadMenuSeparatorItem());
            e.ContextMenu.Items.Insert(0, item);
        }

        // Adds a "Monday columns..." button to Telerik's column chooser
        void AddMondayColumnsButton(Form chooser)
        {
            var more = new Button { Text = "Monday columns...", Dock = DockStyle.Bottom, Height = 32 };
            more.Click += (s, e) => ChooseMondayColumns(chooser);
            chooser.Controls.Add(more);
        }

        void ChooseMondayColumns(IWin32Window owner)
        {
            var board = MondayClient.LoadBoardColumns();
            if (board.Count == 0)
            {
                MessageBox.Show(owner, "The monday column list hasn't been loaded yet. Try again after the tasks finish loading.", "Monday columns");
                return;
            }
            // The fixed grid columns aren't offered again
            var offered = board.Where(c => !Columns.Any(t => string.Equals(t, c.Title.Trim(), StringComparison.OrdinalIgnoreCase))).ToList();
            using (var f = new MondayColumnsForm(offered, extraIds, Font, Icon))
            {
                if (f.ShowDialog(owner) != DialogResult.OK) return;
                ApplyExtraColumns(f.SelectedIds.ToList());
            }
        }

        void ApplyExtraColumns(List<string> ids)
        {
            var removed = extraIds.Except(ids).ToList();
            var added = ids.Except(extraIds).ToList();
            if (removed.Count == 0 && added.Count == 0) return;

            // RadGridView doesn't follow column removal on a bound DataTable, so rebind around the
            // schema change and carry the user's layout across it
            var layout = new System.IO.MemoryStream();
            grid.SaveLayout(layout);
            grid.DataSource = null;
            foreach (var id in removed) table.Columns.Remove(ExtraPrefix + id);
            foreach (var id in added) table.Columns.Add(ExtraPrefix + id, typeof(object));
            extraIds = extraIds.Except(removed).Concat(added).ToList();
            grid.DataSource = table;
            SetupColumns(() => { layout.Position = 0; try { grid.LoadLayout(layout); } catch (Exception) { } });
            FitColumns();
            UpdateCount();
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ExtraColumnsFile));
                System.IO.File.WriteAllLines(ExtraColumnsFile, extraIds);
            }
            catch (Exception) { }
            if (added.Count > 0) LoadData(); // fetch the new columns' values
        }

        // Sum of SW Duration over a group's rows, including nested sub-groups
        static double GroupDuration(Telerik.WinControls.Data.Group<GridViewRowInfo> group)
        {
            if (group.Groups.Count > 0) return group.Groups.Sum(g => GroupDuration(g));
            double sum = 0;
            foreach (GridViewRowInfo row in group)
            {
                var v = row.Cells["SW Duration"].Value;
                if (v is double) sum += (double)v;
            }
            return sum;
        }

        static string ExtraValue(MondayTask t, string id)
        {
            string v;
            return t.Extra != null && t.Extra.TryGetValue(id, out v) ? v : null;
        }

        static bool IsIsoDate(string s)
        {
            DateTime d;
            return DateTime.TryParseExact(s.Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                                          System.Globalization.DateTimeStyles.None, out d);
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
                status.Text = table.Rows.Count + " tasks from " + System.IO.File.GetLastWriteTime(TasksFile).ToString("dd-MM-yyyy HH:mm")
                              + " (cached) - updating from monday...";
            }
            catch (Exception) { /* no cache yet */ }
        }

        void FillTable(List<MondayTask> tasks)
        {
            grid.BeginUpdate();
            table.BeginLoadData();
            table.Rows.Clear();

            // An extra column whose values are all yyyy-MM-dd dates (e.g. a mirrored date) holds DateTime values,
            // so it sorts by date and shows as dd-MM-yyyy; any other column keeps its text
            var dateIds = new HashSet<string>(extraIds.Where(id =>
            {
                var vals = tasks.Select(t => ExtraValue(t, id)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                return vals.Count > 0 && vals.All(IsIsoDate);
            }));

            foreach (var t in tasks)
            {
                var values = new List<object> { t.Task, t.Eng, t.TT, Number(t.Duration), t.Status, t.IsTT, t.Team, Number(t.Priority),
                                                t.Customer, Date(t.ReadyForRelease), Date(t.RdDD), t.Url };
                foreach (var id in extraIds)
                {
                    string v = ExtraValue(t, id);
                    values.Add(v == null ? null : dateIds.Contains(id) ? Date(v) : v);
                }
                table.Rows.Add(values.ToArray());
            }
            table.EndLoadData();
            grid.EndUpdate();
            FitColumns();
        }

        // Sizes every column to its header and content; very long text (e.g. task names) is capped
        void FitColumns()
        {
            grid.BestFitColumns(BestFitColumnMode.AllCells);
            foreach (var col in grid.Columns.Where(c => c.Width > MaxColumnWidth)) col.Width = MaxColumnWidth;
        }
        const int MaxColumnWidth = 450;

        async void LoadData()
        {
            refresh.Enabled = false;
            bool haveCache = table.Rows.Count > 0;
            if (!haveCache) status.Text = "Loading from monday...";
            try
            {
                var ids = extraIds.ToList();
                var tasks = await Task.Run(() => new MondayClient().FetchTasks(ids, msg => BeginInvoke((Action)(() => { if (!haveCache) status.Text = msg; }))));
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
