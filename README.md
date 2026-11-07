<p align="center">
  <img src="src/Filee.App/Assets/Icons/theodrop.png" alt="theOdrop" width="128">
</p>

<h1 align="center">theOdrop</h1>

<p align="center">
  <b>Drag a file, drop it on a format, done.</b><br>
  A free file converter for macOS and Windows that works offline.
</p>

<p align="center">
  <a href="https://github.com/Noah7601/theodrop/releases/latest"><b>Download</b></a> ·
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

Download from [Releases](https://github.com/Noah7601/theodrop/releases/latest).

| Your computer | File |
|---|---|
| Mac with Apple Silicon (M1 or later) | `theOdrop-<version>-osx-arm64.dmg` |
| Mac with Intel | `theOdrop-<version>-osx-x64.dmg` |
| Windows 10/11 (Intel/AMD) | `theOdrop-<version>-win-Setup.exe` (or `win-Portable.zip`) |

**macOS 14 or later**
1. Open the `.dmg` and drag theOdrop into Applications.
2. theOdrop is not notarized by Apple, so the first start is blocked once. Click **Done**, then go to
   System Settings → Privacy & Security and click **Open Anyway**.

**Windows 10/11**
1. Run `theOdrop-<version>-win-Setup.exe`.
2. It is not code-signed, so "Windows protected your PC" may appear. Click **More info** → **Run anyway**.
3. Setup can add the right-click menu (Windows 11), start at sign-in and optional engines.

theOdrop checks this repository for updates and keeps your settings when it updates.
Every file's SHA-256 hash is in `theOdrop-<version>-SHA256SUMS.txt`.

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

Files are converted on your computer and never uploaded. theOdrop collects no usage data. It connects to the
internet only to check GitHub for a newer release (can be turned off in Settings → About), to download engines
you choose, and when you click a link.

## Build from source

```bash
git clone https://github.com/Noah7601/theodrop.git
cd theodrop
dotnet run --project src/Filee.App   # .NET 10 SDK
dotnet test
```

Internal project names stay `Filee` so changes from the original project can still be merged.
[Architecture](docs/ARCHITECTURE.md) · [Adding a converter](docs/ADDING-A-CONVERTER.md)

## License and credits

theOdrop is a fork of [Filee](https://github.com/KnifeLemon/Filee) by Myeonggu Kim (KnifeLemon) and contributors,
which provides the conversion engine. theOdrop adds its own design, icon and macOS/Windows packaging.

MIT license, © 2026 Myeonggu Kim (KnifeLemon), Filee contributors and theOdrop contributors. See [LICENSE](LICENSE)
and [NOTICE.md](NOTICE.md). Bundled third-party components keep their own licenses:
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
