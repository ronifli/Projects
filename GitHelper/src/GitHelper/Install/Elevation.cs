using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace GitHelper.Install
{
    /// <summary>
    /// Loading a registry hive needs administrator rights, and the commit
    /// dialog itself deliberately does not run elevated - so the install step
    /// re-launches this same executable with <c>--install</c> and lets Windows
    /// prompt for consent.
    /// </summary>
    public static class Elevation
    {
        /// <summary>The switch that opens the installer window instead of the dialog.</summary>
        public const string InstallSwitch = "--install";

        public static bool IsElevated()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Starts an elevated copy of this exe on the installer window.
        /// Returns false when the UAC prompt is declined - which is a normal
        /// answer, not an error worth a stack trace.
        /// </summary>
        public static bool RelaunchElevated()
        {
            var exe = Assembly.GetExecutingAssembly().Location;
            var psi = new ProcessStartInfo(exe, InstallSwitch)
            {
                UseShellExecute = true,
                Verb = "runas",
            };

            try
            {
                Process.Start(psi);
                return true;
            }
            catch (Win32Exception)
            {
                return false;   // ERROR_CANCELLED, or no consent given
            }
        }
    }
}
