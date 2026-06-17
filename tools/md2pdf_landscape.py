# -*- coding: utf-8 -*-
import os, re, sys, markdown

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src = sys.argv[1]
out_html = sys.argv[2]
title = sys.argv[3] if len(sys.argv) > 3 else "遊戲企劃書"

with open(os.path.join(ROOT, src), encoding="utf-8") as f:
    text = f.read()

body = markdown.markdown(
    text,
    extensions=["tables", "fenced_code", "codehilite", "toc", "sane_lists", "md_in_html"],
    extension_configs={"codehilite": {"guess_lang": False, "noclasses": True}},
)

def to_file_url(path):
    path = os.path.normpath(path)
    return "file:///" + path.replace("\\", "/")

# 將 <img src="Docs/..."> 轉成絕對 file://，Edge 列印才讀得到
def fix_img_src(html):
    def repl(m):
        rel = m.group(1)
        abs_path = os.path.join(ROOT, rel.replace("/", os.sep))
        return f'src="{to_file_url(abs_path)}"'
    return re.sub(r'src="([^"]+)"', repl, html)

body = fix_img_src(body)

css = """
@page { size: A4 landscape; margin: 11mm 13mm; }
* { box-sizing: border-box; }
body {
  font-family: "Microsoft JhengHei","Microsoft YaHei","Segoe UI","PingFang TC",sans-serif;
  font-size: 13px; line-height: 1.62; color: #24292f; margin: 0; padding: 0 2px;
}
h1,h2,h3 { line-height: 1.25; margin: .85em 0 .4em; font-weight: 700; page-break-after: avoid; }
h1 { font-size: 1.7em; border-bottom: 2px solid #d0d7de; padding-bottom: .22em; }
h2 { font-size: 1.2em; color: #3a434d; border-bottom: 1px solid #e2e8ef; padding-bottom: .15em; margin-top: .65em; }
p { margin: .4em 0; }
blockquote { margin: .5em 0; padding: .22em .85em; color: #475059;
  border-left: .28em solid #c8d1da; background: #f6f8fa; font-size: .92em; }
table { border-collapse: collapse; width: 100%; margin: .55em 0; font-size: .86em; page-break-inside: avoid; }
th,td { border: 1px solid #d0d7de; padding: 4px 8px; text-align: left; vertical-align: top; }
th { background: #f6f8fa; font-weight: 700; }
tr:nth-child(even) td { background: #fafbfc; }
strong { color: #1f2328; }
.cover { height: 165mm; display: flex; flex-direction: column; justify-content: center;
  align-items: center; text-align: center; page-break-after: always; }
.cover-kicker { letter-spacing: .26em; font-size: .8em; color: #6e7781; margin-bottom: 12px; }
.cover-title { font-size: 3em; border: none; margin: 0; }
.cover-sub { font-size: 1.22em; color: #424a53; margin-top: 8px; max-width: 90%; }
.cover-meta { margin-top: 24px; color: #6e7781; font-size: .92em; }
.page-break { page-break-before: always; }
.source-note { font-size: .76em; color: #8a929b; margin-top: .5em; }
.lead { background: linear-gradient(135deg,#eef4ff,#f3f0ff); border: 1px solid #d6e0f5;
  border-radius: 11px; padding: 14px 20px; margin: 2px 0 6px; font-size: 1.05em;
  line-height: 1.72; text-align: center; page-break-inside: avoid; }
.fantasy-box { border: 1px solid #c8d8f0; border-radius: 10px; background: #f8fbff;
  padding: 10px 14px; margin: 8px 0; page-break-inside: avoid; }
.fantasy-h { font-weight: 800; color: #4a7fd4; font-size: .95em; margin-bottom: 4px; }
.fantasy-body { font-size: .92em; line-height: 1.65; }
.fantasy-tag { display: inline-block; margin-top: 6px; font-size: .82em; color: #6e7781;
  background: #eef2f7; padding: 2px 8px; border-radius: 4px; }
.pillars { display: flex; flex-wrap: nowrap; gap: 8px; margin: 8px 0; }
.pillar { flex: 1 1 25%; border: 1px solid #dce3ea; border-radius: 9px;
  padding: 8px 10px; background: #fbfcfe; page-break-inside: avoid; }
.pillar-n { font-size: 1.15em; font-weight: 800; color: #5b8def; }
.pillar-t { font-size: .98em; font-weight: 700; margin: 1px 0 3px; }
.pillar-d { font-size: .8em; color: #3a434d; line-height: 1.5; }
.loop { display: flex; flex-wrap: nowrap; align-items: stretch; justify-content: center;
  gap: 3px; margin: 8px 0 3px; page-break-inside: avoid; }
.loop-step { flex: 1 1 auto; border: 1px solid #cdd7e2; border-radius: 9px;
  padding: 8px 6px; text-align: center; background: #f4f8ff; min-width: 0; }
.loop-step-highlight { background: #e8f4ff; border-color: #9ec5f5; }
.loop-step-reward { background: #fff4e6; border-color: #f0d3a8; }
.loop-roadmap .loop-step { background: #eefaf1; border-color: #bfe4c9; }
.loop-h { font-weight: 700; font-size: .88em; }
.loop-s { font-size: .74em; color: #5a636d; margin-top: 2px; }
.arrow { align-self: center; font-size: 1.2em; color: #9aa4af; flex: 0 0 auto; }
.arrow-back { color: #e2a23b; }
.loop-caption { text-align: center; font-size: .8em; color: #6e7781; margin-bottom: 3px; }
.timeline { display: flex; flex-wrap: wrap; gap: 5px; margin: 6px 0; }
.tl { flex: 1 1 calc(50% - 5px); border: 1px solid #e2e8ef; border-radius: 7px;
  padding: 6px 9px; background: #fbfcfe; font-size: .84em; page-break-inside: avoid; }
.tl-n { display: inline-block; width: 18px; height: 18px; line-height: 18px; text-align: center;
  background: #5b8def; color: #fff; border-radius: 50%; font-size: .75em; margin-right: 3px; font-weight: 700; }
.usp { display: flex; flex-direction: row; gap: 7px; margin: 8px 0; }
.usp-item { flex: 1 1 33%; border: 1px solid #dce3ea; border-left: 4px solid #5b8def;
  border-radius: 7px; padding: 7px 9px; background: #fbfcfe; font-size: .84em; page-break-inside: avoid; }
.usp-h { font-weight: 800; color: #5b8def; margin-bottom: 2px; }
.evidence-grid { display: flex; gap: 10px; margin: 8px 0; align-items: stretch; }
.shot { flex: 1 1 33%; border: 1px solid #b8c4d0; border-radius: 9px; overflow: hidden;
  background: #fff; page-break-inside: avoid; display: flex; flex-direction: column; }
.shot-img { width: 100%; height: auto; max-height: 130px; object-fit: contain; object-position: center top;
  background: #1a1a1a; display: block; border-bottom: 1px solid #e2e8ef; }
.shot-ph { height: 72px; display: flex; align-items: center; justify-content: center;
  font-size: .95em; color: #6e7781; background: #e8ecf0; border-bottom: 1px dashed #b8c4d0; }
.shot-cap { padding: 6px 8px; font-size: .78em; line-height: 1.45; color: #3a434d; flex: 1; }
.player-callout { border: 1px solid #bfe4c9; border-radius: 10px; background: #f0faf3;
  padding: 10px 14px; margin: 8px 0; font-size: .92em; page-break-inside: avoid; }
.player-callout-h { font-weight: 800; color: #2d7a4a; margin-bottom: 4px; }
.player-steps { margin: 4px 0 0; padding-left: 1.2em; }
.player-steps li { margin: 3px 0; }
"""

html = f"""<!DOCTYPE html><html lang="zh-Hant"><head><meta charset="utf-8">
<title>{title}</title><style>{css}</style></head><body>{body}</body></html>"""

with open(out_html, "w", encoding="utf-8") as f:
    f.write(html)
print("HTML:", out_html)
