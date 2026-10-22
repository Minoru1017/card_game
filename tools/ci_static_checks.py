#!/usr/bin/env python3
"""Read-only CI checks that do not require a Unity installation."""

from __future__ import annotations

import ast
import json
import re
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EXPECTED_UNITY_VERSION = "2023.1.22f1"
POINTER_RE = re.compile(
    rb"\Aversion https://git-lfs\.github\.com/spec/v1\n"
    rb"oid sha256:[0-9a-f]{64}\n"
    rb"size [0-9]+\n?\Z"
)


def error(message: str, errors: list[str]) -> None:
    errors.append(message)
    print(f"ERROR: {message}")


def run_git(*args: str, input_bytes: bytes | None = None) -> bytes:
    return subprocess.run(
        ["git", *args],
        cwd=ROOT,
        input=input_bytes,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=True,
    ).stdout


def check_project_files(errors: list[str]) -> None:
    version_file = ROOT / "ProjectSettings" / "ProjectVersion.txt"
    version_match = re.search(
        r"^m_EditorVersion:\s*(\S+)\s*$",
        version_file.read_text(encoding="utf-8"),
        re.MULTILINE,
    )
    actual_version = version_match.group(1) if version_match else None
    if actual_version != EXPECTED_UNITY_VERSION:
        error(
            f"Unity version is {actual_version!r}; expected {EXPECTED_UNITY_VERSION!r}",
            errors,
        )

    for relative in ("Packages/manifest.json", "Packages/packages-lock.json"):
        try:
            data = json.loads((ROOT / relative).read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            error(f"{relative} is not valid JSON: {exc}", errors)
            continue
        if not isinstance(data.get("dependencies"), dict):
            error(f"{relative} has no dependencies object", errors)

    scanner = ROOT / "tools" / "code_quality_scan.py"
    try:
        ast.parse(scanner.read_text(encoding="utf-8"), filename=str(scanner))
    except (OSError, SyntaxError) as exc:
        error(f"tools/code_quality_scan.py cannot be parsed: {exc}", errors)


def check_lfs_index(errors: list[str]) -> None:
    paths = [p for p in run_git("ls-files", "-z").split(b"\0") if p]
    attrs = run_git("check-attr", "-z", "--stdin", "filter", input_bytes=b"\0".join(paths) + b"\0")
    fields = attrs.split(b"\0")
    lfs_paths: list[bytes] = []
    for index in range(0, len(fields) - 2, 3):
        path, attribute, value = fields[index : index + 3]
        if attribute == b"filter" and value == b"lfs":
            lfs_paths.append(path)

    malformed: list[str] = []
    for path in lfs_paths:
        blob = run_git("show", ":" + path.decode("utf-8", errors="surrogateescape"))
        if not POINTER_RE.fullmatch(blob):
            malformed.append(path.decode("utf-8", errors="replace"))

    if malformed:
        preview = ", ".join(malformed[:10])
        suffix = "" if len(malformed) <= 10 else f" (+{len(malformed) - 10} more)"
        error(f"{len(malformed)} LFS index blobs are not pointers: {preview}{suffix}", errors)
    else:
        print(f"OK: {len(lfs_paths)} Git LFS index blobs are valid pointers")


def report_meta_warnings() -> None:
    assets = ROOT / "Assets"
    if not assets.is_dir():
        print("WARNING: Assets directory is missing")
        return

    tracked = {
        ROOT / item.decode("utf-8", errors="surrogateescape")
        for item in run_git("ls-files", "-z").split(b"\0")
        if item
    }
    missing_meta: list[Path] = []
    orphan_meta: list[Path] = []

    for path in tracked:
        if assets not in path.parents:
            continue
        if path.suffix == ".meta":
            target = path.with_suffix("")
            if target not in tracked and not target.is_dir():
                orphan_meta.append(path)
        elif path.with_name(path.name + ".meta") not in tracked:
            missing_meta.append(path)

    if missing_meta:
        print(f"WARNING: {len(missing_meta)} tracked Assets files have no tracked .meta")
    if orphan_meta:
        print(f"WARNING: {len(orphan_meta)} tracked .meta files have no tracked asset")
    if not missing_meta and not orphan_meta:
        print("OK: no tracked Unity .meta anomalies found")


def main() -> int:
    errors: list[str] = []
    try:
        check_project_files(errors)
        check_lfs_index(errors)
        report_meta_warnings()
    except (OSError, subprocess.CalledProcessError) as exc:
        error(f"static check could not complete: {exc}", errors)

    if errors:
        print(f"Static checks failed with {len(errors)} error(s).")
        return 1
    print("Static checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
