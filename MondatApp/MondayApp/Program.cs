using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MondayApp
{
    static class Program
    {
        // The app is published to NetDir and always runs from a local copy in LocalDir
        const string NetDir = @"\\10.3.3.10\Pub\Monday";
        const string LocalDir = @"C:\MondayApp";

        [STAThread]
        static void Main()
        {
            try
            {
                if (SamePath(AppDir, NetDir))
                {
                    // Started from the network: copy locally and run the local copy
                    SyncFromNetwork();
                    Process.Start(Path.Combine(LocalDir, Path.GetFileName(Application.ExecutablePath)));
                    return;
                }
                if (SamePath(AppDir, LocalDir) && SyncFromNetwork())
                {
                    // Our own files were replaced: restart on the new version
                    Process.Start(Application.ExecutablePath);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update from " + NetDir + " failed:\n" + ex.Message + "\n\nStarting the current version.", "Monday Reports",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        static string AppDir { get { return AppDomain.CurrentDomain.BaseDirectory; } }

        static bool SamePath(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }

        // Copies files from NetDir that are missing locally or have a newer date. Returns true if any file changed.
        // Files in use (the running exe and its dlls) can't be overwritten but can be renamed, so they are moved to *.old first.
        static bool SyncFromNetwork()
        {
            foreach (var old in Directory.Exists(LocalDir) ? Directory.GetFiles(LocalDir, "*.old") : new string[0])
                try { File.Delete(old); } catch (Exception) { /* still in use by a closing instance */ }

            // Unreachable share: don't hang the start-up, just run the local version
            var reachable = Task.Run(() => Directory.Exists(NetDir));
            if (!reachable.Wait(TimeSpan.FromSeconds(3)) || !reachable.Result) return false;

            Directory.CreateDirectory(LocalDir);
            bool changed = false;
            foreach (var src in Directory.GetFiles(NetDir).Where(f => !f.EndsWith(".old", StringComparison.OrdinalIgnoreCase)))
            {
                string dst = Path.Combine(LocalDir, Path.GetFileName(src));
                if (File.Exists(dst) && File.GetLastWriteTimeUtc(src) <= File.GetLastWriteTimeUtc(dst)) continue;
                if (File.Exists(dst))
                {
                    string old = dst + ".old";
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(dst, old);
                }
                File.Copy(src, dst);
                changed = true;
            }
            return changed;
        }
    }
}
