#!/usr/bin/env python3
"""Downloads every engine in src/Filee.Engines/Infrastructure/engines.json and checks its size and SHA-256.

Prints one line per download (OK, or what differs) and exits 1 when any download is missing or different, so a
broken engine link fails CI instead of failing in the app. --only <text> checks only entries whose path contains it.
"""
import hashlib, json, sys, urllib.request

only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
data = json.load(open("src/Filee.Engines/Infrastructure/engines.json", encoding="utf-8-sig"))
entries = []

def walk(node, path):
    if isinstance(node, dict):
        if "url" in node and "sha256" in node:
            entries.append((path, node))
        for key, value in node.items():
            walk(value, f"{path}/{key}")
    elif isinstance(node, list):
        for index, value in enumerate(node):
            walk(value, f"{path}[{index}]")

walk(data, "")
failed = 0
for path, entry in entries:
    if only and only not in path:
        continue
    sha, size = hashlib.sha256(), 0
    try:
        request = urllib.request.Request(entry["url"], headers={"User-Agent": "Oneon-engine-check"})
        with urllib.request.urlopen(request, timeout=120) as response:
            while chunk := response.read(1 << 20):
                sha.update(chunk)
                size += len(chunk)
    except Exception as error:  # noqa: BLE001 - report every kind of failure the same way
        failed += 1
        print(f"FAIL {path}: {entry['url']} -> {error}")
        continue
    actual = sha.hexdigest().upper()
    problems = []
    if actual != entry["sha256"].upper():
        problems.append(f"sha256 {actual} (expected {entry['sha256'].upper()})")
    if entry.get("size") and size != entry["size"]:
        problems.append(f"size {size} (expected {entry['size']})")
    if problems:
        failed += 1
        print(f"FAIL {path}: {entry['url']} -> " + "; ".join(problems))
    else:
        print(f"OK   {path} ({size} bytes)")
    sys.stdout.flush()

print(f"{failed} of {len(entries)} engine downloads failed" if failed else f"All {len(entries)} engine downloads OK")
sys.exit(1 if failed else 0)
