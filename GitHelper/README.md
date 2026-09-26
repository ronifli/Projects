# GitHelper

A C# WinForms port of `PreCommit.ps1` — the structured commit dialog launched
from Visual Studio. Targets **.NET Framework 4.7.2**. Behaviour, message format
and layout are unchanged; see `C:\Camtek\Build\Tools\README-git-helper.md` for
*why* the fields exist and what the RTM audit needs from them. That document
remains the design reference, including the fields (`Change-Id`, `Origin`,
`Applies-To`, `Port`, `Port-Reason`) that are designed but still not implemented
here.

## Layout

| Path | Role |
|---|---|
| `src/GitHelper/frmMain.Designer.cs` | **every control**, its properties and its design-time position |
| `src/GitHelper/frmMain.cs` | the same form's behaviour, plus the runtime layout pass |
| `src/GitHelper/Program.cs` | entry point; resolves the repository and the extras list |
| `src/GitHelper/CommitMessage.cs` | `Build` / `Parse` — the message format, UI-free |
| `src/GitHelper/CommitHook.cs` | installs and enables the `commit-msg` hook in a working copy, UI-free |
| `src/GitHelper/Hooks/commit-msg` | the hook script itself, embedded in the exe |
| `src/GitHelper/Git.cs` | every `git -C <repo>` call, output capture, repo queries |
| `src/GitHelper/RepoInfo.cs` | one working copy: branch, staged/dirty/untracked, unpushed |
| `src/GitHelper/VisualStudio.cs` | reads VS's Git Changes box over UI Automation |
| `src/GitHelper/Settings.cs` | the built-in repositories and the registry-remembered ones |
| `src/GitHelper/ShellIntegration.cs` | the Explorer folder context-menu entry |
| `src/GitHelper/LocalDeployment.cs` | self-copy to `C:\Camtek\Build\Tools` when run from a share |
| `src/GitHelper/frmInstall.cs` | the **Install VS Plugin** window |
| `src/GitHelper/frmSwitchBranch.cs` | the **Git ▸ Switch** branch picker |
| `src/GitHelper/MergeCheck.cs` | the branch comparison, UI-free |
| `src/GitHelper/frmCheckMerge.cs` | the **Tools ▸ Check Merge** report |
| `src/GitHelper/frmShowLog.cs` | the **Git ▸ Show Log** window |
| `src/GitHelper/CommitLog.cs` | reads and parses the history for it, UI-free |
| `src/GitHelper/Install/VsInstall.cs` | finds the installed Visual Studios and picks one |
| `src/GitHelper/Install/ExternalToolInstaller.cs` | writes the External Tools entry into VS's private hive |
| `src/GitHelper/Install/VsSettingsPatcher.cs` | keyboard shortcut and toolbar button, via a .vssettings round trip |
| `src/GitHelper/Install/Elevation.cs` | the UAC re-launch |
| `src/GitHelper/GitHelper.ico` | the application icon, nine sizes |
| `tools/make-icon.ps1` | regenerates that .ico |
| `tests/GitHelper.Tests` | `Build`/`Parse` round-trip checks, no UI needed |

## The form

All 35 controls are created, configured and added to `frmMain` in
`InitializeComponent`, so the form opens in the Windows Forms designer and
controls can be dragged onto it like any other. The conventions the rows follow:

- rows step down 34px from y=12; labels at x=12, inputs at x=136, help text at
  x=326
- `frmMain.cs` holds only handlers and logic — the designer file owns
  positions, sizes, fonts, colours and the event wiring

Two things are still adjusted at runtime, in `UpdateButtonRow`:

1. the two button rows re-flow left-to-right, widening any button whose caption
   no longer fits — captions grow under DPI scaling and a fixed layout pushes
   the outer buttons off the form;
2. the subject, body and preview boxes stretch to the current window width.

It reads its starting positions from the designer (`UI_cmd_Copy.Top`,
`UI_cmd_FromVs.Top`), so moving a row in the designer moves it at runtime too.

Adding a field means: a label + control in `InitializeComponent` following the
row pattern, its change event pointed at `field_Changed`, the line in
`CommitMessage.Build`, the key in `Known` plus the assignment in
`CommitMessage.Parse`, the restore in `ImportLastCommit`, and — if it is
mandatory — the `ready` test in `UpdatePreview`.

**Body** is a `LineLimitedTextBox` (`MaxLines = 30`, set in the designer). It
never holds more than 30 lines: Enter is refused on the 30th, a paste inserts
only as many clipboard lines as still fit (the text already there is never
pushed out), and text set from code — **From VS**, **From clipboard**, **Last
commit** — loses its surplus lines at the end. Each refusal beeps and shows a
tip under the box. Lines are newline-separated lines; word-wrapped rows do not
count.

**AI** (`UI_cmd_AI`, on the body row) writes the body with **GitHub Copilot
CLI** with Copilot's **auto** model selection (`AiBody.DefaultModel` =
`auto`: Copilot picks a model the plan and admin policy allow — Claude Sonnet /
Haiku among them — at a 10% premium-request discount). Set `GITHELPER_AI_MODEL`
to force one by id, e.g. `claude-sonnet-4.5` or `gpt-5-mini`, or to empty for the
user's own Copilot setting.
It sends the diff of exactly what Commit would commit — staged changes if
anything is staged, otherwise all tracked modifications, plus the *names* of
new files when they are included — trimmed at 60,000 characters, together with
the Subject, Commit Type, Area, TT and anything already in the body (as notes).
The instructions ask for at most **30 lines** of at most 72 characters, plain
text, no field lines; the answer is tidied (`AiBody.Clean`: colours, code fences,
`Body:` and field lines removed) and the body box cuts anything past line 30
regardless.

