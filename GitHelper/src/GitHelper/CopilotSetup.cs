using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GitHelper
{
    /// <summary>
    /// Gets GitHub Copilot CLI ready for the AI button: is it installed, can it
    /// be installed, is someone signed in, and who is the GitHub user on this
    /// machine. UI-free; frmMain asks the questions.
    ///
    /// Signing in is GitHub's own browser flow (<c>copilot login</c>): GitHub
    /// does not accept a password from a program for Copilot, so the user name
    /// and password are typed on GitHub's page, never into GitHelper.
    /// </summary>
    public static class CopilotSetup
    {
        public static bool IsInstalled { get { return AiBody.FindCopilot() != null; } }

        /// <summary>
        /// How it would be installed, for the question: winget first (GitHub's
        /// recommended way, no Node needed), npm when winget is missing. Null
        /// when neither is available.
        /// </summary>
        public static string InstallDescription
        {
            get
            {
                if (FindOnPath("winget.exe") != null) return "winget install GitHub.Copilot";
                if (FindOnPath("npm.cmd") != null) return "npm install -g @github/copilot";
                return null;
            }
        }

        /// <summary>Installs for the current user. Long-running: call off the UI thread.</summary>
        public static GitResult Install()
        {
            var winget = FindOnPath("winget.exe");
            if (winget != null)
            {
                return RunHidden(winget,
                    "install --id GitHub.Copilot --exact --silent --disable-interactivity " +
                    "--accept-source-agreements --accept-package-agreements", 600000);
            }

            var npm = FindOnPath("npm.cmd");
            if (npm != null)
                return RunHidden("cmd.exe", "/d /s /c \"\"" + npm + "\" install -g @github/copilot\"", 600000);

            return new GitResult(-1, "Neither winget nor npm was found, so Copilot CLI cannot be installed automatically.\n\n" +
                                     "Install it by hand: https://docs.github.com/copilot/how-tos/copilot-cli");
        }

        /// <summary>
        /// Best guess that Copilot CLI already has a login, without starting it
        /// (which takes seconds and may use a request): a token in the
        /// environment, a Windows credential Copilot wrote, or an earlier
        /// successful sign-in / AI answer recorded by GitHelper.
        /// </summary>
        public static bool LooksSignedIn()
        {
            if (new[] { "COPILOT_GITHUB_TOKEN", "GH_TOKEN", "GITHUB_TOKEN" }
                .Any(v => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(v))))
                return true;

            if (WindowsCredentials.ForHost("copilot").Count > 0) return true;

            return Settings.CopilotSignedIn;
        }

        /// <summary>
        /// The GitHub user this machine already signs in as - from git's
        /// credential helper for github.com, else from the entries Visual
        /// Studio's GitHub extension stores ("GitHub for Visual Studio -
        /// https://USER@github.com/"). Names only; no secret is read. Empty when
        /// nothing is found.
        /// </summary>
        public static string GitHubUserName(string anyRepo)
        {
            var fromGit = GitCredentials.StoredUser(anyRepo, new CredentialTarget { Protocol = "https", Host = "github.com" });
            if (IsPlausibleUser(fromGit)) return fromGit;

            foreach (var c in WindowsCredentials.ForHost("github.com"))
            {
                if (IsPlausibleUser(c.UserName)) return c.UserName;

                // the user is sometimes only in the target URL: https://USER@github.com/
                var at = c.Target.IndexOf("@github.com", StringComparison.OrdinalIgnoreCase);
                var slashes = c.Target.LastIndexOf("//", at < 0 ? 0 : at, StringComparison.Ordinal);
                if (at > 0 && slashes >= 0)
                {
                    var name = Uri.UnescapeDataString(c.Target.Substring(slashes + 2, at - slashes - 2));
                    if (IsPlausibleUser(name)) return name;
                }
            }
            return string.Empty;
        }

        /// <summary>Tokens and helper placeholders are not user names.</summary>
        internal static bool IsPlausibleUser(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var n = name.Trim();
            var lower = n.ToLowerInvariant();
            if (lower.Contains("token") || lower == "oauth2" || lower == "x-access-token" || lower == "git") return false;
            if (n.StartsWith("gh", StringComparison.Ordinal) && n.Length > 30) return false;   // ghp_/gho_... pasted as a name
            return n.IndexOfAny(new[] { ' ', '/', ':' }) < 0;
        }

        /// <summary>
        /// Opens a console window running <c>copilot login</c> and waits for it
        /// to close. Copilot shows a one-time code and opens the browser at
        /// github.com/login/device; the user signs in there (user name,
        /// password, 2FA - on GitHub's page) and enters the code. Returns true
        /// when the login exited successfully.
        /// </summary>
        public static bool Login(out string error)
        {
            error = null;
            var exe = AiBody.FindCopilot();
            if (exe == null) { error = "GitHub Copilot CLI is not installed."; return false; }

            // A visible console on purpose: the login is interactive (account
            // type, the code to type). /k would leave it open; plain /c closes
            // it when the login finishes, which is the signal we wait for.
            // '||' pauses only on failure, so the error stays readable, and
            // 'exit /b 1' then makes cmd's own exit code say it failed.
            var psi = new ProcessStartInfo("cmd.exe",
                "/d /s /c \"title GitHub Copilot sign-in & \"" + exe + "\" login || " +
                "(echo. & echo Sign-in did not complete. & pause & exit /b 1)\"")
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
            };

            try
            {
                using (var p = Process.Start(psi))
                {
                    if (p == null) { error = "Could not start the sign-in window."; return false; }
                    if (!p.WaitForExit(15 * 60 * 1000))
                    {
                        error = "The sign-in window was left open for 15 minutes - close it and try again.";
                        return false;
                    }
                    if (p.ExitCode != 0)
                    {
                        error = "Sign-in did not complete (exit " + p.ExitCode + ").";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                error = "Could not start the sign-in: " + ex.Message;
                return false;
            }

            Settings.CopilotSignedIn = true;
            return true;
        }

        /// <summary>Does an AI failure look like "not signed in"?</summary>
        public static bool IsAuthError(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var t = text.ToLowerInvariant();
            return t.Contains("login") || t.Contains("log in") || t.Contains("authenticat") ||
                   t.Contains("not signed in") || t.Contains("unauthorized") || t.Contains("401");
        }

        internal static string FindOnPath(string file)
        {
            var dirs = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator)
                .Concat(new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps") });
            foreach (var d in dirs.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                try
                {
                    var path = Path.Combine(d.Trim().Trim('"'), file);
                    if (File.Exists(path)) return path;
                }
                catch (ArgumentException) { }
            }
            return null;
        }

        private static GitResult RunHidden(string exe, string args, int timeoutMs)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
            };
            try
            {
                using (var p = Process.Start(psi))
                {
                    if (p == null) return new GitResult(-1, "could not start " + exe);
                    var o = p.StandardOutput.ReadToEndAsync();
                    var e = p.StandardError.ReadToEndAsync();
                    if (!p.WaitForExit(timeoutMs))
                    {
                        try { p.Kill(); } catch { }
                        return new GitResult(-1, "Install did not finish within " + timeoutMs / 60000 + " minutes.");
                    }
                    p.WaitForExit();
                    return new GitResult(p.ExitCode, (o.Result + e.Result).Trim());
                }
            }
            catch (Exception ex)
            {
                return new GitResult(-1, ex.Message);
            }
        }
    }
}
