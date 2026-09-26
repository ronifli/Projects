using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHelper
{
    /// <summary>Result of one git invocation: exit code plus combined output.</summary>
    public sealed class GitResult
    {
        public GitResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
        }

        public int ExitCode { get; private set; }
        public string Output { get; private set; }

        public bool Ok { get { return ExitCode == 0; } }

        /// <summary>Output split into non-empty trimmed lines.</summary>
        public string[] Lines
        {
            get
            {
                return Output
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.TrimEnd('\r'))
                    .Where(l => l.Length > 0)
                    .ToArray();
            }
        }
    }

    /// <summary>
    /// Every call is addressed with <c>git -C &lt;repo&gt;</c>, never "cd then
    /// run", so the repository selected in the dropdown is always the one acted
    /// on.
    /// </summary>
    public static class Git
    {
        public static GitResult Run(string repo, params string[] args)
        {
            return RunWithInput(repo, null, args);
        }

        /// <summary>
        /// Runs git with something written to its standard input.
        ///
        /// This exists for <c>git credential</c>, whose whole protocol is on
        /// stdin. A password must never be passed as an argument: command lines
        /// are readable by any process on the machine through the process list.
        /// </summary>
        public static GitResult RunWithInput(string repo, string input, params string[] args)
        {
            return RunWithInput(repo, input, null, args);
        }

        /// <summary>
        /// As above, with environment variables for the child process - used to
        /// set GIT_TERMINAL_PROMPT=0, so a credential query can never sit there
        /// waiting for an answer nobody is watching for.
        /// </summary>
        public static GitResult RunWithInput(string repo, string input,
                                             IDictionary<string, string> environment, params string[] args)
        {
            var psi = new ProcessStartInfo("git")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = input != null,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
            };

            var all = new List<string>();
            if (!string.IsNullOrEmpty(repo))
            {
                all.Add("-C");
                all.Add(repo);
            }
            all.AddRange(args);
            psi.Arguments = Quote(all);

            if (environment != null)
            {
                foreach (var pair in environment) psi.EnvironmentVariables[pair.Key] = pair.Value;
            }

            try
            {
                using (var p = Process.Start(psi))
                {
                    if (p == null) return new GitResult(-1, "could not start git");

                    if (input != null)
                    {
                        // Git's credential protocol is LF-terminated and ends
                        // with a blank line; the stream must then be closed or
                        // git waits for more.
                        using (var writer = new StreamWriter(p.StandardInput.BaseStream, new UTF8Encoding(false)))
                        {
                            writer.NewLine = "\n";
                            writer.Write(input);
                            writer.Flush();
                        }
                    }

                    // Read both streams before waiting, or a large diff can
                    // fill a pipe buffer and deadlock.
                    var stdout = p.StandardOutput.ReadToEndAsync();
                    var stderr = p.StandardError.ReadToEndAsync();
                    p.WaitForExit();
                    var text = (stdout.Result + stderr.Result).TrimEnd();
                    return new GitResult(p.ExitCode, text);
                }
            }
            catch (Exception ex)
            {
                return new GitResult(-1, ex.Message);
            }
        }

        /// <summary>
        /// .NET Framework has no ProcessStartInfo.ArgumentList, so arguments
        /// are quoted here by the rule CommandLineToArgvW actually uses: wrap
        /// in double quotes, escape an inner quote, and double the backslashes
        /// that precede one. Repository paths with spaces then survive intact.
        /// </summary>
        private static string Quote(IEnumerable<string> args)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
            {
                if (sb.Length > 0) sb.Append(' ');

                if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
                {
                    sb.Append(arg);
                    continue;
                }

                sb.Append('"');
                var slashes = 0;
                foreach (var c in arg)
                {
                    if (c == '\\') { slashes++; continue; }

                    if (c == '"')
                    {
                        sb.Append('\\', slashes * 2 + 1);
                        slashes = 0;
                    }
                    else if (slashes > 0)
                    {
                        sb.Append('\\', slashes);
                        slashes = 0;
                    }
                    sb.Append(c);
                }
                sb.Append('\\', slashes * 2);   // the closing quote doubles these too
                sb.Append('"');
            }
            return sb.ToString();
        }

        /// <summary>First line of output, trimmed; empty when the command failed.</summary>
        public static string Line(string repo, params string[] args)
        {
            var r = Run(repo, args);
            if (!r.Ok) return string.Empty;
            var lines = r.Lines;
            return lines.Length > 0 ? lines[0].Trim() : string.Empty;
        }

        public static string[] StagedFiles(string repo)
        {
            return Run(repo, "diff", "--cached", "--name-only").Lines;
        }

        public static string[] DirtyFiles(string repo)
        {
            return Run(repo, "diff", "--name-only").Lines;
        }

        public static string[] UntrackedFiles(string repo)
        {
            return Run(repo, "ls-files", "--others", "--exclude-standard").Lines;
        }

        /// <summary>Repository root containing <paramref name="path"/>, or null.</summary>
        public static string ResolveRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return null;
            var root = Line(path, "rev-parse", "--show-toplevel");
            if (string.IsNullOrEmpty(root)) return null;
            try { return Path.GetFullPath(root); } catch { return null; }
        }

        public static string Upstream(string repo)
        {
            return Line(repo, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{upstream}");
        }

        /// <summary>
        /// Branches on 'origin', as <c>origin/name</c>, alphabetical.
        /// <c>origin/HEAD</c> is dropped: it is a symbolic ref pointing at the
        /// default branch, not a branch anyone means to check out.
        /// </summary>
        public static string[] RemoteBranches(string repo)
        {
            return Run(repo, "branch", "--remotes", "--format=%(refname:short)").Lines
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Where(l => l.StartsWith("origin/", StringComparison.OrdinalIgnoreCase))
                .Where(l => !l.EndsWith("/HEAD", StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>'origin/RTM0' -> 'RTM0'; anything without a slash unchanged.</summary>
        public static string ShortBranchName(string remoteBranch)
        {
            var slash = remoteBranch.IndexOf('/');
            return slash > 0 ? remoteBranch.Substring(slash + 1) : remoteBranch;
        }

        /// <summary>Is there already a local branch by this name?</summary>
        public static bool LocalBranchExists(string repo, string branch)
        {
            return Run(repo, "show-ref", "--verify", "--quiet", "refs/heads/" + branch).Ok;
        }

        /// <summary>Commits since the last push, or -1 when the branch has no upstream.</summary>
        public static int UnpushedCount(string repo)
        {
            var r = Run(repo, "rev-list", "--count", "@{upstream}..HEAD");
            int n;
            if (r.Ok && int.TryParse(r.Output.Trim(), out n)) return n;
            return -1;
        }

        public static bool HasCommit(string repo)
        {
            return Run(repo, "rev-parse", "--verify", "--quiet", "HEAD").Ok;
        }

        /// <summary>The branch a pull request would target. Falls back to 'main'.</summary>
        public static string DefaultBranch(string repo)
        {
            var refName = Line(repo, "symbolic-ref", "--quiet", "refs/remotes/origin/HEAD");
            if (!string.IsNullOrEmpty(refName))
                return refName.Replace("refs/remotes/origin/", string.Empty).Trim();
            return "main";
        }

        /// <summary>owner/repo parsed out of the 'origin' remote, or empty.</summary>
        public static string GitHubSlug(string repo)
        {
            var url = Line(repo, "remote", "get-url", "origin");
            if (string.IsNullOrEmpty(url)) return string.Empty;

            // https://github.com/OWNER/REPO(.git)  or  git@github.com:OWNER/REPO(.git)
            var m = Regex.Match(url, @"github\.com[:/](?<owner>[^/]+)/(?<repo>[^/]+?)(\.git)?$");
            return m.Success ? m.Groups["owner"].Value + "/" + m.Groups["repo"].Value : string.Empty;
        }
    }
}
