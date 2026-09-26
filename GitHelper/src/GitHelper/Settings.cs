using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Win32;

namespace GitHelper
{
    /// <summary>
    /// The repositories the dialog offers, and the per-user memory of the ones
    /// added by hand.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// The repositories every install offers. Not a <c>const</c>, because
        /// an array cannot be one - these are fixed all the same, and a
        /// path that is not a working copy on this machine is skipped
        /// silently by <see cref="RepoInfo.Discover"/>.
        /// </summary>
        public static readonly string[] BuiltIn =
        {
            @"C:\Camtek",
            @"C:\vscan90",
        };

        private const string KeyPath = @"Software\Cims\GitHelper";

        /// <summary>Where the same list lived before the tool was renamed.</summary>
        private const string LegacyKeyPath = @"Software\Cims\CommitUI";

        private const string ValueName = "Repositories";
        private const string WindowValue = "WindowBounds";
        private const string MaximizedValue = "WindowMaximized";

        /// <summary>
        /// Where the main window was last time, as "x,y,width,height", or null
        /// when nothing has been stored or the value is unreadable. Validating
        /// that the rectangle is still on a screen is the caller's job - the
        /// monitor it was saved on may be gone.
        /// </summary>
        public static Rectangle? WindowBounds()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
                {
                    var value = key == null ? null : key.GetValue(WindowValue) as string;
                    if (string.IsNullOrEmpty(value)) return null;

                    var parts = value.Split(',');
                    if (parts.Length != 4) return null;

                    int x, y, w, h;
                    if (!int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y) ||
                        !int.TryParse(parts[2], out w) || !int.TryParse(parts[3], out h))
                        return null;

                    if (w <= 0 || h <= 0) return null;
                    return new Rectangle(x, y, w, h);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Was the window maximized when it was last closed?</summary>
        public static bool WindowMaximized()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
                {
                    if (key == null) return false;
                    return Convert.ToInt32(key.GetValue(MaximizedValue, 0)) != 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Records where the window was. Silent on failure, like the rest of
        /// this class: not being able to write a window position must never
        /// interfere with closing the dialog.
        /// </summary>
        public static void SaveWindow(Rectangle bounds, bool maximized)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    if (key == null) return;
                    key.SetValue(WindowValue,
                        bounds.X + "," + bounds.Y + "," + bounds.Width + "," + bounds.Height,
                        RegistryValueKind.String);
                    key.SetValue(MaximizedValue, maximized ? 1 : 0, RegistryValueKind.DWord);
                }
            }
            catch
            {
                // no write access to HKCU; the window just opens centred again
            }
        }

        private const string CopilotDeclinedValue = "CopilotSetupDeclined";
        private const string CopilotSignedInValue = "CopilotSignedIn";

        /// <summary>The user said No to setting up Copilot at startup - do not ask again there.</summary>
        public static bool CopilotSetupDeclined
        {
            get { return ReadFlag(CopilotDeclinedValue); }
            set { WriteFlag(CopilotDeclinedValue, value); }
        }

        /// <summary>A Copilot sign-in (or an AI answer) succeeded on this machine.</summary>
        public static bool CopilotSignedIn
        {
            get { return ReadFlag(CopilotSignedInValue); }
            set { WriteFlag(CopilotSignedInValue, value); }
        }

        /// <summary>
        /// A remembered date ("yyyy-MM-dd"), or <paramref name="fallback"/>
        /// when none was stored or it cannot be read.
        /// </summary>
        public static DateTime GetDate(string name, DateTime fallback)
        {
            DateTime d;
            return DateTime.TryParseExact(GetString(name), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                                          System.Globalization.DateTimeStyles.None, out d)
                ? d : fallback;
        }

        public static void SetDate(string name, DateTime value)
        {
            SetString(name, value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>A string value under the GitHelper key, or null. Never throws.</summary>
        public static string GetString(string name)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
                    return key == null ? null : key.GetValue(name) as string;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Stores a string value under the GitHelper key. Silent on failure.</summary>
        public static void SetString(string name, string value)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    if (key != null) key.SetValue(name, value ?? string.Empty, RegistryValueKind.String);
                }
            }
            catch
            {
                // no write access to HKCU; it simply is not remembered
            }
        }

        private static bool ReadFlag(string name)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
                {
                    var v = key == null ? null : key.GetValue(name);
                    return v is int && (int)v != 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void WriteFlag(string name, bool value)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    if (key != null) key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
                }
            }
            catch
            {
                // no write access to HKCU; the question just comes back next time
            }
        }

        /// <summary>
        /// Repositories the user picked with "Add repo...", most recently added
        /// last. Returns an empty list when the key is missing or unreadable -
        /// a broken setting must not stop the dialog opening.
        /// </summary>
        public static List<string> Remembered()
        {
            // Fall back to the pre-rename key, so an existing list is not lost.
            return Read(KeyPath) ?? Read(LegacyKeyPath) ?? new List<string>();
        }

        private static List<string> Read(string keyPath)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    if (key == null) return null;
                    var value = key.GetValue(ValueName) as string[];
                    if (value == null) return null;
                    return value.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Adds a repository to the remembered list, ignoring one that is
        /// already there under any casing. Silent on failure: the repo is still
        /// usable in this session, it just will not come back next time.
        /// </summary>
        public static void Remember(string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(repoRoot)) return;

            var list = Remembered();
            if (list.Any(p => string.Equals(p, repoRoot, StringComparison.OrdinalIgnoreCase))) return;
            list.Add(repoRoot);

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    if (key == null) return;
                    key.SetValue(ValueName, list.ToArray(), RegistryValueKind.MultiString);
                }
            }
            catch
            {
                // no write access to HKCU is not worth failing the commit over
            }
        }

        /// <summary>Drops a repository from the remembered list.</summary>
        public static void Forget(string repoRoot)
        {
            var list = Remembered()
                .Where(p => !string.Equals(p, repoRoot, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    if (key == null) return;
                    key.SetValue(ValueName, list, RegistryValueKind.MultiString);
                }
            }
            catch
            {
                // as above
            }
        }
    }
}
