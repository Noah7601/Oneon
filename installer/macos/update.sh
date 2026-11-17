#!/bin/bash
# Installs or updates Oneon on macOS in one step:
#   curl -fsSL https://raw.githubusercontent.com/Noah7601/Oneon/main/installer/macos/update.sh | bash
# Downloads the latest release's .dmg for this Mac (Apple Silicon or Intel), checks its SHA-256 against the release's
# checksum file, quits Oneon, moves the old app to the Trash, installs the new one in /Applications, clears the old
# Accessibility entry (an ad-hoc signed update is a new app to macOS) and opens Oneon again.
# --force reinstalls the latest version even when it is already installed.
set -euo pipefail

repo="Noah7601/Oneon"
app="/Applications/Oneon.app"
force=0
[ "${1:-}" = "--force" ] && force=1

say() { printf '\033[1m%s\033[0m\n' "$*"; }

[ "$(uname -s)" = "Darwin" ] || { echo "Oneon update.sh is for macOS." >&2; exit 1; }
arch="x64"; [ "$(uname -m)" = "arm64" ] && arch="arm64"

say "Checking the latest Oneon release… / 최신 버전 확인 중…"
tag="$(curl -fsSLI -o /dev/null -w '%{url_effective}' "https://github.com/$repo/releases/latest" | sed 's#.*/tag/##')"
case "$tag" in v[0-9]*) ;; *) echo "Could not find the latest release (got: $tag)." >&2; exit 1 ;; esac
version="${tag#v}"
installed="$(defaults read "$app/Contents/Info.plist" CFBundleShortVersionString 2>/dev/null || echo "")"
if [ "$installed" = "$version" ] && [ "$force" = 0 ]; then
  say "Oneon $version is already installed. / 이미 최신 버전($version)이에요."
  exit 0
fi

work="$(mktemp -d)"
trap 'hdiutil detach -quiet "$work/mnt" 2>/dev/null || true; rm -rf "$work"' EXIT
name="Oneon-$version-osx-$arch.dmg"
base="https://github.com/$repo/releases/download/$tag"

say "Downloading ${name}… / 내려받는 중…"
curl -fL --progress-bar -o "$work/$name" "$base/$name"
curl -fsSL -o "$work/sums.txt" "$base/Oneon-$version-SHA256SUMS.txt"
# The checksum file is written on Windows (CRLF line ends).
expected="$(awk -v f="$name" '{ sub(/\r$/, "") } $2 == f { print tolower($1) }' "$work/sums.txt")"
actual="$(shasum -a 256 "$work/$name" | awk '{ print tolower($1) }')"
if [ -z "$expected" ] || [ "$expected" != "$actual" ]; then
  echo "Checksum mismatch for $name: the download is not the released file. Nothing was changed." >&2
  exit 1
fi

say "Installing Oneon ${version}… / 설치 중…"
# Oneon keeps running in the menu bar when asked to quit by a signal, so a running copy that is still there after a
# moment is stopped for good: an old build left running beside the new one would keep writing its own data folder.
for running in "odrop.app/Contents/MacOS/Filee" "Oneon.app/Contents/MacOS/Filee"; do
  pkill -f "$running" 2>/dev/null || true
done
sleep 2
for running in "odrop.app/Contents/MacOS/Filee" "Oneon.app/Contents/MacOS/Filee"; do
  if pgrep -f "$running" >/dev/null 2>&1; then pkill -9 -f "$running" 2>/dev/null || true; fi
done
sleep 1
for folder in /Applications "$HOME/Applications"; do
  for old in Oneon theOdrop TheoDrop; do
    if [ -d "$folder/$old.app" ]; then
      mv "$folder/$old.app" "$HOME/.Trash/$old-$(date +%Y%m%d-%H%M%S).app"
    fi
  done
done
mkdir -p "$work/mnt"
hdiutil attach -nobrowse -quiet -mountpoint "$work/mnt" "$work/$name"
ditto "$work/mnt/Oneon.app" "$app"
hdiutil detach -quiet "$work/mnt"

# An ad-hoc signed build is a new identity to macOS, so the old Accessibility entry no longer applies and has to go.
# A build signed with Oneon's own certificate stays the same app: its permission carries over and is left alone.
adhoc=0
if codesign -dv "$app" 2>&1 | grep -q "Signature=adhoc"; then
  adhoc=1
  tccutil reset Accessibility com.theo.drop >/dev/null 2>&1 || true
fi
open "$app"

say "Done: Oneon $(defaults read "$app/Contents/Info.plist" CFBundleShortVersionString) / 완료"
if [ "$adhoc" = 0 ]; then exit 0; fi
echo "Allow Oneon again: System Settings > Privacy & Security > Accessibility (Device control & data access) > +"
echo "Oneon을 다시 허용해 주세요: 시스템 설정 > 개인정보 보호 및 보안 > 손쉬운 사용(기기 제어 및 데이터 접근) > +"
open "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility" 2>/dev/null || true
