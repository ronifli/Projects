using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitHelper
{
    /// <summary>
    /// Started from a share, Git Helper installs itself to
    /// <see cref="TargetFolder"/> and hands over to that copy.
    ///
    /// Running from a network path is slow, breaks when the share is
    /// unavailable, and leaves the Visual Studio External Tool entry pointing
    /// at a path other machines may not have. Copying first means the version
    /// on the share is only ever a distribution point.
    /// </summary>
    public static class LocalDeployment
    {
        /// <summary>Where the tool lives locally - the path the VS entry registers.</summary>
        public const string TargetFolder = @"C:\Camtek\Build\Tools";

        /// <summary>Set on the relaunched copy, so it can never bounce again.</summary>
        public const string NoRelaunchSwitch = "--no-relaunch";

        /// <summary>
        /// Returns true when the local copy has been started and this process
        /// should exit. False means carry on here: either it is already local,
        /// or the copy could not be made and running from the share is better
        /// than not running at all.
        /// </summary>
        public static bool TryRelaunchLocally(string[] args)
        {
            if (args.Any(a => string.Equals(a, NoRelaunchSwitch, StringComparison.OrdinalIgnoreCase)))
                return false;

            var exe = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(exe) || !IsNetworkPath(exe)) return false;

            var target = Path.Combine(TargetFolder, Path.GetFileName(exe));
            if (string.Equals(exe, target, StringComparison.OrdinalIgnoreCase)) return false;

            try
            {
                Deploy(exe, target);
            }
            catch
            {
                // Most likely the local copy is running and holds the file, or
                // C:\Camtek is not writable. An existing copy is still the
                // better one to start; with none, run from the share.
                if (!File.Exists(target)) return false;
            }

            try
            {
                // The repository normally comes from the working directory, so
                // it has to survive the handover. Inheriting it is not enough:
                // a process started from a share often has the share itself as
                // its working directory, and Windows will not give a new
                // process a UNC working directory - it silently substitutes
                // C:\Windows\System32, and the repo is lost. So the folder is
                // passed as an argument as well, which is what the dialog reads
                // first anyway.
                var cwd = Directory.GetCurrentDirectory();
                var localCwd = !IsNetworkPath(cwd) && Directory.Exists(cwd) ? cwd : null;

                var psi = new ProcessStartInfo(target)
                {
                    UseShellExecute = false,
                    WorkingDirectory = localCwd ?? TargetFolder,
                    Arguments = BuildArguments(args, localCwd),
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;   // could not start it; fall through and run here
            }
        }

        /// <summary>
        /// A UNC path, or a path on a mapped network drive. A drive letter that
        /// no longer resolves is treated as local: there is nothing useful to
        /// do about it here.
        /// </summary>
        public static bool IsNetworkPath(string path)
        {
            try
            {
                if (path.StartsWith(@"\\", StringComparison.Ordinal)) return true;

                var root = Path.GetPathRoot(Path.GetFullPath(path));
                if (string.IsNullOrEmpty(root) || root.StartsWith(@"\\", StringComparison.Ordinal)) return true;

                return new DriveInfo(root).DriveType == DriveType.Network;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Copies the exe and the files that must travel with it, skipping any
        /// that are already identical - a share is slow enough that copying
        /// unchanged files every launch would be noticeable.
        /// </summary>
        private static void Deploy(string exe, string target)
        {
            Directory.CreateDirectory(TargetFolder);

            var sourceDir = Path.GetDirectoryName(exe);
            var baseName = Path.GetFileNameWithoutExtension(exe);

            CopyIfDifferent(exe, target);

            // .config carries the DPI setting; .pdb only helps when debugging,
            // and its absence must not fail the deployment.
            foreach (var extra in new[] { Path.GetFileName(exe) + ".config", baseName + ".pdb" })
            {
                var from = Path.Combine(sourceDir, extra);
                if (!File.Exists(from)) continue;
                try { CopyIfDifferent(from, Path.Combine(TargetFolder, extra)); }
                catch { /* the exe is what matters */ }
            }
        }

        private static void CopyIfDifferent(string from, string to)
        {
            if (File.Exists(to))
            {
                var a = new FileInfo(from);
                var b = new FileInfo(to);
                if (a.Length == b.Length && a.LastWriteTimeUtc == b.LastWriteTimeUtc) return;
            }
            File.Copy(from, to, true);
            // Keep the timestamp so the check above holds next time.
            File.SetLastWriteTimeUtc(to, File.GetLastWriteTimeUtc(from));
        }

        /// <summary>
        /// The original arguments, the working directory when the caller gave
        /// no path of its own, and the no-relaunch guard - quoted the way
        /// CommandLineToArgvW reads them back.
        /// </summary>
        private static string BuildArguments(string[] args, string workingDirectory)
        {
            var all = args.ToList();

            // Only when nothing was passed: an explicit path the user gave must
            // stay first, since that is the repository they asked for.
            var hasPath = args.Any(a => !a.StartsWith("--", StringComparison.Ordinal));
            if (!hasPath && workingDirectory != null) all.Insert(0, workingDirectory);

            all.Add(NoRelaunchSwitch);

            var sb = new StringBuilder();
            foreach (var a in all)
            {
                if (sb.Length > 0) sb.Append(' ');
                if (a.Length > 0 && a.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) { sb.Append(a); continue; }

                sb.Append('"');
                var slashes = 0;
                foreach (var c in a)
                {
                    if (c == '\\') { slashes++; continue; }
                    if (c == '"') { sb.Append('\\', slashes * 2 + 1); slashes = 0; }
                    else if (slashes > 0) { sb.Append('\\', slashes); slashes = 0; }
                    sb.Append(c);
                }
                sb.Append('\\', slashes * 2);
                sb.Append('"');
            }
            return sb.ToString();
        }
    }
}
