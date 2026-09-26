using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace Regression
{
    public class Runner
    {
        static Properties.Settings Cfg => Properties.Settings.Default;

        readonly string sparkRunnerPath = Cfg.SparkRunnerPath;
        readonly string sparkRunnerArgs = Cfg.SparkRunnerArguments;
        readonly double waitHours = Cfg.WaitHours;
        readonly int pollSeconds = Cfg.PollSeconds;

        public ProcessRunner Proc { get; }
        public GitHelper Git { get; }
        public DevEnv DevEnv { get; }

        public Runner()
        {
            Proc = new ProcessRunner(Cfg.LogFile);
            Git = new GitHelper(Cfg.RepoPaths, Proc);
            DevEnv = new DevEnv(Cfg.SolutionPath, Cfg.BuildConfiguration, Proc);
        }

        void Write(string msg) => Proc.Write(msg);

        public event Action<string> StateChanged;

        void State(string msg)
        {
            Write(">>> " + msg);
            StateChanged?.Invoke($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {msg}");
        }

        readonly string SparkRunnerProcessName = Path.GetFileNameWithoutExtension(Cfg.SparkRunner);

        public void RegressionLoop(CancellationToken ct)
        {
            State("Regression started");
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (Process.GetProcessesByName(SparkRunnerProcessName).Any())
                        State($"{SparkRunnerProcessName} is running - waiting for it to exit");
                    while (Process.GetProcessesByName(SparkRunnerProcessName).Any())
                    {
                        Write($"{SparkRunnerProcessName} is running, waiting {pollSeconds}s...");
                        if (ct.WaitHandle.WaitOne(TimeSpan.FromSeconds(pollSeconds))) return;
                    }

                    State("Check git");
                    if (!Git.HasChanges(ct))
                    {
                        State($"No changes - waiting {waitHours} hours");
                        ct.WaitHandle.WaitOne(TimeSpan.FromHours(waitHours));
                        continue;
                    }

                    State("Stop apps");
                    StopApps(ct);
                    State("Git revert and pull");
                    Git.RevertAndPull(ct);

                    State("Clean...");
                    if (!DevEnv.Clean(ct, out string cleanErrors))
                    {
                        if (ct.IsCancellationRequested) return;
                        State($"Clean FAILED - mail sent, waiting {waitHours} hours for a git update");
                        SendMail("Regression: clean FAILED", cleanErrors);
                        ct.WaitHandle.WaitOne(TimeSpan.FromHours(waitHours));
                        continue;
                    }

                    State("Git restore deleted files");
                    Git.RestoreDeleted(ct);

                    State("Build...");
                    if (!DevEnv.Build(ct, out string buildErrors, stopOnError: true))
                    {
                        if (ct.IsCancellationRequested) return;
                        State($"Build FAILED - mail sent, waiting {waitHours} hours for a git update");
                        SendMail("Regression: build FAILED", buildErrors);
                        ct.WaitHandle.WaitOne(TimeSpan.FromHours(waitHours));
                        continue;
                    }
                    State("Build OK");

                    State($"Start {Path.GetFileName(sparkRunnerPath)} {sparkRunnerArgs}");
                    Process.Start(new ProcessStartInfo(sparkRunnerPath, sparkRunnerArgs)
                    {
                        WorkingDirectory = Path.GetDirectoryName(sparkRunnerPath),
                        UseShellExecute = false
                    });

                    State($"Waiting {waitHours} hours");
                    ct.WaitHandle.WaitOne(TimeSpan.FromHours(waitHours)) ;
                }
            }
            catch (Exception ex)
            {
                State("ERROR: " + ex.Message);
                Write("ERROR: " + ex);
                SendMail("Regression: ERROR", ex.ToString());
            }
            finally
            {
                State("Regression stopped");
            }
        }

        public void StopApps(CancellationToken ct)
        {
            string script = Cfg.StopAppsScript;
            if (!File.Exists(script))
            {
                Write($"StopApps: {script} not found - skipping.");
                return;
            }
            Write("Stopping related applications...");
            Proc.Run("cmd.exe", $"/c \"\"{script}\"\"", Path.GetDirectoryName(script), ct, out _);
            // The script uses "start /b", so its taskkill / net stop commands are still running when it returns.
            ct.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
        }

        public void SendMail(string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(Cfg.SmtpHost, Cfg.SmtpPort))
                {
                    client.EnableSsl = Cfg.SmtpSsl;
                    if (Cfg.SmtpUser != "")
                        client.Credentials = new NetworkCredential(Cfg.SmtpUser, Cfg.SmtpPassword);
                    var msg = new MailMessage { From = new MailAddress(Cfg.MailFrom), Subject = $"{subject} ({Environment.MachineName})", Body = body };
                    foreach (var to in Cfg.MailTo.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                        msg.To.Add(to.Trim());
                    client.Send(msg);
                }
                Write("Mail sent.");
            }
            catch (Exception ex)
            {
                Write("Mail send failed: " + ex.Message);
            }
        }
    }
}
