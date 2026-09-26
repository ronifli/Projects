using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace GitHelper.Install
{
    /// <summary>
    /// The keyboard binding and the toolbar button are not plain registry
    /// values - they live in the settings store, and the only supported way in
    /// is a .vssettings import.
    ///
    /// To avoid clobbering anything else you have customised, this exports your
    /// current settings, adds one element to each category, and imports the
    /// file back. Both steps drive devenv.exe from the command line, so each
    /// one starts and closes a Visual Studio instance: expect it to take a
    /// minute or two.
    /// </summary>
    public static class VsSettingsPatcher
    {
        // Toolbar customisations are addressed by command set GUID and numeric
        // ids rather than by name. These are not guesses: they were read back
        // out of an export after adding the button by hand in Tools > Customize.
        private const string StandardToolbar = "{D309F791-903F-11D0-9EFC-00A0C911004F}:00000001";
        // Group 0179 matters - 0172 exists but every stock command in it is
        // removed, so a button added there is never drawn: it verifies as
        // present in the settings and is invisible on screen.
        private const string StandardGroup = "{D309F791-903F-11D0-9EFC-00A0C911004F}:00000179";
        private const string ExternalCommandSet = "{5EFC7975-14BC-11CF-9B2B-00AA00573819}";

        /// <summary>
        /// Binds <paramref name="shortcut"/> to <paramref name="commandName"/>
        /// and optionally puts a button on the Standard toolbar. Returns the
        /// path of the untouched export, kept as a `.orig` alongside.
        /// </summary>
        public static string Apply(VsInstall vs, string commandName, string shortcut,
                                   bool bindShortcut, bool addToolbarButton, Action<string> log)
        {
            if (Process.GetProcessesByName("devenv").Length > 0)
                throw new InvalidOperationException(
                    "Visual Studio is running again - close it and re-run, or bind " +
                    commandName + " by hand.");

            var devenv = vs.FindDevEnv();
            if (devenv == null)
                throw new InvalidOperationException(
                    "Could not locate devenv.exe for " + vs.Name + " - bind " + commandName + " by hand.");

            var temp = Path.GetTempPath();
            var settingsFile = Path.Combine(temp,
                "vs-settings-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".vssettings");
            if (settingsFile.Any(char.IsWhiteSpace) && ShortPath(temp).Any(char.IsWhiteSpace))
                throw new InvalidOperationException("TEMP path contains spaces: " + temp);

            log("Exporting current settings (this starts Visual Studio briefly)...");
            RunDevEnvSettings(devenv, "export", settingsFile, settingsFile, log);
            if (!File.Exists(settingsFile))
                throw new InvalidOperationException("Export produced no file: " + settingsFile);

            File.Copy(settingsFile, settingsFile + ".orig", true);
            log("Exported: " + settingsFile);

            var xml = new XmlDocument { PreserveWhitespace = true };
            xml.Load(settingsFile);

            if (bindShortcut) PatchShortcut(xml, commandName, shortcut, log);
            if (addToolbarButton) PatchToolbar(xml, commandName, log);

            xml.Save(settingsFile);

            log("Importing settings back...");
            RunDevEnvSettings(devenv, "import", settingsFile, vs.VskPath, log);

            return settingsFile + ".orig";
        }

        private static void PatchShortcut(XmlDocument xml, string commandName, string shortcut, Action<string> log)
        {
            var kb = xml.SelectSingleNode("//Category[@RegisteredName='Environment_KeyBindings']");
            if (kb == null)
                throw new InvalidOperationException(
                    "No keyboard category in the exported settings - bind " + commandName + " by hand.");

            var shortcuts = kb.SelectSingleNode("KeyboardShortcuts");
            if (shortcuts == null) shortcuts = kb.AppendChild(xml.CreateElement("KeyboardShortcuts"));

            var userShortcuts = shortcuts.SelectSingleNode("UserShortcuts");
            if (userShortcuts == null) userShortcuts = shortcuts.AppendChild(xml.CreateElement("UserShortcuts"));

            // Drop any previous binding for this command so re-running doesn't
            // stack them, and drop any RemoveShortcut for it too: VS records
            // one when a binding is deleted, and leaving it in place would
            // cancel the binding being added here.
            var stale = userShortcuts.SelectNodes("Shortcut[@Command='" + commandName + "']").Cast<XmlNode>()
                .Concat(userShortcuts.SelectNodes("RemoveShortcut[@Command='" + commandName + "']").Cast<XmlNode>())
                .ToList();
            foreach (var n in stale) userShortcuts.RemoveChild(n);
            if (stale.Count > 0) log("Cleared " + stale.Count + " stale entr(ies) for " + commandName);

            var clash = userShortcuts.SelectNodes("Shortcut").Cast<XmlElement>()
                .Where(e => e.InnerText == shortcut && e.GetAttribute("Scope") == "Global")
                .Select(e => e.GetAttribute("Command"))
                .ToList();
            if (clash.Count > 0)
            {
                log("WARNING: " + shortcut + " is already bound to: " + string.Join(", ", clash));
                log("WARNING: Visual Studio will use the last binding.");
            }

            var el = xml.CreateElement("Shortcut");
            el.SetAttribute("Command", commandName);
            el.SetAttribute("Scope", "Global");
            el.InnerText = shortcut;
            userShortcuts.AppendChild(el);

            log("Added: <Shortcut Command=\"" + commandName + "\" Scope=\"Global\">" + shortcut + "</Shortcut>");
        }

        private static void PatchToolbar(XmlDocument xml, string commandName, Action<string> log)
        {
            // Command id = 0x275 + N for Tools.ExternalCommandN
            var slotNumber = int.Parse(new string(commandName.Where(char.IsDigit).ToArray()));
            var cmdRef = ExternalCommandSet + ":" + (0x275 + slotNumber).ToString("x8");

            var bars = xml.SelectSingleNode("//Category[@RegisteredName='Environment_CommandBars']");
            if (bars == null)
            {
                log("WARNING: no Environment_CommandBars category in the export - skipping the toolbar button.");
                return;
            }

            var cbNode = bars.SelectSingleNode("CommandBars");
            if (cbNode == null)
            {
                log("WARNING: the command bars category is empty - skipping the toolbar button.");
                return;
            }

            var defaults = cbNode.SelectSingleNode("DefaultCustomizations");
            if (defaults == null) defaults = cbNode.AppendChild(xml.CreateElement("DefaultCustomizations"));

            // Sweep the WHOLE document, not just this block: an export can hold
            // more than one <DefaultCustomizations>, and a stale entry left in
            // another one shows up as a second, duplicate button.
            var old = xml.SelectNodes("//add[@Cmd='" + cmdRef + "']").Cast<XmlNode>()
                .Concat(xml.SelectNodes("//remove[@Cmd='" + cmdRef + "']").Cast<XmlNode>())
                .ToList();
            foreach (var n in old) n.ParentNode.RemoveChild(n);
            if (old.Count > 0) log("Cleared " + old.Count + " existing toolbar entr(ies)");

            var add = xml.CreateElement("add");
            add.SetAttribute("Cmd", cmdRef);
            add.SetAttribute("CmdPri", "01800001");
            add.SetAttribute("Group", StandardGroup);
            add.SetAttribute("GroupPri", "00800000");
            add.SetAttribute("Menu", StandardToolbar);
            defaults.AppendChild(add);

            log("Added toolbar button: " + cmdRef + " on the Standard toolbar");
        }

        /// <summary>
        /// Runs one <c>devenv /Command "Tools.ImportandExportSettings /verb:path"</c>
        /// and waits for the work to land.
        ///
        /// The whole command has to arrive as a single /Command argument.
        /// Quoting the path inside it means nested quotes, which Windows splits
        /// apart - VS then reports the pieces as missing files. So the path is
        /// kept space-free (8.3 form if needed) with one level of quoting.
        /// </summary>
        private static void RunDevEnvSettings(string devenv, string verb, string file, string watch, Action<string> log)
        {
            var p = file;
            if (p.Any(char.IsWhiteSpace)) p = ShortPath(file);
            if (p.Any(char.IsWhiteSpace))
                throw new InvalidOperationException("Cannot build a space-free path for: " + file);

            var arguments = "/Command \"Tools.ImportandExportSettings /" + verb + ":" + p + "\"";

            // What tells us the work is done differs per direction: the export
            // writes the file, the import rewrites the keyboard scheme.
            var before = File.Exists(watch) ? new FileInfo(watch).LastWriteTimeUtc : DateTime.MinValue;

            var proc = Process.Start(new ProcessStartInfo(devenv, arguments) { UseShellExecute = false });
            if (proc == null) throw new InvalidOperationException("Could not start " + devenv);

            try
            {
                var deadline = DateTime.UtcNow.AddMinutes(4);
                var done = false;
                while (DateTime.UtcNow < deadline)
                {
                    System.Threading.Thread.Sleep(2000);
                    if (proc.HasExited) { done = true; break; }

                    if (!File.Exists(watch)) continue;

                    var item = new FileInfo(watch);
                    if (item.LastWriteTimeUtc <= before || item.Length == 0) continue;

                    // let it finish flushing, then confirm the size settled
                    System.Threading.Thread.Sleep(3000);
                    if (new FileInfo(watch).Length == item.Length) { done = true; break; }
                }
                if (!done)
                    throw new InvalidOperationException("devenv did not finish the " + verb + " within 4 minutes.");
            }
            finally
            {
                StopDevEnv(proc, log);
            }
        }

        /// <summary>
        /// devenv /Command runs the command and then just sits there with the
        /// IDE open - there is no switch to make it exit - so close it here.
        /// </summary>
        private static void StopDevEnv(Process proc, Action<string> log)
        {
            try
            {
                if (proc.HasExited) return;
                proc.CloseMainWindow();
                if (!proc.WaitForExit(30000))
                {
                    log("WARNING: Visual Studio did not close on request; terminating it.");
                    proc.Kill();
                    proc.WaitForExit(15000);
                }
            }
            catch (Exception ex)
            {
                log("WARNING: could not close Visual Studio: " + ex.Message);
            }
            finally
            {
                proc.Dispose();
            }
        }

        /// <summary>
        /// 8.3 form, so a path with spaces needs no inner quoting. The API only
        /// shortens something that exists, so for a file that is about to be
        /// written the folder is shortened and the name appended.
        /// </summary>
        private static string ShortPath(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                var dir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(dir)) return path;
                return Path.Combine(ShortPath(dir), Path.GetFileName(path));
            }

            var sb = new StringBuilder(260);
            var n = GetShortPathName(path, sb, sb.Capacity);
            return n > 0 && n < sb.Capacity ? sb.ToString() : path;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);
    }
}
