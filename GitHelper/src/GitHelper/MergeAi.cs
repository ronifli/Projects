using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHelper
{
    /// <summary>What the AI (or git) concluded about one source commit.</summary>
    public enum AiVerdict { Present, SamePatch, Partial, Missing, Unsure }

    public sealed class AiMergeItem
    {
        public string Source { get; set; }
        public string Sha { get; set; }
        public string ShortSha { get { return Sha != null && Sha.Length > 8 ? Sha.Substring(0, 8) : Sha; } }
        public string Date { get; set; }
        public string Author { get; set; }
        public string Subject { get; set; }
        public AiVerdict Verdict { get; set; }

        /// <summary>Target commit(s) the AI points at as the port, if any.</summary>
        public string Evidence { get; set; }
        public string Reason { get; set; }

        public string VerdictText
        {
            get
            {
                switch (Verdict)
                {
                    case AiVerdict.SamePatch: return "present (same patch)";
                    case AiVerdict.Present: return "present (AI)";
                    case AiVerdict.Partial: return "PARTIAL (AI)";
                    case AiVerdict.Missing: return "MISSING (AI)";
                    default: return "unsure";
                }
            }
        }
    }

    public sealed class AiMergeResult
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public List<AiMergeItem> Items { get; set; }

        /// <summary>A note for the summary - e.g. commits left out to keep the request small.</summary>
        public string Note { get; set; }
    }

    /// <summary>
    /// The AI half of Check Merge: for one source branch, every commit since a
    /// date that is not in the target's history, judged by Copilot as present,
    /// partial or missing in the target - by its CONTENT, which is what the
    /// message-based signals cannot see (a fix re-done by hand under another
    /// subject, a port that took only half the change).
    ///
    /// What is sent: each source commit's message and diff (trimmed), and the
    /// target's commits since the same date (message, files, trimmed diff).
    /// Commits git already proves are ported (identical patch, 'git cherry')
    /// are not sent - they are reported as "present (same patch)" directly.
    /// UI-free; <see cref="Parse"/> is public for the tests.
    /// </summary>
    public static class MergeAi
    {
        public const int MaxSourceCommits = 60;
        public const int MaxSourceDiffChars = 4000;
        public const int MaxTargetDiffChars = 1500;
        public const int MaxRequestChars = 200000;
        public const int TimeoutMs = 300000;

        private const char Rs = '\x1e';
        private const char Us = '\x1f';

        public static AiMergeResult Check(string repo, string source, string target, DateTime since)
        {
            var result = new AiMergeResult { Source = source, Target = target, Items = new List<AiMergeItem>() };
            var sinceArg = "--since=" + since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // source commits since the date that the target's history does not contain
            var sourceCommits = ReadCommits(repo, sinceArg, target + ".." + source);
            if (sourceCommits.Count == 0)
            {
                result.Note = "no commits since " + since.ToString("yyyy-MM-dd") + " that are not in " + target;
                return result;
            }

            // identical patches need no AI
            var samePatch = new HashSet<string>(
                Git.Run(repo, "cherry", target, source).Lines
                    .Where(l => l.StartsWith("- ", StringComparison.Ordinal))
                    .Select(l => l.Substring(2).Trim()),
                StringComparer.OrdinalIgnoreCase);

            var toAsk = new List<AiMergeItem>();
            foreach (var c in sourceCommits)
            {
                c.Source = source;
                if (samePatch.Contains(c.Sha))
                {
                    c.Verdict = AiVerdict.SamePatch;
                    c.Reason = "identical change already in " + target;
                }
                else toAsk.Add(c);
                result.Items.Add(c);
            }
            if (toAsk.Count == 0) return result;

            if (toAsk.Count > MaxSourceCommits)
            {
                foreach (var c in toAsk.Skip(MaxSourceCommits))
                {
                    c.Verdict = AiVerdict.Unsure;
                    c.Reason = "not sent - more than " + MaxSourceCommits + " commits; pick a later date";
                }
                result.Note = (toAsk.Count - MaxSourceCommits) + " older commit(s) not sent - choose a later date";
                toAsk = toAsk.Take(MaxSourceCommits).ToList();
            }

            var request = BuildRequest(repo, source, target, since, toAsk);
            var answer = AiBody.Ask(request,
                "Reply with the verdict lines inside one code block, nothing else.", TimeoutMs);
            if (!answer.Ok) throw new InvalidOperationException(answer.Error);

            var verdicts = Parse(answer.Body);
            foreach (var c in toAsk)
            {
                var v = verdicts.FirstOrDefault(x => c.Sha.StartsWith(x.Sha, StringComparison.OrdinalIgnoreCase) ||
                                                     x.Sha.StartsWith(c.ShortSha, StringComparison.OrdinalIgnoreCase));
                if (v == null)
                {
                    c.Verdict = AiVerdict.Unsure;
                    c.Reason = "the AI gave no verdict for this commit";
                    continue;
                }
                c.Verdict = v.Verdict;
                c.Evidence = v.Evidence;
                c.Reason = v.Reason;
            }
            return result;
        }

        private static List<AiMergeItem> ReadCommits(string repo, string sinceArg, string range)
        {
            var output = Git.Run(repo, "log", "--no-merges", sinceArg, "--date=short",
                "--format=%x1e%H%x1f%ad%x1f%an%x1f%s", range, "--").Output;
            return output.Split(Rs)
                .Select(r => r.Trim().Split(Us))
                .Where(f => f.Length >= 4)
                .Select(f => new AiMergeItem { Sha = f[0], Date = f[1], Author = f[2], Subject = f[3] })
                .ToList();
        }

        private static string BuildRequest(string repo, string source, string target, DateTime since, List<AiMergeItem> toAsk)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are auditing a port between two git branches of the same C# product.");
            sb.AppendLine("SOURCE branch: " + source + "   TARGET branch: " + target);
            sb.AppendLine();
            sb.AppendLine("For EVERY source commit listed below, decide whether its change is already in the");
            sb.AppendLine("TARGET branch - judging by what the code change does, not by the commit message.");
            sb.AppendLine("A port may have a different message, be split across commits, be merged with other");
            sb.AppendLine("work, or be re-done by hand in a slightly different way.");
            sb.AppendLine();
            sb.AppendLine("Answer with exactly one line per source commit, inside ONE fenced code block:");
            sb.AppendLine("  <source sha> | <VERDICT> | <target sha(s) that contain it, or -> | <short reason>");
            sb.AppendLine("VERDICT is one of:");
            sb.AppendLine("  PRESENT  - the same change is in the target (name the target commit)");
            sb.AppendLine("  PARTIAL  - only part of it is in the target (say what is missing)");
            sb.AppendLine("  MISSING  - nothing in the target does this");
            sb.AppendLine("  UNSURE   - cannot tell from what is shown");
            sb.AppendLine("Keep each reason under 120 characters. No other text.");
            sb.AppendLine("The target commits below are only those since " + since.ToString("yyyy-MM-dd") +
                          "; a change ported earlier would not be listed - say UNSURE rather than MISSING when that seems likely.");
            sb.AppendLine();

            sb.AppendLine("==================== SOURCE COMMITS (" + source + ") ====================");
            foreach (var c in toAsk)
            {
                sb.AppendLine();
                sb.AppendLine("----- source " + c.ShortSha + " (" + c.Date + ", " + c.Author + ")");
                sb.AppendLine(Trim(Git.Run(repo, "show", "--no-color", "--stat", "--patch", "--format=%B", c.Sha).Output,
                                   MaxSourceDiffChars));
            }

            sb.AppendLine();
            sb.AppendLine("==================== TARGET COMMITS since " + since.ToString("yyyy-MM-dd") + " (" + target + ") ====================");
            var sinceArg = "--since=" + since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            foreach (var t in ReadCommits(repo, sinceArg, target))
            {
                if (sb.Length > MaxRequestChars)
                {
                    sb.AppendLine();
                    sb.AppendLine("[... older target commits left out to keep this request small ...]");
                    break;
                }
                sb.AppendLine();
                sb.AppendLine("----- target " + t.ShortSha + " (" + t.Date + ", " + t.Author + ")");
                sb.AppendLine(Trim(Git.Run(repo, "show", "--no-color", "--stat", "--patch", "--format=%B", t.Sha).Output,
                                   MaxTargetDiffChars));
            }
            return sb.ToString();
        }

        private static string Trim(string text, int max)
        {
            text = (text ?? string.Empty).Trim();
            return text.Length <= max ? text : text.Substring(0, max) + "\n[... diff trimmed ...]";
        }

        private static readonly Regex VerdictLine = new Regex(
            @"^\s*[-*]?\s*`?(?<sha>[0-9a-fA-F]{7,40})`?\s*\|\s*\**(?<v>PRESENT|PARTIAL|MISSING|UNSURE)\**\s*\|\s*(?<ev>[^|]*)\|\s*(?<why>.*)$",
            RegexOptions.IgnoreCase);

        /// <summary>
        /// Reads the verdict lines back, ignoring anything else (fences,
        /// headings, chatter). Tolerates markdown bullets, backticks and bold.
        /// </summary>
        public static List<AiMergeItem> Parse(string answer)
        {
            var list = new List<AiMergeItem>();
            foreach (var raw in (answer ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                var m = VerdictLine.Match(raw);
                if (!m.Success) continue;

                AiVerdict v;
                switch (m.Groups["v"].Value.ToUpperInvariant())
                {
                    case "PRESENT": v = AiVerdict.Present; break;
                    case "PARTIAL": v = AiVerdict.Partial; break;
                    case "MISSING": v = AiVerdict.Missing; break;
                    default: v = AiVerdict.Unsure; break;
                }
                var evidence = m.Groups["ev"].Value.Trim().Trim('`');
                list.Add(new AiMergeItem
                {
                    Sha = m.Groups["sha"].Value.ToLowerInvariant(),
                    Verdict = v,
                    Evidence = evidence == "-" ? string.Empty : evidence,
                    Reason = m.Groups["why"].Value.Trim(),
                });
            }
            return list;
        }
    }
}
