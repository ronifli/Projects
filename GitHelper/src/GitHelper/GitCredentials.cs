using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHelper
{
    /// <summary>Where a credential applies: the protocol and host of 'origin'.</summary>
    public sealed class CredentialTarget
    {
        public string Protocol { get; set; }
        public string Host { get; set; }

        /// <summary>The url as 'origin' has it, for showing the user.</summary>
        public string RemoteUrl { get; set; }

        /// <summary>
        /// True for git@host:... remotes, where authentication is an SSH key
        /// and there is no stored username or password to change.
        /// </summary>
        public bool IsSsh { get; set; }

        public override string ToString() { return Protocol + "://" + Host; }
    }

    /// <summary>
    /// Clears and stores the credential git uses for a remote, by talking to
    /// whatever credential helper is configured - on these machines, the Git
    /// Credential Manager, which keeps them in Windows Credential Manager.
    ///
    /// Nothing is stored by this application. The password is written to
    /// <c>git credential</c> on standard input and forgotten; it is never put
    /// in a command line (visible in the process list), a file, or the
    /// registry.
    /// </summary>
    public static class GitCredentials
    {
        /// <summary>
        /// Works out what 'origin' authenticates against. Returns null when the
        /// repository has no origin.
        /// </summary>
        public static CredentialTarget TargetFor(string repo)
        {
            var url = Git.Line(repo, "remote", "get-url", "origin");
            if (string.IsNullOrEmpty(url)) return null;

            // git@host:owner/repo.git - an SSH remote
            var ssh = Regex.Match(url, @"^(?:ssh://)?(?:[^@/]+@)([^:/]+)[:/]");
            if (ssh.Success && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return new CredentialTarget
                {
                    Protocol = "ssh",
                    Host = ssh.Groups[1].Value,
                    RemoteUrl = url,
                    IsSsh = true,
                };
            }

            // https://host/owner/repo.git, possibly with a username in it
            var http = Regex.Match(url, @"^(https?)://(?:[^@/]+@)?([^/:]+)", RegexOptions.IgnoreCase);
            if (!http.Success) return null;

            return new CredentialTarget
            {
                Protocol = http.Groups[1].Value.ToLowerInvariant(),
                Host = http.Groups[2].Value,
                RemoteUrl = url,
                IsSsh = false,
            };
        }

        /// <summary>
        /// Which user git would authenticate as right now, by asking the
        /// credential helper what it has. Empty when nothing is stored.
        ///
        /// Interactivity is turned off twice over - `credential.interactive`
        /// for the Git Credential Manager and GIT_TERMINAL_PROMPT for git
        /// itself - so this only ever *reads* what is already there. Without
        /// that, simply opening the dialog could pop a sign-in prompt.
        ///
        /// The password comes back on the same stream and is ignored: this
        /// returns a name, never a secret.
        /// </summary>
        public static string StoredUser(string repo, CredentialTarget target)
        {
            if (target == null || target.IsSsh) return string.Empty;

            var environment = new Dictionary<string, string>
            {
                { "GIT_TERMINAL_PROMPT", "0" },
                { "GCM_INTERACTIVE", "never" },
            };

            var result = Git.RunWithInput(repo, Describe(target, null, null), environment,
                "-c", "credential.interactive=false", "credential", "fill");

            if (!result.Ok) return string.Empty;

            foreach (var line in result.Output.Split('\n'))
            {
                var text = line.Trim();
                if (text.StartsWith("username=", StringComparison.Ordinal))
                    return text.Substring("username=".Length).Trim();
            }
            return string.Empty;
        }

        /// <summary>The identity commits are attributed to, at any config level.</summary>
        public static string CurrentName(string repo) { return Git.Line(repo, "config", "user.name"); }

        public static string CurrentEmail(string repo) { return Git.Line(repo, "config", "user.email"); }

        /// <summary>
        /// Which config file the identity comes from, in words - "global",
        /// "this repository", "system" - or empty when it is not set anywhere.
        ///
        /// This is the difference between a change that fixes one working copy
        /// and one that fixes the machine: on these PCs user.name lives in the
        /// global .gitconfig, so a repository-local change leaves every other
        /// clone still committing as the old user.
        /// </summary>
        public static string IdentityOrigin(string repo)
        {
            // '<origin>\t<value>', e.g. 'file:C:/Users/x/.gitconfig<TAB>ronifli'
            var line = Git.Line(repo, "config", "--show-origin", "--get", "user.name");
            if (string.IsNullOrEmpty(line)) return string.Empty;

            var tab = line.IndexOf('\t');
            var origin = tab > 0 ? line.Substring(0, tab) : line;
            var file = origin.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                ? origin.Substring("file:".Length)
                : origin;

            // git reports the repository's own file as a relative '.git/config',
            // so match the tail rather than a rooted path.
            if (file.Replace('\\', '/').EndsWith(".git/config", StringComparison.OrdinalIgnoreCase))
                return "this repository";

            var globalFile = Git.Line(repo, "config", "--global", "--list", "--show-origin");
            if (globalFile.IndexOf(file, StringComparison.OrdinalIgnoreCase) >= 0) return "global: " + file;

            return file;
        }

        /// <summary>
        /// Removes user.name and user.email from the config file that actually
        /// defines them, so the next person has to say who they are instead of
        /// committing under the previous author's name.
        ///
        /// The level matters: unsetting the repository's copy when the value
        /// comes from global config removes nothing anyone can see, because
        /// global still answers. <c>--unset-all</c> rather than <c>--unset</c>,
        /// which fails outright on a key that has more than one value.
        ///
        /// Returns the scope used, so the caller can say what it did.
        /// </summary>
        public static string ForgetIdentity(string repo, out GitResult result)
        {
            var origin = IdentityOrigin(repo);
            var scope = origin.StartsWith("global", StringComparison.OrdinalIgnoreCase)
                ? "--global"
                : "--local";

            var name = Git.Run(repo, "config", scope, "--unset-all", "user.name");
            var email = Git.Run(repo, "config", scope, "--unset-all", "user.email");

            // Exit 5 is "that key was not there", which is a success for this.
            result = !name.Ok && name.ExitCode != 5 ? name
                   : !email.Ok && email.ExitCode != 5 ? email
                   : new GitResult(0, string.Empty);

            return scope == "--global" ? "global" : "this repository";
        }

        /// <summary>
        /// Tells the credential helper to forget what it has for this host, so
        /// the next fetch or push asks again. Anything already stored for a
        /// *different* host is untouched.
        /// </summary>
        public static GitResult Clear(string repo, CredentialTarget target)
        {
            var refusal = Refuse(target);
            if (refusal != null) return refusal;

            return Git.RunWithInput(repo, Describe(target, null, null), "credential", "reject");
        }

        /// <summary>
        /// Hands a new username and password (or token) to the credential
        /// helper, which is what a later push will use.
        /// </summary>
        public static GitResult Store(string repo, CredentialTarget target, string username, string password)
        {
            var refusal = Refuse(target);
            if (refusal != null) return refusal;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return new GitResult(-1, "A user name and a password or token are both required.");

            return Git.RunWithInput(repo, Describe(target, username, password), "credential", "approve");
        }

        /// <summary>
        /// Guards the two calls that talk to the credential helper. An SSH
        /// remote authenticates with a key, so storing a user name and password
        /// against it writes an entry nothing will ever read - the UI hides
        /// that case, and this makes it impossible from anywhere else.
        /// </summary>
        private static GitResult Refuse(CredentialTarget target)
        {
            if (target == null)
                return new GitResult(-1, "No 'origin' remote to authenticate against.");

            if (target.IsSsh)
                return new GitResult(-1,
                    "'" + target.RemoteUrl + "' is an SSH remote: it authenticates with a key, " +
                    "not with a stored user name and password.");

            return null;
        }

        /// <summary>
        /// git's credential protocol: key=value lines, LF endings, terminated
        /// by a blank line.
        /// </summary>
        private static string Describe(CredentialTarget target, string username, string password)
        {
            var sb = new StringBuilder();
            sb.Append("protocol=").Append(target.Protocol).Append('\n');
            sb.Append("host=").Append(target.Host).Append('\n');

            if (!string.IsNullOrEmpty(username)) sb.Append("username=").Append(username).Append('\n');
            if (!string.IsNullOrEmpty(password)) sb.Append("password=").Append(password).Append('\n');

            sb.Append('\n');
            return sb.ToString();
        }
    }
}
