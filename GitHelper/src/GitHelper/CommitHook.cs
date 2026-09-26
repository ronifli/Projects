// ---------------------------------------------------------------------------
// COMMIT_HOOK - the commit-msg hook support is switched OFF for now: kept, not
// deleted, and not compiled. To bring it back:
//   1. add  <DefineConstants>$(DefineConstants);COMMIT_HOOK</DefineConstants>
//      to GitHelper.csproj (and GitHelper.Tests.csproj for its tests);
//   2. rebuild. frmMain then checks/installs the hook again on startup and
//      whenever another repository is selected.
// Everything behind the switch: this file, frmMain.CheckCommitHook and its
// two call sites, the embedded Hooks\commit-msg, and the hook tests.
// ---------------------------------------------------------------------------
#if COMMIT_HOOK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitHelper
{
    /// <summary>What <see cref="CommitHook.Ensure"/> found or did.</summary>
    public enum HookSetup
    {
        /// <summary>Already on - nothing changed.</summary>
        AlreadyActive,
        /// <summary>The hook file was in the repository; only core.hooksPath was set.</summary>
        Enabled,
        /// <summary>Hook set-up files are new or changed in the working copy and need committing.</summary>
        Installed,
        /// <summary>core.hooksPath points somewhere else - left alone.</summary>
        OtherHooksPath,
        Failed,
    }

    public sealed class HookSetupResult
    {
        public HookSetupResult(HookSetup state, string detail, IList<string> pending)
        {
            State = state;
            Detail = detail ?? string.Empty;
            Pending = pending ?? new string[0];
        }

        public HookSetup State { get; private set; }
        public string Detail { get; private set; }

        /// <summary>
        /// Repository-relative hook set-up files not committed yet - what
        /// <see cref="CommitHook.CommitSetup"/> should commit.
        /// </summary>
        public IList<string> Pending { get; private set; }
    }

    /// <summary>
    /// The commit-msg hook that enforces the message policy for commits made
    /// <b>outside</b> this dialog (VS's commit box, the command line,
    /// TortoiseGit). The script is embedded in the exe; <see cref="Ensure"/>
    /// puts it into a working copy as <c>.githooks/commit-msg</c>, pins its
    /// line endings in <c>.gitattributes</c> and points
    /// <c>core.hooksPath</c> at it. Git never turns hooks on by itself, so
    /// every clone needs that last step - which is why it runs on startup.
    ///
    /// Idempotent and conservative: a hook file already in the repository is
    /// never overwritten (the repository's copy is the policy), and a
    /// <c>core.hooksPath</c> set to some other folder is reported, not
    /// replaced. Nothing is staged or committed - the new files show up as
    /// ordinary untracked changes; <see cref="CommitSetup"/> then commits
    /// exactly those files on their own.
    /// </summary>
    public static class CommitHook
    {
        public const string HooksDir = ".githooks";
        public const string HookName = "commit-msg";
        public const string HookRelativePath = HooksDir + "/" + HookName;

        /// <summary>Subject of the commit <see cref="CommitSetup"/> makes.</summary>
        public const string CommitSubject = "Git Hook Update";

        private const string ResourceName = "GitHelper.commit-msg";
        private const string AttributesLine = ".githooks/* text eol=lf";

        public static string HookFile(string repo)
        {
            return Path.Combine(repo, HooksDir, HookName);
        }

        /// <summary>True when core.hooksPath is ours and the hook file is there.</summary>
        public static bool IsActive(string repo)
        {
            return PointsAtOurs(repo, HooksPath(repo)) && File.Exists(HookFile(repo));
        }

        public static HookSetupResult Ensure(string repo)
        {
            if (string.IsNullOrEmpty(repo) || !Directory.Exists(repo))
                return new HookSetupResult(HookSetup.Failed, "Repository not found: " + repo, null);

            var current = HooksPath(repo);
            var ours = PointsAtOurs(repo, current);

            if (current.Length > 0 && !ours)
            {
                return new HookSetupResult(HookSetup.OtherHooksPath,
                    "core.hooksPath is already set to '" + current + "', so the commit message hook was not " +
                    "enabled. Set it to '" + HooksDir + "' to use it:\n\n  git config core.hooksPath " + HooksDir,
                    null);
            }

            var note = string.Empty;
            try
            {
                if (!File.Exists(HookFile(repo)))
                {
                    Directory.CreateDirectory(Path.Combine(repo, HooksDir));
                    // LF only: Git for Windows runs this with sh, and a CR at
                    // the end of each line breaks the script.
                    File.WriteAllText(HookFile(repo), EmbeddedScript(), new UTF8Encoding(false));
                }

                EnsureAttributes(repo);
            }
            catch (Exception ex)
            {
                return new HookSetupResult(HookSetup.Failed,
                    "Could not write the commit message hook into " + repo + ":\n\n" + ex.Message, null);
            }

            if (!ours)
            {
                var r = Git.Run(repo, "config", "core.hooksPath", HooksDir);
                if (!r.Ok)
                {
                    return new HookSetupResult(HookSetup.Failed,
                        "git config core.hooksPath failed (exit " + r.ExitCode + "):\n\n" + r.Output, null);
                }
            }

            // Git LFS keeps its own hooks in the hooks folder. With hooksPath
            // moved to .githooks, .git/hooks is no longer read, so without
            // copies here 'pre-push' would stop uploading LFS objects. LFS
            // would recreate them on its next checkout anyway - as stray new
            // files nobody asked for - so write them now and commit them
            // together with the rest.
            if (UsesLfs(repo) && LfsHooks.Any(h => !File.Exists(Path.Combine(repo, HooksDir, h))))
            {
                var lfs = Git.Run(repo, "lfs", "update");
                if (!lfs.Ok)
                {
                    note = "This repository uses Git LFS, but 'git lfs update' failed, so the LFS hooks are " +
                           "missing from " + HooksDir + " and LFS pushes will not upload files:\n\n" + lfs.Output;
                }
            }

            var pending = PendingFiles(repo);
            var state = pending.Count > 0 ? HookSetup.Installed
                      : !ours ? HookSetup.Enabled
                      : HookSetup.AlreadyActive;
            return new HookSetupResult(state, note, pending);
        }

        /// <summary>The hooks 'git lfs update' writes.</summary>
        public static readonly string[] LfsHooks = { "pre-push", "post-checkout", "post-commit", "post-merge" };

        /// <summary>Does any tracked .gitattributes route files through LFS?</summary>
        private static bool UsesLfs(string repo)
        {
            if (Git.Run(repo, "grep", "-q", "-F", "filter=lfs", "--", "*.gitattributes").Ok) return true;

            // not committed yet
            var root = Path.Combine(repo, ".gitattributes");
            return File.Exists(root) && File.ReadAllText(root).Contains("filter=lfs");
        }

        /// <summary>
        /// The hook set-up still to be committed: anything new or changed under
        /// .githooks, and .gitattributes only when it is new or its uncommitted
        /// change is the rule this class adds - a user's own unrelated edit to
        /// it is not ours to commit.
        /// </summary>
        private static List<string> PendingFiles(string repo)
        {
            var files = new List<string>();
            var status = Git.Run(repo, "status", "--porcelain", "--untracked-files=all", "--", HooksDir, ".gitattributes");
            if (!status.Ok) return files;

            foreach (var line in status.Lines)
            {
                if (line.Length < 4) continue;
                var code = line.Substring(0, 2);
                var path = line.Substring(3).Trim().Trim('"');
                if (code.Contains("D")) continue;   // a deletion is someone's decision, not set-up

                if (path == ".gitattributes" && code != "??" && code.Trim() != "A")
                {
                    var diff = Git.Run(repo, "diff", "HEAD", "--", ".gitattributes").Output;
                    var ownOnly = diff.Split('\n')
                        .Select(l => l.TrimEnd('\r'))
                        .Where(l => (l.StartsWith("+") || l.StartsWith("-")) && !l.StartsWith("+++") && !l.StartsWith("---"))
                        .All(l => l == "+" + AttributesLine || l.StartsWith("+# commit-msg hook"));
                    if (!ownOnly) continue;
                }
                files.Add(path);
            }
            return files;
        }

        /// <summary>
        /// Commits the files <see cref="Ensure"/> created - and only those -
        /// as a structured "Git Hook Update" message, on the current branch.
        /// Not pushed.
        ///
        /// <c>git commit --only -- &lt;paths&gt;</c> leaves everything else as
        /// it was: work the user had already staged stays staged and is not
        /// swept into this commit. The files are added first because --only
        /// takes tracked paths, and .githooks/commit-msg is new.
        ///
        /// The message goes through the hook just installed, so it is also
        /// the first test that the hook runs on this machine.
        /// </summary>
        public static GitResult CommitSetup(string repo, IList<string> files)
        {
            if (files == null || files.Count == 0) return new GitResult(0, "nothing to commit");

            var add = new List<string> { "add", "--" };
            add.AddRange(files);
            var added = Git.Run(repo, add.ToArray());
            if (!added.Ok) return added;

            var message = CommitMessage.Build(new CommitFields
            {
                Subject = CommitSubject,
                Body = "Commit message policy hook, added by GitHelper: " + string.Join(", ", files),
                CommitType = "Development",
                Risk = "Low",
                Areas = new[] { "Other" },
            });

            var tmp = CommitMessage.WriteTempFile(message);
            try
            {
                var commit = new List<string> { "commit", "-q", "-F", tmp, "--only", "--" };
                commit.AddRange(files);
                return Git.Run(repo, commit.ToArray());
            }
            finally
            {
                try { File.Delete(tmp); } catch { /* temp file */ }
            }
        }

        /// <summary>The effective core.hooksPath (any config level), or empty.</summary>
        private static string HooksPath(string repo)
        {
            return Git.Line(repo, "config", "--get", "core.hooksPath");
        }

        /// <summary>'.githooks', './.githooks/', or the same folder spelled absolutely.</summary>
        internal static bool PointsAtOurs(string repo, string hooksPath)
        {
            if (string.IsNullOrWhiteSpace(hooksPath)) return false;

            var value = hooksPath.Trim().Replace('\\', '/').TrimEnd('/');
            if (value.StartsWith("./", StringComparison.Ordinal)) value = value.Substring(2);
            if (string.Equals(value, HooksDir, StringComparison.OrdinalIgnoreCase)) return true;

            try
            {
                var full = Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(repo, value));
                var mine = Path.GetFullPath(Path.Combine(repo, HooksDir));
                return string.Equals(full.TrimEnd('\\', '/'), mine.TrimEnd('\\', '/'),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Adds the eol=lf rule unless some line already covers .githooks.
        /// Keeps the file's own line-ending style. Returns true when it wrote.
        /// </summary>
        private static bool EnsureAttributes(string repo)
        {
            var path = Path.Combine(repo, ".gitattributes");
            var text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;

            var covered = text.Split('\n')
                .Select(l => l.Trim())
                .Any(l => l.StartsWith(".githooks", StringComparison.OrdinalIgnoreCase) && l.Contains("eol=lf"));
            if (covered) return false;

            var nl = text.Contains("\r\n") || text.Length == 0 ? "\r\n" : "\n";
            var sb = new StringBuilder(text);
            if (text.Length > 0 && !text.EndsWith("\n", StringComparison.Ordinal)) sb.Append(nl);
            sb.Append("# commit-msg hook: sh needs LF line endings (added by GitHelper)").Append(nl);
            sb.Append(AttributesLine).Append(nl);

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return true;
        }

        /// <summary>The hook script shipped inside GitHelper.exe, with LF line endings.</summary>
        internal static string EmbeddedScript()
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
            {
                if (s == null) throw new InvalidOperationException("Embedded resource '" + ResourceName + "' is missing.");
                using (var reader = new StreamReader(s, new UTF8Encoding(false)))
                    return reader.ReadToEnd().Replace("\r\n", "\n");
            }
        }
    }
}
#endif
