# Regression

WinForms tool (.NET Framework 4.7.2) that runs the Camtek regression cycle unattended:
it pulls the latest code, cleans and builds `ace.sln`, then runs SparkRunner, repeating every N hours.
It replaces the old `RunRegression.cmd` script.

## Regression loop

Pressing **Start Regression** runs this cycle until **Stop** is pressed:

1. **Wait for SparkRunner.** If `SparkRunner.exe` is running, check again every `PollSeconds` until it exits.
2. **Check git.** Look for local edits to tracked files (`git status --porcelain --untracked-files=no`) and new commits on the remote (`git fetch` + `git rev-list --count HEAD..@{u}`).
   - No changes: wait `WaitHours`, then go back to step 1.
3. **Stop apps.** Run `StopApps.cmd` to close Camtek processes and services, then wait 15 s (the script uses `start /b`, so it returns before its commands finish).
4. **Revert and pull.** `git reset --hard`, `git pull`. Untracked files are kept (no `git clean`).
5. **Clean** the solution (devenv `/Clean` or MSBuild `/t:Clean`).
6. **Restore deleted files.** `git checkout -- .` brings back tracked files the clean removed.
7. **Build** the solution. The build is stopped at the first error.
   - Clean or build failed: email the error lines, wait `WaitHours`, go back to step 1. It rebuilds only when a new git change arrives.
8. **Run SparkRunner** with `SparkRunnerArguments` (it isn't waited on).
9. **Wait `WaitHours`**, then go back to step 1.

An unexpected error (for example a failing git command) sends an email and stops the loop.

## UI

| Control | Action |
|---|---|
| **Start Regression** | Start the loop above |
| **Stop** | Cancel the current task and kill its whole process tree (git / devenv / MSBuild) |
| **Show Log** | Open the log file in Notepad |
| **Use devenv (else MSBuild)** | Build and clean with `devenv.com` (uses all cores) or `MSBuild.exe` |
| **Actions > Build Only** | Build once, stop at the first error |
| **Actions > Clean Only** | Clean once |
| **Actions > Git Restore Only** | `git reset --hard` + `git pull` |
| **Actions > Send Test Email** | Send a test mail to check the SMTP settings |

- **Top log:** every command that runs, its output (warnings hidden), and exit codes. Error lines are shown in bold red.
- **Bottom log:** one timestamped line per regression step (Check git, Clean..., Build..., Build OK, ...).
- **Status bar:** the current task (Idle, Building, Regression running, ...).
- Window position and size are saved to `%AppData%\Regression\window.txt`.

## Settings

All values are project settings (**Project > Properties > Settings**), application-scoped.
They're stored in `App.config`, which is deployed as `Regression.exe.config` next to the exe.
Edit that file on the target machine and restart the app to change a value without rebuilding.

| Setting | Meaning |
|---|---|
| `RepoPaths` | Git repositories used by the solution, `;`-separated (`C:\camtek;C:\vscan90`). Check, revert + pull, and restore run on each one |
| `SolutionPath` | Solution to clean and build (`C:\camtek\build\ace.sln`) |
| `StopAppsScript` | Script run before each build (`C:\Camtek\Build\StopApps.cmd`) |
| `BuildConfiguration` | `Release`; for devenv it can include the platform, e.g. `Release\|x64` |
| `MSBuildPath` / `DevenvPath` | Leave empty to find them automatically with `vswhere` |
| `SparkRunnerPath` | Exe to run after a successful build |
| `SparkRunner` | Process name to wait for (`SparkRunner.exe`) |
| `SparkRunnerArguments` | Arguments passed to SparkRunner |
| `WaitHours` | Hours between cycles |
| `PollSeconds` | Seconds between "is SparkRunner still running" checks |
| `LogFile` | Main log file; devenv logs (`devenv_Build.log`, `devenv_Clean.log`) are written to the same folder |
| `SmtpHost`, `SmtpPort`, `SmtpSsl`, `SmtpUser`, `SmtpPassword` | Mail server |
| `MailFrom`, `MailTo` | Sender, and comma-separated recipients |

**SMTP password:** set it only in `App.config` / `Regression.exe.config`, never on the Settings page.
Values entered on the Settings page are compiled into `Regression.exe` as defaults.
Keep `App.config` out of source control and shared folders.

## Code layout

| File | Contents |
|---|---|
| `Runner.cs` | Regression loop, StopApps, SparkRunner launch, email, state log |
| `GitHelper.cs` | Git checks, revert + pull, restore deleted files |
| `DevEnv.cs` | Clean / build with devenv or MSBuild, error-line extraction, vswhere lookup |
| `ProcessRunner.cs` | Runs external commands, logging, warning filter, stop on first error, process-tree kill |
| `MainForm.cs` / `MainForm.Designer.cs` | UI |
| `Properties\Settings.settings` | Project settings |

## Requirements and notes

- Visual Studio (devenv / MSBuild) and git on `PATH`.
- **Run as administrator:** `StopApps.cmd` uses `net stop` for the Camtek services.
- Builds are started with `MSBUILDDISABLENODEREUSE=1` (MSBuild: `/nr:false`). Without it, MSBuild worker processes that stay alive for reuse keep the output pipe open and the app appears stuck after a clean or build.
- The Clean step never stops early; only Build stops at the first error.

## Build

Open `Regression.sln` in Visual Studio, or:

```
msbuild Regression.sln /p:Configuration=Release
```

Deploy `Regression.exe` together with `Regression.exe.config`.
