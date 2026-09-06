#!/usr/bin/env python3
"""One-time migration: draft versions.yaml from versions.txt/sdkversions.txt + GitHub releases API.

Run from the repo root:
    python tools/bootstrap_versions_yaml.py

Never derived from name templates:
  - primary url comes verbatim from versions.txt (asset names in releases are not reliable:
    1.20.32's asset is lowercase .appx, zip-era primaries are CDN links absent from releases)
  - mirror_urls / xdelta_url come verbatim from the release asset list, by shape (.zip.NNN / .xdelta3)

This script is NOT part of CI; it needs network access and runs once.
"""

import argparse
import json
import re
import sys
import urllib.request

REPO = "OnixClient/onix_compatible_appx"
RAW_BASE = f"https://raw.githubusercontent.com/{REPO}/main"
API = f"https://api.github.com/repos/{REPO}/releases"


def read_groups(path):
    lines = [l.rstrip("\r\n") for l in open(path, encoding="utf-8")]
    return [(lines[i], lines[i + 1], lines[i + 2]) for i in range(0, len(lines), 3)]


def fetch_releases():
    out = {}
    page = 1
    while True:
        req = urllib.request.Request(f"{API}?per_page=100&page={page}", headers={"User-Agent": "bootstrap"})
        rels = json.load(urllib.request.urlopen(req, timeout=30))
        if not rels:
            break
        for r in rels:
            out[r["tag_name"]] = [a["name"] for a in r["assets"]]
        page += 1
        if page > 10:
            break
    return out


def part_number(name):
    m = re.search(r"\.zip\.(\d+)$", name)
    return int(m.group(1)) if m else -1


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="versions.yaml")
    args = ap.parse_args()

    versions = read_groups("versions.txt")
    sdk = {v for v, _, _ in read_groups("sdkversions.txt")}
    releases = fetch_releases()
    print(f"loaded {len(versions)} versions, {len(sdk)} sdk versions, {len(releases)} releases")

    yaml = [
        "# versions.yaml is the single source of truth. It generates: versions.txt, sdkversions.txt,",
        "# profiles.txt, versions.json, sdkversions.json, versionsv2.json. Edit this file and push;",
        "# do NOT hand-edit any of the generated files.",
        f"raw_base: {RAW_BASE}",
        "",
        "versions:",
    ]

    for i, (ver, pkg, url) in enumerate(versions):
        assets = releases.get(ver)
        if assets is None:
            print(f"WARNING: no GitHub release for {ver}")
            assets = []
        yaml.append(f"  - version: {ver}")
        yaml.append(f"    package_version: {pkg}")
        if ver in sdk:
            yaml.append(f"    sdk: true")
        yaml.append(f"    url: {url}")

        zips = sorted((a for a in assets if re.search(r"\.zip\.\d+$", a)), key=part_number)
        if zips:
            yaml.append("    mirror_urls:")
            for a in zips:
                yaml.append(f"      - https://github.com/{REPO}/releases/download/{ver}/{a}")

        deltas = [a for a in assets if a.endswith(".xdelta3")]
        if len(deltas) > 1:
            pick = next((a for a in deltas if a.startswith(ver)), deltas[0])
            print(f"WARNING: {ver} has {len(deltas)} xdelta assets {[d for d in deltas]}; picking {pick}")
            deltas = [pick]
        if len(deltas) == 1:
            yaml.append(f"    xdelta_url: https://github.com/{REPO}/releases/download/{ver}/{deltas[0]}")

        if not zips:
            # sanity: the published url should match this release's full asset (name may differ in case)
            full = [a for a in assets if a.lower() in (f"{ver}.appx", f"{ver}.msixvc")]
            if len(full) == 1:
                expect = f"https://github.com/{REPO}/releases/download/{ver}/{full[0]}"
                if url != expect and url.lower() != expect.lower():
                    print(f"WARNING: {ver} url in versions.txt differs from release asset:")
                    print(f"         txt: {url}")
                    print(f"         api: {expect}")
            elif len(full) == 0:
                print(f"WARNING: {ver} has no full asset (.Appx/.msixvc) in its release and no zip mirrors")

        if i < len(versions) - 1:
            pass  # no separator needed; comments optional

    with open(args.out, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(yaml) + "\n")
    print(f"wrote {args.out} ({len(versions)} version entries)")


if __name__ == "__main__":
    sys.exit(main())
