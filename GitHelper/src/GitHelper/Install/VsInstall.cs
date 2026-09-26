using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHelper.Install
{
    /// <summary>
    /// One Visual Studio installation, identified the way VS itself does:
    /// a config name like <c>18.0_539452bc</c>, where the hash identifies the
    /// *installation* and differs on every machine. Never hard-code the full
    /// name.
    /// </summary>
    public sealed class VsInstall
    {
        private VsInstall(string name, string configDir)
        {
            Name = name;
            ConfigDir = configDir;
            var major = name.Split('.')[0];
            int m;
            Major = int.TryParse(major, out m) ? m : 0;
        }

        /// <summary>The config name, e.g. <c>18.0_539452bc</c>.</summary>
        public string Name { get; private set; }

        /// <summary>%LOCALAPPDATA%\Microsoft\VisualStudio\&lt;Name&gt;</summary>
        public string ConfigDir { get; private set; }

        public int Major { get; private set; }

        public string HivePath { get { return Path.Combine(ConfigDir, "privateregistry.bin"); } }

        /// <summary>
        /// The keyboard scheme file. Its timestamp is how an import is known
        /// to have finished.
        /// </summary>
        public string VskPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\VisualStudio\" + Name + @"\User.vsk");
            }
        }

        /// <summary>The year people actually call it by.</summary>
        public string ProductName
        {
            get
            {
                switch (Major)
                {
                    case 18: return "Visual Studio 2026";
                    case 17: return "Visual Studio 2022";
                    case 16: return "Visual Studio 2019";
                    case 15: return "Visual Studio 2017";
                    default: return "Visual Studio " + Major;
                }
            }
        }

        public override string ToString() { return ProductName + "   (" + Name + ")"; }

        /// <summary>
        /// Every installation that has a private registry hive, newest first.
        /// </summary>
        public static List<VsInstall> Discover()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\VisualStudio");

            if (!Directory.Exists(root)) return new List<VsInstall>();

            var pattern = new Regex(@"^\d+\.\d+_[0-9a-f]+$", RegexOptions.IgnoreCase);

            return Directory.GetDirectories(root)
                .Select(dir => new { Dir = dir, Name = Path.GetFileName(dir) })
                .Where(x => pattern.IsMatch(x.Name))
                .Where(x => File.Exists(Path.Combine(x.Dir, "privateregistry.bin")))
                .Select(x => new VsInstall(x.Name, x.Dir))
                .OrderByDescending(v => v.Major)
                .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Which installation to patch by default: 2026 when it is there,
        /// otherwise 2022, otherwise simply the newest found. Returns null for
        /// an empty list.
        /// </summary>
        public static VsInstall Preferred(IEnumerable<VsInstall> installs)
        {
            var list = installs as IList<VsInstall> ?? installs.ToList();
            if (list.Count == 0) return null;

            foreach (var major in new[] { 18, 17 })
            {
                var hit = list.FirstOrDefault(v => v.Major == major);
                if (hit != null) return hit;
            }
            return list.OrderByDescending(v => v.Major).First();   // already sorted, but be explicit
        }

        /// <summary>
        /// devenv.exe for this installation, or null. The layout is
        /// &lt;root&gt;\&lt;version&gt;\&lt;edition&gt;\Common7\IDE\devenv.exe —
        /// two levels, not one — and 17.x lives under '2022' rather than '17'.
        /// </summary>
        public string FindDevEnv()
        {
            var roots = new[]
            {
                Environment.GetEnvironmentVariable("ProgramFiles"),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
            };

            var candidates = new List<KeyValuePair<string, string>>();   // folder name -> exe
            foreach (var r in roots)
            {
                if (string.IsNullOrEmpty(r)) continue;
                var vsRoot = Path.Combine(r, "Microsoft Visual Studio");
                if (!Directory.Exists(vsRoot)) continue;

                foreach (var versionDir in Directory.GetDirectories(vsRoot))
                {
                    foreach (var editionDir in Directory.GetDirectories(versionDir))
                    {
                        var exe = Path.Combine(editionDir, @"Common7\IDE\devenv.exe");
                        if (File.Exists(exe))
                            candidates.Add(new KeyValuePair<string, string>(Path.GetFileName(versionDir), exe));
                    }
                }
            }

            var wanted = Major == 17 ? new[] { "2022", "17" } : new[] { Major.ToString() };
            foreach (var w in wanted)
            {
                var hit = candidates.FirstOrDefault(c => string.Equals(c.Key, w, StringComparison.OrdinalIgnoreCase));
                if (hit.Value != null) return hit.Value;
            }
            return null;
        }
    }
}
