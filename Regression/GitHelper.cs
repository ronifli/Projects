using System;
using System.Linq;
using System.Threading;

namespace Regression
{
    public class GitHelper
    {
        readonly string[] repos;
        readonly ProcessRunner runner;

        public GitHelper(string repoPaths, ProcessRunner runner)
        {
            repos = repoPaths.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .ToArray();
            this.runner = runner;
        }

        public bool HasLocalChanges(string repo, CancellationToken ct)
        {
            string status = runner.Run("git", "status --porcelain --untracked-files=no", repo, ct, out _);
            return !string.IsNullOrWhiteSpace(status);
        }

        public bool HasRemoteChanges(string repo, CancellationToken ct)
        {
            runner.RunChecked("git", "fetch", repo, ct);
            string count = runner.Run("git", "rev-list --count HEAD..@{u}", repo, ct, out int code);
            return code == 0 && int.TryParse(count.Trim(), out int n) && n > 0;
        }

        public bool HasChanges(CancellationToken ct)
        {
            bool any = false;
            foreach (var repo in repos)
            {
                bool local = HasLocalChanges(repo, ct);
                bool remote = HasRemoteChanges(repo, ct);
                runner.Write($"Git [{repo}]: local changes: {(local ? "yes" : "no")}, new commits on remote: {(remote ? "yes" : "no")}");
                any |= local || remote;
            }
            return any;
        }

        public void RevertAndPull(CancellationToken ct)
        {
            foreach (var repo in repos)
            {
                runner.Write($"Git [{repo}]: reverting all local changes and pulling latest.");
                runner.RunChecked("git", "reset --hard", repo, ct);
                runner.RunChecked("git", "pull", repo, ct);
            }
        }

        public void RestoreDeleted(CancellationToken ct)
        {
            foreach (var repo in repos)
            {
                runner.Write($"Git [{repo}]: restoring files deleted by clean.");
                runner.RunChecked("git", "checkout -- .", repo, ct);
            }
        }
    }
}
