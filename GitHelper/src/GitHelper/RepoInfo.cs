using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHelper
{
    /// <summary>One working copy offered in the Repository dropdown.</summary>
    public sealed class RepoInfo
    {
        public RepoInfo(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Branch = string.Empty;
            Staged = new string[0];
            Dirty = new string[0];
            Untracked = new string[0];
        }

        public string Path { get; private set; }
        public string Name { get; private set; }

        public string Branch { get; private set; }
        public string[] Staged { get; private set; }
        public string[] Dirty { get; private set; }
        public string[] Untracked { get; private set; }

        /// <summary>Commits since the last push; -1 when there is no upstream.</summary>
        public int Unpushed { get; private set; }

        public void Refresh()
        {
            Branch = Git.Line(Path, "rev-parse", "--abbrev-ref", "HEAD");
            if (string.IsNullOrEmpty(Branch)) Branch = "(no commits)";
            Staged = Git.StagedFiles(Path);
            Dirty = Git.DirtyFiles(Path);
            Untracked = Git.UntrackedFiles(Path);
            Unpushed = Git.UnpushedCount(Path);
        }

        /// <summary>A repo has work when there is something to commit.</summary>
        public bool HasWork(bool includeNew)
        {
            return Staged.Length > 0 || Dirty.Length > 0 || (includeNew && Untracked.Length > 0);
        }

        public override string ToString() { return Name; }

        /// <summary>
        /// Builds the dropdown list from the candidate paths, in order: the
        /// directory the dialog was launched from first, then the exe's own
        /// folder, then <see cref="Settings.BuiltIn"/>, then the repositories
        /// remembered in the registry. Non-repos are skipped silently and
        /// duplicates removed case-insensitively on the resolved root, so the
        /// first candidate that is a repository ends up selected by default.
        /// </summary>
        public static List<RepoInfo> Discover(IEnumerable<string> candidates)
        {
            var list = new List<RepoInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates)
            {
                var root = Git.ResolveRoot(candidate);
                if (root == null || !seen.Add(root)) continue;
                list.Add(new RepoInfo(root));
            }
            return list;
        }

    }
}
