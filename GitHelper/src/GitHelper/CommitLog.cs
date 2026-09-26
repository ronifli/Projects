using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHelper
{
    /// <summary>One commit as the log window shows it.</summary>
    public sealed class LogEntry
    {
        public string Sha { get; set; }
        public string ShortSha { get; set; }
        public string Date { get; set; }
        public string Author { get; set; }
        public string Message { get; set; }

        /// <summary>
        /// Carries a "Commit Type:" - written by the dialog, or by hand under the
        /// commit-msg hook. Only these get the field columns filled.
        /// </summary>
        public bool IsStructured { get; set; }

        /// <summary>
        /// The parsed fields. For an unstructured commit only Subject (line 1)
        /// and Body are filled - <see cref="CommitMessage.Parse"/> never
        /// invents the rest.
        /// </summary>
        public CommitFields Fields { get; set; }

        public string AreaText { get { return string.Join(", ", Fields.Areas); } }

        /// <summary>Case-insensitive match on everything a row shows, plus the body.</summary>
        public bool Matches(string needle)
        {
            if (string.IsNullOrWhiteSpace(needle)) return true;
            needle = needle.Trim();
            return new[] { Sha, Author, Date, Fields.Subject, Fields.Body, Fields.TT, Fields.Customer,
                           Fields.CommitType, Fields.Risk, AreaText }
                .Any(s => !string.IsNullOrEmpty(s) && s.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }

    /// <summary>
    /// Reads the history of the checked-out branch for Git ▸ Show Log. UI-free.
    /// Messages are split with ASCII record/unit separators, which never occur
    /// in a commit message, so multi-line bodies come through intact.
    /// </summary>
    public static class CommitLog
    {
        private const char RecordSep = '\x1e';
        private const char FieldSep = '\x1f';

        /// <param name="max">Newest <paramref name="max"/> commits; 0 = all.</param>
        /// <param name="rev">What to log: HEAD (the local branch) or e.g. origin/main.</param>
        public static List<LogEntry> Read(string repo, int max, string rev = "HEAD")
        {
            var args = new List<string> { "log", "--date=format:%Y-%m-%d %H:%M",
                                          "--format=%x1e%H%x1f%h%x1f%ad%x1f%an%x1f%B" };
            if (max > 0) args.Add("-n" + max);
            args.Add(string.IsNullOrEmpty(rev) ? "HEAD" : rev);
            args.Add("--");   // a ref, never a path

            var result = Git.Run(repo, args.ToArray());
            if (!result.Ok)
            {
                // A repository with no commits yet is not an error worth a box.
                if (!Git.HasCommit(repo)) return new List<LogEntry>();
                throw new InvalidOperationException("git log failed (exit " + result.ExitCode + "):\n\n" + result.Output);
            }
            return Parse(result.Output);
        }

        /// <summary>Splits raw 'git log' output in the format above. Public for the tests.</summary>
        public static List<LogEntry> Parse(string output)
        {
            var list = new List<LogEntry>();
            foreach (var record in (output ?? string.Empty).Split(RecordSep))
            {
                var f = record.Split(new[] { FieldSep }, 5);
                if (f.Length < 5) continue;

                var message = f[4].Trim('\r', '\n');
                var structured = HasCommitType(message);
                list.Add(new LogEntry
                {
                    Sha = f[0].Trim(),
                    ShortSha = f[1].Trim(),
                    Date = f[2].Trim(),
                    Author = f[3].Trim(),
                    Message = message,
                    IsStructured = structured,
                    Fields = structured ? ParseFields(message) : SubjectOnly(message),
                });
            }
            return list;
        }

        /// <summary>Does the message contain "Commit Type:" with a value?</summary>
        public static bool HasCommitType(string message)
        {
            return FindValue(message, "Commit Type") != null;
        }

        /// <summary>
        /// Fields of a message that has a Commit Type. The dialog's own format
        /// goes through <see cref="CommitMessage.Parse"/> (bottom-up field
        /// block, so prose in the body is never mistaken for a field). A message
        /// written by hand under the hook may carry its keys anywhere - even
        /// "Commit Type: Feature - add export" as the subject - so any key the
        /// block did not yield is looked up line by line.
        /// </summary>
        public static CommitFields ParseFields(string message)
        {
            var f = CommitMessage.Parse(message);
            if (f.CommitType.Length == 0) f.CommitType = FindValue(message, "Commit Type") ?? string.Empty;
            if (f.Risk.Length == 0) f.Risk = FindValue(message, "Risk") ?? string.Empty;
            if (f.TT.Length == 0) f.TT = FindValue(message, "TT") ?? string.Empty;
            if (f.Areas.Length == 0)
            {
                var area = FindValue(message, "Area");
                if (area != null)
                    f.Areas = area.Split(',').Select(a => a.Trim()).Where(a => a.Length > 0).ToArray();
            }
            return f;
        }

        /// <summary>
        /// Value after "key:" on the first line that has it (after optional
        /// "Subject:"), or null when absent or empty.
        /// </summary>
        private static string FindValue(string message, string key)
        {
            foreach (var raw in (message ?? string.Empty).Split('\n'))
            {
                var line = raw.TrimEnd('\r').Trim();
                if (line.StartsWith("Subject:", StringComparison.Ordinal))
                    line = line.Substring("Subject:".Length).Trim();

                var p = key == "Commit Type"
                    ? line.IndexOf(key + ":", StringComparison.Ordinal)   // the hook accepts it anywhere
                    : (line.StartsWith(key + ":", StringComparison.Ordinal) ? 0 : -1);
                if (p < 0) continue;

                var value = line.Substring(p + key.Length + 1).Trim();
                if (value.Length > 0) return value;
            }
            return null;
        }

        /// <summary>A free-form commit: line 1 as the subject, nothing else invented.</summary>
        private static CommitFields SubjectOnly(string message)
        {
            var f = CommitMessage.Parse(message);
            f.CommitType = string.Empty;
            f.Risk = string.Empty;
            f.TT = string.Empty;
            f.Customer = string.Empty;
            f.Areas = new string[0];
            f.OtherVersions = false;
            return f;
        }

        /// <summary>
        /// Updates origin/* so the "from origin" view is current. Failure
        /// (offline, no credentials) is returned, not thrown - the last fetched
        /// state can still be shown.
        /// </summary>
        public static GitResult Fetch(string repo)
        {
            // No console prompt that nobody can see; the credential manager may
            // still show its own sign-in window, as it does for Push.
            var env = new Dictionary<string, string> { { "GIT_TERMINAL_PROMPT", "0" } };
            return Git.RunWithInput(repo, null, env, "fetch", "--prune", "origin");
        }

        /// <summary>
        /// Every file the commit changed, with its status and +/- line counts,
        /// for the files list under the message. Compared with the first parent
        /// (for a merge: what the merge brought into the branch); a root commit
        /// against the empty tree. Renames and copies are detected (-M -C).
        /// Uses -z output, so paths with spaces, quotes or non-ASCII characters
        /// come through as they are.
        /// </summary>
        public static List<ChangedFile> ChangedFiles(string repo, string sha)
        {
            var parents = Git.Line(repo, "rev-list", "--parents", "-n", "1", sha)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var isRoot = parents.Length <= 1;

            string[] range = isRoot
                ? new[] { "diff-tree", "-r", "--root", "--no-commit-id" }
                : new[] { "diff" };
            Func<string, GitResult> run = mode =>
            {
                var args = new List<string>(range) { "-M", "-C", mode, "-z" };
                if (isRoot) args.Add(sha); else { args.Add(parents[1]); args.Add(sha); }
                return Git.Run(repo, args.ToArray());
            };

            var files = ParseNameStatus(run("--name-status").Output);
            var counts = ParseNumStat(run("--numstat").Output);
            foreach (var f in files)
            {
                // what each side of this file is, for File compare
                f.OldRev = isRoot ? null : parents[1];
                f.NewRev = sha;

                int[] c;
                if (counts.TryGetValue(f.Path, out c)) { f.Added = c[0]; f.Removed = c[1]; }
            }
            return files;
        }

        /// <summary>"M\0path\0" / "R087\0old\0new\0" records. Public for the tests.</summary>
        public static List<ChangedFile> ParseNameStatus(string z)
        {
            var list = new List<ChangedFile>();
            var parts = (z ?? string.Empty).Split('\0');
            for (var i = 0; i < parts.Length; i++)
            {
                var code = parts[i].Trim();
                if (code.Length == 0) continue;

                var letter = code[0];
                var twoPaths = letter == 'R' || letter == 'C';
                if (i + 1 >= parts.Length) break;

                var f = new ChangedFile { Code = letter, Added = -1, Removed = -1 };
                if (twoPaths)
                {
                    f.OldPath = parts[i + 1];
                    f.Path = i + 2 < parts.Length ? parts[i + 2] : string.Empty;
                    i += 2;
                }
                else
                {
                    f.Path = parts[i + 1];
                    i += 1;
                }
                if (f.Path.Length > 0) list.Add(f);
            }
            return list;
        }

        /// <summary>
        /// "added\tremoved\tpath\0", or for a rename "added\tremoved\t\0old\0new\0";
        /// binary files have "-" counts. Keyed by the (new) path. Public for the tests.
        /// </summary>
        public static Dictionary<string, int[]> ParseNumStat(string z)
        {
            var map = new Dictionary<string, int[]>(StringComparer.Ordinal);
            var parts = (z ?? string.Empty).Split('\0');
            for (var i = 0; i < parts.Length; i++)
            {
                var rec = parts[i].TrimStart('\n');
                var t = rec.Split('\t');
                if (t.Length < 3) continue;

                int a, r;
                var added = int.TryParse(t[0], out a) ? a : -1;
                var removed = int.TryParse(t[1], out r) ? r : -1;

                string path;
                if (t[2].Length > 0) path = t[2];
                else
                {
                    // rename/copy: the two paths follow as their own records
                    path = i + 2 < parts.Length ? parts[i + 2] : string.Empty;
                    i += 2;
                }
                if (path.Length > 0) map[path] = new[] { added, removed };
            }
            return map;
        }

        /// <summary>Changed files with their +/- counts, for the details pane.</summary>
        public static string Stat(string repo, string sha)
        {
            var r = Git.Run(repo, "show", "--stat=120", "--format=", sha);
            return r.Ok ? r.Output.Trim('\r', '\n') : r.Output;
        }
    }

    /// <summary>One file in a commit, for the files list under the message.</summary>
    public sealed class ChangedFile
    {
        /// <summary>git's letter: A, M, D, R, C, T, U, X.</summary>
        public char Code { get; set; }
        public string Path { get; set; }

        /// <summary>For a rename or copy, where it came from; else null.</summary>
        public string OldPath { get; set; }

        /// <summary>The commit the "before" side comes from (first parent); null for a root commit.</summary>
        public string OldRev { get; set; }

        /// <summary>The commit itself - the "after" side.</summary>
        public string NewRev { get; set; }

        /// <summary>Lines added / removed; -1 for a binary file or when unknown.</summary>
        public int Added { get; set; }
        public int Removed { get; set; }

        public string Status
        {
            get
            {
                switch (Code)
                {
                    case 'A': return "Added";
                    case 'M': return "Modified";
                    case 'D': return "Deleted";
                    case 'R': return "Renamed";
                    case 'C': return "Copied";
                    case 'T': return "Type changed";
                    case 'U': return "Unmerged";
                    default: return "Changed";
                }
            }
        }

        /// <summary>"new" or "old  ->  new" for renames and copies.</summary>
        public string DisplayPath
        {
            get { return OldPath == null ? Path : OldPath + "  ->  " + Path; }
        }

        /// <summary>"+12 -3", "binary", or empty when not known.</summary>
        public string Lines
        {
            get
            {
                if (Added < 0 && Removed < 0) return "binary";
                return "+" + Math.Max(0, Added) + " -" + Math.Max(0, Removed);
            }
        }
    }

    /// <summary>The Show Log columns, in display order - the value is the column index.</summary>
    public enum LogColumn { Date = 0, Author, CommitType, Area, TT, Risk, Subject, Commit }

    /// <summary>
    /// Sorting for the Show Log columns. UI-free so the tests can check it.
    ///
    /// Stable: equal keys keep git's order between them. Empty values (no
    /// Commit Type, no TT) always go last in either direction, so sorting by
    /// Type never opens with a screenful of blank rows. TT numbers sort
    /// numerically (9 before 10), Risk as Low &lt; Medium &lt; High, Date by its
    /// yyyy-MM-dd HH:mm text, the rest as case-insensitive text.
    /// </summary>
    public static class LogSort
    {
        /// <summary>The text a Show Log cell displays - also what its column filter matches.</summary>
        public static string CellText(LogEntry x, LogColumn column)
        {
            switch (column)
            {
                case LogColumn.Date: return x.Date ?? string.Empty;
                case LogColumn.Author: return x.Author ?? string.Empty;
                case LogColumn.CommitType: return x.Fields.CommitType;
                case LogColumn.Area: return x.AreaText;
                case LogColumn.TT: return x.Fields.TT;
                case LogColumn.Risk: return x.Fields.Risk;
                case LogColumn.Subject: return x.Fields.Subject;
                case LogColumn.Commit: return x.ShortSha ?? string.Empty;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Per-column filters, one entry per <see cref="LogColumn"/> (null or
        /// blank = no filter). A row passes when every non-blank filter is a
        /// case-insensitive substring of its cell. The Commit column also
        /// matches against the full SHA, so a pasted 40-char hash finds it.
        /// </summary>
        public static bool PassesColumnFilters(LogEntry x, IList<string> filters)
        {
            if (filters == null) return true;
            for (var i = 0; i < filters.Count; i++)
            {
                var f = filters[i] == null ? string.Empty : filters[i].Trim();
                if (f.Length == 0) continue;

                var cell = CellText(x, (LogColumn)i);
                if ((LogColumn)i == LogColumn.Commit) cell = x.Sha ?? cell;
                if (cell.IndexOf(f, StringComparison.OrdinalIgnoreCase) < 0) return false;
            }
            return true;
        }

        public static List<LogEntry> Sort(List<LogEntry> list, LogColumn column, bool descending)
        {
            var indexed = list.Select((x, i) => new { x, i }).ToList();
            indexed.Sort((a, b) =>
            {
                var ka = Key(a.x, column);
                var kb = Key(b.x, column);
                var ea = IsBlank(ka);
                var eb = IsBlank(kb);
                if (ea != eb) return ea ? 1 : -1;

                var c = ea ? 0 : Compare(ka, kb);
                if (descending) c = -c;
                return c != 0 ? c : a.i.CompareTo(b.i);
            });
            return indexed.Select(p => p.x).ToList();
        }

        private static object Key(LogEntry x, LogColumn column)
        {
            switch (column)
            {
                case LogColumn.Date: return x.Date ?? string.Empty;
                case LogColumn.Author: return x.Author ?? string.Empty;
                case LogColumn.CommitType: return x.Fields.CommitType;
                case LogColumn.Area: return x.AreaText;
                case LogColumn.TT: return TTKey(x.Fields.TT);
                case LogColumn.Risk: return RiskKey(x.Fields.Risk);
                case LogColumn.Subject: return x.Fields.Subject;
                case LogColumn.Commit: return x.Sha ?? string.Empty;
                default: return string.Empty;
            }
        }

        private static object TTKey(string tt)
        {
            long n;
            if (string.IsNullOrWhiteSpace(tt)) return string.Empty;
            return long.TryParse(tt.Trim(), out n) ? (object)n : tt.Trim();
        }

        private static object RiskKey(string risk)
        {
            if (string.IsNullOrWhiteSpace(risk)) return string.Empty;
            switch (risk.Trim().ToLowerInvariant())
            {
                case "low": return 1L;
                case "medium": return 2L;
                case "high": return 3L;
                default: return risk.Trim();
            }
        }

        private static bool IsBlank(object key)
        {
            var s = key as string;
            return s != null && s.Length == 0;
        }

        /// <summary>Numbers before text; numbers numerically, text case-insensitively.</summary>
        private static int Compare(object a, object b)
        {
            if (a is long && b is long) return ((long)a).CompareTo((long)b);
            if (a is long) return -1;
            if (b is long) return 1;
            return string.Compare((string)a, (string)b, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
