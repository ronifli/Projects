using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHelper
{
    /// <summary>What the target has for one changed line of a source commit.</summary>
    public enum LineStatus
    {
        /// <summary>Added line found as-is / removed line gone - nothing to do.</summary>
        Ok,
        /// <summary>Added line nowhere in the target, not even in a changed form.</summary>
        Missing,
        /// <summary>
        /// The target has it in a DIFFERENT form: a similar line (same
        /// identifiers, most of the text) - ported with changes, or changed
        /// since. For a removed line: a modified version is still there.
        /// </summary>
        Changed,
        /// <summary>Removed line still in the target, unchanged.</summary>
        StillThere,
        /// <summary>Braces, "else"... - matches everywhere, not judged.</summary>
        Trivial,
    }

    /// <summary>One changed line of a source commit, checked against the target.</summary>
    public sealed class LineGap
    {
        /// <summary>'+' added by the commit, '-' removed by it.</summary>
        public char Kind { get; set; }

        /// <summary>Line number in the source file after the commit (added lines), or before it (removed).</summary>
        public int LineNo { get; set; }
        public string Text { get; set; }

        public LineStatus Status { get; set; }

        /// <summary>Where the target has it (exact, still-there or the similar line); 0 when nowhere.</summary>
        public int TargetLineNo { get; set; }

        /// <summary>The target's version, for a Changed line.</summary>
        public string TargetText { get; set; }

        /// <summary>0..1 - how close TargetText is, for a Changed line.</summary>
        public double Similarity { get; set; }

        /// <summary>The gap: an added line the target does not have, or a removed line it still has.</summary>
        public bool Missing { get { return Status == LineStatus.Missing || Status == LineStatus.StillThere; } }

        public bool Changed { get { return Status == LineStatus.Changed; } }

        /// <summary>Too short or all punctuation ("}", "else") to judge by text.</summary>
        public bool Trivial { get { return Status == LineStatus.Trivial; } }
    }

    public sealed class FileGap
    {
        public string Path { get; set; }
        public string OldPath { get; set; }

        /// <summary>The file exists in the target (at Path, or at OldPath for a rename).</summary>
        public bool InTarget { get; set; }
        public bool Binary { get; set; }
        public List<LineGap> Lines { get; set; }

        public int MissingAdded { get { return Lines.Count(l => l.Kind == '+' && l.Missing); } }
        public int StillRemoved { get { return Lines.Count(l => l.Kind == '-' && l.Missing); } }
        public int ChangedLines { get { return Lines.Count(l => l.Changed); } }
        public int Checked { get { return Lines.Count(l => !l.Trivial); } }
    }

    /// <summary>
    /// The evidence under an AI verdict: which changed lines of a source
    /// commit are missing from the target branch, and which are there but in
    /// a CHANGED form. For every file the commit changed, each added line is
    /// looked for in the target's copy of that file:
    ///
    ///   found as-is                -> ok
    ///   a similar line found       -> CHANGED (shown next to the source line)
    ///   nothing like it            -> MISSING
    ///
    /// and each removed line is checked for still being there (as-is, or
    /// changed). Lines are compared trimmed with runs of spaces collapsed, and
    /// anywhere in the file (a port may move code). "Similar" means the lines
    /// share their identifiers (Jaccard >= 0.5 over names of 2+ characters)
    /// and most of their text (edit-distance ratio >= 0.6) - so NewCall(x)
    /// against OldCall(x) is missing, not changed, while an extra condition
    /// or argument is a change. Trivial lines - braces, "else" - are not
    /// judged. UI-free.
    /// </summary>
    public static class MissingLines
    {
        private static readonly Regex Hunk = new Regex(@"^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@");

        public const double MinSimilarity = 0.6;
        public const double MinSharedNames = 0.5;

        private sealed class TargetLine
        {
            public int No;
            public string Raw;
            public string Norm;
            public HashSet<string> Names;
        }

        public static List<FileGap> Analyze(string repo, string sourceSha, string target)
        {
            var diff = Git.Run(repo, "show", "--format=", "--no-color", "-M", "--unified=0", sourceSha).Output;
            var files = ParseDiff(diff);

            foreach (var f in files)
            {
                var text = TargetFile(repo, target, f.Path);
                if (text == null && f.OldPath != null) text = TargetFile(repo, target, f.OldPath);
                f.InTarget = text != null;

                var lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n')
                    .Select((raw, i) => new TargetLine { No = i + 1, Raw = raw, Norm = Norm(raw) })
                    .Where(t => t.Norm.Length > 0 && !IsTrivial(t.Norm))
                    .ToList();
                foreach (var t in lines) t.Names = Names(t.Norm);

                var exact = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var t in lines) if (!exact.ContainsKey(t.Norm)) exact[t.Norm] = t.No;

                foreach (var l in f.Lines) Judge(l, lines, exact);
            }
            return files;
        }

        private static void Judge(LineGap l, List<TargetLine> target, Dictionary<string, int> exact)
        {
            var n = Norm(l.Text);
            if (IsTrivial(n)) { l.Status = LineStatus.Trivial; return; }

            int at;
            if (exact.TryGetValue(n, out at))
            {
                l.TargetLineNo = at;
                l.Status = l.Kind == '+' ? LineStatus.Ok : LineStatus.StillThere;
                return;
            }

            var best = MostSimilar(n, target);
            if (best != null)
            {
                l.Status = LineStatus.Changed;
                l.TargetLineNo = best.Item1.No;
                l.TargetText = best.Item1.Raw.Trim();
                l.Similarity = best.Item2;
                return;
            }
            l.Status = l.Kind == '+' ? LineStatus.Missing : LineStatus.Ok;
        }

        /// <summary>The target line closest to <paramref name="norm"/>, if close enough to call it the same line changed.</summary>
        private static Tuple<TargetLine, double> MostSimilar(string norm, List<TargetLine> target)
        {
            var names = Names(norm);
            if (names.Count == 0) return null;

            // cheap filter first - shared names - then edit distance on the few that pass
            var candidates = target
                .Select(t => new { t, j = Jaccard(names, t.Names) })
                .Where(x => x.j >= MinSharedNames)
                .OrderByDescending(x => x.j)
                .Take(8);

            Tuple<TargetLine, double> best = null;
            foreach (var c in candidates)
            {
                var ratio = Ratio(norm, c.t.Norm);
                if (ratio >= MinSimilarity && (best == null || ratio > best.Item2)) best = Tuple.Create(c.t, ratio);
            }
            return best;
        }

        /// <summary>Identifiers and numbers of 2+ characters - what a line is "about".</summary>
        private static HashSet<string> Names(string line)
        {
            return new HashSet<string>(
                Regex.Matches(line, @"[A-Za-z_][A-Za-z0-9_]+|\d{2,}").Cast<Match>().Select(m => m.Value),
                StringComparer.Ordinal);
        }

        private static double Jaccard(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 || b.Count == 0) return 0;
            var common = a.Count(b.Contains);
            return (double)common / (a.Count + b.Count - common);
        }

        /// <summary>1 - Levenshtein distance / longer length; strings capped at 300 chars.</summary>
        internal static double Ratio(string a, string b)
        {
            if (a.Length > 300) a = a.Substring(0, 300);
            if (b.Length > 300) b = b.Substring(0, 300);
            if (a.Length == 0 && b.Length == 0) return 1;

            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) prev[j] = j;
            for (var i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                var t = prev; prev = cur; cur = t;
            }
            return 1.0 - (double)prev[b.Length] / Math.Max(a.Length, b.Length);
        }

        /// <summary>A --unified=0 patch into files and their +/- lines. Public for the tests.</summary>
        public static List<FileGap> ParseDiff(string diff)
        {
            var files = new List<FileGap>();
            FileGap current = null;
            string oldPath = null;
            int oldNo = 0, newNo = 0;

            foreach (var raw in (diff ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                if (raw.StartsWith("diff --git ", StringComparison.Ordinal))
                {
                    current = null;
                    oldPath = null;
                    continue;
                }
                if (raw.StartsWith("--- ", StringComparison.Ordinal))
                {
                    var p = raw.Substring(4).Trim();
                    oldPath = p == "/dev/null" ? null : StripPrefix(p);
                    continue;
                }
                if (raw.StartsWith("+++ ", StringComparison.Ordinal))
                {
                    var p = raw.Substring(4).Trim();
                    var path = p == "/dev/null" ? oldPath : StripPrefix(p);   // deleted file: the old path
                    current = new FileGap
                    {
                        Path = path,
                        OldPath = oldPath != null && oldPath != path ? oldPath : null,
                        Lines = new List<LineGap>(),
                    };
                    files.Add(current);
                    continue;
                }
                if (raw.StartsWith("Binary files ", StringComparison.Ordinal))
                {
                    var m = Regex.Match(raw, @" and (?:b/)?(.+) differ$");
                    files.Add(new FileGap { Path = m.Success ? m.Groups[1].Value : raw, Binary = true, Lines = new List<LineGap>() });
                    continue;
                }

                var h = Hunk.Match(raw);
                if (h.Success)
                {
                    oldNo = int.Parse(h.Groups[1].Value);
                    newNo = int.Parse(h.Groups[2].Value);
                    continue;
                }
                if (current == null || raw.Length == 0) continue;

                if (raw[0] == '+') current.Lines.Add(new LineGap { Kind = '+', LineNo = newNo++, Text = raw.Substring(1) });
                else if (raw[0] == '-') current.Lines.Add(new LineGap { Kind = '-', LineNo = oldNo++, Text = raw.Substring(1) });
            }
            return files;
        }

        /// <summary>The report for one commit, as plain text - also what "Copy all" puts on the clipboard.</summary>
        public static string Report(AiMergeItem item, string target, List<FileGap> files)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Commit  " + item.ShortSha + "   " + item.Source + "   " + item.Date + "   " + item.Author);
            sb.AppendLine("Subject " + item.Subject);
            sb.AppendLine("AI      " + item.VerdictText + (string.IsNullOrEmpty(item.Reason) ? string.Empty : " - " + item.Reason));
            sb.AppendLine("Target  " + target + (string.IsNullOrEmpty(item.Evidence) ? string.Empty : "   (AI points at " + item.Evidence + ")"));
            sb.AppendLine();

            if (files.Count == 0) sb.AppendLine("  (no file changes - a merge or an empty commit)");
            foreach (var f in files)
            {
                var name = f.OldPath == null ? f.Path : f.OldPath + " -> " + f.Path;
                if (f.Binary)
                {
                    sb.AppendLine("== " + name + "   binary - not compared");
                    sb.AppendLine();
                    continue;
                }

                var added = f.Lines.Count(l => l.Kind == '+' && !l.Trivial);
                var removed = f.Lines.Count(l => l.Kind == '-' && !l.Trivial);
                sb.AppendLine("== " + name +
                    (f.InTarget ? string.Empty : "   ** FILE NOT IN TARGET **") +
                    "   added: " + f.MissingAdded + " missing, " + f.Lines.Count(l => l.Kind == '+' && l.Changed) +
                    " changed, " + f.Lines.Count(l => l.Kind == '+' && l.Status == LineStatus.Ok) + " ok (of " + added + ")" +
                    (removed > 0
                        ? "   removed: " + f.StillRemoved + " still there, " + f.Lines.Count(l => l.Kind == '-' && l.Changed) +
                          " changed (of " + removed + ")"
                        : string.Empty));

                var gaps = f.Lines.Where(l => l.Missing || l.Changed).ToList();
                if (gaps.Count == 0) sb.AppendLine("   all changed lines are in the target as they are");
                foreach (var l in gaps)
                {
                    var tag = l.Status == LineStatus.Missing ? "MISSING"
                            : l.Status == LineStatus.StillThere ? "STILL IN TARGET (L" + l.TargetLineNo + ")"
                            : "CHANGED " + (int)Math.Round(l.Similarity * 100) + "%";
                    var marker = l.Changed ? "  ~ " : l.Kind == '+' ? "  + " : "  - ";
                    sb.AppendLine(marker + ("L" + l.LineNo).PadRight(7) + l.Text.Trim().PadRight(60) + "  " + tag);
                    if (l.Changed)
                        sb.AppendLine("    " + ("T" + l.TargetLineNo).PadRight(7) + l.TargetText + "      <- target" +
                                      (l.Kind == '-' ? " still has this version" : string.Empty));
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string TargetFile(string repo, string target, string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var r = Git.Run(repo, "cat-file", "-p", target + ":" + path);
            return r.Ok ? r.Output : null;
        }

        private static string StripPrefix(string p)
        {
            p = p.Trim('"');
            return p.StartsWith("a/", StringComparison.Ordinal) || p.StartsWith("b/", StringComparison.Ordinal) ? p.Substring(2) : p;
        }

        private static string Norm(string line)
        {
            return Regex.Replace((line ?? string.Empty).Trim(), @"\s+", " ");
        }

        private static bool IsTrivial(string normalised)
        {
            if (normalised.Length < 4) return true;
            if (Regex.IsMatch(normalised, @"^[\s{}()\[\];,]*$")) return true;
            var t = normalised.TrimEnd(';');
            return t == "else" || t == "break" || t == "return" || t == "continue" || t == "#endif" ||
                   t == "#else" || t == "try" || t == "finally" || t == "default:";
        }
    }
}
