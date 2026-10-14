using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Regression
{
    public class DevEnv
    {
        readonly string sln;
        readonly string config;
        readonly ProcessRunner runner;

        public bool UseDevenv { get; set; }

        public DevEnv(string solutionPath, string configuration, ProcessRunner runner)
        {
            sln = solutionPath;
            config = configuration;
            this.runner = runner;
        }

        public bool Clean(CancellationToken ct, out string errors) => RunTarget("Clean", ct, out errors, false);

        public bool Build(CancellationToken ct, out string errors, bool stopOnError = false) => RunTarget("Build", ct, out errors, stopOnError);

        bool RunTarget(string target, CancellationToken ct, out string errors, bool stopOnError)
        {
            runner.Write($"{target}: {sln}  ({(UseDevenv ? "devenv" : "MSBuild")})");
            string output;
            int code;
            try
            {
                if (UseDevenv)
                {
                    if (target == "Build") PreBuildProjects(ct);
                    string devenvLog = Path.Combine(Path.GetDirectoryName(runner.LogFile), $"devenv_{target}.log");
                    if (File.Exists(devenvLog)) File.Delete(devenvLog);
                    output = runner.Run(Devenv(), $"\"{sln}\" /{target} \"{config}\" /Out \"{devenvLog}\"", Path.GetDirectoryName(sln), ct, out code, stopOnError: stopOnError, minimal: true);
                    if (File.Exists(devenvLog)) output += Environment.NewLine + File.ReadAllText(devenvLog);
                    runner.Write("devenv log: " + devenvLog);
                }
                else
                {
                    output = runner.Run(MsBuild(), $"\"{sln}\" /t:{target} /p:Configuration={config} /m /nr:false /v:minimal", Path.GetDirectoryName(sln), ct, out code, stopOnError: stopOnError);
                }
            }
            catch (Exception ex)
            {
                errors = ex.Message;
                runner.Write($"{target} FAILED: {errors}");
                return false;
            }

            if (code == 0)
            {
                errors = "";
                runner.Write($"{target} succeeded.");
                return true;
            }

            var errorLines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => ProcessRunner.ErrorLine.IsMatch(l))
                .Distinct()
                .ToList();
            errors = errorLines.Count > 0 ? string.Join(Environment.NewLine, errorLines) : output;

            runner.Write($"{target} FAILED (exit {code}), {errorLines.Count} error(s):");
            runner.Write("----- ERRORS -----" + Environment.NewLine + errors + Environment.NewLine + "------------------");
            return false;
        }

        // Built with MSBuild, one at a time, before devenv gets the solution (same as GitHelper).
        // 'xy' builds cleanly on its own but not as part of devenv's solution build.
        // In order: xy depends on Cfc2.
        static readonly string[] PreBuildProjectNames = { "Cfc2", "xy" };

        void PreBuildProjects(CancellationToken ct)
        {
            string ms;
            try { ms = MsBuild(); }
            catch (Exception ex) { runner.Write("MSBuild not found (" + ex.Message + ") - skipping pre-build projects."); return; }

            foreach (var project in PreBuildProjectNames)
            {
                if (ct.IsCancellationRequested) return;
                if (!FindProjectInSolution(project, out string projectPath, out string configuration, out string platform))
                {
                    runner.Write($"{project}: not found in the solution, skipped.");
                    continue;
                }
                if (!File.Exists(projectPath))
                {
                    runner.Write($"{project}: {projectPath} does not exist, skipped.");
                    continue;
                }

                // The project file is the target (not "sln /t:project", which fails with MSB4057);
                // SolutionDir is passed explicitly because projects use it.
                string args = $"\"{projectPath}\" /p:Configuration=\"{configuration}\" /p:Platform=\"{platform}\"" +
                              $" /p:SolutionDir=\"{Path.GetDirectoryName(sln)}\\\\\" /m /nr:false /v:minimal";
                runner.Write("> msbuild " + args);
                runner.Run(ms, args, Path.GetDirectoryName(projectPath), ct, out int code);
                runner.Write(code == 0 ? $"{project}: built." : $"{project}: MSBuild exit {code} - carrying on, devenv will report it.");
            }
        }

        // Finds the project file and the configuration|platform the solution maps `config` to for it
        // (e.g. xy is Release|Win32 when the solution is Debug|x64).
        bool FindProjectInSolution(string project, out string projectPath, out string configuration, out string platform)
        {
            projectPath = null;
            var cfg = config.Split('|');
            configuration = cfg[0];
            platform = cfg.Length > 1 ? cfg[1] : "Any CPU";
            try
            {
                var lines = File.ReadAllLines(sln);
                var projectLine = new Regex("^Project\\(\"\\{[^}]+\\}\"\\)\\s*=\\s*\"([^\"]+)\",\\s*\"([^\"]+)\",\\s*\"(\\{[^}]+\\})\"", RegexOptions.IgnoreCase);
                string guid = null;
                foreach (var line in lines)
                {
                    var m = projectLine.Match(line.Trim());
                    if (!m.Success || !string.Equals(m.Groups[1].Value, project, StringComparison.OrdinalIgnoreCase)) continue;
                    guid = m.Groups[3].Value;
                    projectPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sln), m.Groups[2].Value));
                    break;
                }
                if (guid == null) return false;

                // {guid}.Debug|x64.ActiveCfg = Release|Win32
                var mapping = new Regex(@"^\s*" + Regex.Escape(guid) + @"\." + Regex.Escape(config) + @"\.ActiveCfg\s*=\s*(.+)$", RegexOptions.IgnoreCase);
                foreach (var line in lines)
                {
                    var m = mapping.Match(line);
                    if (!m.Success) continue;
                    var parts = m.Groups[1].Value.Trim().Split('|');
                    configuration = parts[0].Trim();
                    if (parts.Length > 1) platform = parts[1].Trim();
                    break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        static string VsWhere => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            @"Microsoft Visual Studio\Installer\vswhere.exe");

        string msbuild;
        string MsBuild()
        {
            if (msbuild != null) return msbuild;
            msbuild = Properties.Settings.Default.MSBuildPath;
            if (string.IsNullOrEmpty(msbuild))
                msbuild = runner.Run(VsWhere, @"-latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe",
                    null, CancellationToken.None, out _, quiet: true).Split('\n')[0].Trim();
            if (!File.Exists(msbuild)) throw new FileNotFoundException("MSBuild not found. Set MSBuildPath in App.config.");
            return msbuild;
        }

        string devenv;
        string Devenv()
        {
            if (devenv != null) return devenv;
            devenv = Properties.Settings.Default.DevenvPath;
            if (string.IsNullOrEmpty(devenv))
            {
                string exe = runner.Run(VsWhere, "-latest -property productPath", null, CancellationToken.None, out _, quiet: true).Split('\n')[0].Trim();
                devenv = Path.ChangeExtension(exe, ".com");
            }
            if (!File.Exists(devenv)) throw new FileNotFoundException("devenv.com not found. Set DevenvPath in App.config.");
            return devenv;
        }
    }
}
