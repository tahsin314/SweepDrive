# 🧹 SweepDrive — a simple, safe Windows disk cleaner

[![Downloads](https://img.shields.io/github/downloads/USERNAME/sweepdrive/total?label=downloads&color=1E6EBE)](../../releases)
[![Latest release](https://img.shields.io/github/v/release/USERNAME/sweepdrive?color=1E6EBE)](../../releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-0078D6)

> Got tired of paying for bloated "PC cleaner" tools that nag you to upgrade every
> five minutes — so I vibe-coded my own one lazy afternoon. It's free, open-source,
> and does exactly one thing: clears out the junk and hands your disk space back.
> No ads, no upsells, no account. Check it out! 🙌

SweepDrive frees up disk space on Windows by removing caches, temporary files,
and other safe-to-delete junk — without touching your documents, photos, logins,
or settings. It's a single portable `.exe`: no installer, no admin required, no ads.

<sub>📊 The **downloads** badge above counts every download of the release files
automatically once you publish a Release. Replace `USERNAME` in the badge links
with your GitHub username.</sub>

> **Safe by design.** The one-click **Safe Clean** only removes things that
> regenerate on their own (temp files and caches). Anything that could contain
> real data is opt-in, clearly labelled, and — in the Downloads tab — sent to
> the Recycle Bin so it's recoverable.

## ✨ Features

- **Safe Clean (Recommended)** — one click clears temp files, browser caches,
  shader caches, and developer caches. Keeps every file, password, login, and
  setting. No admin needed.
- **Tabbed, uncluttered UI** — Overview · Windows Junk · Browser Cache ·
  Developer · Graphics · Advanced · Downloads.
- **Details pane** — hover any item to see *what it is*, *whether it regenerates*,
  and *what you lose* before selecting it.
- **Scan first** — measure the size of every category before deleting anything.
- **Downloads analyzer** — lists your biggest Downloads items; deletes only what
  you tick, straight to the **Recycle Bin** (recoverable).
- **Advanced (admin)** — Windows Temp, Windows Update leftovers, Delivery
  Optimization, logs, memory dumps, Prefetch, **WinSxS component cleanup (DISM)**,
  and **WSL2 disk compaction** (reclaims empty space with no data loss).

## 📦 Download & run (portable)

1. Download **`SweepDrive.exe`** from the [Releases](../../releases) page (or the repo root).
2. Double-click it — it runs from anywhere, no installation, **no admin needed**.
3. First launch may show a blue **“Windows protected your PC”** SmartScreen box
   (the app isn't code-signed). Click **More info → Run anyway** (one time).
4. Click **Scan sizes**, then **Safe Clean** — or pick individual items and **Clean checked**.

For the **Advanced** tab, click **Restart as Admin** (or right-click the exe →
**Run as administrator**) and approve the prompt.

## 🗂️ What each category is

| Category | Removes | Regenerates? |
|---|---|---|
| User / Windows Temp | Leftover temporary files | Yes |
| Recycle Bin | Deleted files still on disk | — |
| Thumbnail cache | Explorer preview thumbnails | Yes |
| Crash dumps & error reports | Data written when apps crash | Yes |
| Browser cache (Chrome/Edge/Firefox) | Cached web files only — **not** logins/history | Yes |
| Developer caches (pip, npm/pnpm, Gradle, Playwright, conda, ~/.cache) | Downloaded packages/build caches | Yes |
| Shader caches (DirectX, NVIDIA) | Compiled GPU shaders | Yes |
| Windows Update leftovers / Delivery Optimization / Logs | Update installers & diagnostic logs | Yes |
| Memory dumps / Prefetch | BSOD dumps, launch-optimization data | Yes |
| WinSxS cleanup (DISM) | Superseded Windows components | Managed by Windows |
| Compact WSL disk | Empty space in the WSL2 virtual disk | No data lost |

## 🔧 Build from source

Requires Windows with **.NET Framework 4.x** (already on Windows 10/11).

```bat
build.bat
```

This runs the C# compiler bundled with Windows (`csc.exe`) and produces
`SweepDrive.exe`. Source is a single file: [`src/SweepDrive.cs`](src/SweepDrive.cs).

## ✅ Requirements

- Windows 10 or 11
- .NET Framework 4.x (preinstalled on Windows 10/11)

## 🤝 Contributing, feature requests & bugs

Ideas and fixes are welcome — this is a hobby project, so no formal process:

- **Found a bug or want a feature?** Open an [issue](../../issues/new) and describe
  what happened (or what you'd like). Screenshots and your Windows version help.
- **Want to contribute code?**
  1. **Fork** this repo (button, top-right).
  2. Create a branch: `git checkout -b my-change`
  3. Make your edit in `src/SweepDrive.cs`, run `build.bat` to test.
  4. Commit and push: `git commit -am "Describe your change" && git push origin my-change`
  5. Open a **Pull Request** from your fork — describe what you changed and why.

No contribution is too small — typo fixes and new cleanup categories are all fair game.

## ⚠️ Disclaimer

SweepDrive deletes files. While the defaults are conservative and everything is
shown before deletion, **use at your own risk** — review selections and keep
backups of anything important. The authors are not liable for data loss
(see [LICENSE](LICENSE)).

## 📄 License

[MIT](LICENSE) © 2026 Tahsin
