using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHelper.Tests
{
    /// <summary>
    /// Build and Parse are a round trip and the "Last commit" button relies on
    /// it, so this checks the pairs the UI cannot easily produce: an empty
    /// body, a body containing prose that looks like a field, and a message in
    /// no known format. Exit code 0 = all passed, 1 = something regressed.
    /// </summary>
    internal static class Program
    {
        private static readonly List<string> Failures = new List<string>();

        private static int Main()
        {
            Console.WriteLine("round trip");

            RoundTrip("all fields", new CommitFields
            {
                Subject = "prevent null deref in barcode scanner",
                Body = "Guard against an empty scan result before dereferencing.",
                TT = "12345",
                Customer = "Acme Semiconductor",
                CommitType = "Bug Fix",
                Risk = "Low",
                Areas = new[] { "CDB", "Orcal" },
                OtherVersions = true,
            });

            RoundTrip("single area", new CommitFields
            {
                Subject = "retune the algorithm",
                CommitType = "Feature",
                Risk = "Medium",
                Areas = new[] { "Algo" },
            });

            RoundTrip("every area", new CommitFields
            {
                Subject = "rename a shared constant",
                CommitType = "Development",
                Risk = "Low",
                Areas = new[] { "AOI", "AVI", "CDB", "Spark", "Algo", "Machine", "Sprint", "Orcal", "Other" },
            });

            RoundTrip("empty body and optionals", new CommitFields
            {
                Subject = "tidy up logging",
                CommitType = "Development",
                Risk = "Medium",
                Areas = new[] { "Machine" },
            });

            // The field block is read from the bottom upward and stops at the
            // first line that isn't a known key, so prose like this must stay
            // in the body.
            RoundTrip("body containing a field-like line", new CommitFields
            {
                Subject = "lower the risk rating",
                Body = "we lowered Risk: High to low\r\nafter the review",
                CommitType = "Feature",
                Risk = "High",
            });

            RoundTrip("multi-line body", new CommitFields
            {
                Subject = "rework the scheduler",
                Body = "first line\r\nsecond line\r\n\r\nfourth line",
                TT = "999",
                CommitType = "Feature",
                Risk = "High",
            });

            Console.WriteLine("omitted fields");
            var msg = CommitMessage.Build(new CommitFields
            {
                Subject = "s",
                CommitType = "Bug Fix",
                Risk = "Low",
            });
            Check("no TT line when empty", !msg.Contains("TT:"), msg);
            Check("no Customer line when empty", !msg.Contains("Customer:"), msg);
            Check("no Body line when empty", !msg.Contains("Body:"), msg);
            Check("Commit Type always written", msg.Contains("Commit Type: Bug Fix"), msg);
            Check("Risk always written", msg.Contains("Risk: Low"), msg);
            Check("Other Versions always written", msg.Contains("Other Versions: No"), msg);

            Console.WriteLine("area formatting");
            var areaMsg = CommitMessage.Build(new CommitFields
            {
                Subject = "s",
                CommitType = "Bug Fix",
                Risk = "Low",
                Areas = new[] { "CDB", "Orcal" },
            });
            Check("comma separated on one line", areaMsg.Contains("Area: CDB, Orcal"), areaMsg);
            Check("area sits after Risk",
                areaMsg.IndexOf("Area:", StringComparison.Ordinal) >
                areaMsg.IndexOf("Risk:", StringComparison.Ordinal), areaMsg);
            Check("area sits before Other Versions",
                areaMsg.IndexOf("Area:", StringComparison.Ordinal) <
                areaMsg.IndexOf("Other Versions:", StringComparison.Ordinal), areaMsg);

            // Extra whitespace in the stored line must not become an empty area
            var spaced = CommitMessage.Parse(
                "Subject: s\r\n\r\nCommit Type: Bug Fix\r\nRisk: Low\r\nArea:  CDB ,  Orcal ,\r\nOther Versions: No");
            Check("parses a loosely spaced list",
                spaced.Areas.Length == 2 && spaced.Areas[0] == "CDB" && spaced.Areas[1] == "Orcal",
                string.Join("|", spaced.Areas));

            Console.WriteLine("placeholders");
            var blank = CommitMessage.Build(new CommitFields { Subject = "s" });
            Check("commit type placeholder", blank.Contains(CommitMessage.TypePlaceholder), blank);
            Check("risk placeholder", blank.Contains(CommitMessage.RiskPlaceholder), blank);
            Check("area placeholder", blank.Contains(CommitMessage.AreaPlaceholder), blank);
            Check("placeholder parses back as no areas",
                CommitMessage.Parse(blank).Areas.Length == 0, blank);

            Console.WriteLine("foreign message degrades");
            // An older commit in no known format: line 1 is the subject, the
            // rest the body, and no fields are invented.
            var f = CommitMessage.Parse("fix the thing\n\nsome explanation\nover two lines\n");
            Check("subject", f.Subject == "fix the thing", "'" + f.Subject + "'");
            Check("body", f.Body == "some explanation\r\nover two lines", "'" + f.Body + "'");
            Check("no commit type invented", f.CommitType.Length == 0, "'" + f.CommitType + "'");
            Check("no risk invented", f.Risk.Length == 0, "'" + f.Risk + "'");
            Check("no areas invented", f.Areas.Length == 0, string.Join("|", f.Areas));
            Check("other versions defaults false", !f.OtherVersions, string.Empty);

            // A commit made before the Area field existed: everything else must
            // still come back, with no areas.
            var older = CommitMessage.Parse(
                "Subject: prevent null deref\r\n\r\nTT: 12345\r\nCommit Type: Bug Fix\r\nRisk: Low\r\nOther Versions: Yes");
            Check("pre-Area message keeps its subject", older.Subject == "prevent null deref", older.Subject);
            Check("pre-Area message keeps its TT", older.TT == "12345", older.TT);
            Check("pre-Area message keeps its type", older.CommitType == "Bug Fix", older.CommitType);
            Check("pre-Area message has no areas", older.Areas.Length == 0, string.Join("|", older.Areas));

#if COMMIT_HOOK
            Console.WriteLine("commit hook path");
            Check(".githooks is ours", CommitHook.PointsAtOurs(@"C:\repo", ".githooks"), string.Empty);
            Check("./.githooks/ is ours", CommitHook.PointsAtOurs(@"C:\repo", "./.githooks/"), string.Empty);
            var someRepo = Path.Combine(Path.GetTempPath(), "repo");
            Check("absolute is ours", CommitHook.PointsAtOurs(someRepo, Path.Combine(someRepo, ".githooks")), string.Empty);
            Check(".husky is not ours", !CommitHook.PointsAtOurs(@"C:\repo", ".husky"), string.Empty);
            Check("empty is not ours", !CommitHook.PointsAtOurs(@"C:\repo", string.Empty), string.Empty);
            Check("embedded script is LF only", !CommitHook.EmbeddedScript().Contains("\r"), string.Empty);
#endif

            Console.WriteLine("body line limit");
            Check("empty box has no lines", LineLimitedTextBox.CountLines(string.Empty) == 0, string.Empty);
            Check("one line", LineLimitedTextBox.CountLines("abc") == 1, string.Empty);
            Check("trailing newline starts a line", LineLimitedTextBox.CountLines("a\r\nb\r\n") == 3, string.Empty);
            var forty = string.Join("\r\n", Enumerable.Range(1, 40).Select(i => "line " + i));
            var cut = LineLimitedTextBox.FirstLines(forty, 30);
            Check("40 lines cut to 30", LineLimitedTextBox.CountLines(cut) == 30, LineLimitedTextBox.CountLines(cut).ToString());
            Check("cut keeps the first lines", cut.StartsWith("line 1\r\n") && cut.EndsWith("line 30"), string.Empty);
            Check("LF input cut too", LineLimitedTextBox.CountLines(LineLimitedTextBox.FirstLines("a\nb\nc", 2)) == 2, string.Empty);
            Check("short text untouched", LineLimitedTextBox.FirstLines("a\r\nb", 30) == "a\r\nb", string.Empty);

#if COMMIT_HOOK
            HookEndToEnd();
            HookSetupCommit();
            HookLfs();
#endif
            LogReading();
            LogSorting();
            LogChangedFiles();
            AiBodyChecks();
            MergeAiChecks();
            MissingLinesChecks();
            MessageAuditChecks();

            Console.WriteLine();
            if (Failures.Count == 0)
            {
                Console.WriteLine("PASS");
                return 0;
            }
            Console.WriteLine("FAILED: " + Failures.Count + " check(s)");
            return 1;
        }

#if COMMIT_HOOK
        /// <summary>
        /// A throwaway repository: Ensure installs, a second Ensure does
        /// nothing, and real 'git commit' calls are accepted or rejected by the
        /// installed hook. Needs git on PATH; skipped when it is missing.
        /// </summary>
        private static void HookEndToEnd()
        {
            Console.WriteLine("commit hook end to end");

            var repo = Path.Combine(Path.GetTempPath(), "githelper-hook-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(repo);
            try
            {
                if (!Git.Run(repo, "init", "-q").Ok) { Console.WriteLine("  skip  git not available"); return; }
                Git.Run(repo, "config", "user.name", "Hook Test");
                Git.Run(repo, "config", "user.email", "hook@test.invalid");

                var first = CommitHook.Ensure(repo);
                Check("first run installs", first.State == HookSetup.Installed, first.State + " " + first.Detail);
                Check("hook file written", File.Exists(CommitHook.HookFile(repo)), string.Empty);
                Check(".gitattributes written", File.ReadAllText(Path.Combine(repo, ".gitattributes")).Contains("eol=lf"), string.Empty);
                Check("hook is active", CommitHook.IsActive(repo), string.Empty);

                var again = CommitHook.Ensure(repo);
                Check("uncommitted set-up is still pending", again.State == HookSetup.Installed, again.State.ToString());

                MakeHooksExecutable(repo);
                Check("set-up committed", CommitHook.CommitSetup(repo, again.Pending).Ok, string.Empty);

                var second = CommitHook.Ensure(repo);
                Check("run after commit changes nothing", second.State == HookSetup.AlreadyActive, second.State.ToString());

                File.WriteAllText(Path.Combine(repo, "a.txt"), "x");
                Git.Run(repo, "add", "-A");
                Check("bad message rejected", !CommitWith(repo, "no tag here"), string.Empty);

                var good = CommitMessage.Build(new CommitFields
                {
                    Subject = "add the hook",
                    Body = "One long line of body text, well past seventy-two characters, as the dialog's body box produces it.",
                    CommitType = "Development",
                    Risk = "Low",
                    Areas = new[] { "Other" },
                });
                Check("dialog message accepted", CommitWith(repo, good), string.Empty);

                // The largest message the dialog allows: a 30-line body and
                // every optional field.
                var full = CommitMessage.Build(new CommitFields
                {
                    Subject = "largest message the dialog allows",
                    Body = string.Join("\r\n", Enumerable.Range(1, 30).Select(i => "body line " + i)),
                    TT = "12345",
                    Customer = "Acme",
                    CommitType = "Feature",
                    Risk = "Medium",
                    Areas = new[] { "CDB", "Algo" },
                    OtherVersions = true,
                });
                File.WriteAllText(Path.Combine(repo, "b.txt"), "y");
                Git.Run(repo, "add", "-A");
                Check("30-line body with all fields accepted", CommitWith(repo, full), string.Empty);

                Git.Run(repo, "config", "--unset", "core.hooksPath");
                var third = CommitHook.Ensure(repo);
                Check("config missing, file present: enable only", third.State == HookSetup.Enabled, third.State.ToString());

                Git.Run(repo, "config", "core.hooksPath", ".husky");
                var fourth = CommitHook.Ensure(repo);
                Check("foreign hooksPath left alone", fourth.State == HookSetup.OtherHooksPath, fourth.State.ToString());
            }
            finally
            {
                try { Directory.Delete(repo, true); } catch { /* read-only .git objects; temp folder */ }
            }
        }

        /// <summary>
        /// The automatic "Git Hook Update" commit: only the hook files go in,
        /// work the user had staged stays staged, and the message is the
        /// dialog's structured format - accepted by the hook it installs.
        /// </summary>
        private static void HookSetupCommit()
        {
            Console.WriteLine("hook setup commit");

            var repo = Path.Combine(Path.GetTempPath(), "githelper-hookc-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(repo);
            try
            {
                if (!Git.Run(repo, "init", "-q").Ok) { Console.WriteLine("  skip  git not available"); return; }
                Git.Run(repo, "config", "user.name", "Hook Test");
                Git.Run(repo, "config", "user.email", "hook@test.invalid");

                File.WriteAllText(Path.Combine(repo, "work.txt"), "user's own change");
                Git.Run(repo, "add", "work.txt");

                var setup = CommitHook.Ensure(repo);
                MakeHooksExecutable(repo);

                var commit = CommitHook.CommitSetup(repo, setup.Pending);
                Check("setup commit succeeds", commit.Ok, commit.Output);

                var files = Git.Run(repo, "show", "--name-only", "--format=", "HEAD").Lines;
                Check("commit holds only the hook files",
                    files.Length == 2 && files.Contains(".githooks/commit-msg") && files.Contains(".gitattributes"),
                    string.Join(",", files));

                var parsed = CommitMessage.Parse(Git.Run(repo, "log", "-1", "--format=%B").Output);
                Check("subject is Git Hook Update", parsed.Subject == CommitHook.CommitSubject, parsed.Subject);
                Check("structured message", CommitMessage.IsStructured(Git.Run(repo, "log", "-1", "--format=%B").Output), string.Empty);
                Check("commit type Development", parsed.CommitType == "Development", parsed.CommitType);

                var staged = Git.StagedFiles(repo);
                Check("user's staged work untouched", staged.Length == 1 && staged[0] == "work.txt", string.Join(",", staged));
            }
            finally
            {
                try { Directory.Delete(repo, true); } catch { /* temp folder */ }
            }
        }

        /// <summary>
        /// A repository that uses Git LFS gets the four LFS hooks in .githooks
        /// as well, committed in the same "Git Hook Update"; a user's own
        /// uncommitted edit to .gitattributes keeps that file out of it.
        /// </summary>
        private static void HookLfs()
        {
            Console.WriteLine("commit hook with git lfs");

            var repo = Path.Combine(Path.GetTempPath(), "githelper-hookl-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(repo);
            try
            {
                if (!Git.Run(repo, "init", "-q").Ok || !Git.Run(repo, "lfs", "version").Ok)
                {
                    Console.WriteLine("  skip  git or git-lfs not available");
                    return;
                }
                Git.Run(repo, "config", "user.name", "Hook Test");
                Git.Run(repo, "config", "user.email", "hook@test.invalid");

                var attributes = Path.Combine(repo, ".gitattributes");
                File.WriteAllText(attributes, "*.bin filter=lfs diff=lfs merge=lfs -text\n");
                Git.Run(repo, "add", ".gitattributes");
                Git.Run(repo, "commit", "-q", "-m", "lfs");

                // the user's own edit, not committed yet
                File.AppendAllText(attributes, "*.psd filter=lfs diff=lfs merge=lfs -text\n");

                var setup = CommitHook.Ensure(repo);
                Check("lfs repo installs", setup.State == HookSetup.Installed, setup.State + " " + setup.Detail);
                foreach (var h in CommitHook.LfsHooks)
                {
                    Check("lfs hook " + h + " written", File.Exists(Path.Combine(repo, ".githooks", h)), string.Empty);
                    Check("lfs hook " + h + " pending", setup.Pending.Contains(".githooks/" + h), string.Join(",", setup.Pending));
                }
                Check("user-edited .gitattributes not taken", !setup.Pending.Contains(".gitattributes"), string.Join(",", setup.Pending));

                MakeHooksExecutable(repo);
                var commit = CommitHook.CommitSetup(repo, setup.Pending);
                Check("lfs set-up committed", commit.Ok, commit.Output);

                var files = Git.Run(repo, "show", "--name-only", "--format=", "HEAD").Lines;
                Check("commit holds commit-msg and the 4 lfs hooks", files.Length == 5, string.Join(",", files));
                Check("user's .gitattributes edit still uncommitted",
                    Git.DirtyFiles(repo).Contains(".gitattributes"), string.Empty);
            }
            finally
            {
                try { Directory.Delete(repo, true); } catch { /* temp folder */ }
            }
        }


#endif
        /// <summary>
        /// Git > Show Log: newest first, structured fields parsed, free-form
        /// commits flagged, multi-line bodies intact, search over the fields.
        /// </summary>
        private static void LogReading()
        {
            Console.WriteLine("show log");

            var repo = Path.Combine(Path.GetTempPath(), "githelper-log-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(repo);
            try
            {
                if (!Git.Run(repo, "init", "-q").Ok) { Console.WriteLine("  skip  git not available"); return; }
                Git.Run(repo, "config", "user.name", "Log Test");
                Git.Run(repo, "config", "user.email", "log@test.invalid");

                Check("empty repository reads as no commits", CommitLog.Read(repo, 100).Count == 0, string.Empty);

                File.WriteAllText(Path.Combine(repo, "a.txt"), "1");
                Git.Run(repo, "add", "-A");
                CommitWith(repo, "plain old commit\n\nsecond paragraph");

                File.WriteAllText(Path.Combine(repo, "a.txt"), "2");
                Git.Run(repo, "add", "-A");
                CommitWith(repo, CommitMessage.Build(new CommitFields
                {
                    Subject = "fix the scanner",
                    Body = "line one\r\nline two",
                    TT = "4711",
                    CommitType = "Bug Fix",
                    Risk = "High",
                    Areas = new[] { "CDB", "Spark" },
                }));

                var log = CommitLog.Read(repo, 100);
                Check("two commits", log.Count == 2, log.Count.ToString());
                if (log.Count == 2)
                {
                    Check("newest first", log[0].Fields.Subject == "fix the scanner", log[0].Fields.Subject);
                    Check("structured flagged", log[0].IsStructured && !log[1].IsStructured, string.Empty);
                    Check("fields parsed", log[0].Fields.TT == "4711" && log[0].Fields.Risk == "High"
                                           && log[0].AreaText == "CDB, Spark", log[0].AreaText);
                    Check("multi-line body kept", log[0].Fields.Body.Contains("line two"), log[0].Fields.Body);
                    Check("plain commit subject is line 1", log[1].Fields.Subject == "plain old commit", log[1].Fields.Subject);
                    Check("plain commit has no type", log[1].Fields.CommitType.Length == 0, log[1].Fields.CommitType);
                    Check("sha is full", log[0].Sha.Length == 40, log[0].Sha);
                    Check("search by TT", log[0].Matches("4711") && !log[1].Matches("4711"), string.Empty);
                    Check("search by area, any case", log[0].Matches("spark"), string.Empty);
                    Check("search in body", log[1].Matches("second paragraph"), string.Empty);
                    Check("stat lists the file", CommitLog.Stat(repo, log[0].Sha).Contains("a.txt"), string.Empty);
                }
                Check("limit honoured", CommitLog.Read(repo, 1).Count == 1, string.Empty);

                // Messages written by hand under the hook: fields found
                // wherever the keys are; no Commit Type means no fields at all.
                var hand = CommitLog.Parse("\u001e" + new string('a', 40) + "\u001f" + "aaaaaaa\u001f2026-01-01 10:00\u001fX\u001f" +
                                           "Fix zone export\n\nCommit Type: Feature\nArea: CDB, AOI\nTT: 99\nRisk: Medium\n");
                Check("hand-written message parsed", hand.Count == 1 && hand[0].IsStructured
                      && hand[0].Fields.CommitType == "Feature" && hand[0].AreaText == "CDB, AOI"
                      && hand[0].Fields.TT == "99" && hand[0].Fields.Risk == "Medium",
                      hand.Count == 1 ? hand[0].Fields.CommitType + "|" + hand[0].AreaText : "none");

                var inSubject = CommitLog.ParseFields("Commit Type: Bugfix - null check");
                Check("Commit Type in the subject line", inSubject.CommitType == "Bugfix - null check", inSubject.CommitType);

                var prose = CommitLog.Parse("\u001e" + new string('b', 40) + "\u001f" + "bbbbbbb\u001f2026-01-01 10:00\u001fX\u001f" +
                                            "Tune threshold\n\nRisk: High was too cautious\nTT: 5\n");
                Check("no Commit Type: no fields", prose.Count == 1 && !prose[0].IsStructured
                      && prose[0].Fields.Risk.Length == 0 && prose[0].Fields.TT.Length == 0, string.Empty);

                // From origin: a bare remote, one pushed commit, one local only.
                var remote = repo + "-remote.git";
                Git.Run(null, "init", "-q", "--bare", remote);
                Git.Run(repo, "remote", "add", "origin", remote);
                var branch = Git.Line(repo, "rev-parse", "--abbrev-ref", "HEAD");
                Git.Run(repo, "push", "-q", "-u", "origin", branch);
                File.WriteAllText(Path.Combine(repo, "a.txt"), "3");
                Git.Run(repo, "add", "-A");
                CommitWith(repo, "local only, not pushed");

                Check("fetch works", CommitLog.Fetch(repo).Ok, string.Empty);
                var upstream = Git.Upstream(repo);
                var local = CommitLog.Read(repo, 0);
                var origin = CommitLog.Read(repo, 0, upstream);
                Check("local shows the unpushed commit", local.Count == 3 && local[0].Fields.Subject == "local only, not pushed", local.Count.ToString());
                Check("origin does not", origin.Count == 2 && origin[0].Fields.Subject == "fix the scanner", origin.Count.ToString());
                try { Directory.Delete(remote, true); } catch { /* temp */ }
            }
            finally
            {
                try { Directory.Delete(repo, true); } catch { /* temp folder */ }
            }
        }

        /// <summary>The files list under the message: status, renames, counts, binaries, root commit.</summary>
        private static void LogChangedFiles()
        {
            Console.WriteLine("show log changed files");

            var repo = Path.Combine(Path.GetTempPath(), "githelper-files-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(repo);
            try
            {
                if (!Git.Run(repo, "init", "-q").Ok) { Console.WriteLine("  skip  git not available"); return; }
                Git.Run(repo, "config", "user.name", "Files Test");
                Git.Run(repo, "config", "user.email", "files@test.invalid");

                var body = string.Join("\n", Enumerable.Range(1, 20).Select(i => "line " + i)) + "\n";
                File.WriteAllText(Path.Combine(repo, "keep.cs"), "a\n");
                File.WriteAllText(Path.Combine(repo, "old name.cs"), body);
                File.WriteAllText(Path.Combine(repo, "gone.cs"), "x\n");
                Git.Run(repo, "add", "-A");
                CommitWith(repo, "root");

                var root = CommitLog.ChangedFiles(repo, Git.Line(repo, "rev-parse", "HEAD"));
                Check("root commit: all files Added", root.Count == 3 && root.All(f => f.Status == "Added"), string.Join(",", root.Select(f => f.Status)));

                File.WriteAllText(Path.Combine(repo, "keep.cs"), "a\nb\n");
                Git.Run(repo, "mv", "old name.cs", "new name.cs");
                File.Delete(Path.Combine(repo, "gone.cs"));
                File.WriteAllText(Path.Combine(repo, "added.cs"), "1\n2\n3\n");
                File.WriteAllBytes(Path.Combine(repo, "image.bin"), new byte[] { 0, 1, 2, 0, 255 });
                Git.Run(repo, "add", "-A");
                CommitWith(repo, "changes");

                var files = CommitLog.ChangedFiles(repo, Git.Line(repo, "rev-parse", "HEAD"));
                Func<string, ChangedFile> by = p => files.FirstOrDefault(f => f.Path == p);
                Check("five files", files.Count == 5, string.Join(",", files.Select(f => f.Code + ":" + f.Path)));
                Check("modified +1 -0", by("keep.cs") != null && by("keep.cs").Status == "Modified" && by("keep.cs").Lines == "+1 -0",
                      by("keep.cs") == null ? "missing" : by("keep.cs").Lines);
                Check("rename with a space in the name", by("new name.cs") != null && by("new name.cs").Status == "Renamed"
                      && by("new name.cs").OldPath == "old name.cs", by("new name.cs") == null ? "missing" : by("new name.cs").DisplayPath);
                Check("deleted", by("gone.cs") != null && by("gone.cs").Status == "Deleted" && by("gone.cs").Lines == "+0 -1", string.Empty);
                Check("added +3", by("added.cs") != null && by("added.cs").Status == "Added" && by("added.cs").Lines == "+3 -0", string.Empty);
                Check("binary", by("image.bin") != null && by("image.bin").Lines == "binary", by("image.bin") == null ? "missing" : by("image.bin").Lines);

                // File compare: both sides extracted byte for byte
                Console.WriteLine("file compare extraction");
                Check("label", FileCompare.Label("src/new name.cs", "abc1234^") == "new name (abc1234^).cs", FileCompare.Label("src/new name.cs", "abc1234^"));
                var tmp = Path.Combine(Path.GetTempPath(), "githelper-cmp-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(tmp);
                try
                {
                    string err;
                    var keep = by("keep.cs");
                    Check("before side", FileCompare.Extract(repo, keep.OldRev, keep.Path, Path.Combine(tmp, "l"), out err)
                          && File.ReadAllText(Path.Combine(tmp, "l")) == "a\n", err);
                    Check("after side", FileCompare.Extract(repo, keep.NewRev, keep.Path, Path.Combine(tmp, "r"), out err)
                          && File.ReadAllText(Path.Combine(tmp, "r")) == "a\nb\n", err);
                    var bin = by("image.bin");
                    Check("binary bytes intact", FileCompare.Extract(repo, bin.NewRev, bin.Path, Path.Combine(tmp, "b"), out err)
                          && File.ReadAllBytes(Path.Combine(tmp, "b")).SequenceEqual(new byte[] { 0, 1, 2, 0, 255 }), err);
                    var ren = by("new name.cs");
                    Check("rename: before side from the old path", FileCompare.Extract(repo, ren.OldRev, ren.OldPath, Path.Combine(tmp, "o"), out err)
                          && File.ReadAllText(Path.Combine(tmp, "o")).StartsWith("line 1"), err);
                    Check("missing side is an empty file", FileCompare.Extract(repo, null, "x", Path.Combine(tmp, "e"), out err)
                          && new FileInfo(Path.Combine(tmp, "e")).Length == 0, err);
                }
                finally
                {
                    foreach (var f in Directory.GetFiles(tmp)) File.SetAttributes(f, FileAttributes.Normal);
                    Directory.Delete(tmp, true);
                }
            }
            finally
            {
                try { Directory.Delete(repo, true); } catch { /* temp folder */ }
            }
        }

        private static void LogSorting()
        {
            Console.WriteLine("show log sorting");

            Func<string, string, string, string, string, LogEntry> e = (pSha, pDate, pType, pTT, pRisk) => new LogEntry
            {
                Sha = pSha, Date = pDate, Author = "A",
                Fields = new CommitFields { Subject = pSha, CommitType = pType, TT = pTT, Risk = pRisk },
            };
            // git order: newest first
            var list = new List<LogEntry>
            {
                e("c1", "2026-09-03 10:00", "Feature", "10", "High"),
                e("c2", "2026-09-02 10:00", "", "", ""),
                e("c3", "2026-09-01 10:00", "Bug Fix", "9", "Low"),
                e("c4", "2026-08-31 10:00", "Feature", "ABC", "Medium"),
            };
            Func<List<LogEntry>, string> order = l => string.Join(",", l.Select(x => x.Sha));

            var tt = order(LogSort.Sort(list, LogColumn.TT, false));
            Check("TT numeric, text after, blank last", tt == "c3,c1,c4,c2", tt);
            var ttDown = order(LogSort.Sort(list, LogColumn.TT, true));
            Check("TT descending, blank still last", ttDown == "c4,c1,c3,c2", ttDown);

            var risk = order(LogSort.Sort(list, LogColumn.Risk, false));
            Check("risk Low < Medium < High", risk == "c3,c4,c1,c2", risk);

            var type = order(LogSort.Sort(list, LogColumn.CommitType, false));
            Check("type alphabetical, ties keep git order", type == "c3,c1,c4,c2", type);

            var date = order(LogSort.Sort(list, LogColumn.Date, false));
            Check("date ascending = oldest first", date == "c4,c3,c2,c1", date);

            Check("input list untouched", order(list) == "c1,c2,c3,c4", order(list));

            Console.WriteLine("show log column filters");
            Func<string[], string> passing = f => string.Join(",", list.Where(x => LogSort.PassesColumnFilters(x, f)).Select(x => x.Sha));
            var none = new string[8];
            Check("no filters: all rows", passing(none) == "c1,c2,c3,c4", passing(none));

            var byType = new string[8]; byType[(int)LogColumn.CommitType] = "feat";
            Check("type filter, any case", passing(byType) == "c1,c4", passing(byType));

            var both = new string[8]; both[(int)LogColumn.CommitType] = "feature"; both[(int)LogColumn.Risk] = "high";
            Check("two filters combine (AND)", passing(both) == "c1", passing(both));

            var byDate = new string[8]; byDate[(int)LogColumn.Date] = "2026-09";
            Check("date filter by month", passing(byDate) == "c1,c2,c3", passing(byDate));

            var blank = new string[8]; blank[(int)LogColumn.TT] = "   ";
            Check("blank filter ignored", passing(blank) == "c1,c2,c3,c4", passing(blank));

            var noHit = new string[8]; noHit[(int)LogColumn.TT] = "777";
            Check("no match: empty list", passing(noHit) == string.Empty, passing(noHit));
        }

        /// <summary>
        /// The AI button's plumbing without the model: the answer is tidied
        /// into a body, the request carries the rules and the diff of exactly
        /// what would be committed.
        /// </summary>
        private static void AiBodyChecks()
        {
            Console.WriteLine("ai body");

            Check("user name accepted", CopilotSetup.IsPlausibleUser("roni-fli"), string.Empty);
            Check("token placeholder rejected", !CopilotSetup.IsPlausibleUser("PersonalAccessToken"), string.Empty);
            Check("oauth2 placeholder rejected", !CopilotSetup.IsPlausibleUser("oauth2"), string.Empty);
            Check("pasted token rejected", !CopilotSetup.IsPlausibleUser("gho_" + new string('x', 36)), string.Empty);
            Check("url rejected", !CopilotSetup.IsPlausibleUser("https://github.com"), string.Empty);
            Check("auth error recognised", CopilotSetup.IsAuthError("Error: No authentication information found. Run copilot login."), string.Empty);
            Check("other error not auth", !CopilotSetup.IsAuthError("Model claude-x is not available"), string.Empty);

            var messy = "\u001b[32mBody: Fixes the null check.\u001b[0m\n```\n- guard empty scan\n```\nCommit Type: Bug Fix\nRisk: Low\n\n";
            var clean = AiBody.Clean(messy);
            Check("ansi, fences, Body: and field lines removed",
                clean == "Fixes the null check.\r\n- guard empty scan", clean.Replace("\r\n", "|"));

            string subj, bodyPart;
            AiBody.SplitAnswer("SUBJECT: Fix null deref in scanner.\nBODY:\nGuard the empty scan.\n- add test", true, out subj, out bodyPart);
            Check("subject split off", subj == "Fix null deref in scanner", subj);
            Check("body after BODY:", AiBody.Clean(bodyPart) == "Guard the empty scan.\r\n- add test", bodyPart);

            AiBody.SplitAnswer("**Subject:** \"Add zone export\"\n\nExports zones to CSV.", true, out subj, out bodyPart);
            Check("markdown, quotes, no BODY: marker", subj == "Add zone export" && AiBody.Clean(bodyPart) == "Exports zones to CSV.", subj + "|" + bodyPart);

            AiBody.SplitAnswer("Rework the calibration loop\nBODY: It now converges faster.", true, out subj, out bodyPart);
            Check("no SUBJECT: marker - line 1 is the subject", subj == "Rework the calibration loop" && AiBody.Clean(bodyPart) == "It now converges faster.", subj + "|" + bodyPart);

            AiBody.SplitAnswer("SUBJECT: " + string.Join(" ", Enumerable.Repeat("word", 30)) + "\nBODY:\nx", true, out subj, out bodyPart);
            Check("long subject cut to 63 at a word", subj.Length <= 63 && subj.EndsWith("word"), subj.Length.ToString());

            AiBody.SplitAnswer("SUBJECT: Add log viewer BODY: Adds Show Log.", true, out subj, out bodyPart);
            Check("line breaks lost: split at BODY:", subj == "Add log viewer" && AiBody.Clean(bodyPart) == "Adds Show Log.", subj + "|" + bodyPart);

            AiBody.SplitAnswer("```\nSUBJECT: Fix scan\nBODY:\nLine one\nLine two\n```", true, out subj, out bodyPart);
            Check("inside a code block", subj == "Fix scan" && AiBody.Clean(bodyPart) == "Line one\r\nLine two", subj + "|" + bodyPart);

            AiBody.SplitAnswer("just a body", false, out subj, out bodyPart);
            Check("no subject wanted: all body", subj.Length == 0 && bodyPart == "just a body", subj);

            var wanting = AiBody.BuildRequest(new CommitFields { CommitType = "Feature" }, "diff", true);
            Check("request asks for the SUBJECT:/BODY: layout", wanting.Contains("SUBJECT:") && wanting.Contains("BODY:"), string.Empty);

            var long40 = string.Join("\n", Enumerable.Range(1, 40).Select(i => "line " + i));
            Check("cut to 30 lines", LineLimitedTextBox.CountLines(AiBody.Clean(long40)) == 30, string.Empty);

            var repo = Path.Combine(Path.GetTempPath(), "githelper-ai-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(repo);
            try
            {
                if (!Git.Run(repo, "init", "-q").Ok) { Console.WriteLine("  skip  git not available"); return; }
                Git.Run(repo, "config", "user.name", "AI Test");
                Git.Run(repo, "config", "user.email", "ai@test.invalid");
                File.WriteAllText(Path.Combine(repo, "scan.cs"), "int a = 1;\n");
                File.WriteAllText(Path.Combine(repo, "other.cs"), "int b = 1;\n");
                Git.Run(repo, "add", "-A");
                Git.Run(repo, "commit", "-q", "-m", "base");

                File.WriteAllText(Path.Combine(repo, "scan.cs"), "int a = 2;\n");
                File.WriteAllText(Path.Combine(repo, "other.cs"), "int b = 2;\n");
                File.WriteAllText(Path.Combine(repo, "new.cs"), "new\n");

                var all = AiBody.CollectDiff(repo, true);
                Check("nothing staged: all tracked changes", all.Contains("scan.cs") && all.Contains("other.cs"), string.Empty);
                Check("new files named when included", all.Contains("new.cs"), string.Empty);
                Check("new files left out when not", !AiBody.CollectDiff(repo, false).Contains("new.cs"), string.Empty);

                Git.Run(repo, "add", "scan.cs");
                var staged = AiBody.CollectDiff(repo, true);
                Check("staged: only what is staged", staged.Contains("int a = 2") && !staged.Contains("int b = 2"), string.Empty);

                var request = AiBody.BuildRequest(new CommitFields
                {
                    Subject = "fix scan", CommitType = "Bug Fix", TT = "123", Areas = new[] { "CDB" },
                }, staged);
                Check("request states the 30-line limit", request.Contains("At most 30 lines"), string.Empty);
                Check("request carries fields and diff", request.Contains("Subject: fix scan") && request.Contains("TT: 123")
                                                         && request.Contains("int a = 2"), string.Empty);
            }
            finally
            {
                try { Directory.Delete(repo, true); } catch { /* temp folder */ }
            }
        }

        /// <summary>
        /// The AI merge check without the AI: verdict lines are read back, and a
        /// branch whose only change is already ported (same patch) or older
        /// than the date never reaches Copilot.
        /// </summary>
        private static void MergeAiChecks()
        {
            Console.WriteLine("ai merge check");

            var parsed = MergeAi.Parse("```\nabc1234f | MISSING | - | null check never ported\n" +
                                       "- `def56789` | **PRESENT** | 99aa11bb | re-done by hand in Scan.cs\n" +
                                       "some chatter\n0123456 | partial | 7777777 | only the UI half\n```");
            Check("three verdicts read", parsed.Count == 3, parsed.Count.ToString());
            Check("missing, no evidence", parsed[0].Verdict == AiVerdict.Missing && parsed[0].Evidence.Length == 0, parsed[0].Evidence);
            Check("markdown bullet/backticks/bold tolerated", parsed[1].Verdict == AiVerdict.Present && parsed[1].Sha == "def56789"
                  && parsed[1].Evidence == "99aa11bb", parsed[1].Sha);
            Check("lower-case verdict", parsed[2].Verdict == AiVerdict.Partial && parsed[2].Reason == "only the UI half", parsed[2].Reason);

            var repo = Path.Combine(Path.GetTempPath(), "githelper-mai-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(repo);
            try
            {
                if (!Git.Run(repo, "init", "-q").Ok) { Console.WriteLine("  skip  git not available"); return; }
                Git.Run(repo, "config", "user.name", "MAI Test");
                Git.Run(repo, "config", "user.email", "mai@test.invalid");
                File.WriteAllText(Path.Combine(repo, "a.cs"), "1\n");
                Git.Run(repo, "add", "-A"); CommitWith(repo, "base");
                var main = Git.Line(repo, "rev-parse", "--abbrev-ref", "HEAD");

                Git.Run(repo, "checkout", "-q", "-b", "rtm");
                File.WriteAllText(Path.Combine(repo, "a.cs"), "1\n2\n");
                Git.Run(repo, "add", "-A"); CommitWith(repo, "fix on rtm");
                var fix = Git.Line(repo, "rev-parse", "HEAD");

                Git.Run(repo, "checkout", "-q", main);
                // other work first, so the port gets a different parent and sha
                File.WriteAllText(Path.Combine(repo, "b.cs"), "other\n");
                Git.Run(repo, "add", "-A"); CommitWith(repo, "other work on main");
                Git.Run(repo, "cherry-pick", fix);   // ported: same patch, new sha

                var r = MergeAi.Check(repo, "rtm", main, DateTime.Today.AddDays(-1));
                Check("ported commit found without AI", r.Items.Count == 1 && r.Items[0].Verdict == AiVerdict.SamePatch,
                      r.Items.Count == 1 ? r.Items[0].VerdictText : r.Items.Count.ToString());

                var none = MergeAi.Check(repo, "rtm", main, DateTime.Today.AddDays(2));
                Check("nothing since a future date", none.Items.Count == 0, none.Items.Count.ToString());
            }
            finally
            {
                try { Directory.Delete(repo, true); } catch { /* temp folder */ }
            }
        }

        /// <summary>The missing-lines log: which changed lines of a commit the target lacks.</summary>
        private static void MissingLinesChecks()
        {
            Console.WriteLine("missing lines");

            var repo = Path.Combine(Path.GetTempPath(), "githelper-ml-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(repo);
            try
            {
                if (!Git.Run(repo, "init", "-q").Ok) { Console.WriteLine("  skip  git not available"); return; }
                Git.Run(repo, "config", "user.name", "ML Test");
                Git.Run(repo, "config", "user.email", "ml@test.invalid");
                File.WriteAllText(Path.Combine(repo, "Scan.cs"), "class Scan\n{\n    void Run()\n    {\n        OldCall(x);\n    }\n}\n");
                Git.Run(repo, "add", "-A"); CommitWith(repo, "base");
                var main = Git.Line(repo, "rev-parse", "--abbrev-ref", "HEAD");

                // source: replaces OldCall with two new lines, adds a new file
                Git.Run(repo, "checkout", "-q", "-b", "rtm");
                File.WriteAllText(Path.Combine(repo, "Scan.cs"),
                    "class Scan\n{\n    void Run()\n    {\n        if (x == null) return;\n        NewCall(x);\n" +
                    "        Log.Warn(\"empty scan\", scanId);\n    }\n}\n");
                File.WriteAllText(Path.Combine(repo, "Extra.cs"), "class Extra { int Value = 42; }\n");
                Git.Run(repo, "add", "-A"); CommitWith(repo, "fix on rtm");
                var fix = Git.Line(repo, "rev-parse", "HEAD");

                // target: ported only the null check, by hand, indented differently; OldCall still there
                Git.Run(repo, "checkout", "-q", main);
                File.WriteAllText(Path.Combine(repo, "Scan.cs"),
                    "class Scan\n{\n    void Run()\n    {\n            if (x == null)   return;\n        OldCall(x);\n" +
                    "        Log.Warn(\"empty scan\");\n    }\n}\n");
                Git.Run(repo, "add", "-A"); CommitWith(repo, "partial port");

                var files = MissingLines.Analyze(repo, fix, main);
                var scan = files.FirstOrDefault(f => f.Path == "Scan.cs");
                var extra = files.FirstOrDefault(f => f.Path == "Extra.cs");
                Check("two files", files.Count == 2, files.Count.ToString());
                Check("null check found despite other spacing", scan != null &&
                      scan.Lines.Any(l => l.Kind == '+' && l.Text.Contains("x == null") && !l.Missing), string.Empty);
                Check("NewCall missing", scan != null && scan.Lines.Any(l => l.Kind == '+' && l.Text.Contains("NewCall") && l.Missing), string.Empty);
                Check("removed OldCall still in target", scan != null && scan.StillRemoved == 1, scan == null ? "-" : scan.StillRemoved.ToString());
                var warn = scan == null ? null : scan.Lines.FirstOrDefault(l => l.Text.Contains("Log.Warn"));
                Check("ported with a change: CHANGED, not missing", warn != null && warn.Status == LineStatus.Changed
                      && warn.TargetText == "Log.Warn(\"empty scan\");" && warn.TargetLineNo == 7, warn == null ? "-" : warn.Status.ToString());
                Check("NewCall vs OldCall is missing, not changed", scan != null &&
                      scan.Lines.First(l => l.Text.Contains("NewCall")).Status == LineStatus.Missing, string.Empty);
                Check("line number of the missing line", scan != null && scan.Lines.First(l => l.Text.Contains("NewCall")).LineNo == 6, string.Empty);
                Check("new file not in target", extra != null && !extra.InTarget && extra.MissingAdded == 1, string.Empty);

                var report = MissingLines.Report(new AiMergeItem { Sha = fix, Source = "rtm", Subject = "fix on rtm", Verdict = AiVerdict.Partial }, main, files);
                Check("report names file, line and source", report.Contains("== Scan.cs") && report.Contains("L6") &&
                      report.Contains("NewCall(x);") && report.Contains("rtm") && report.Contains("FILE NOT IN TARGET")
                      && report.Contains("CHANGED") && report.Contains("T7"), report);
            }
            finally
            {
                try { Directory.Delete(repo, true); } catch { /* temp folder */ }
            }
        }

        /// <summary>Admin: which commits do not follow the structured message.</summary>
        private static void MessageAuditChecks()
        {
            Console.WriteLine("message audit");

            var good = CommitMessage.Build(new CommitFields
            {
                Subject = "fix scan", CommitType = "Bug Fix", Risk = "Low", Areas = new[] { "CDB" },
            });
            Check("dialog message passes", MessageAudit.Problems(good).Count == 0, string.Join("; ", MessageAudit.Problems(good)));

            var free = MessageAudit.Problems("Fixed the thing\n\nsome text");
            Check("free text: no Subject/Type/Risk/Area", free.Contains("no Subject: line") && free.Contains("no Commit Type:")
                  && free.Contains("no Risk:") && free.Contains("no Area:"), string.Join("; ", free));

            var placeholder = MessageAudit.Problems("Subject: x\n\nCommit Type: Feature\nRisk: <Risk Level>\nArea: CDB");
            Check("placeholder counts as empty", placeholder.Count == 1 && placeholder[0] == "empty Risk", string.Join("; ", placeholder));

            var longOne = MessageAudit.Problems("Subject: " + new string('x', 70) + "\n\nCommit Type: F\nRisk: Low\nArea: CDB");
            Check("first line too long", longOne.Count == 1 && longOne[0].StartsWith("first line 79"), string.Join("; ", longOne));

            var repo = Path.Combine(Path.GetTempPath(), "githelper-audit-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(repo);
            try
            {
                if (!Git.Run(repo, "init", "-q").Ok) { Console.WriteLine("  skip  git not available"); return; }
                Git.Run(repo, "config", "user.name", "Audit Test");
                Git.Run(repo, "config", "user.email", "audit@test.invalid");
                File.WriteAllText(Path.Combine(repo, "a.txt"), "1");
                Git.Run(repo, "add", "-A"); CommitWith(repo, good);
                File.WriteAllText(Path.Combine(repo, "a.txt"), "2");
                Git.Run(repo, "add", "-A"); CommitWith(repo, "quick fix");

                var result = MessageAudit.Check(repo, "HEAD", DateTime.Today.AddDays(-1), false);
                Check("two checked, one offender", result.Checked == 2 && result.Items.Count == 1
                      && result.Items[0].FirstLine == "quick fix", result.Checked + "/" + result.Items.Count);
                Check("nothing since a future date", MessageAudit.Check(repo, "HEAD", DateTime.Today.AddDays(2), false).Checked == 0, string.Empty);

                // several branches: a shared commit is listed once, with both branches
                var main = Git.Line(repo, "rev-parse", "--abbrev-ref", "HEAD");
                Git.Run(repo, "checkout", "-q", "-b", "root/rtm1");
                File.WriteAllText(Path.Combine(repo, "a.txt"), "3");
                Git.Run(repo, "add", "-A"); CommitWith(repo, "rtm only, no format");
                var all = MessageAudit.CheckAll(repo, new[] { main, "root/rtm1" }, DateTime.Today.AddDays(-1), false);
                var shared = all.Items.FirstOrDefault(i => i.FirstLine == "quick fix");
                var rtmOnly = all.Items.FirstOrDefault(i => i.FirstLine == "rtm only, no format");
                Check("3 distinct commits checked", all.Checked == 3, all.Checked.ToString());
                Check("shared offender listed once, on both branches", all.Items.Count == 2 && shared != null
                      && shared.Branches.Count == 2, shared == null ? "-" : shared.BranchText);
                Check("rtm-only offender on its branch", rtmOnly != null && rtmOnly.BranchText == "root/rtm1", rtmOnly == null ? "-" : rtmOnly.BranchText);
                Check("release branch rule", MergeCheck.IsReleaseBranch("origin/root/RTM0") && !MergeCheck.IsReleaseBranch("origin/RTM0")
                      && !MergeCheck.IsReleaseBranch("origin/root-main"), string.Empty);
            }
            finally
            {
                try { Directory.Delete(repo, true); } catch { /* temp folder */ }
            }
        }

        /// <summary>Git for Windows runs any hook; elsewhere it must be executable.</summary>
        private static void MakeHooksExecutable(string repo)
        {
            if (Path.DirectorySeparatorChar != '/') return;
            foreach (var f in Directory.GetFiles(Path.Combine(repo, ".githooks")))
                System.Diagnostics.Process.Start("chmod", "+x \"" + f + "\"").WaitForExit();
        }

        private static bool CommitWith(string repo, string message)
        {
            var tmp = CommitMessage.WriteTempFile(message);
            try { return Git.Run(repo, "commit", "-q", "-F", tmp).Ok; }
            finally { File.Delete(tmp); }
        }

        private static void Check(string name, bool condition, string detail)
        {
            if (condition)
            {
                Console.WriteLine("  ok    " + name);
                return;
            }
            Console.WriteLine("  FAIL  " + name + "  " + detail);
            Failures.Add(name);
        }

        private static void RoundTrip(string name, CommitFields f)
        {
            var back = CommitMessage.Parse(CommitMessage.Build(f));

            Check(name + ": subject", back.Subject == f.Subject.Trim(), "'" + back.Subject + "'");
            Check(name + ": body", back.Body == f.Body.Trim(), "'" + back.Body + "'");
            Check(name + ": TT", back.TT == f.TT.Trim(), "'" + back.TT + "'");
            Check(name + ": customer", back.Customer == f.Customer.Trim(), "'" + back.Customer + "'");
            Check(name + ": commit type", back.CommitType == f.CommitType, "'" + back.CommitType + "'");
            Check(name + ": risk", back.Risk == f.Risk, "'" + back.Risk + "'");
            Check(name + ": areas",
                string.Join(",", back.Areas) == string.Join(",", f.Areas ?? new string[0]),
                "'" + string.Join(",", back.Areas) + "'");
            Check(name + ": other versions", back.OtherVersions == f.OtherVersions, back.OtherVersions.ToString());
        }
    }
}
