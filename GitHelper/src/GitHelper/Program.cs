using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace GitHelper
{
    internal static class Program
    {
        /// <summary>
        /// Usage: GitHelper.exe [repoPath] [extraRepo ...]
        /// repoPath defaults to the current directory - Visual Studio's
        /// External Tool entry passes $(SolutionDir) as the initial directory.
        /// </summary>
        [STAThread]
        private static void Main(string[] rawArgs)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            InstallCrashReporting();

            // $(SolutionDir) ends with a backslash, which survives here as a
            // trailing separator and can swallow a closing quote; trim it so
            // the title and the dedupe key look right.
            var args = rawArgs.Select(a => a.Trim().TrimEnd('"')).Where(a => a.Length > 0).ToArray();

            // Launched from a share: install to C:\Camtek\Build\Tools and let
            // that copy do the work, with the same arguments and the same
            // working directory. Nothing else here should run first.
            if (LocalDeployment.TryRelaunchLocally(args)) return;

            // The elevated copy launched by "Install VS Plugin" goes straight
            // to the installer and never touches a repository.
            if (args.Any(a => string.Equals(a, Install.Elevation.InstallSwitch, StringComparison.OrdinalIgnoreCase)))
            {
                Application.Run(new frmInstall());
                return;
            }

            // Switches are not paths: '--no-relaunch' arriving as args[0] would
            // otherwise be taken for the repository to open.
            var paths = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
            var repoPath = paths.Length > 0 ? paths[0] : Directory.GetCurrentDirectory();

            var here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // In order of preference, and all of them optional: the directory
            // the dialog was launched from, the exe's own folder, the built-in
            // repositories, then whatever the user added with "Add repo...".
            // Running from bin\Debug under F5 hits none of the first two, which
            // is exactly when the rest has to carry the dialog on its own.
            var candidates = new[] { repoPath, here }
                .Concat(Settings.BuiltIn)
                .Concat(Settings.Remembered())
                .Concat(paths.Skip(1));

            // "Git Helper Log" from the folder context menu: only the log
            // window, for the repository containing that folder.
            if (args.Any(a => string.Equals(a, ShellIntegration.LogSwitch, StringComparison.OrdinalIgnoreCase)))
            {
                var logRepo = RepoInfo.Discover(new[] { repoPath }).FirstOrDefault();
                if (logRepo == null)
                {
                    MessageBox.Show("Not a git repository:\n\n  " + repoPath,
                        "Git Helper Log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                logRepo.Refresh();
                Application.Run(new frmShowLog(logRepo, true));
                return;
            }

            var repos = RepoInfo.Discover(candidates);
            if (repos.Count == 0)
            {
                MessageBox.Show(
                    "No git repository found.\n\n" +
                    "Looked in:\n" +
                    "  " + repoPath + "  (working directory)\n" +
                    "  " + here + "  (next to GitHelper.exe)\n" +
                    "  " + string.Join("\n  ", Settings.BuiltIn) + "\n\n" +
                    "Launch the dialog from inside a working copy, or pass one as the\n" +
                    "first argument.",
                    "Git Helper", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new frmMain(repos));
        }

        /// <summary>
        /// Turns an unhandled exception into a message that says what happened
        /// and where, instead of the WinForms "Unhandled exception has
        /// occurred" box - which names nothing useful and, on a tool people
        /// run from a toolbar button, produces bug reports that say "it
        /// crashed".
        ///
        /// A UI-thread exception leaves the dialog usable: a failed button
        /// does not mean the typed commit message has to be lost.
        /// </summary>
        private static void InstallCrashReporting()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (sender, e) => Report(e.Exception, false);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => Report(e.ExceptionObject as Exception, true);
        }

        private static void Report(Exception ex, bool fatal)
        {
            if (ex == null) return;

            var where = ex.StackTrace == null
                ? string.Empty
                : string.Join(Environment.NewLine,
                    ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                        .Take(6));

            MessageBox.Show(
                ex.GetType().Name + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                where + Environment.NewLine + Environment.NewLine +
                (fatal ? "Git Helper has to close." : "Git Helper is still running - what you typed is not lost."),
                "Git Helper", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
