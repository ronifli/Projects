using System;
using System.IO;
using System.Linq;
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