**Empty subject**: when the Subject box is empty, AI writes a one-line subject
too (imperative, at most 60 characters asked, cut at a word to 63 so
`Subject: …` stays within the hook's 72). The answer then comes as
`SUBJECT: …` / `BODY:` and `AiBody.SplitAnswer` takes it apart, tolerating
markdown, quotes and a missing marker. The subject is only filled in if the box
is still empty when the answer arrives. The status reads *Generating subject and
message…* instead of *Generating message…*. No questions are asked either way.

How it runs: the request is written to a file in a fresh temp folder and
`copilot -p "Read the file …" -s --no-ask-user --add-dir <temp> --allow-tool=read
--deny-tool=shell --deny-tool=write --deny-tool=url --model …` is started there,
in the background, with a 3-minute limit — Copilot can read that one folder and
cannot run commands, write files or browse. The file is needed because a diff
does not fit on a command line (npm installs `copilot.cmd`, which goes through
cmd.exe's 8191-character limit). Setup per machine: `npm install -g
@github/copilot`, then `copilot` once in a terminal to sign in; GitHelper finds
`copilot.exe`/`copilot.cmd` on PATH, in `%APPDATA%\npm` or WinGet's Links folder.
**Setup at startup** (`CopilotSetup`, `frmMain.CheckCopilot`), once the
window is up:

1. **Not installed** → asks, then `winget install GitHub.Copilot` (GitHub's
   recommended way, no Node needed; `npm install -g @github/copilot` when winget
   is missing), in the background.
2. **Not signed in** (no token in the environment, no Windows credential
   mentioning copilot, no earlier success recorded) → asks, then opens a
   console running `copilot login`: it shows a one-time code and opens the
   browser at GitHub, where the user signs in and enters the code. **GitHub does
   not let a program sign in to Copilot with a user name and password**, so
   GitHelper never asks for them; the password goes into GitHub's page. What
   GitHelper does do is find the GitHub **user name** this PC already uses —
   `git credential fill` for github.com, else the entries Visual Studio's GitHub
   extension keeps in Windows Credential Manager (names only, never the secret)
   — show it, and put it on the clipboard for GitHub's user name box.
3. A **No** at startup is remembered (`HKCU\Software\Camtek\GitHelper\
   CopilotSetupDeclined`) and not asked again; **Tools ▸ Set up Copilot (AI)…**
   runs the same steps on demand. `CopilotSignedIn` records a successful sign-in
   or AI answer; an AI failure that reads like "not signed in" clears it and
   offers the sign-in again.

The code **leaves the machine** to GitHub Copilot — use it only where that is
allowed.

**Area** is the worked example of a multi-value field: nine checkboxes in
`UI_panel_Area`, a `FlowLayoutPanel` that wraps onto a second row. Each box's `Text`
*is* the value written to the message, and `frmMain.AreaBoxes` reads them back
off the panel — so the list of areas exists once, in the designer, and adding
one is a designer-only change. It writes `Area: CDB, Orcal` and parses back by
splitting on the comma; at least one must be ticked or the Commit buttons stay
disabled. Older commits have no `Area:` line and load with nothing ticked.

Inserting that row meant pushing every control below it down 60px in the
designer coordinates — the same arithmetic shift the menu bar needed.

## Remembered between runs

In `HKCU\Software\Camtek\GitHelper`, alongside the repository list:

| Value | |
|---|---|
| `WindowBounds` | `x,y,width,height` of the main window |
| `WindowMaximized` | DWORD, 1 when it was maximized |

`RestoreWindow` applies them on open, but only after checking the rectangle is
still usable. A saved position is **ignored** when no screen shows at least
80×8 px of its title bar — the second monitor is gone, or the desktop is
smaller than last time — because the window would otherwise open somewhere it
cannot be reached or dragged back. An unparseable value is ignored too; both
cases fall back to the designer's CenterScreen.

A stored size below `MinimumSize` is clamped up, one above the virtual desktop
clamped down. `FormClosing` saves `RestoreBounds`, not `Bounds`, when maximized:
storing the maximized rectangle would leave the window pinned to the screen
edges once un-maximized.

## Repositories

There is no configuration file. The dropdown is filled, in order, from:

1. the directory the dialog was launched from (VS passes `$(SolutionDir)` as
   the initial directory), or the first command-line argument;
2. the folder holding `GitHelper.exe`;
3. `Settings.BuiltIn` — `C:\Camtek` and `C:\vscan90`, edited in
   `src/GitHelper/Settings.cs`;
4. whatever **Add repo...** has recorded.

Paths that are not working copies are skipped silently and duplicates are
removed case-insensitively on the resolved root, so the first entry that *is* a
repository ends up selected. Every git call is `git -C <repo>`, so the selection
in the dropdown is always what gets committed.

**Add repo...** opens a folder picker; any folder inside a working copy will do,
since git resolves it to the root. The root is written to:

```
HKEY_CURRENT_USER\Software\Camtek\GitHelper    Repositories  (REG_MULTI_SZ)
```

and offered again on every later run. Picking a folder that is already listed
just selects it. A registry that cannot be read or written is not fatal — the
repo still works for the current session, it simply is not remembered. Delete
that value (or the key) to forget the added repositories.

## Build and run

```bash
dotnet build C:\Camtek\Sources\Tools\GitHelper\GitHelper.sln -c Release
```

```bash
C:\Camtek\Sources\Tools\GitHelper\tests\GitHelper.Tests\bin\Release\net472\GitHelper.Tests.exe
```

The test program exits 0 when every check passes and prints the failures
otherwise. `Build` and `Parse` are a round trip that the **Last commit** button
relies on — change both together and run this.

`GitHelper.exe [repoPath] [extraRepo ...]`. With no arguments it uses the current
directory, which is what the Visual Studio External Tool entry supplies.

## Running from a share

Started from a UNC path or a mapped network drive, `LocalDeployment` copies the
exe (plus `.config` and `.pdb`) to `C:\Camtek\Build\Tools`, starts that copy and
exits. The share is only ever a distribution point: running from it is slow,
breaks when the share is away, and would register a VS External Tool path other
machines may not have.

Details that matter:

- **The repository is passed as an argument, not inherited.** A process started
  from a share often has the share as its working directory, and Windows will
  not give a new process a UNC working directory — it substitutes
  `C:\Windows\System32` and the repo is lost. The folder is appended as the
  first argument (only when the caller passed no path of its own), so the right
  repo opens either way.
- **`--no-relaunch`** is added to the relaunched command line, so a copy can
  never bounce again. `Program` ignores `--` arguments when picking the repo
  path.
- **Copy failures are not fatal.** If the local exe is locked (a copy already
  running) or `C:\Camtek` is not writable, an existing local copy is started
  anyway; with no local copy at all, it just runs from the share.
- Files are compared by length and timestamp and skipped when identical, so a
  slow share is not re-read on every launch.

## Where it builds, and how VS launches it

`GitHelper.csproj` sets `OutputPath` to **`C:\Camtek\Build\Tools\`**, with no
per-framework or per-configuration subfolder, because that exact path is what
Visual Studio's External Tool entry stores. A rebuild is therefore the
deployment. Close the dialog first — a running instance locks the exe and the
build fails with MSB3027.

`PreCommit.vbs` and `wscript.exe` are gone. They existed only because
`powershell.exe` is a console application and flashed a window before
`-WindowStyle Hidden` took effect; a WinExe has no console at all, so VS
launches the executable directly:

| Field | Value |
|---|---|
| Command | `C:\Camtek\Build\Tools\GitHelper.exe` |
| Arguments | *(empty)* |
| Initial directory | `$(SolutionDir)` |
| Use Output window | unchecked |

Leaving Arguments empty side-steps the trap that `$(SolutionDir)` ends with a
backslash, which escapes the closing quote when passed as `"$(SolutionDir)"` —
the initial directory carries the same information and needs no quoting.

## Renamed from CommitUI

The tool was `CommitUI`, and its Visual Studio entry was `Commit &Message`.
Everything is now `GitHelper` / `Git &Helper`: assembly, namespace, window
caption, exe, solution. Two migrations keep an already-installed machine
working:

- the installer looks for the old External Tools title as well and **renames
  that slot in place**, because `Tools.ExternalCommandN` is a slot number — a
  leftover entry would silently repoint the shortcut and the toolbar button at
  a different tool;
- `Settings.Remembered` falls back to `HKCU\Software\Camtek\CommitUI` when the
  `GitHelper` key does not exist yet, so repositories added with **Add repo...**
  survive the rename.

Re-run **Install VS Plugin** after upgrading: a previously registered entry
still points at `CommitUI.exe`, which no longer exists.

## Install VS Plugin

The old `Install-CommitTool.ps1` is now **Tools ▸ Install VS Plugin...** (it
used to be a button at the bottom right of the dialog). It writes exactly the entry above, binds the
shortcut and adds the toolbar button — all three idempotent, so re-running is
safe and reuses the same External Tools slot.

**Version detection.** `VsInstall.Discover` lists every installation with a
private registry hive under `%LOCALAPPDATA%\Microsoft\VisualStudio`, and
`VsInstall.Preferred` picks **2026 when installed, otherwise 2022**, otherwise
the newest found. The window's dropdown lists them all if you want another one.

**Elevation.** Loading a hive needs administrator rights, and the commit dialog
deliberately runs unelevated, so the button re-launches the exe through UAC:

```bash
"C:\Camtek\Build\Tools\GitHelper.exe" --install
```

Declining the UAC prompt changes nothing. Visual Studio must be **closed** — the
hive is locked while it runs, and VS would overwrite the change on exit. The
hive is backed up next to itself first, and the untouched settings export is
kept as a `.orig`.

The shortcut and toolbar steps drive `devenv /Command`, which starts and closes
Visual Studio twice, so allow a minute or two. Both are checkboxes; the External
Tools entry alone is immediate.

The parts that were expensive to learn — no `ToolSourceKey`, toolbar group
`0x179` not `0x172`, command id `0x275 + N`, no nested quotes in `/Command`,
`devenv /Command` never exiting on its own — are carried over with their
comments. `C:\Camtek\Build\Tools\README-git-helper.md` lists them.

## Startup: keep it off the UI thread

Two rules the startup path now follows, both learned from the dialog appearing
as a process with no window until Visual Studio was closed:

- **The UI Automation read of VS's commit box never blocks the UI.** It is
  served by VS's own UI thread and searches its whole element tree, so while VS
  is busy it can hang for as long as VS lives. `VisualStudio.GetCommitMessage`
  runs it on a background MTA thread with a timeout (3s on startup, 10s when
  **From VS** is pressed), returns empty when the deadline passes, and exits
  immediately when no `devenv` is running.

  A timeout alone is not enough: *waiting* for it is what matters.
  `frmMain.BeginVsLookup` starts the lookup and returns, and the result is
  delivered later through `BeginInvoke`. An earlier version joined the worker
  from the UI thread — the window appeared on time but the message pump was
  stalled for the whole timeout, which looks exactly like a window drawing
  itself slowly. Nothing on the UI thread may wait on this call.
- **`UpdateStagedLabel` refreshes only the selected repository.** Refreshing
  every entry in the dropdown meant running `git ls-files --others` over
  `C:\Camtek` and `C:\vscan90` before the window could appear. Changing the
  selection comes back through the same method, so what is on screen is current.

Anything slow added to the constructor lands *before* `Application.Run` and is
invisible to the user — put it in `frmMain_Shown` instead.

## Commit message hook

> **Switched off for now.** Everything below is kept but not compiled: it sits
> behind the `COMMIT_HOOK` symbol (`CommitHook.cs`, `frmMain.CheckCommitHook`
> and its two call sites, the embedded `Hooks\commit-msg`, the hook tests).
> GitHelper therefore neither installs nor commits a hook. To turn it back on,
> add `<DefineConstants>$(DefineConstants);COMMIT_HOOK</DefineConstants>` to
> `GitHelper.csproj` (and `GitHelper.Tests.csproj`) and rebuild. Clones where it
> already ran keep `core.hooksPath = .githooks` until it is unset by hand.


Commits made **outside** the dialog — VS's own commit box, the command line,
TortoiseGit — are checked by a `commit-msg` hook, so the policy holds whichever
tool made the commit. The script lives in `src/GitHelper/Hooks/commit-msg` and
is embedded in the exe.

When the window opens, and whenever another repository is selected (once per
repository per run), `CommitHook.Ensure` checks the selected working copy:

| Found | Done |
|---|---|
| `core.hooksPath = .githooks` and the hook file present | nothing |
| hook file in the repository, config not set (a fresh clone) | `git config core.hooksPath .githooks`, silently |
| no hook file | writes `.githooks/commit-msg` and a `.githooks/* text eol=lf` line in `.gitattributes`, sets the config, and **commits those files** as *Git Hook Update* |
| hook set-up files new or changed but not committed | commits them as *Git Hook Update* |
| repository uses Git LFS and its hooks are missing from `.githooks` | `git lfs update` writes them; they go into the same commit |
| `core.hooksPath` set to some other folder | nothing changed; a warning says how to switch |

Git never enables hooks by itself — `core.hooksPath` is per clone and is not
pulled — which is why this runs on every start rather than once. A hook file
already in the repository is never overwritten; the repository's copy is the
policy.

**Git LFS.** LFS keeps four hooks of its own (`pre-push`, `post-checkout`,
`post-commit`, `post-merge`) in the hooks folder. Once `core.hooksPath` is
`.githooks`, `.git/hooks` is not read at all, so without copies in `.githooks`
`pre-push` stops uploading LFS objects — and LFS recreates them there on its next
checkout, as unexplained new files in Git Changes. So when any tracked
`.gitattributes` contains `filter=lfs`, `Ensure` runs `git lfs update` and the
hooks are committed with the rest. If that fails (LFS not installed) a warning
says so.

What gets committed is worked out from `git status` on `.githooks` and
`.gitattributes`, not from what this run wrote, so a set-up left uncommitted by
an earlier run is picked up too. `.gitattributes` is only included when it is
new or its uncommitted change is exactly the `eol=lf` rule — a user's own edit
to it keeps it out.

`CommitHook.CommitSetup` commits them straight away, on the current branch, **not pushed** — they go out with the next push:

```
Subject: Git Hook Update
Body: Commit message policy hook, added by GitHelper: .githooks/commit-msg, .gitattributes

Commit Type: Development
Risk: Low
Area: Other
Other Versions: No
```

It is `git commit --only -- <those files>`, so only they go in: anything the
user had already staged stays staged and out of this commit. The commit passes through the hook it has just installed. If it fails —
a merge in progress, no `user.name` — the files and the config stay in place
and the error says to commit them by hand. The `eol=lf` rule matters because Git for Windows runs the hook
with `sh`, and CRLF endings break it.

The policy, as numbers at the top of the script:

| Rule | Limit |
|---|---|
| subject (line 1) | 72 chars |
| lines after the subject, blank lines not counted | 36 (30 body + up to 6 field lines) |
| whole message, newlines included | 2200 chars |
| must contain | `Commit Type:` followed by a value |

Everything `CommitMessage.Build` writes passes: its first line is
`Subject: …`, `Body:` sits on line 2 without a blank line before it, and the
body may be one long line — which is why the hook has no blank-line-2 rule and
no per-line limit. The line limit counts the field block (`TT:`, `Customer:`,
`Commit Type:`, `Risk:`, `Area:`, `Other Versions:` — up to 6 lines) too, which
is why it is 36: the dialog's 30-line body plus every field always fits. The
2200-character limit is not checked in the dialog; a body that exceeds it is
rejected by the hook at commit time, with the reason.

To change the policy, edit `Hooks/commit-msg`, rebuild, and update the
`.githooks/commit-msg` already committed in each repository — `Ensure` does not
replace an existing copy. The tests install the hook into a scratch repository
and run real `git commit`s against it.

## Menus

A `MenuStrip` docked at the top, split by what the item acts on:

- **Git** — the selected repository: **Switch...**, **Show Log...** and **Reset Local...**
- **Tools** — **Check Merge...** and **Switch Git User...**, then this machine:
  **Install VS Plugin...** and **Add to Folder Context Menu**, which is a
  checked item whose caption flips to *Remove from...* when the entry is
  present. `DropDownOpening` reads the live registry state rather than trusting
  a cached flag.

Adding the menu meant every control moved down by its 28px height: a `MenuStrip`
paints over the top of the client area, and these controls are positioned
absolutely, so the first row would have been hidden behind it. The designer
coordinates were shifted rather than nudged at runtime, so the designer still
shows the true layout. **Anything added above the menu height must account for
it.**

### Switch Git User

Two separate things, either usable on its own:

- **Clear credentials** — `git credential reject` for the protocol and host of
  `origin`, **and** the selected entries in Windows Credential Manager. Scoped
  to that host; other hosts are untouched.

  The second half is not redundant. `git credential reject` only reaches the
  helper git is *configured with*; anything written by another tool survives it
  and stays visible under Windows Credentials, which is what makes a clear look
  like it did nothing. On this machine the two entries for `github.com` are
  `GitHub for Visual Studio - https://…@github.com/` and
  `GitHub - https://api.github.com/…`, both written by **Visual Studio's GitHub
  extension** — git has never owned them.

  **"Also forget the commit identity"**, ticked by default, additionally removes
  `user.name` / `user.email` — from the config file that actually defines them,
  which `IdentityOrigin` works out first. Unsetting the repository's copy when
  the value lives in global config would remove nothing anyone can see. It uses
  `--unset-all`, since `--unset` fails outright on a key with more than one
  value.

  Afterwards git refuses to commit — *"Author identity unknown"* — which is the
  point: the next person has to say who they are instead of committing under the
  previous author's name. That is what makes this a sign-*out* rather than only
  a credential wipe, and it is why the box is ticked by default in a dialog
  whose job is handing the machine to someone else. The confirmation names the
  config file before anything happens.

  `WindowsCredentials.ForHost` lists every entry whose target mentions the host
  and flags the ones git did not write (target starts with `git:`). They are
  pre-selected but listed before anything happens, and the confirmation says out
  loud that deleting Visual Studio's entries signs VS out of GitHub — usually
  the point when switching user, but not something to discover afterwards.
- **Sign in** — `git credential approve` with a new user name and password or
  token. It rejects first, because a helper asked to approve a second
  credential for the same host can keep handing back the old one. Afterwards it
  **reads back** what git now answers with, and says so plainly if that is not
  the account just entered.

**The dialog names the stored account**, not just the host:
`roni.fli@gmail.com   at https://github.com`. `GitCredentials.StoredUser` asks
`git credential fill` with `credential.interactive=false`, `GCM_INTERACTIVE=never`
and `GIT_TERMINAL_PROMPT=0` — all three, so opening the dialog can only ever
*read* what is there and can never pop a sign-in prompt. The password comes back
on the same stream and is discarded; this returns a name, never a secret.

Without that read-back, "I signed in and it still shows the previous user" had
no visible cause. The two things it separates:

- **the login** — what the credential helper hands to a push;
- **the identity** — `user.name` / `user.email`, which is what appears *on the
  commits* and which a sign-in does **not** change. When the identity checkbox
  is left unticked the status line now says whose name commits still carry.

If some other helper earlier in `credential.helper` keeps answering first, the
read-back catches it: *"Stored, but git still answers with 'X' — another
credential helper earlier in the list is winning."* Verified with a rigged
helper that ignores `erase`.

**Git Helper stores no credential itself.** The password goes to
`git credential` on **standard input** — never as an argument, which the process
list would expose — and from there to whatever helper git is configured with; on
these machines the Git Credential Manager, which uses Windows Credential
Manager. The password box is cleared as soon as the call returns, pass or fail.

The checkbox also sets `user.name` / `user.email`, at a **scope you choose** —
*this repository only* (default) or *all repositories (global)*. Global asks for
confirmation first, because it changes every clone on the machine.

Both are needed, and the dialog shows **which config file the current identity
comes from** so you can tell which you want:

```
Commits as   ronifli  <roni.fli@gmail.com>   (from global: C:/Users/x/.gitconfig)
```

On these machines the identity lives in **global** config. That is why "I
switched user and the old one is still there on another computer" is usually
neither a bug nor a stale credential: `user.name` is per-machine, a
repository-local change fixes one clone and leaves the rest, and a different PC
has its own global file entirely. A local setting still wins over global, which
the confirmation says.

`GitCredentials` refuses an **SSH** remote — it authenticates with a key, so a
stored user name and password would be an entry nothing ever reads. The dialog
disables the fields and says so; the API refuses independently, so the guard
does not depend on the UI.

Tested against a throwaway `credential.helper=store --file=…` in a scratch repo:
`store` wrote the entry, `credential fill` handed back `username`/`password`,
`reject` emptied it, and the SSH and empty-password guards both refused. Windows
Credential Manager was never involved.

### Toolbar

A `ToolStrip` (`UI_pnl_Toolbar`) under the menu, with **Show Log**
(`UI_cmd_ShowLog`, same handler as Git ▸ Show Log). Its image is drawn in code
(`ToolbarIcons.ShowLog`, 16×16): a branch line with three commits — the newest
amber, like the application icon's branch tip — each with a log line beside it.
Adding the toolbar moved every control below it down by its 25px height in the
designer, the same way as the menu; the toolbar is added to `Controls` before
the menu so the menu docks above it.

### Explorer folder context menu

**Tools ▸ Add Git Helper to Context Menu** (reads **Remove Git Helper from
Context Menu** once it is there) adds two entries to a folder's right-click menu:

| Entry | Runs |
|---|---|
| **Open Git Helper** | `"<exe>" "%V"` — the commit dialog for that folder's repository |
| **Git Helper Log** | `"<exe>" --log "%V"` — only the log window (`Program` sees `--log`, opens `frmShowLog` standalone with its own taskbar button) |

An older install with the single "Open with Git Helper" entry, or entries for an
exe that has moved, shows **Add** again, which rewrites them.

`ShellIntegration` writes the verbs under **`HKCU\Software\Classes`** — the
per-user half of HKCR, so no elevation and nothing changed for other accounts:

| Key | Covers |
|---|---|
| `Directory\shell\GitHelper` | right-clicking a folder |
| `Directory\Background\shell\GitHelper` | right-clicking inside an open folder |
| `…\shell\GitHelperLog` (both places) | the **Git Helper Log** entry |

Each entry runs with `"%V"`, the clicked folder — for the background verb
`%1` would be empty, which is why it is not used. The folder becomes the first
argument, so any folder inside a working copy opens that repo.

`IsRegistered` also treats a command pointing at a *different* exe as not
registered, so after the tool moves or is renamed the menu offers to write it
again rather than reporting a dead entry as installed.

On Windows 11 the entry lives under **Show more options** (Shift+F10); that is
where all classic verbs go, and putting one in the top-level menu needs a
packaged shell extension, not a registry key.

### Check Merge

The RTM audit: *is every change on that branch also on this one?* Pick a source
branch (RTM0), the target is whatever is checked out (Main), and every source
commit since the branch point gets a verdict.

A cherry-pick gets a new SHA, so commit ids cannot answer this. `MergeCheck`
uses four signals, strongest first, and reports the weak ones as **probable**:

| Signal | Status | Why |
|---|---|---|
| `git cherry` patch-id | present (same patch) | git compares content, so a reworded port still matches |
| `(cherry picked from commit …)` | present (cherry-pick) | what `cherry-pick -x` writes — exact, survives conflict edits |
| `Subject:` from the structured message | probably present (subject) | the message, not the content |
| `TT:` number | probably present (TT) | survives rewording; catches a fix re-done by hand |

**Origin against origin.** Both sides are server branches: "Changes from" lists
only `origin/*` branches, and "present in" is the **origin copy of the
checked-out branch** (its upstream, else `origin/<same name>`) — never the local
branch, so unpushed or un-pulled commits cannot change the verdict. Origin is
fetched (`git fetch --prune origin`) when the window opens — the list is refilled
afterwards — and again before every check; a failed fetch is said in the summary
and the last fetched state is used. A branch that is not on origin cannot be
checked: the window says to push it first.

**Changes from** lists only branches whose name contains **both /root and
/rtm** (any case), e.g. `origin/root/RTM0`; `origin/RTM0` or `origin/root-main` are
not listed — `frmCheckMerge.SourceBranchKeywords`; the
search box filters within those.

**ALL** is the first entry whenever the list offers more than one branch: it
runs the check against every branch the list currently shows (so a search
narrows it), one after the other on the background thread, with progress in the
summary line. The results go into one list — the **From** column says which
branch each commit is on — and the summary totals them and names the branches
with something missing ("… 3 MISSING - in origin/root/RTM2 (2),
origin/root/RTM5 (1)"). A branch that cannot be compared (no common ancestor)
is listed under "Could not compare" and does not stop the rest. Copy as TSV
includes the From column.

Both sides are read with `CommitMessage.Parse` — the same parser the dialog
writes with — so the fields round-trip. Subjects are compared with whitespace
collapsed, case-insensitively. The branch list has a search box, since a release
repo has more branches than fit a dropdown.

**Scope: `target..source`** — every commit on the source branch that is not
reachable from the target, merges excluded. That is the question asked directly:
*did this work get across?* Nothing is assumed about how it should have got
there.

- **The merge never happened** → every source commit since the split is listed,
  each with a verdict. That is how a missing merge announces itself.
- **The merge did happen** → the commits are reachable, the list is empty, and
  the summary says so.
- **Merged, then more work** → exactly the commits made since the merge.

**Only structured commits are audited.** `CommitMessage.IsStructured` requires
both a non-empty `Subject:` and a non-empty `Commit Type:` line — what
`Build` always writes and free prose never does. Commits made before the dialog
existed, or from VS's own commit box, carry none of the fields this check
matches on, so a verdict for them would be guesswork dressed up as a result.
They are skipped **and counted**: the summary always says how many, because a
large number there means the audit covers less than the result suggests.

Note `Parse` cannot be used for this test — it deliberately degrades an
unstructured message to line 1 = Subject, so every message looks like it has
one.

The filter applies to the **source** side only. The *target* side keeps its
unstructured commits: a fix ported through VS's commit box has no fields, but
its subject line can still match, and dropping it would turn a real port into a
false MISSING. What gets audited and what counts as evidence are different
questions.

The target side is read separately, only to recognise ports that arrived as
new commits (subject/TT). It is scoped by **date** — 90 days before the oldest
commit being checked — not by merge-base. That matters: merging Main *into* RTM0
drags the merge-base forward onto Main's own history, so a merge-base scope
would hide target commits made before it, and a port sitting among them would be
reported MISSING. Verified: with the merge-base on Main's side, a port made
before it is still found.

The two message-based signals are why the commit message is structured at all.
They are also the weak point: **a `Change-Id` trailer would turn this into an
exact set difference** and make rows 3 and 4 unnecessary. Until that exists,
treat "probably present" as a row to look at, and MISSING as the queue.

Rows are coloured green/amber/red, "Show only what is missing" filters to the
queue, and **Copy as TSV** pastes into a spreadsheet.

The comparison runs on a background thread — `git log` over a release branch is
not instant.

### Show Log

**Git ▸ Show Log** lists the history of the selected repository, newest first:
Date, Author, **Commit Type, Area, TT Number, Risk Level**, Subject, Commit.

**Which commits get the field columns:** any message containing `Commit Type:`
with a value — the dialog's own format, or one written by hand under the
commit-msg hook. The dialog's format is read with `CommitMessage.Parse`
(bottom-up field block, so prose like "Risk: High was too cautious" in a body is
never taken for a field); a hand-written message may put its keys anywhere,
even `Commit Type: Feature - …` as the subject, so any key the block did not
yield is looked up line by line (`CommitLog.ParseFields`). Commits **without**
`Commit Type:` are listed in **grey**, subject = first line, field columns
empty — nothing is guessed from them.

**Branch** (`UI_cmb_Branch`): click the name to pick another branch, or type
to **search**: the list narrows to branches containing the text anywhere, any
case (`rtm` finds `RTM0` and `origin/RTM0`). Enter or leaving the box takes the
exact name, else the first match; Escape puts the current one back.
The list is the checked-out branch first, the other local branches, then
`origin/*` (as of the last fetch). Picking a branch only changes what is
*shown* — nothing is checked out. A local branch's **From origin** means its
upstream (or `origin/<same name>` when none is configured); an `origin/…`
branch is fetched and shown as the server has it, with the box ticked and
greyed. The window title names what is on screen.

**From origin** (checkbox):

| | Shows | Includes |
|---|---|---|
| unticked (default) | the local branch, `HEAD` | commits not pushed yet; *not* commits others pushed that you have not pulled |
| ticked | `git fetch --prune origin`, then the upstream, e.g. `origin/main` | exactly what the server has |

The branch label and the summary line say which one is on screen, and when it
was fetched. A failed fetch (offline, no credentials) is not fatal: the list
shows origin as of the last successful fetch and the summary says **FETCH
FAILED**. A branch with no upstream has nothing on the server to show; the box
is then disabled and reads *From origin (none)*. The fetch runs with
`GIT_TERMINAL_PROMPT=0`, so it can never hang on a console prompt; the
credential manager may still show its own sign-in window, as for Push.

- **Column filters**: a text box above each column header. Typing filters the
  list to rows whose cell in that column contains the text (any case); boxes
  combine (Commit Type `feat` + Risk Level `high`), and combine with Search.
  The Commit box also matches the full SHA. The boxes are placed at runtime
  (`LayoutColumnFilters`) over their columns and follow resized columns and
  sideways scrolling — `ScrollNotifyListView` adds the scroll event ListView
  lacks. Matching is `LogSort.PassesColumnFilters`, on the same `CellText` the
  rows display.
- **Search** filters what is loaded: subject, body, TT, customer, type, risk,
  area, author, date and SHA, case-insensitively.
- **Sorting**: click a column header — ascending, again descending, a third
  time back to git's order (newest first along the history). ▲/▼ marks the
  column. TT Number sorts numerically (9 before 10), Risk Level as Low < Medium
  < High, Date chronologically; empty cells (commits without Commit Type) stay
  at the bottom in both directions, and equal values keep git's order. The sort
  survives a search, a refresh and switching From origin. `LogSort`, UI-free.
- **Show** 100 / 500 (default) / 2000 / All.
- Below the list, the selected commit: its **message** (with commit, author,
  date) and, under it in a second split, the **files it changed** — Status
  (Added green / Modified / Deleted red / Renamed or Copied blue, `old -> new`),
  File, and Lines (`+12 -3`, or `binary`). The File header sums them up
  ("14: 3 added, 10 modified, 1 deleted"). Compared with the first parent — for
  a merge, what the merge brought in; a root commit shows everything as Added.
  `CommitLog.ChangedFiles`, read with `-z` so odd file names survive, on a
  background thread.
- **Double-click a file** in the files list to compare it in **ExamDiff Pro**
  (`C:\Program Files\ExamDiff Pro\ExamDiff.exe`, or any `ExamDiff*.exe` in that
  folder): left = before the commit (first parent), right = the commit. Both
  sides are written to `%TEMP%\githelper-compare` with git's exact bytes
  (`git cat-file blob`, no text decoding), read-only, named
  `Name (abc1234^).ext` / `Name (abc1234).ext` so ExamDiff's titles say which is
  which; an added file compares against an empty left side, a deleted one
  against an empty right side, a rename uses the old path on the left. Folders
  older than a day are removed on the next compare. `FileCompare`.
- **Copy SHA** puts the full hash on the clipboard.
- **Remembered between runs** (`WindowPlacement`, HKCU\Software\Camtek\GitHelper):
  `ShowLog.Window` = position, size, maximized; `ShowLog.Split.UI_pnl_Split`
  and `ShowLog.Split.UI_pnl_DetailsSplit` = where the two dividers were. Same
  safety rules as the main window: ignored when no screen shows the title bar,
  clamped to the minimum size and the desktop, RestoreBounds saved while
  maximized, and a divider that no longer fits is clamped. Delete those values
  to get the defaults back.

`CommitLog.Read` asks for `%H %h %ad %an %B` separated by ASCII record/unit
separators (0x1E/0x1F), which never occur in a message, so multi-line bodies
come through intact. Fetch, log and the per-commit stat all run on background
threads, like Check Merge.

Controls on this form follow the `UI_<type>_<Name>` naming convention
(`UI_grd_Commits`, `UI_txt_Search`, `UI_txt_FilterType`, `UI_panel_ColumnFilters`, `UI_chk_Origin`, `UI_cmd_Refresh`, …).

### Check Merge - AI check

A second panel under the results, **AI check** (`UI_pnl_AICheck`): pick a date
in **Commits since** (the last one used is remembered, `CheckMerge.AISince`) and press **Check with AI**. It runs on what "Changes from"
has selected — one branch, or ALL — against the same origin target, after a
fetch. `MergeAi.Check`, per source branch:

1. the source commits since the date that are **not in the target's history**
   (`git log --no-merges --since=<date> target..source`);
2. those whose patch is already in the target (`git cherry`) are reported
   **present (same patch)** without asking the AI;
3. the rest (at most 60 per branch — choose a later date for more) go to
   GitHub Copilot in one request with each commit's message and diff (4,000
   chars each), plus the target's commits since the same date (message, files,
   1,500 chars of diff; request capped at 200,000 chars);
4. Copilot answers one line per commit —
   `sha | PRESENT/PARTIAL/MISSING/UNSURE | target sha | reason` — judged by what
   the code does, so a port under another subject, a fix re-done by hand or a
   half-ported change is recognised. `MergeAi.Parse` reads the lines back,
   tolerating markdown; a commit without an answer is "unsure".

Results list MISSING first (red), then partial (amber), unsure (grey), present
(green), with From, Commit, Date, Subject, **In target** (the port the AI points
at) and Reason. The status line totals them. These are **suggestions** — the
AI sees trimmed diffs and only target commits since the date, which is why it is
told to say UNSURE rather than MISSING when a port may be older. Copilot runs
with the same restrictions as the AI button (reads the request file only; no
shell, no writes, no web) and up to 5 minutes per branch. The diffs of both
branches **leave the machine** to GitHub Copilot.

### Check Merge - missing and changed lines log

After an AI check, **frmMissingLog** opens by itself when anything is missing,
partial or unsure (and again from **Missing lines…** in the AI panel). **Double-click**
a row in either Check Merge list to open it on that commit: from the AI list with
the other gaps listed too, from the regular results for that one commit. It is not
modal. Left: those commits (From, Verdict, Commit, number of missing/changed
lines, Subject); right: the selected commit's lines, per file:

```
Commit  1a2b3c4d   origin/root/RTM2   2026-09-10   Dana
Subject Fix null deref in scanner
AI      PARTIAL (AI) - warning ported without the scan id
Target  origin/main

== src/Scan.cs   added: 1 missing, 1 changed, 1 ok (of 3)   removed: 1 still there, 0 changed (of 1)
  + L6     NewCall(x);                          MISSING
  ~ L7     Log.Warn("empty scan", scanId);      CHANGED 79%
    T7     Log.Warn("empty scan");      <- target
  - L5     OldCall(x);                          STILL IN TARGET (L6)
```

`MissingLines.Analyze` reads the commit's diff (`--unified=0`) and judges every
changed line against the target's copy of the file (`git cat-file -p
target:path`, the old path for a rename), anywhere in the file:

| Added line | Removed line |
|---|---|
| found as-is → ok | gone → ok |
| **similar line** → **CHANGED**, with the target's version (`T<line>`) and similarity | similar line still there → **CHANGED** |
| nothing like it → **MISSING** | still there as-is → **STILL IN TARGET** |

Lines are compared trimmed with runs of spaces collapsed. *Similar* = the two
lines share their identifiers (Jaccard ≥ 0.5 over names of 2+ characters) and
most of their text (edit-distance ratio ≥ 0.6): an extra condition or argument
is a change, while `NewCall(x)` against `OldCall(x)` is missing. Braces, `else`
and the like are not judged. A file the target lacks is flagged **FILE NOT IN
TARGET**. Colours: red missing, blue changed, amber still there. A MISSING
verdict whose lines are all there as they are shows green — the AI was wrong.
**Copy all** puts every report on the clipboard; size and divider are remembered
(`MissingLog.*`).

### Admin (Alt+F12)

**frmAdmin** is on no menu: press **Alt+F12** in the main window
(`frmMain.ProcessCmdKey`, so it works whatever has focus). It opens for the
selected repository.

**Branch** works like Check Merge's "Changes from": **ALL**, then **main**
(origin's default branch, e.g. `origin/main`), then the release branches —
origin branches containing both `/rtm` and `/root`
(`MergeCheck.ReleaseBranchKeywords`, shared by both windows). The list is
refreshed after each fetch.

**Check commit messages** fetches origin, then lists every commit on the chosen
branch(es) since **From Date:** whose message does not follow the structured
format (`MessageAudit.CheckAll`). A commit on several of the checked branches —
history a release branch shares with main — is listed once, and the **Branch**
column names all of them:

| Required | Reported as |
|---|---|
| first line `Subject: …`, non-empty | no Subject: line / empty Subject |
| first line at most 72 characters | first line N chars (max 72) |
| `Commit Type:`, `Risk:`, `Area:` with values (a `<…>` placeholder counts as empty) | no Commit Type: / empty Risk … |

Merge commits are skipped unless **Include merge commits** is ticked — git or
GitHub writes those. The list shows Branch, Date, Author, Commit, Problems and the first
line; the selected commit's full message is below it; the summary counts the
offenders per author. **Copy as TSV** for a spreadsheet. Size, divider and the
last **From Date** are remembered (`Admin.*`, `Admin.FromDate`).

### Switch

`frmSwitchBranch` lists every `origin/*` ref — `origin/HEAD` excluded, since it
is a symbolic ref rather than a branch anyone checks out — and filters as you
type on a case-insensitive substring of the full `origin/name`, so `rtm` and
`origin/rtm` find the same thing. Typing filters, Down moves into the list,
Enter or a double-click switches.

Two kinds of entry, marked in the list:

| Case | What runs |
|---|---|
| a local branch of that name exists | `git checkout <name>` — as it stands, with any local commits it carries |
| it only exists on origin | `git checkout -b <name> --track origin/<name>` |

The second matters on a fresh machine: the branch is created *and* given an
upstream, so **Reset Local** can pull it afterwards.

**Fetch** runs `git fetch --prune origin`; without it the list only shows what
the server had at the last fetch, so a branch made this morning would be
missing.

Nothing is discarded. A checkout that would overwrite local edits is refused by
git, and the error says to commit them or use **Reset Local** — the dialog also
warns up front when the working copy is dirty.

### Reset Local

`ResetLocal` is the only action that destroys uncommitted work, so it names the
file counts and the exact commands, and its confirmation defaults to No:
`git reset --hard HEAD`, then `git clean -fd` *only* on a separate confirmation,
then `git pull --ff-only`. Untracked files are a second question because
`reset --hard` never touches them. `--ff-only` is deliberate — a plain `pull`
would merge or rebase unpushed commits, so instead it fails and the message
points at **Undo Local Commits**.

## Icon

`GitHelper.ico` is a commit graph — a trunk with two commits and a branch off
it, white on an indigo-to-blue plate with an amber branch tip. It is both the
exe's `ApplicationIcon` and an embedded resource that `AppIcon.Apply` puts on
each form, so the title bar and Alt-Tab get the full set of sizes instead of the
single 32px one `Icon.ExtractAssociatedIcon` returns.

It holds 16, 20, 24, 32, 40, 48, 64, 128 and 256px. **The small sizes are drawn
differently**: below 24px the branch is a straight stroke and the dots are
proportionally larger, because a bezier plus thin strokes turns into a blob at
16px — which is the size the taskbar and VS's Tools menu actually use.

To change it, edit and re-run the generator, then rebuild:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File C:\Camtek\Sources\Tools\GitHelper\tools\make-icon.ps1 -OutIco C:\Camtek\Sources\Tools\GitHelper\src\GitHelper\GitHelper.ico -PngDir C:\temp\icon-preview
```

`-PngDir` is optional and drops the individual sizes as PNGs to look at. The
script writes the ICO container by hand (PNG-compressed entries, understood from
Vista on) because `System.Drawing` cannot save a multi-size .ico.

## Framework notes

- **4.7.2 specifics.** `ProcessStartInfo.ArgumentList` does not exist here, so
  `Git.Quote` builds the command line by the rule `CommandLineToArgvW` actually
  uses (double the backslashes that precede a quote). Repository paths with
  spaces depend on it.
- **DPI: the application is declared unaware, on purpose.**
  `app.manifest` says `<dpiAware>false</dpiAware>`. Windows then reports 96 dpi
  whatever the display is set to, the designer's coordinates are the pixels that
  get drawn, and Windows stretches the finished window on a scaled display.
  Slightly soft above 100%, never misaligned.

  Both alternatives were tried and both failed on a scaled Windows 11 machine:

  | Approach | Result |
  |---|---|
  | per-monitor v2 | the app must re-lay itself out on every DPI change; WinForms on .NET Framework only does that for some controls |
  | system aware + `AutoScaleMode.Font` | the font grew faster than the row heights — every label clipped, rows collided |

  A dialog that is readable but soft beats one whose controls overlap. Test any
  change to this at 150%, not at 100%, or you will not see the difference.

  **Consequence for layout code**: plain design-time pixels are correct here.
  `InputLeft = 136` and the literal margins in `UpdateButtonRow` are right
  precisely *because* nothing scales. If awareness is ever turned back on, all
  of that has to become scale-relative, and the forms need
  `AutoScaleMode`/`AutoScaleDimensions` — with the row heights re-checked.

  `Application.SetHighDpiMode` does not exist on .NET Framework (verified by
  compiling against the 4.7.2 and 4.8 reference assemblies), so the manifest is
  the only lever.

- **`longPathAware`** is set in the same manifest: it lifts the 260-character
  path limit for deep working copies, and needs
  `HKLM\SYSTEM\CurrentControlSet\Control\FileSystem\LongPathsEnabled` on the
  machine as well.
- **UI Automation** comes from the `UIAutomationClient`, `UIAutomationTypes` and
  `WindowsBase` references.
- `Microsoft.VisualBasic.Interaction.InputBox` still supplies the soft-reset
  count prompt, as in the PowerShell version.
- The projects are SDK-style, so they restore and build with `dotnet build` as
  well as inside Visual Studio.

## Still missing, as in the original

- Commits one repository at a time, whichever is selected in the dropdown.
- No amend; use **Undo Local Commits** and re-commit.
- Writing the finished message back into VS's box is not implemented — use
  **Copy to clipboard** and paste.
- Commits made outside this dialog still carry only what the hook demands
  (`Commit Type:` and the size limits), not the full field set.
