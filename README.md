<p align="center">
  <img src="src/Filee.App/Assets/Icons/theodrop.png" alt="Oneon" width="128">
</p>

<h1 align="center">Oneon</h1>

<p align="center">
  <b>Drag a file, drop it on a format, done.</b><br>
  A free file converter for macOS and Windows that works offline.
</p>

<p align="center">
  <a href="https://github.com/Noah7601/oneon/releases/latest"><b>Download</b></a> ·
  <a href="#install">Install</a> ·
  <a href="#features">Features</a> ·
  <a href="#license-and-credits">License</a>
</p>

<p align="center"><b>English</b> · <a href="README.ko.md">한국어</a></p>

## What it does

Hold a key while you drag files. A ring of formats opens at the cursor, showing only the ones that fit those files.
Drop on one and the converted file appears next to the original. Nothing is uploaded.

| | macOS | Windows |
|---|---|---|
| Convert | <kbd>Shift</kbd> + drag | <kbd>Ctrl</kbd> + drag |
| Tools (resize, merge, …) | <kbd>Command</kbd> + drag | <kbd>Alt</kbd> + drag |

You can change the keys in Settings.

## Install

Download from [Releases](https://github.com/Noah7601/oneon/releases/latest).

| Your computer | File |
|---|---|
| Mac with Apple Silicon (M1 or later) | `Oneon-<version>-osx-arm64.dmg` |
| Mac with Intel | `Oneon-<version>-osx-x64.dmg` |
| Windows 10/11 (Intel/AMD) | `Oneon-<version>-win-Setup.exe` (or `win-Portable.zip`) |

**macOS 14 or later**

Install or update in one step (paste into Terminal):

```bash
curl -fsSL https://raw.githubusercontent.com/Noah7601/oneon/main/installer/macos/update.sh | bash
```

It picks the build for your Mac, checks its SHA-256, replaces the old app (into the Trash) and opens the Accessibility
settings so you can allow Oneon again. From 1.11.7 on, the Update button in the app does the same. Or by hand:

1. Open the `.dmg` and drag Oneon into Applications.
2. Oneon is not notarized by Apple, so the first start is blocked once. Click **Done**, then go to
   System Settings → Privacy & Security and click **Open Anyway**.

**Windows 10/11**
1. Run `Oneon-<version>-win-Setup.exe`.
2. It is not code-signed, so "Windows protected your PC" may appear. Click **More info** → **Run anyway**.
3. Setup can add the right-click menu (Windows 11), start at sign-in and optional engines.

Oneon checks this repository for updates and keeps your settings when it updates.
Every file's SHA-256 hash is in `Oneon-<version>-SHA256SUMS.txt`.

## Features

- **Format ring at the cursor**: only the formats that make sense for the files you drag.
- **Many file types without other software**: images, PDF, Word, Excel, PowerPoint, HWP, e-books, archives,
  fonts, CAD and 3D models, with no Office, Hancom Office or LibreOffice needed.
- **Optional engines** for more (FFmpeg for video and audio, LibreOffice, Pandoc, calibre, Ghostscript),
  downloaded only when you choose them. See [ENGINES.md](docs/ENGINES.md).
- **Many files at once**, with presets for quality, size, PDF pages, output folder and file names.
- **Watch folders** that convert every file dropped into them.
- **Other ways in**: right-click menu (Finder Services, Windows 11 Explorer), keyboard shortcut, drop zone and a
  `theodrop` command line (Windows Setup option; macOS via `install.sh` from the `.tar.gz`).
- Light and dark themes; English, 한국어 and 简体中文.

## Privacy

Files are converted on your computer and never uploaded. Oneon collects no usage data. It connects to the
internet only to check GitHub for a newer release (can be turned off in Settings → About), to download engines
you choose, and when you click a link.

## Build from source

```bash
git clone https://github.com/Noah7601/oneon.git
cd oneon
dotnet run --project src/Filee.App   # .NET 10 SDK
dotnet test
```

[Architecture](docs/ARCHITECTURE.md) · [Adding a converter](docs/ADDING-A-CONVERTER.md)

## License and credits

MIT license. See [LICENSE](LICENSE). Bundled third-party components keep their own licenses:
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Reference: Oneon is based on [Filee](https://github.com/KnifeLemon/Filee) (MIT).
