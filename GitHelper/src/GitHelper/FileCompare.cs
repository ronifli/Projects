using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitHelper
{
    /// <summary>
    /// Opens one file of a commit in ExamDiff Pro: the version before the
    /// commit (first parent) on the left, the commit's version on the right.
    ///
    /// Both sides are written to a temp folder with git's exact bytes
    /// (<c>git cat-file blob</c>, piped straight to disk - no text decoding, so
    /// encodings, line endings and binary files are untouched) and named
    /// "Name (abc1234^).ext" / "Name (abc1234).ext", which is what ExamDiff
    /// shows as the titles. A side that does not exist - an added file has no
    /// "before", a deleted one no "after" - is an empty file. The copies are
    /// read-only and are cleaned up after a day.
    /// </summary>
    public static class FileCompare
    {
        public const string ExamDiffFolder = @"C:\Program Files\ExamDiff Pro";

        private static readonly string TempRoot = Path.Combine(Path.GetTempPath(), "githelper-compare");

        /// <summary>ExamDiff.exe in the ExamDiff Pro folder (or any ExamDiff*.exe there), or null.</summary>
        public static string FindExamDiff()
        {
            var candidates = new[]
            {
                Path.Combine(ExamDiffFolder, "ExamDiff.exe"),
                Path.Combine(ExamDiffFolder, "ExamDiffPro.exe"),
            };
            var found = candidates.FirstOrDefault(File.Exists);
            if (found != null) return found;

            try
            {
                return Directory.Exists(ExamDiffFolder)
                    ? Directory.GetFiles(ExamDiffFolder, "ExamDiff*.exe").FirstOrDefault()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Extracts both sides and starts ExamDiff. Returns false with a reason.</summary>
        public static bool Open(string repo, ChangedFile file, out string error)
        {
            error = null;
            var exe = FindExamDiff();
            if (exe == null)
            {
                error = "ExamDiff Pro was not found in " + ExamDiffFolder + ".";
                return false;
            }

            CleanOld();

            var shortSha = file.NewRev.Length > 7 ? file.NewRev.Substring(0, 7) : file.NewRev;
            var dir = Path.Combine(TempRoot, shortSha + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            Directory.CreateDirectory(dir);

            var oldPath = file.OldPath ?? file.Path;

            // A side that does not exist is an empty file, and its name says
            // why - "Scan (not before abc1234).cs" for a file this commit
            // added - so an empty pane is not mistaken for a failed read.
            var hasOld = file.Code != 'A' && file.OldRev != null;
            var hasNew = file.Code != 'D';
            var left = Path.Combine(dir, Label(oldPath, hasOld ? shortSha + "^" : "not before " + shortSha));
            var right = Path.Combine(dir, Label(file.Path, hasNew ? shortSha : "deleted in " + shortSha));

            if (!Extract(repo, hasOld ? file.OldRev : null, oldPath, left, out error)) return false;
            if (!Extract(repo, hasNew ? file.NewRev : null, file.Path, right, out error)) return false;

            try
            {
                Process.Start(new ProcessStartInfo(exe, "\"" + left + "\" \"" + right + "\"")
                {
                    UseShellExecute = false,
                    WorkingDirectory = dir,
                });
                return true;
            }
            catch (Exception ex)
            {
                error = "Could not start ExamDiff Pro: " + ex.Message;
                return false;
            }
        }

        /// <summary>"src/Scan.cs" at "abc1234^" -> "Scan (abc1234^).cs"</summary>
        internal static string Label(string repoPath, string rev)
        {
            var name = Path.GetFileName(repoPath.Replace('/', Path.DirectorySeparatorChar));
            var ext = Path.GetExtension(name);
            var stem = Path.GetFileNameWithoutExtension(name);
            var label = stem + " (" + rev + ")" + ext;
            return new string(label.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
        }

        /// <summary>
        /// Writes rev:path to <paramref name="target"/> byte for byte; an empty
        /// file when <paramref name="rev"/> is null. Read-only afterwards.
        /// </summary>
        internal static bool Extract(string repo, string rev, string repoPath, string target, out string error)
        {
            error = null;
            try
            {
                if (rev == null)
                {
                    File.WriteAllBytes(target, new byte[0]);
                }
                else
                {
                    var psi = new ProcessStartInfo("git",
                        "-C \"" + repo + "\" cat-file blob \"" + rev + ":" + repoPath.Replace("\"", "\\\"") + "\"")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    using (var p = Process.Start(psi))
                    using (var output = File.Create(target))
                    {
                        if (p == null) { error = "could not start git"; return false; }
                        var stderr = p.StandardError.ReadToEndAsync();
                        p.StandardOutput.BaseStream.CopyTo(output);
                        p.WaitForExit();
                        if (p.ExitCode != 0)
                        {
                            error = "git could not read " + repoPath + " at " + rev + ":\n\n" + stderr.Result.Trim();
                            return false;
                        }
                    }
                }
                File.SetAttributes(target, FileAttributes.ReadOnly);
                return true;
            }
            catch (Exception ex)
            {
                error = "Could not extract " + repoPath + ": " + ex.Message;
                return false;
            }
        }

        /// <summary>Removes compare folders older than a day. Best effort.</summary>
        private static void CleanOld()
        {
            try
            {
                if (!Directory.Exists(TempRoot)) return;
                foreach (var d in Directory.GetDirectories(TempRoot))
                {
                    if (Directory.GetLastWriteTimeUtc(d) > DateTime.UtcNow.AddDays(-1)) continue;
                    foreach (var f in Directory.GetFiles(d)) File.SetAttributes(f, FileAttributes.Normal);
                    Directory.Delete(d, true);
                }
            }
            catch
            {
                // a file still open in ExamDiff - next time
            }
        }
    }
}
