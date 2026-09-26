using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHelper
{
    /// <summary>How a source commit was (or was not) found in the target branch.</summary>
    public enum MergeStatus
    {
        /// <summary>Same change, by patch-id. The strongest answer available.</summary>
        SamePatch,

        /// <summary>The target says it cherry-picked this exact commit.</summary>
        CherryPicked,

        /// <summary>A commit with the same Subject line is on the target.</summary>
        SameSubject,

        /// <summary>A commit quoting the same TT number is on the target.</summary>
        SameTT,

        /// <summary>Nothing on the target looks like this commit.</summary>
        Missing,
    }

    /// <summary>One commit on the source branch, and the verdict for it.</summary>
    public sealed class MergeCheckItem
    {
        public string Sha { get; set; }
        public string ShortSha { get { return Sha != null && Sha.Length >= 8 ? Sha.Substring(0, 8) : Sha; } }
        public string Author { get; set; }
        public string Date { get; set; }

        public string Subject { get; set; }
        public string TT { get; set; }
        public string Customer { get; set; }
        public string CommitType { get; set; }

        /// <summary>
        /// The message as git stored it. Kept because the cherry-pick trailer
        /// lives in the body, which the field parser folds away.
        /// </summary>
        public string RawMessage { get; set; }

        /// <summary>Written by the dialog, rather than free prose.</summary>
        public bool IsStructured { get; set; }

        public MergeStatus Status { get; set; }

        /// <summary>The matching commit on the target, when there is one.</summary>
        public string MatchedBy { get; set; }

        public bool IsPresent { get { return Status != MergeStatus.Missing; } }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case MergeStatus.SamePatch: return "present (same patch)";
                    case MergeStatus.CherryPicked: return "present (cherry-pick)";
                    case MergeStatus.SameSubject: return "probably present (subject)";
                    case MergeStatus.SameTT: return "probably present (TT)";
                    default: return "MISSING";
                }
            }
        }
    }

    public sealed class MergeCheckResult
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public string MergeBase { get; set; }
        public List<MergeCheckItem> Items { get; set; }

        /// <summary>
        /// Commits on the source branch that were left out because they carry
        /// no structured message - written before the dialog existed, or made
        /// outside it. They are reported, not hidden: a large number here means
        /// the audit covers less than it appears to.
        /// </summary>
        public int SkippedUnstructured { get; set; }

        public int MissingCount { get { return Items.Count(i => !i.IsPresent); } }
        public int PresentCount { get { return Items.Count(i => i.IsPresent); } }
    }

    /// <summary>
    /// Answers "is every change on that branch also on this one?".
    ///
    /// A cherry-pick gets a new SHA, so commit ids cannot answer it. Four
    /// signals are used instead, strongest first, and the weaker ones are
    /// reported as *probable* rather than treated as proof:
    ///
    /// 1. <c>git cherry</c> - patch-id equivalence. Catches a port whose
    ///    message was reworded, and is the only signal git computes from the
    ///    content itself.
    /// 2. <c>(cherry picked from commit &lt;sha&gt;)</c>, which
    ///    <c>git cherry-pick -x</c> appends - free provenance, exact.
    /// 3. the <c>Subject:</c> line of the structured message.
    /// 4. the <c>TT:</c> number, which survives rewording.
    ///
    /// Only commits after the branch point are considered on either side: a
    /// port cannot have landed before the branches split.
    ///
    /// This is a report, not a verdict. Anything it calls MISSING is what a
    /// human should look at - which is the whole point of the structured
    /// message. A <c>Change-Id</c> trailer would replace signals 3 and 4 with
    /// an exact set difference; until then, this is the best git can do.
    /// </summary>
    public static class MergeCheck
    {
        /// <summary>
        /// Release branches: origin branches whose name contains ALL of these
        /// (any case), e.g. origin/root/RTM0. Used by Check Merge ("Changes
        /// from") and the admin message audit.
        /// </summary>
        public static readonly string[] ReleaseBranchKeywords = { "/rtm", "/root" };

        public static bool IsReleaseBranch(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                   ReleaseBranchKeywords.All(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>The release branches on origin, sorted.</summary>
        public static string[] ReleaseBranches(string repo)
        {
            return Git.RemoteBranches(repo).Where(IsReleaseBranch)
                .OrderBy(b => b, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private const char FieldSeparator = '';
        private const char RecordSeparator = '';

        /// <summary>
        /// Compares <paramref name="source"/> against <paramref name="target"/>
        /// and lists every source commit with its verdict. Throws on a git
        /// failure the caller should report.
        /// </summary>
        public static MergeCheckResult Run(string repo, string source, string target)
        {
            var mergeBase = Git.Line(repo, "merge-base", target, source);
            if (string.IsNullOrEmpty(mergeBase))
                throw new InvalidOperationException(
                    "'" + source + "' and '" + target + "' have no common ancestor - nothing to compare.");

            // Every commit on the source branch that is NOT reachable from the
            // target: 'target..source'. That is the question - "did this work
            // get across?" - asked directly. A commit that arrived by a real
            // merge is reachable and so is not listed at all; if the merge
            // never happened, every commit since the split shows up here and
            // gets a verdict, which is exactly how a missing merge surfaces.
            var allSource = ReadCommits(repo, target + ".." + source, null);

            // Only commits the dialog wrote are audited. A message with no
            // 'Subject:'/'Commit Type:' carries none of the fields this check
            // matches on, so a verdict for it would be guesswork dressed up as
            // a result. They are counted and reported instead.
            var sourceCommits = allSource.Where(c => c.IsStructured).ToList();
            var skipped = allSource.Count - sourceCommits.Count;

            // The target side keeps its unstructured commits: a fix ported
            // through VS's own commit box has no fields, but its subject line
            // can still match, and dropping it would turn a real port into a
            // false MISSING. The filter above is about what gets *audited*,
            // not about what counts as evidence.
            //
            // Scoping it by date rather than by merge-base matters: a merge in the other direction (Main into
            // RTM0) drags the merge-base forward onto the target's own
            // history, which would hide target commits made before it - ports
            // among them.
            var targetCommits = ReadCommits(repo, target, SinceDateFor(sourceCommits));

            var samePatch = ReadPatchIdMatches(repo, target, source);
            var pickedFrom = ReadCherryPickedShas(targetCommits);

            // Indexes over the target side, built once: a per-commit 'git log
            // --grep' would be one process per commit, which on a release
            // branch is hundreds of them.
            var bySubject = new Dictionary<string, MergeCheckItem>(StringComparer.OrdinalIgnoreCase);
            var byTT = new Dictionary<string, MergeCheckItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in targetCommits)
            {
                var key = NormalizeSubject(c.Subject);
                if (key.Length > 0 && !bySubject.ContainsKey(key)) bySubject[key] = c;
                if (c.TT.Length > 0 && !byTT.ContainsKey(c.TT)) byTT[c.TT] = c;
            }

            foreach (var item in sourceCommits)
            {
                MergeCheckItem hit;

                if (samePatch.Contains(item.Sha))
                {
                    item.Status = MergeStatus.SamePatch;
                }
                else if (pickedFrom.Contains(item.Sha))
                {
                    item.Status = MergeStatus.CherryPicked;
                }
                else if (bySubject.TryGetValue(NormalizeSubject(item.Subject), out hit))
                {
                    item.Status = MergeStatus.SameSubject;
                    item.MatchedBy = hit.ShortSha;
                }
                else if (item.TT.Length > 0 && byTT.TryGetValue(item.TT, out hit))
                {
                    item.Status = MergeStatus.SameTT;
                    item.MatchedBy = hit.ShortSha;
                }
                else
                {
                    item.Status = MergeStatus.Missing;
                }
            }

            return new MergeCheckResult
            {
                Source = source,
                Target = target,
                MergeBase = mergeBase,
                Items = sourceCommits,
                SkippedUnstructured = skipped,
            };
        }

        /// <summary>
        /// Commits in <paramref name="range"/> - a revision range or a single
        /// branch - newest first, with the structured message already parsed.
        /// Merges are left out: they carry no change of their own.
        /// </summary>
        /// <param name="since">
        /// Optional <c>--since</c> cut-off, for the target side where the whole
        /// history would be read otherwise.
        /// </param>
        private static List<MergeCheckItem> ReadCommits(string repo, string range, string since)
        {
            var format = "--format=%H" + FieldSeparator + "%an" + FieldSeparator +
                         "%ad" + FieldSeparator + "%B" + RecordSeparator;

            var args = new List<string> { "log", "--no-merges", "--date=short", format };
            if (!string.IsNullOrEmpty(since)) args.Add("--since=" + since);
            args.Add(range);

            var result = Git.Run(repo, args.ToArray());
            if (!result.Ok)
                throw new InvalidOperationException("git log " + range + " failed:\r\n" + result.Output);

            var items = new List<MergeCheckItem>();
            foreach (var record in result.Output.Split(RecordSeparator))
            {
                var text = record.Trim('\r', '\n');
                if (text.Trim().Length == 0) continue;

                var parts = text.Split(FieldSeparator);
                if (parts.Length < 4) continue;

                // The same parser the dialog writes with, so a message it built
                // round-trips into the same fields here.
                var fields = CommitMessage.Parse(parts[3]);

                items.Add(new MergeCheckItem
                {
                    Sha = parts[0].Trim(),
                    Author = parts[1].Trim(),
                    Date = parts[2].Trim(),
                    Subject = fields.Subject,
                    TT = fields.TT,
                    Customer = fields.Customer,
                    CommitType = fields.CommitType,
                    RawMessage = parts[3],
                    IsStructured = CommitMessage.IsStructured(parts[3]),
                });
            }
            return items;
        }

        /// <summary>
        /// How far back to read the target branch: a little before the oldest
        /// commit being checked, since a port cannot have landed long before
        /// the change it ports. Empty when there is nothing to check.
        /// </summary>
        private static string SinceDateFor(List<MergeCheckItem> sourceCommits)
        {
            var oldest = DateTime.MaxValue;
            foreach (var c in sourceCommits)
            {
                DateTime d;
                if (DateTime.TryParse(c.Date, out d) && d < oldest) oldest = d;
            }
            if (oldest == DateTime.MaxValue) return null;

            // A generous margin: clock skew, a port committed slightly earlier
            // on another machine, a rebase that rewrote dates.
            return oldest.AddDays(-90).ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Source commits whose patch is already on the target, from
        /// <c>git cherry</c>: '-' means an equivalent exists there, '+' means
        /// it does not.
        /// </summary>
        private static HashSet<string> ReadPatchIdMatches(string repo, string target, string source)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var result = Git.Run(repo, "cherry", target, source);
            if (!result.Ok) return found;   // an unborn branch, say: fall back to the other signals

            foreach (var line in result.Lines)
            {
                var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                if (parts[0] == "-") found.Add(parts[1].Trim());
            }
            return found;
        }

        /// <summary>
        /// SHAs quoted by '(cherry picked from commit ...)' on the target -
        /// what <c>git cherry-pick -x</c> writes.
        /// </summary>
        private static HashSet<string> ReadCherryPickedShas(IEnumerable<MergeCheckItem> targetCommits)
        {
            // The body is not kept on the item, so this reads the trailer out of
            // the subject+body that Parse left behind. Callers pass the parsed
            // items, so match on whatever text is available.
            var shas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in targetCommits)
            {
                foreach (Match m in CherryPickTrailer.Matches(c.RawMessage ?? string.Empty))
                    shas.Add(m.Groups[1].Value);
            }
            return shas;
        }

        private static readonly Regex CherryPickTrailer =
            new Regex(@"cherry picked from commit ([0-9a-f]{7,40})", RegexOptions.IgnoreCase);

        /// <summary>
        /// Subjects are compared case-insensitively with runs of whitespace
        /// collapsed, so re-wrapping during a conflict resolution does not make
        /// a ported commit look missing.
        /// </summary>
        private static string NormalizeSubject(string subject)
        {
            if (string.IsNullOrEmpty(subject)) return string.Empty;
            return Regex.Replace(subject.Trim(), @"\s+", " ");
        }
    }
}
