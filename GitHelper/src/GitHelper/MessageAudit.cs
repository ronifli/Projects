using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GitHelper
{
    /// <summary>One commit whose message does not follow the structured format.</summary>
    public sealed class AuditItem
    {
        public string Sha { get; set; }
        public string ShortSha { get { return Sha != null && Sha.Length > 8 ? Sha.Substring(0, 8) : Sha; } }
        public string Date { get; set; }
        public string Author { get; set; }
        public string Message { get; set; }
        public List<string> Problems { get; set; }

        /// <summary>The audited branches this commit is on (one commit, listed once).</summary>
        public List<string> Branches { get; set; }

        public string BranchText { get { return Branches == null ? string.Empty : string.Join(", ", Branches); } }

        public string FirstLine
        {
            get { return (Message ?? string.Empty).Replace("\r\n", "\n").Split('\n')[0].Trim(); }
        }
    }

    public sealed class AuditResult
    {
        /// <summary>The branch checked, or for several "origin/main + 3 release branch(es)".</summary>
        public string Rev { get; set; }
        public List<string> Revs { get; set; }
        public int Checked { get; set; }
        public List<AuditItem> Items { get; set; }
    }

    /// <summary>
    /// Admin: which commits on the main branch since a date do not follow the
    /// structured commit message the dialog writes. What is required is what
    /// <see cref="CommitMessage.Build"/> always produces and the audit
    /// (<see cref="MergeCheck"/>) relies on:
    ///
    ///   Subject: ...        first line, non-empty, at most 72 characters in all
    ///   Commit Type: ...    Risk: ...    Area: ...   (with values)
    ///
    /// Merge commits are skipped unless asked for - their messages are written
    /// by git or GitHub, not by a person. UI-free.
    /// </summary>
    public static class MessageAudit
    {
        public const int MaxFirstLine = 72;

        public static AuditResult Check(string repo, string rev, DateTime since, bool includeMerges)
        {
            var args = new List<string> { "log", "--date=format:%Y-%m-%d %H:%M",
                "--since=" + since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "--format=%x1e%H%x1f%ad%x1f%an%x1f%B" };
            if (!includeMerges) args.Add("--no-merges");
            args.Add(rev);
            args.Add("--");

            var r = Git.Run(repo, args.ToArray());
            if (!r.Ok) throw new InvalidOperationException("git log " + rev + " failed:\n\n" + r.Output);

            var result = new AuditResult { Rev = rev, Items = new List<AuditItem>() };
            foreach (var record in r.Output.Split('\x1e'))
            {
                var f = record.Split(new[] { '\x1f' }, 4);
                if (f.Length < 4) continue;
                result.Checked++;

                var message = f[3].Trim('\r', '\n');
                var problems = Problems(message);
                if (problems.Count == 0) continue;

                result.Items.Add(new AuditItem
                {
                    Branches = new List<string> { rev },
                    Sha = f[0].Trim(),
                    Date = f[1].Trim(),
                    Author = f[2].Trim(),
                    Message = message,
                    Problems = problems,
                });
            }
            return result;
        }

        /// <summary>
        /// Several branches at once - main and the release branches. A commit
        /// reachable from more than one of them (the history a release branch
        /// shares with main, or a merge between them) is listed once, with every
        /// branch it is on; Checked counts distinct commits.
        /// </summary>
        public static AuditResult CheckAll(string repo, IList<string> revs, DateTime since, bool includeMerges)
        {
            var bySha = new Dictionary<string, AuditItem>(StringComparer.OrdinalIgnoreCase);
            var order = new List<AuditItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rev in revs)
            {
                var one = Check(repo, rev, since, includeMerges);
                var list = new List<string> { "rev-list", "--since=" + since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
                if (!includeMerges) list.Add("--no-merges");
                list.Add(rev);
                list.Add("--");
                foreach (var sha in Git.Run(repo, list.ToArray()).Lines) seen.Add(sha.Trim());

                foreach (var item in one.Items)
                {
                    AuditItem existing;
                    if (bySha.TryGetValue(item.Sha, out existing)) existing.Branches.Add(rev);
                    else { bySha[item.Sha] = item; order.Add(item); }
                }
            }

            return new AuditResult
            {
                Rev = revs.Count == 1 ? revs[0] : revs[0] + " + " + (revs.Count - 1) + " release branch(es)",
                Revs = revs.ToList(),
                Checked = seen.Count,
                Items = order.OrderByDescending(i => i.Date, StringComparer.Ordinal).ToList(),
            };
        }

        /// <summary>Main (origin) first, then the release branches - what the admin audit covers.</summary>
        public static List<string> AuditRevs(string repo)
        {
            var list = new List<string> { MainRev(repo) };
            list.AddRange(MergeCheck.ReleaseBranches(repo).Where(b => !list.Contains(b)));
            return list;
        }

        /// <summary>What is wrong with a message; empty when it follows the format. Public for the tests.</summary>
        public static List<string> Problems(string message)
        {
            var problems = new List<string>();
            var lines = (message ?? string.Empty).Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToList();
            var first = lines.Count > 0 ? lines[0] : string.Empty;

            if (!first.StartsWith("Subject:", StringComparison.Ordinal))
                problems.Add("no Subject: line");
            else if (first.Substring("Subject:".Length).Trim().Length == 0)
                problems.Add("empty Subject");

            if (first.Length > MaxFirstLine)
                problems.Add("first line " + first.Length + " chars (max " + MaxFirstLine + ")");

            foreach (var key in new[] { "Commit Type", "Risk", "Area" })
            {
                var line = lines.FirstOrDefault(l => l.TrimStart().StartsWith(key + ":", StringComparison.Ordinal));
                if (line == null) problems.Add("no " + key + ":");
                else
                {
                    var value = line.TrimStart().Substring(key.Length + 1).Trim();
                    if (value.Length == 0 || value.StartsWith("<", StringComparison.Ordinal))   // "<Risk Level>" placeholder
                        problems.Add("empty " + key);
                }
            }
            return problems;
        }

        /// <summary>The branch the audit runs on: origin's default branch, as the server has it.</summary>
        public static string MainRev(string repo)
        {
            var name = Git.DefaultBranch(repo);
            var remote = "origin/" + name;
            return Git.RemoteBranches(repo).Contains(remote, StringComparer.Ordinal) ? remote : name;
        }
    }
}
