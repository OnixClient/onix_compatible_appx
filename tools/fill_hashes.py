#!/usr/bin/env python3
"""One-time pass: copy sha256 + xxh3_64 values from a hash table jsonl into versions.yaml.

Run from the repo root:
    python tools/fill_hashes.py <hashes.jsonl>
xxh3_64 values are written as 16 lowercase hex chars without the 0x prefix.

Every url in versionsv2.json must have a row in the table (matched by url, case-insensitive).
Primary urls get sha256:/xxh3_64: lines after the url line, xdelta urls get
xdelta_sha256:/xdelta_xxh3_64:, and mirror parts are rewritten to the mapping form
(- url: / sha256: / xxh3_64:). Idempotent: existing hash lines are replaced.
Repo-root dependency files (raw.githubusercontent urls) are hashed from disk.

This script is NOT part of CI; it runs once per hash-table refresh.
"""

import hashlib
import json
import re
import sys

import xxhash

SHA = re.compile(r"^[0-9a-f]{64}$")
XXH = re.compile(r"^[0-9a-f]{16}$")


def load_table(path):
    rows = {}
    for line in open(path, encoding="utf-8"):
        r = json.loads(line)
        rows[r["url"]] = r["hashes"]
    return rows


def lookup(table, url):
    if url in table:
        return table[url]
    for u, h in table.items():
        if u.lower() == url.lower():
            return h
    return None


def file_hashes(path):
    h = hashlib.sha256()
    x = xxhash.xxh3_64()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 22), b""):
            h.update(chunk)
            x.update(chunk)
    return {"sha256": h.hexdigest(), "xxh3_64": "%016x" % x.intdigest()}


def main():
    table_path = sys.argv[1] if len(sys.argv) > 1 else "hashes.jsonl"
    table = load_table(table_path)
    payload = json.load(open("versionsv2.json", encoding="utf-8"))

    urls = []
    for v in payload["versions"]:
        urls.append(v["url"]["url"])
        m = v.get("mirror_url")
        if m:
            mu = m["url"]
            if isinstance(mu, list):
                urls.extend(part["url"] if isinstance(part, dict) else part for part in mu)
            else:
                urls.append(mu)
        if v.get("xdelta_url"):
            urls.append(v["xdelta_url"]["url"])
    for p in payload["dependency_profiles"].values():
        for d in p:
            if d["url"]:
                urls.append(d["url"]["url"])

    hashes = {}
    for url in dict.fromkeys(urls):
        h = lookup(table, url)
        if h is None:
            fname = url.rsplit("/", 1)[-1]
            try:
                h = file_hashes(fname)
            except FileNotFoundError:
                print(f"keeping existing yaml hashes for {url} (not in table, no local file)")
                continue
        hashes[url] = h
    for url, h in hashes.items():
        if not SHA.match(h["sha256"]) or not XXH.match(h["xxh3_64"]):
            sys.exit(f"bad hash format for {url}: {h}")

    lines = open("versions.yaml", encoding="utf-8").read().splitlines()
    out = []
    i = 0
    n = 0
    while i < len(lines):
        line = lines[i]
        s = line.strip()

        if s == "mirror_urls:":
            out.append(line)
            i += 1
            while i < len(lines) and (lines[i].strip().startswith("- ") or re.match(r"^\s{8,}(sha256|xxh3_64):", lines[i])):
                ms = lines[i].strip()
                indent = lines[i][: len(lines[i]) - len(lines[i].lstrip())]
                m = re.match(r"^- (?:url: )?(\S+)$", ms)
                if m:
                    url = m.group(1)
                    h = hashes.get(url)
                    if h:
                        out.append(f"{indent}- url: {url}")
                        out.append(f"{indent}  sha256: {h['sha256']}")
                        out.append(f"{indent}  xxh3_64: {h['xxh3_64']}")
                        n += 1
                        i += 1
                        while i < len(lines) and re.match(r"^\s{8,}(sha256|xxh3_64):", lines[i]):
                            i += 1
                        continue
                out.append(lines[i])
                i += 1
            continue

        m = re.match(r"^(\s+)(url|xdelta_url): (\S+)$", line)
        if m:
            indent, key, url = m.groups()
            out.append(line)
            i += 1
            h = hashes.get(url)
            if h:
                p = "xdelta_" if key == "xdelta_url" else ""
                out.append(f"{indent}{p}sha256: {h['sha256']}")
                out.append(f"{indent}{p}xxh3_64: {h['xxh3_64']}")
                n += 1
                while i < len(lines) and re.match(r"\s*(xdelta_)?(sha256|xxh3_64):", lines[i].strip()):
                    i += 1
                    continue
            elif i < len(lines) and re.match(r"\s*(xdelta_)?(sha256|xxh3_64):", lines[i].strip()):
                while i < len(lines) and re.match(r"\s*(xdelta_)?(sha256|xxh3_64):", lines[i].strip()):
                    out.append(lines[i])
                    i += 1
            continue

        out.append(line)
        i += 1

    open("versions.yaml", "w", encoding="utf-8", newline="\n").write("\n".join(out) + "\n")
    print(f"filled {n} hash lines")
    if n < len(hashes):
        sys.exit("count mismatch: some urls in versionsv2.json were not found in versions.yaml")


if __name__ == "__main__":
    main()
