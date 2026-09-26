using System;
using System.Reflection;
using Microsoft.Win32;

namespace GitHelper
{
    /// <summary>
    /// The Explorer folder context-menu entries, so Git Helper can be opened
    /// on the folder that was right-clicked:
    ///
    ///   Open Git Helper   - the commit dialog for that folder's repository
    ///   Git Helper Log    - only the log window (<c>--log</c>)
    ///
    /// Everything is written under <c>HKCU\Software\Classes</c>, which is the
    /// per-user half of HKCR: no elevation, and nothing changed for other
    /// accounts on the machine.
    /// </summary>
    public static class ShellIntegration
    {
        /// <summary>The command-line switch the log entry passes.</summary>
        public const string LogSwitch = "--log";

        private sealed class Verb
        {
            public Verb(string key, string caption, string extraArgs, bool separatorBefore, bool separatorAfter)
            {
                Key = key;
                Caption = caption;
                ExtraArgs = extraArgs;
                SeparatorBefore = separatorBefore;
                SeparatorAfter = separatorAfter;
            }

            public bool SeparatorBefore { get; private set; }
            public bool SeparatorAfter { get; private set; }

            /// <summary>Registry key name - unique; Explorer orders entries by it.</summary>
            public string Key { get; private set; }

            /// <summary>What Explorer shows.</summary>
            public string Caption { get; private set; }

            public string ExtraArgs { get; private set; }
        }

        private static readonly Verb[] Verbs =
        {
            // A separator above the first entry and below the last one keeps
            // the pair together as a group of its own in the menu.
            new Verb("GitHelper", "Open Git Helper", string.Empty, true, false),
            new Verb("GitHelperLog", "Git Helper Log", LogSwitch + " ", false, true),
        };

        // Right-clicking a folder, and right-clicking the empty space inside an
        // open folder: different keys, and people expect both to work.
        private static readonly string[] Roots =
        {
            @"Software\Classes\Directory\shell\",
            @"Software\Classes\Directory\Background\shell\",
        };

        private static string ExePath
        {
            get { return Assembly.GetExecutingAssembly().Location; }
        }

        /// <summary>
        /// %V is the folder that was clicked - for the background verb it is
        /// the open folder itself, where %1 would be empty.
        /// </summary>
        private static string CommandFor(Verb verb)
        {
            return "\"" + ExePath + "\" " + verb.ExtraArgs + "\"%V\"";
        }

        // Separators on a static verb: the "SeparatorBefore"/"SeparatorAfter"
        // values, and the same as ECF_ flags in CommandFlags - both are
        // written, since which one Explorer honours depends on the Windows
        // version; adjacent separators are merged, so there is never a double
        // line.
        private const string SeparatorBeforeValue = "SeparatorBefore";
        private const string SeparatorAfterValue = "SeparatorAfter";
        private const string CommandFlagsValue = "CommandFlags";
        private const int EcfSeparatorBefore = 0x20;
        private const int EcfSeparatorAfter = 0x40;

        /// <summary>
        /// True only when every entry exists and runs this exe. An older
        /// install (one entry, "Open with Git Helper") or one pointing at an
        /// exe that has since moved counts as not registered, so the menu
        /// offers to write the entries again.
        /// </summary>
        public static bool IsRegistered()
        {
            try
            {
                foreach (var root in Roots)
                {
                    foreach (var verb in Verbs)
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(root + verb.Key + @"\command"))
                        {
                            var command = key == null ? null : key.GetValue(string.Empty) as string;
                            if (!string.Equals(command, CommandFor(verb), StringComparison.OrdinalIgnoreCase)) return false;
                        }

                        // written before the separators existed: offer Add, which refreshes it
                        using (var key = Registry.CurrentUser.OpenSubKey(root + verb.Key))
                        {
                            if (key == null || !(key.GetValue(CommandFlagsValue) is int)) return false;
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Anything of ours in the registry, complete or not.</summary>
        public static bool AnyRegistered()
        {
            try
            {
                foreach (var root in Roots)
                    foreach (var verb in Verbs)
                        using (var key = Registry.CurrentUser.OpenSubKey(root + verb.Key))
                            if (key != null) return true;
            }
            catch
            {
                // unreadable counts as absent
            }
            return false;
        }

        /// <summary>
        /// Adds (or refreshes) both entries. Throws when the registry refuses,
        /// which the caller reports - this is something the user asked for, so
        /// failing silently would be wrong.
        /// </summary>
        public static void Register()
        {
            var exe = ExePath;
            foreach (var root in Roots)
            {
                foreach (var verb in Verbs)
                {
                    var path = root + verb.Key;
                    using (var key = Registry.CurrentUser.CreateSubKey(path))
                    {
                        if (key == null) throw new InvalidOperationException("Could not create HKCU\\" + path);
                        key.SetValue(string.Empty, verb.Caption, RegistryValueKind.String);
                        key.SetValue("Icon", exe + ",0", RegistryValueKind.String);

                        var flags = 0;
                        if (verb.SeparatorBefore) { key.SetValue(SeparatorBeforeValue, string.Empty, RegistryValueKind.String); flags |= EcfSeparatorBefore; }
                        else key.DeleteValue(SeparatorBeforeValue, false);
                        if (verb.SeparatorAfter) { key.SetValue(SeparatorAfterValue, string.Empty, RegistryValueKind.String); flags |= EcfSeparatorAfter; }
                        else key.DeleteValue(SeparatorAfterValue, false);
                        key.SetValue(CommandFlagsValue, flags, RegistryValueKind.DWord);
                    }
                    using (var key = Registry.CurrentUser.CreateSubKey(path + @"\command"))
                    {
                        if (key == null) throw new InvalidOperationException("Could not create HKCU\\" + path + "\\command");
                        key.SetValue(string.Empty, CommandFor(verb), RegistryValueKind.String);
                    }
                }
            }
        }

        public static void Unregister()
        {
            foreach (var root in Roots)
            {
                foreach (var verb in Verbs)
                {
                    try { Registry.CurrentUser.DeleteSubKeyTree(root + verb.Key, false); }
                    catch (ArgumentException) { /* already gone */ }
                }
            }
        }

        /// <summary>The command lines Explorer will run, for showing the user.</summary>
        public static string CommandLine
        {
            get { return CommandFor(Verbs[0]) + "\r\n" + CommandFor(Verbs[1]); }
        }
    }
}
