using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHelper
{
    /// <summary>Outcome of one <see cref="AiBody.Generate"/> call.</summary>
    public sealed class AiBodyResult
    {
        public bool Ok { get; set; }
        public string Body { get; set; }

        /// <summary>A generated one-line subject, when one was asked for; else empty.</summary>
        public string Subject { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// The "AI" button: writes the commit body with GitHub Copilot CLI
    /// (<c>copilot -p</c>), with Copilot's auto model selection. UI-free.
    ///
    /// What is sent: the diff of exactly what the dialog would commit (staged
    /// changes if anything is staged, otherwise every modified tracked file,
    /// plus the names of new files when they are included) and the fields
    /// already typed. The request goes into a temporary folder as a file,
    /// because a diff does not fit on a command line (8191 chars through
    /// cmd.exe, which runs npm's copilot.cmd). Copilot may read that folder
    /// and nothing else: shell, write and url tools are denied, so it can
    /// neither run commands nor touch the repository.
    ///
    /// The model is <see cref="DefaultModel"/> - "auto": Copilot routes each
    /// request to a model allowed by the plan and admin policy (Claude Sonnet /
    /// Haiku among them) and bills it at a 10% discount. GITHELPER_AI_MODEL
    /// overrides it with a model id, e.g. "claude-sonnet-4.5" or "gpt-5-mini",
    /// or empty to use the user's own Copilot setting.
    /// </summary>
    public static class AiBody
    {
        public const string DefaultModel = "auto";
        public const int MaxLines = 30;
        public const int MaxDiffChars = 60000;
        public const int TimeoutMs = 180000;

        /// <summary>
        /// The dialog writes "Subject: " (9 chars) before it, and the hook
        /// allows 72 on that line.
        /// </summary>
        public const int MaxSubjectChars = 63;

        private const string SubjectMarker = "SUBJECT:";
        private const string BodyMarker = "BODY:";

        public static string Model
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("GITHELPER_AI_MODEL");
                return env == null ? DefaultModel : env.Trim();
            }
        }

        /// <summary>copilot.exe / copilot.cmd on PATH or in the usual install folders, or null.</summary>
        public static string FindCopilot()
        {
            var dirs = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator)
                .Concat(new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links"),
                });

            foreach (var dir in dirs.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                foreach (var name in new[] { "copilot.exe", "copilot.cmd", "copilot.bat" })
                {
                    try
                    {
                        var path = Path.Combine(dir.Trim().Trim('"'), name);
                        if (File.Exists(path)) return path;
                    }
                    catch (ArgumentException) { /* malformed PATH entry */ }
                }
            }
            return null;
        }

        /// <summary>
        /// The changes the dialog would commit, as a diff - the same choice
        /// InvokeCommit makes: staged if anything is staged, else all tracked
        /// modifications (-a). New files are listed by name when included.
        /// </summary>
        public static string CollectDiff(string repo, bool includeNew)
        {
            var staged = Git.StagedFiles(repo).Length > 0;
            var diff = staged
                ? Git.Run(repo, "diff", "--cached", "--no-color", "--stat", "--patch").Output
                : Git.Run(repo, "diff", "--no-color", "--stat", "--patch").Output;

            var sb = new StringBuilder(diff);
            if (!staged && includeNew)
            {
                var untracked = Git.UntrackedFiles(repo);
                if (untracked.Length > 0)
                {
                    sb.AppendLine().AppendLine().AppendLine("New files (content not shown):");
                    foreach (var f in untracked) sb.AppendLine("  " + f);
                }
            }

            var text = sb.ToString().Trim();
            if (text.Length > MaxDiffChars)
                text = text.Substring(0, MaxDiffChars) + "\n\n[... diff truncated at " + MaxDiffChars + " characters ...]";
            return text;
        }

        /// <param name="wantSubject">
        /// The author left the subject empty: ask for one too, in a fixed
        /// SUBJECT:/BODY: layout that <see cref="SplitAnswer"/> takes apart.
        /// </param>
        public static string BuildRequest(CommitFields f, string diff, bool wantSubject = false)
        {
            var sb = new StringBuilder();
            if (wantSubject)
            {
                sb.AppendLine("Write a git commit message - a one-line SUBJECT and a BODY - for the change below.");
                sb.AppendLine();
                sb.AppendLine("Answer in exactly this layout, nothing before or after it:");
                sb.AppendLine("  " + SubjectMarker + " <the subject>");
                sb.AppendLine("  " + BodyMarker);
                sb.AppendLine("  <the body lines>");
                sb.AppendLine();
                sb.AppendLine("Subject rules:");
                sb.AppendLine("- One line, at most " + (MaxSubjectChars - 3) + " characters, imperative mood");
                sb.AppendLine("  (\"Fix ...\", \"Add ...\"), no trailing period, says what the change does.");
                sb.AppendLine();
                sb.AppendLine("Body rules:");
            }
            else
            {
                sb.AppendLine("Write the BODY of a git commit message for the change below.");
                sb.AppendLine();
                sb.AppendLine("Rules:");
            }
            sb.AppendLine("- At most " + MaxLines + " lines in total. Fewer is better; most changes need 3 to 10.");
            sb.AppendLine("- Each line at most 72 characters.");
            sb.AppendLine("- Plain text only: no markdown, no headings, no code fences. Short '- ' bullets are fine.");
            sb.AppendLine("- Explain what changed and why, for a reviewer. Do not repeat the subject line.");
            sb.AppendLine("- Do NOT include a subject line or any 'Key: value' field lines");
            sb.AppendLine("  (Subject:, Body:, TT:, Commit Type:, Risk:, Area:, Customer:, Other Versions:).");
            if (!wantSubject) sb.AppendLine("- Output the body text only - nothing before or after it.");
            sb.AppendLine("- Do not run commands or open other files; everything you need is in this file.");
            sb.AppendLine();
            sb.AppendLine("IMPORTANT - put your whole answer inside ONE fenced code block (``` on the line");
            sb.AppendLine("before and after it). Your reply is shown through a markdown renderer that joins");
            sb.AppendLine("ordinary lines into one paragraph; only a code block keeps the line breaks.");
            sb.AppendLine();
            sb.AppendLine("Context typed by the author:");
            if (!wantSubject) sb.AppendLine("  Subject: " + f.Subject.Trim());
            if (f.CommitType.Length > 0) sb.AppendLine("  Commit Type: " + f.CommitType);
            if (f.Areas.Length > 0) sb.AppendLine("  Area: " + string.Join(", ", f.Areas));
            if (f.TT.Trim().Length > 0) sb.AppendLine("  TT: " + f.TT.Trim());
            if (f.Body.Trim().Length > 0)
            {
                sb.AppendLine("  Author's notes (use them, improve them):");
                foreach (var l in f.Body.Replace("\r\n", "\n").Split('\n')) sb.AppendLine("    " + l);
            }
            sb.AppendLine();
            sb.AppendLine("The change (git diff):");
            sb.AppendLine("-----");
            sb.AppendLine(diff);
            sb.AppendLine("-----");
            return sb.ToString();
        }

        public static AiBodyResult Generate(string repo, CommitFields fields, bool includeNew)
        {
            if (FindCopilot() == null) return NotInstalled();

            var diff = CollectDiff(repo, includeNew);
            if (diff.Length == 0) return Fail("There are no changes to describe.");

            var wantSubject = fields.Subject.Trim().Length == 0;
            var run = Ask(BuildRequest(fields, diff, wantSubject),
                wantSubject ? "Reply in the SUBJECT:/BODY: layout it asks for, nothing else."
                            : "Reply with the commit message body only.",
                TimeoutMs);
            if (!run.Ok) return run;

            string subject, rawBody;
            SplitAnswer(run.Body, wantSubject, out subject, out rawBody);
            var body = Clean(rawBody);
            return body.Length == 0 && subject.Length == 0
                ? Fail("Copilot returned nothing usable.\n\n" + run.Body)
                : new AiBodyResult { Ok = true, Body = body, Subject = subject };
        }

        /// <summary>
        /// One Copilot request: <paramref name="request"/> is written to a file
        /// in a fresh temp folder - a diff does not fit on a command line - and
        /// Copilot is told to read it and answer. It may read that folder and
        /// nothing else: shell, write and url tools are denied, so it can
        /// neither run commands nor touch the repository. Returns the raw
        /// answer in Body.
        /// </summary>
        public static AiBodyResult Ask(string request, string replyInstruction, int timeoutMs)
        {
            var exe = FindCopilot();
            if (exe == null) return NotInstalled();

            var dir = Path.Combine(Path.GetTempPath(), "githelper-ai-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(dir);
            try
            {
                const string requestName = "request.txt";
                File.WriteAllText(Path.Combine(dir, requestName), request, new UTF8Encoding(false));

                var args = new List<string>
                {
                    "-p", "Read the file " + requestName + " in the current directory and follow its instructions exactly. " +
                          replyInstruction,
                    "-s",
                    "--no-ask-user",
                    "--add-dir", dir,
                    "--allow-tool=read",
                    "--deny-tool=shell",
                    "--deny-tool=write",
                    "--deny-tool=url",
                };
                var model = Model;
                if (model.Length > 0) { args.Add("--model"); args.Add(model); }

                return Run(exe, args, dir, timeoutMs);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* temp folder */ }
            }
        }

        private static AiBodyResult NotInstalled()
        {
            return Fail("GitHub Copilot CLI was not found (copilot.exe / copilot.cmd on PATH).\n\n" +
                        "Set it up from Tools > Set up Copilot (AI).");
        }

        /// <summary>
        /// Takes a SUBJECT:/BODY: answer apart. Forgiving: markers in any case,
        /// markdown bold around them, a missing BODY: marker (everything after
        /// the subject line is the body), or no SUBJECT: at all (then the first
        /// line is the subject). The subject is trimmed of quotes and a trailing
        /// period and cut at a word boundary to <see cref="MaxSubjectChars"/>.
        /// Without <paramref name="wantSubject"/> the whole answer is the body.
        /// </summary>
        public static void SplitAnswer(string raw, bool wantSubject, out string subject, out string body)
        {
            subject = string.Empty;
            body = raw ?? string.Empty;
            if (!wantSubject) return;

            var text = Regex.Replace(body, @"\x1B\[[0-9;?]*[A-Za-z]", string.Empty);
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            while (lines.Count > 0 && (lines[0].Trim().Length == 0 || lines[0].TrimStart().StartsWith("```", StringComparison.Ordinal)))
                lines.RemoveAt(0);
            if (lines.Count == 0) { body = string.Empty; return; }

            Func<string, string> unstar = l => l.Replace("**", string.Empty).Trim();
            var si = lines.FindIndex(l => unstar(l).StartsWith(SubjectMarker, StringComparison.OrdinalIgnoreCase));
            if (si < 0) si = 0;

            var first = unstar(lines[si]);
            if (first.StartsWith(SubjectMarker, StringComparison.OrdinalIgnoreCase))
                first = first.Substring(SubjectMarker.Length);

            // Line breaks lost on the way (markdown rendering joins lines):
            // "SUBJECT: Fix x BODY: More text" - split at the marker.
            var rest = lines.Skip(si + 1).ToList();
            var inline = first.IndexOf(BodyMarker, StringComparison.OrdinalIgnoreCase);
            if (inline >= 0)
            {
                rest.Insert(0, BodyMarker + " " + first.Substring(inline + BodyMarker.Length).Trim());
                first = first.Substring(0, inline);
            }
            subject = TidySubject(first);

            var bi = rest.FindIndex(l => unstar(l).StartsWith(BodyMarker, StringComparison.OrdinalIgnoreCase));
            if (bi >= 0)
            {
                var sameLine = unstar(rest[bi]).Substring(BodyMarker.Length).Trim();
                rest = rest.Skip(bi + 1).ToList();
                if (sameLine.Length > 0) rest.Insert(0, sameLine);
            }
            body = string.Join("\n", rest);
        }

        private static string TidySubject(string s)
        {
            s = s.Trim().Trim('"', '\'', '`').Trim();
            if (s.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase)) s = s.Substring(8).Trim();
            s = s.TrimEnd('.').Trim();
            if (s.Length <= MaxSubjectChars) return s;

            var cut = s.LastIndexOf(' ', MaxSubjectChars);
            return (cut > 20 ? s.Substring(0, cut) : s.Substring(0, MaxSubjectChars)).TrimEnd(',', ';', ':', '-', ' ');
        }

        /// <summary>
        /// Tidies the model's answer into a body: no ANSI colours, code fences,
        /// "Body:" prefix or field lines; trailing spaces and blank edges gone;
        /// at most <see cref="MaxLines"/> lines. Public for the tests.
        /// </summary>
        public static string Clean(string raw)
        {
            var text = Regex.Replace(raw ?? string.Empty, @"\x1B\[[0-9;?]*[A-Za-z]", string.Empty);
            var fieldLine = new Regex(@"^\s*(Subject|TT|Commit Type|Risk|Area|Customer|Other Versions)\s*:", RegexOptions.IgnoreCase);

            var lines = text.Replace("\r\n", "\n").Split('\n')
                .Select(l => l.TrimEnd())
                .Where(l => !l.TrimStart().StartsWith("```", StringComparison.Ordinal))
                .Where(l => !fieldLine.IsMatch(l))
                .ToList();

            if (lines.Count > 0)
                lines[0] = Regex.Replace(lines[0], @"^\s*Body\s*:\s*", string.Empty, RegexOptions.IgnoreCase);

            while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
            while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);

            return string.Join("\r\n", lines.Take(MaxLines));
        }

        private static AiBodyResult Run(string exe, List<string> args, string workDir, int timeoutMs)
        {
            // npm installs copilot.cmd, which only cmd.exe can start. The
            // arguments here are short (the request is in a file), so cmd's
            // command-line limit does not bite.
            var isScript = !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            var line = string.Join(" ", args.Select(QuoteArg));
            var psi = isScript
                ? new ProcessStartInfo("cmd.exe", "/d /s /c \"\"" + exe + "\" " + line + "\"")
                : new ProcessStartInfo(exe, line);

            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.WorkingDirectory = workDir;
            psi.StandardOutputEncoding = new UTF8Encoding(false);
            psi.StandardErrorEncoding = new UTF8Encoding(false);
            psi.EnvironmentVariables["NO_COLOR"] = "1";

            try
            {
                using (var p = Process.Start(psi))
                {
                    if (p == null) return Fail("Could not start " + exe);
                    p.StandardInput.Close();   // nothing will ever be typed

                    var stdout = p.StandardOutput.ReadToEndAsync();
                    var stderr = p.StandardError.ReadToEndAsync();
                    if (!p.WaitForExit(timeoutMs))
                    {
                        KillTree(p.Id);
                        return Fail("Copilot did not answer within " + timeoutMs / 1000 + " seconds.");
                    }
                    p.WaitForExit();

                    var output = stdout.Result;
                    if (p.ExitCode != 0)
                    {
                        return Fail("Copilot failed (exit " + p.ExitCode + "):\n\n" +
                                    (stderr.Result + "\n" + output).Trim());
                    }
                    return new AiBodyResult { Ok = true, Body = output };
                }
            }
            catch (Exception ex)
            {
                return Fail("Could not run Copilot: " + ex.Message);
            }
        }

        private static void KillTree(int pid)
        {
            try
            {
                using (var k = Process.Start(new ProcessStartInfo("taskkill", "/T /F /PID " + pid)
                       { UseShellExecute = false, CreateNoWindow = true }))
                {
                    if (k != null) k.WaitForExit(5000);
                }
            }
            catch { /* best effort */ }
        }

        private static string QuoteArg(string a)
        {
            if (a.Length > 0 && a.IndexOfAny(new[] { ' ', '\t', '"', '&', '|', '<', '>', '^' }) < 0) return a;
            return "\"" + a.Replace("\"", "\\\"") + "\"";
        }

        private static AiBodyResult Fail(string error)
        {
            return new AiBodyResult { Ok = false, Body = string.Empty, Error = error };
        }
    }
}
