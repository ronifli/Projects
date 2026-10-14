// MondayFetch - fetches customers and "SW TT" values from a monday.com board.
// Config: MondayFetch.ini next to the exe (Token, BoardId, CustomerColumn, TTColumn, OutputDir)
// Output: <OutputDir>\Customers.txt, <OutputDir>\TT.txt (TT,customer,task,link)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

class MondayFetch
{
    static string token, boardId, customerCol, ttCol, outputDir, runAtText;
    static JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

    // Runs once immediately, then every day at each RunAt time (e.g. 12:30,23:00). Use --once for a single run.
    static int Main(string[] args)
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
        if (args.Any(a => a.Equals("--once", StringComparison.OrdinalIgnoreCase))) return RunOnce();

        while (true)
        {
            RunOnce(); // errors are logged; keep looping
            List<TimeSpan> times;
            try { LoadConfig(); times = runAtText.Split(',').Select(t => TimeSpan.Parse(t.Trim())).ToList(); }
            catch (Exception ex) { Log("Bad config (" + ex.Message + "), using 23:00"); times = new List<TimeSpan> { new TimeSpan(23, 0, 0) }; }
            // Earliest upcoming time today or tomorrow
            DateTime now = DateTime.Now;
            DateTime next = times.Select(t => DateTime.Today + t)
                                 .Select(d => d <= now ? d.AddDays(1) : d)
                                 .Min();
            Log("Next run at " + next.ToString("yyyy-MM-dd HH:mm"));
            Thread.Sleep(next - DateTime.Now);
        }
    }

    static int RunOnce()
    {
        try
        {
            Log("Start");
            LoadConfig();
            Log("Config loaded: board " + boardId + ", output " + outputDir);

            var customers = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var tts = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase); // TT -> task link (first task found)

            // Only fetch the columns we need (several columns may share a title)
            var colData = (Dictionary<string, object>)Query("query { boards(ids: [" + boardId + "]) { columns { id title } } }")["data"];
            var colBoards = (ArrayList)colData["boards"];
            if (colBoards.Count == 0) throw new Exception("Board " + boardId + " not found.");
            var colIds = new List<string>();
            foreach (Dictionary<string, object> col in (ArrayList)((Dictionary<string, object>)colBoards[0])["columns"])
            {
                string title = (string)col["title"];
                if (Eq(title, ttCol) || (customerCol.Length > 0 && Eq(title, customerCol))) colIds.Add("\"" + col["id"] + "\"");
            }
            if (colIds.Count == 0) throw new Exception("Columns '" + ttCol + "' / '" + customerCol + "' not found on board.");
            Log("Columns resolved: " + string.Join(", ", colIds));
            string fields = "cursor items { name url column_values(ids: [" + string.Join(",", colIds) + "]) { text column { title } ... on MirrorValue { display_value } } }";

            string cursor = null;
            int count = 0, pageNo = 0;
            do
            {
                string q = cursor == null
                    ? "query { boards(ids: [" + boardId + "]) { items_page(limit: 500) { " + fields + " } } }"
                    : "query { next_items_page(limit: 500, cursor: \"" + cursor + "\") { " + fields + " } }";

                pageNo++;
                Log("Fetching page " + pageNo + "...");
                var data = (Dictionary<string, object>)Query(q)["data"];
                Dictionary<string, object> page;
                if (cursor == null)
                {
                    var boards = (ArrayList)data["boards"];
                    if (boards.Count == 0) throw new Exception("Board " + boardId + " not found.");
                    page = (Dictionary<string, object>)((Dictionary<string, object>)boards[0])["items_page"];
                }
                else page = (Dictionary<string, object>)data["next_items_page"];

                foreach (Dictionary<string, object> item in (ArrayList)page["items"])
                {
                    count++;
                    string customer = null, tt = null;
                    foreach (Dictionary<string, object> cv in (ArrayList)item["column_values"])
                    {
                        string title = (string)((Dictionary<string, object>)cv["column"])["title"];
                        string text = cv["text"] as string;
                        if (string.IsNullOrWhiteSpace(text) && cv.ContainsKey("display_value")) text = cv["display_value"] as string;
                        if (string.IsNullOrWhiteSpace(text)) continue; // several columns may share a title; keep the first non-empty
                        if (customerCol.Length > 0 && Eq(title, customerCol) && customer == null) customer = text;
                        if (Eq(title, ttCol) && tt == null) tt = text;
                    }
                    if (customerCol.Length == 0) customer = item["name"] as string;
                    if (customer != null)
                        foreach (var c in customer.Split(',')) Add(customers, c);
                    if (!string.IsNullOrWhiteSpace(tt))
                        foreach (var t in tt.Split(',')) AddTT(tts, t, CustomerField(customer), ((item["name"] as string) ?? "").Replace(",", ";").Trim(), item["url"] as string); // multi-value columns are comma separated
                }
                Log("Page " + pageNo + " done: " + ((ArrayList)page["items"]).Count + " items (total " + count + ")");
                cursor = page["cursor"] as string;
            } while (!string.IsNullOrEmpty(cursor));

            Log("Fetch complete: " + count + " items, " + customers.Count + " customers, " + tts.Count + " TTs");
            Directory.CreateDirectory(outputDir);
            File.WriteAllLines(Path.Combine(outputDir, "Customers.txt"), customers, Encoding.UTF8);
            Log("Wrote " + Path.Combine(outputDir, "Customers.txt"));
            File.WriteAllLines(Path.Combine(outputDir, "TT.txt"), tts.Select(kv => kv.Key + "," + kv.Value), Encoding.UTF8);
            Log("Wrote " + Path.Combine(outputDir, "TT.txt"));
            Log("Done");
            return 0;
        }
        catch (Exception ex)
        {
            Log("ERROR: " + ex.Message);
            return 1;
        }
    }

    // Several customers are joined with ';' so they don't break the comma-separated TT line
    static string CustomerField(string customer)
    {
        if (customer == null) return "";
        return string.Join(";", customer.Split(',').Select(c => c.Trim()).Where(c => c.Length > 0));
    }

    static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

    // Prints "[time] (+elapsed) message"
    static void Log(string msg)
    {
        Console.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] (+" + clock.Elapsed.TotalSeconds.ToString("0.0") + "s) " + msg);
    }

    static bool Eq(string a, string b) { return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase); }

    static void Add(SortedSet<string> set, string v)
    {
        if (!string.IsNullOrWhiteSpace(v)) set.Add(v.Trim());
    }

    // Keeps only the 4-digit TT number (e.g. "Bug-TT5208" -> "5208"); values without one (e.g. "S2S") are ignored
    static void AddTT(SortedDictionary<string, string> map, string v, string customer, string task, string url)
    {
        var m = Regex.Match(v, @"(?<!\d)\d{4}(?!\d)");
        if (m.Success && !map.ContainsKey(m.Value)) map[m.Value] = customer + "," + task + "," + url;
    }

    static void LoadConfig()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MondayFetch.ini");
        if (!File.Exists(path)) throw new Exception("Missing config file: " + path);
        var cfg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            int i = line.IndexOf('=');
            if (i > 0 && !line.TrimStart().StartsWith("#")) cfg[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
        }
        cfg.TryGetValue("Token", out token);
        cfg.TryGetValue("BoardId", out boardId);
        if (!cfg.TryGetValue("CustomerColumn", out customerCol)) customerCol = "";
        if (!cfg.TryGetValue("OutputDir", out outputDir) || outputDir == "") outputDir = @"c:\temp";
        if (!cfg.TryGetValue("RunAt", out runAtText) || runAtText == "") runAtText = "23:00";
        if (!cfg.TryGetValue("TTColumn", out ttCol) || ttCol == "") ttCol = "SW TT";
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(boardId)) throw new Exception("Token and BoardId must be set in " + path);
    }

    static Dictionary<string, object> Query(string q)
    {
        using (var wc = new WebClient())
        {
            wc.Encoding = Encoding.UTF8;
            wc.Headers[HttpRequestHeader.Authorization] = token;
            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
            wc.Headers["API-Version"] = "2024-10";
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
