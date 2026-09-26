using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace GitHelper.Install
{
    /// <summary>
    /// Writes the External Tools entry into Visual Studio's private registry
    /// hive (privateregistry.bin), which is where VS 2017 and later keep them -
    /// not HKCU. The hive is loaded with reg.exe, patched, and unloaded again.
    ///
    /// Visual Studio must be closed: the file is locked while it runs, and VS
    /// caches its settings in memory and would overwrite the change on exit.
    /// Loading a hive needs administrator rights.
    /// </summary>
    public static class ExternalToolInstaller
    {
        private const string MountPoint = "GitHelperInstall";

        /// <summary>
        /// Registers (or updates) the tool and returns the command name to bind
        /// a shortcut or a toolbar button to, e.g. <c>Tools.ExternalCommand4</c>.
        /// </summary>
        public static string Install(VsInstall vs, string exePath, string title,
                                     string[] legacyTitles, Action<string> log)
        {
            if (!File.Exists(vs.HivePath))
                throw new InvalidOperationException("No private registry hive at:\r\n" + vs.HivePath);

            EnsureHiveUnlocked(vs.HivePath);

            var backup = vs.HivePath + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(vs.HivePath, backup);
            log("Backup: " + backup);

            // Call reg.exe by full path: a PATH inherited from Git Bash (or any
            // shell that rewrites it) can stop a bare 'reg' launching.
            var reg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "reg.exe");
            var loaded = false;
            try
            {
                RunReg(reg, "load \"HKLM\\" + MountPoint + "\" \"" + vs.HivePath + "\"");
                loaded = true;

                var commandName = PatchHive(vs, exePath, title, legacyTitles ?? new string[0], log);
                return commandName;
            }
            finally
            {
                if (loaded)
                {
                    // The hive cannot unload while a RegistryKey still holds it
                    // open; the keys above are disposed, this makes sure the
                    // handles are actually gone.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(300);

                    try
                    {
                        RunReg(reg, "unload \"HKLM\\" + MountPoint + "\"");
                    }
                    catch (Exception ex)
                    {
                        log("WARNING: " + ex.Message);
                        log("Run this by hand: reg unload HKLM\\" + MountPoint);
                    }
                }
            }
        }

        private static string PatchHive(VsInstall vs, string exePath, string title,
                                        string[] legacyTitles, Action<string> log)
        {
            var subKey = MountPoint + @"\Software\Microsoft\VisualStudio\" + vs.Name + @"\External Tools";

            using (var key = Registry.LocalMachine.CreateSubKey(subKey))
            {
                if (key == null) throw new InvalidOperationException("Could not open " + subKey);

                var count = Convert.ToInt32(key.GetValue("ToolNumKeys", 0));

                // Reuse the slot when this tool is already registered, so
                // re-running the installer is safe.
                int? slot = null;
                for (var i = 0; i < count; i++)
                {
                    if (Equals(key.GetValue("ToolTitle" + i) as string, title)) { slot = i; break; }
                }

                // Nothing under the current name: take over the slot of an
                // earlier name instead of adding a second entry beside it.
                var renamedFrom = (string)null;
                if (!slot.HasValue)
                {
                    for (var i = 0; i < count && !slot.HasValue; i++)
                    {
                        var existing = key.GetValue("ToolTitle" + i) as string;
                        if (existing == null) continue;
                        foreach (var legacy in legacyTitles)
                        {
                            if (!Equals(existing, legacy)) continue;
                            slot = i;
                            renamedFrom = legacy;
                            break;
                        }
                    }
                }
                if (renamedFrom != null)
                    log("Renaming the existing '" + renamedFrom + "' entry to '" + title + "'");

                var isNew = !slot.HasValue;
                if (isNew) slot = count;

                key.SetValue("ToolTitle" + slot, title, RegistryValueKind.String);
                key.SetValue("ToolCmd" + slot, exePath, RegistryValueKind.String);
                // No arguments: $(SolutionDir) ends with a backslash, which
                // escapes the closing quote when passed as "$(SolutionDir)".
                // The dialog takes its repo from the working directory instead.
                key.SetValue("ToolArg" + slot, string.Empty, RegistryValueKind.String);
                key.SetValue("ToolDir" + slot, "$(SolutionDir)", RegistryValueKind.String);
                // 0 = none of the checkboxes (no output window, no prompt)
                key.SetValue("ToolOpt" + slot, 0, RegistryValueKind.DWord);

                // Deliberately no ToolSourceKey: that value ties an entry to
                // the package that contributed it, and VS's startup merge drops
                // entries whose source package it cannot find - the tool
                // silently vanishes on the next launch. User tools have none.
                try { key.DeleteValue("ToolSourceKey" + slot, false); } catch { }

                if (isNew) key.SetValue("ToolNumKeys", count + 1, RegistryValueKind.DWord);

                var commandName = "Tools.ExternalCommand" + (slot.Value + 1);

                log((isNew ? "Added" : "Updated") + " '" + title + "' in slot " + slot);
                log("  Command:   " + exePath);
                log("  Arguments: (none)");
                log("  Directory: $(SolutionDir)");
                log("  Bind to:   " + commandName);
                return commandName;
            }
        }

        /// <summary>
        /// A hive that is still locked means a VS process has it open, whatever
        /// the process list says.
        /// </summary>
        private static void EnsureHiveUnlocked(string hivePath)
        {
            try
            {
                using (File.Open(hivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            }
            catch
            {
                throw new InvalidOperationException(
                    "The hive is locked by another process:\r\n" + hivePath +
                    "\r\n\r\nClose all Visual Studio windows (check Task Manager for devenv.exe) and retry.");
            }
        }

        private static void RunReg(string reg, string arguments)
        {
            var psi = new ProcessStartInfo(reg, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (var p = Process.Start(psi))
            {
                if (p == null) throw new InvalidOperationException("Could not start reg.exe");
                var output = (p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd()).Trim();
                p.WaitForExit();
                if (p.ExitCode != 0)
                    throw new InvalidOperationException("reg " + arguments + " failed: " + output);
            }
        }
    }
}
