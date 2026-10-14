// MondayClient - reads tasks from a monday.com board.
// Config: MondayApp.ini next to the exe (Token, BoardId, *Column titles)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace MondayApp
{
    class MondayTask
    {
        public string Task, Eng, TT, Duration, Status, IsTT, Team, Priority, Customer, ReadyForRelease, RdDD, Url;
    }

    class MondayClient
    {
        string token, boardId, engCol, ttCol, durCol, statusCol, isTTCol, teamCol, priorityCol, customerCol, readyCol, rdDDCol;
        string[] skipStatuses;
        static readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public MondayClient() { LoadConfig(); }

        static readonly string ColumnCacheFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MondayApp", "columns.txt");

        // Fetches tasks using the cached column ids / status rule; if the cache is stale
        // (API error, or a column was renamed) the columns are resolved again and the fetch retried.
        public List<MondayTask> FetchTasks(Action<string> progress)
        {
            string[] cached = ReadColumnCache();
            if (cached != null)
            {
                try
                {
                    var tasks = FetchItems(cached[0], cached[1], progress);
                    if (tasks != null) return tasks;
                }
                catch (Exception) { }
                progress("Board columns changed, refreshing...");
            }
            string fields, rules;
            ResolveColumns(out fields, out rules);
            WriteColumnCache(fields, rules);
            return FetchItems(fields, rules, progress);
        }

        string CacheKey()
        {
            return string.Join("|", boardId, engCol, ttCol, durCol, statusCol, isTTCol, teamCol, priorityCol, customerCol, readyCol, rdDDCol,
                               string.Join(",", skipStatuses));
        }

        // columns.txt: line 1 = config key, line 2 = fields, line 3 = rules
        string[] ReadColumnCache()
        {
            try
            {
                var lines = File.ReadAllLines(ColumnCacheFile);
                return lines.Length >= 3 && lines[0] == CacheKey() ? new[] { lines[1], lines[2] } : null;
            }
            catch (Exception) { return null; }
        }

        void WriteColumnCache(string fields, string rules)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ColumnCacheFile));
                File.WriteAllLines(ColumnCacheFile, new[] { CacheKey(), fields, rules });
            }
            catch (Exception) { }
        }

        void ResolveColumns(out string fields, out string rules)
        {
            // Only fetch the columns we need (several columns may share a title)
            var colData = (Dictionary<string, object>)Query("query { boards(ids: [" + boardId + "]) { columns { id title settings_str } } }")["data"];
            var colBoards = (ArrayList)colData["boards"];
            if (colBoards.Count == 0) throw new Exception("Board " + boardId + " not found.");
            var wanted = new[] { engCol, ttCol, durCol, statusCol, isTTCol, teamCol, priorityCol, customerCol, readyCol, rdDDCol };
            var cols = ((ArrayList)((Dictionary<string, object>)colBoards[0])["columns"]).Cast<Dictionary<string, object>>().ToList();
            var colIds = new List<string>();
            foreach (var w in wanted)
            {
                // Prefer exact-case title (board has both "IS TT?" and "is TT?"), else ignore case
                var match = cols.Where(c => (string)c["title"] == w).ToList();
                if (match.Count == 0) match = cols.Where(c => Eq((string)c["title"], w)).ToList();
                colIds.AddRange(match.Select(c => "\"" + c["id"] + "\""));
            }
            if (colIds.Count == 0) throw new Exception("None of the columns " + string.Join(", ", wanted) + " were found on the board.");
            fields = "cursor items { name url column_values(ids: [" + string.Join(",", colIds.Distinct()) + "]) { text column { title } ... on MirrorValue { display_value } ... on FormulaValue { display_value } } }";

            // Skip unwanted statuses on the server so they are never downloaded
            rules = "";
            var statusColumn = cols.FirstOrDefault(c => Eq((string)c["title"], statusCol));
            if (statusColumn != null && skipStatuses.Length > 0)
            {
                // Status rules take label indexes (from settings_str "labels": {"1":"Released",...}), not label text
                var settings = json.Deserialize<Dictionary<string, object>>((statusColumn["settings_str"] as string) ?? "{}");
                var labels = settings.ContainsKey("labels") ? (Dictionary<string, object>)settings["labels"] : new Dictionary<string, object>();
                var indexes = labels.Where(l => skipStatuses.Any(s => Eq(s, l.Value as string ?? ""))).Select(l => l.Key).ToList();
                if (indexes.Count > 0)
                    rules = ", query_params: { rules: [{ column_id: \"" + statusColumn["id"] + "\", compare_value: ["
                            + string.Join(",", indexes) + "], operator: not_any_of }] }";
            }
        }

        // Returns null if a returned column title no longer matches the configured titles (stale cache)
        List<MondayTask> FetchItems(string fields, string rules, Action<string> progress)
        {
            var wanted = new[] { engCol, ttCol, durCol, statusCol, isTTCol, teamCol, priorityCol, customerCol, readyCol, rdDDCol };
            var result = new List<MondayTask>();
            string cursor = null;
            do
            {
                string q = cursor == null
                    ? "query { boards(ids: [" + boardId + "]) { items_page(limit: 500" + rules + ") { " + fields + " } } }"
                    : "query { next_items_page(limit: 500, cursor: \"" + cursor + "\") { " + fields + " } }";
                var data = (Dictionary<string, object>)Query(q)["data"];
                Dictionary<string, object> page = cursor == null
                    ? (Dictionary<string, object>)((Dictionary<string, object>)((ArrayList)data["boards"])[0])["items_page"]
                    : (Dictionary<string, object>)data["next_items_page"];

                foreach (Dictionary<string, object> item in (ArrayList)page["items"])
                {
                    var t = new MondayTask { Task = item["name"] as string, Url = item["url"] as string };
                    foreach (Dictionary<string, object> cv in (ArrayList)item["column_values"])
                    {
                        string title = (string)((Dictionary<string, object>)cv["column"])["title"];
                        if (!wanted.Any(w => Eq(title, w))) return null;
                        string text = cv["text"] as string;
                        if (string.IsNullOrWhiteSpace(text) && cv.ContainsKey("display_value")) text = cv["display_value"] as string;
                        if (string.IsNullOrWhiteSpace(text)) continue; // keep the first non-empty of same-titled columns
                        if (Eq(title, engCol) && t.Eng == null) t.Eng = text;
                        else if (Eq(title, ttCol) && t.TT == null) t.TT = text;
                        else if (Eq(title, durCol) && t.Duration == null) t.Duration = text;
                        else if (Eq(title, statusCol) && t.Status == null) t.Status = text;
                        else if (Eq(title, isTTCol) && t.IsTT == null) t.IsTT = text;
                        else if (Eq(title, teamCol) && t.Team == null) t.Team = text;
                        else if (Eq(title, priorityCol) && t.Priority == null) t.Priority = text;
                        else if (Eq(title, customerCol) && t.Customer == null) t.Customer = text;
                        else if (Eq(title, readyCol) && t.ReadyForRelease == null) t.ReadyForRelease = text;
                        else if (Eq(title, rdDDCol) && t.RdDD == null) t.RdDD = text;
                    }
                    result.Add(t);
                }
                progress("Loaded " + result.Count + " tasks...");
                cursor = page["cursor"] as string;
            } while (!string.IsNullOrEmpty(cursor));
            return result;
        }

        static bool Eq(string a, string b) { return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase); }

        void LoadConfig()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MondayApp.ini");
            if (!File.Exists(path)) throw new Exception("Missing config file: " + path);
            var cfg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(path))
            {
                int i = line.IndexOf('=');
                if (i > 0 && !line.TrimStart().StartsWith("#")) cfg[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }
            token = Get(cfg, "Token", "");
            boardId = Get(cfg, "BoardId", "");
            engCol = Get(cfg, "EngColumn", "SW Eng");
            ttCol = Get(cfg, "TTColumn", "SW TT");
            durCol = Get(cfg, "DurationColumn", "SW Duration");
            statusCol = Get(cfg, "StatusColumn", "RD Status");
            isTTCol = Get(cfg, "IsTTColumn", "is TT?");
            teamCol = Get(cfg, "TeamColumn", "Team");
            priorityCol = Get(cfg, "PriorityColumn", "R&D Priority");
            customerCol = Get(cfg, "CustomerColumn", "Customer");
            readyCol = Get(cfg, "ReadyForReleaseColumn", "Ready For Release");
            rdDDCol = Get(cfg, "RdDDColumn", "R&D DD");
            skipStatuses = Get(cfg, "SkipStatuses", "Abandoned,Done,Released").Split(',')
                               .Select(s => s.Trim()).Where(s => s != "").ToArray();
            if (token == "" || boardId == "") throw new Exception("Token and BoardId must be set in " + path);
        }

        static string Get(Dictionary<string, string> cfg, string key, string def)
        {
            string v;
            return cfg.TryGetValue(key, out v) && v != "" ? v : def;
        }

        Dictionary<string, object> Query(string q)
        {
            using (var wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers[HttpRequestHeader.Authorization] = token;
                wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                wc.Headers["API-Version"] = "2025-04"; // needed for FormulaValue.display_value
                string resp;
                try { resp = wc.UploadString("https://api.monday.com/v2", json.Serialize(new { query = q })); }
                catch (WebException we)
                {
                    string body = we.Response != null ? new StreamReader(we.Response.GetResponseStream()).ReadToEnd() : "";
                    throw new Exception(we.Message + " " + body);
                }
                var r = json.Deserialize<Dictionary<string, object>>(resp);
                if (r.ContainsKey("errors") || r.ContainsKey("error_message")) throw new Exception("API error: " + resp);
                return r;
            }
        }
    }
}
