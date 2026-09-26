using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Regression
{
    public class ProcessRunner
    {
        public static readonly Regex ErrorLine = new Regex(@":\s*(fatal\s+)?error\s+\w+", RegexOptions.IgnoreCase);
        static readonly Regex WarningLine = new Regex(@":\s*warning\s+\w+", RegexOptions.IgnoreCase);

        public string LogFile { get; }
        public event Action<string> Log;

        public ProcessRunner(string logFile)
        {
            LogFile = logFile;
        }

        public void Write(string msg)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}";
            try { File.AppendAllText(LogFile, line + Environment.NewLine); } catch { }
            Log?.Invoke(line);
        }

        void WriteFileOnly(string msg)
        {
            try { File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}" + Environment.NewLine); } catch { }
        }

        // devenv has no verbosity switch; mimic MSBuild /v:minimal (errors, project outputs, summary).
        public static bool IsMinimalLine(string line) =>
            ErrorLine.IsMatch(line) || line.Contains(" -> ") || line.StartsWith("==========");

        public void RunChecked(string exe, string args, string dir, CancellationToken ct)
        {
            Run(exe, args, dir, ct, out int code);
            if (ct.IsCancellationRequested) throw new OperationCanceledException();
            if (code != 0) throw new Exception($"'{Path.GetFileName(exe)} {args}' failed with exit code {code}");
        }

        public string Run(string exe, string args, string dir, CancellationToken ct, out int exitCode, bool quiet = false, bool stopOnError = false, bool minimal = false)
        {
            bool stoppedOnError = false;
            if (!quiet) Write($"> {exe} {args}" + (dir != null ? $"   (in {dir})" : ""));
            var sb = new StringBuilder();
            var psi = new ProcessStartInfo(exe, args)
            {
                WorkingDirectory = dir ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            // Reused MSBuild nodes inherit our stdout pipe and keep it open forever.
            psi.EnvironmentVariables["MSBUILDDISABLENODEREUSE"] = "1";
            using (var p = new Process { StartInfo = psi })
            {
                DataReceivedEventHandler h = (s, e) =>
                {
                    if (e.Data == null) return;
                    lock (sb) sb.AppendLine(e.Data);
                    if (!quiet && !WarningLine.IsMatch(e.Data))
                    {
                        if (!minimal || IsMinimalLine(e.Data)) Write("  " + e.Data);
                        else WriteFileOnly("  " + e.Data);
                    }
                    if (stopOnError && !stoppedOnError && ErrorLine.IsMatch(e.Data))
                    {
                        stoppedOnError = true;
                        Write("ERROR detected - stopping build.");
                        KillTree((Process)s);
                    }
                };
                p.OutputDataReceived += h;
                p.ErrorDataReceived += h;
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                using (ct.Register(() => KillTree(p)))
                {
                    // WaitForExit(ms) does not wait for the redirected streams to close.
                    while (!p.WaitForExit(500)) { }
                    Thread.Sleep(500);
                }
                exitCode = stoppedOnError && p.ExitCode == 0 ? 1 : p.ExitCode;
                if (!quiet) Write($"  exit code: {exitCode}");
            }
            return sb.ToString();
        }

        static void KillTree(Process p)
        {
            try
            {
                if (p.HasExited) return;
                using (var tk = Process.Start(new ProcessStartInfo("taskkill", $"/T /F /PID {p.Id}") { UseShellExecute = false, CreateNoWindow = true }))
                    tk.WaitForExit();
            }
            catch { }
        }
    }
}
