#!/usr/bin/env python3
"""Heuristic code quality scan — writes Docs/CODE_QUALITY_SCAN_REPORT.md."""
import re
import uuid
from collections import defaultdict
from datetime import date
from pathlib import Path

TIER = {5: "优", 4: "良", 3: "尚可", 2: "不太好", 1: "差"}
SCENE_FIND = re.compile(
    r"(?:GameObject\.Find\s*\(|Object\.Find(?:FirstObjectByType|ObjectsByType|ObjectOfType|ObjectsOfType)\s*\()"
)


def score_file(lines: int, scene_finds: int) -> int:
    s = 5
    if lines > 2000:
        s = 1
    elif lines > 1200:
        s = 2
    elif lines > 800:
        s = 3
    elif lines > 500:
        s = 4
    if scene_finds > 15:
        s = min(s, 2)
    elif scene_finds > 8:
        s = min(s, 3)
    return s


def scan(repo: Path) -> list[tuple]:
    scripts = repo / "Assets" / "Scripts"
    groups: dict[str, dict] = defaultdict(lambda: {"lines": 0, "find": 0, "files": []})
    for f in sorted(scripts.rglob("*.cs")):
        text = f.read_text(encoding="utf-8", errors="replace")
        lines = len(text.splitlines())
        scene_finds = len(SCENE_FIND.findall(text))
        rel = f.relative_to(repo).as_posix()
        base = f.stem.split(".")[0]
        g = groups[base]
        g["lines"] += lines
        g["find"] += scene_finds
        g["files"].append((rel, lines))
    rows = []
    for base, g in groups.items():
        s = score_file(g["lines"], g["find"])
        rows.append((s, g["lines"], g["find"], TIER[s], base, g["files"]))
    rows.sort(key=lambda x: (x[0], -x[1]))
    return rows


def write_report(repo: Path, rows: list[tuple]) -> None:
    out = repo / "Docs" / "CODE_QUALITY_SCAN_REPORT.md"
    lines_out: list[str] = [
        "# Card Game 程式碼品質分級掃描報告",
        "",
        "> 由 `python tools/code_quality_scan.py` 或 Unity 選單 "
        "`Tools/Code Quality/Generate Scan Report` 產生。",
        "> 同一類別的 `partial` 檔案會合併計算。",
        "",
        "## 分級標準",
        "",
        "| 等級 | 分數 | 主要條件 |",
        "|------|------|----------|",
        "| 优 | 5 | ≤500 行，場景 Find 少 |",
        "| 良 | 4 | ≤800 行 |",
        "| 尚可 | 3 | ≤1200 行 |",
        "| 不太好 | 2 | >1200 行，或場景 Find >15 |",
        "| 差 | 1 | >2000 行 |",
        "",
        f"## 摘要（{date.today().isoformat()}）",
        "",
    ]
    for score, label in [(1, "差"), (2, "不太好"), (3, "尚可"), (4, "良"), (5, "优")]:
        tier = [r for r in rows if r[0] == score]
        lines_out.append(f"### {label}（{len(tier)} 類）")
        lines_out.append("")
        if not tier:
            lines_out.append("_（無）_")
            lines_out.append("")
            continue
        lines_out.append("| 類別 | 總行數 | 場景 Find | 檔案數 |")
        lines_out.append("|------|--------|-----------|--------|")
        limit = 20 if score <= 2 else 12
        for s, total, finds, _t, base, files in tier[:limit]:
            lines_out.append(f"| `{base}` | {total} | {finds} | {len(files)} |")
        lines_out.append("")
    lines_out.extend(
        [
            "## 近期改善",
            "",
            "- **FightingBirdGameSceneController**：單檔 ~1669 行 → `partial` 8 檔（主檔 ~334 行）。",
            "- **SettingsSceneController**：`Awake` 先 `CacheUiRefs`，移除重複 `GameObject.Find`。",
            "",
            "## 建議下一輪",
            "",
            "1. `BackpackCardInspectPanel`、`MainPlotSceneController`：拆 partial。",
            "2. `DeckManager` / `BattleSimulationDebugUI`：分期重構，勿一次大改。",
            "",
        ]
    )
    out.write_text("\n".join(lines_out) + "\n", encoding="utf-8")


def write_meta(repo: Path, rel_cs: str) -> None:
    meta = repo / (rel_cs + ".meta")
    if meta.exists():
        return
    guid = uuid.uuid4().hex
    meta.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "MonoImporter:\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 2\n"
        "  defaultReferences: []\n"
        "  executionOrder: 0\n"
        "  icon: {instanceID: 0}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def main() -> int:
    repo = Path(__file__).resolve().parents[1]
    rows = scan(repo)
    write_report(repo, rows)
    partials = [
        "Assets/Scripts/FightingBirdGameSceneController.UiBuild.cs",
        "Assets/Scripts/FightingBirdGameSceneController.MatchFlow.cs",
        "Assets/Scripts/FightingBirdGameSceneController.Draft.cs",
        "Assets/Scripts/FightingBirdGameSceneController.Input.cs",
        "Assets/Scripts/FightingBirdGameSceneController.Visuals.cs",
        "Assets/Scripts/FightingBirdGameSceneController.Audio.cs",
        "Assets/Scripts/FightingBirdGameSceneController.UiHelpers.cs",
        "Assets/Editor/CodeQualityScanReportGenerator.cs",
    ]
    for p in partials:
        write_meta(repo, p)
    print("report written; meta ensured for new files")
    for r in rows:
        if r[4] in ("FightingBirdGameSceneController", "SettingsSceneController"):
            print(r)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
