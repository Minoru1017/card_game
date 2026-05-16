# 將 BATTLE_UI_COLOR_SPEC.md 匯出為可列印的 HTML（Mermaid 改表格，A4）
$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$mdPath = Join-Path $repoRoot "BATTLE_UI_COLOR_SPEC.md"
$htmlPath = Join-Path $repoRoot "Docs\BATTLE_UI_COLOR_SPEC.print.html"

if (-not (Test-Path $mdPath)) { throw "找不到: $mdPath" }
$docsDir = Split-Path $htmlPath -Parent
if (-not (Test-Path $docsDir)) { New-Item -ItemType Directory -Path $docsDir | Out-Null }

$md = Get-Content -Path $mdPath -Raw -Encoding UTF8

$chartBlock = @'
<div class="chart-replacement page-break-inside-avoid">
<p><strong>【列印用】§2.5 圖表 — 五類來源佔比</strong></p>
<table><tr><th>來源</th><th>%</th></tr>
<tr><td>DIABLO（書）</td><td>15</td></tr>
<tr><td>HEARTY（書）</td><td>9</td></tr>
<tr><td>DANGEROUS（書）</td><td>8</td></tr>
<tr><td>動物森友會 UI</td><td>38</td></tr>
<tr><td>原創／混合</td><td>30</td></tr></table>
<p><strong>【列印用】外部參考 70% · 原創 30%</strong></p>
<p><strong>【列印用】三大區塊</strong> — 配色書三本 32 · 動森 38 · 原創 30</p>
</motion>
'@ -replace '</motion>','</div>'

$md = [regex]::Replace($md, '(?s)```mermaid.*?```', $chartBlock)

function Convert-MdLineToHtml([string]$line) {
    if ($line -match '^\|') { return $null }
    if ($line -match '^---+\s*$') { return $null }
    if ($line -match '^#{6}\s+(.+)$') { return "<h6>$($Matches[1])</h6>" }
    if ($line -match '^#{5}\s+(.+)$') { return "<h5>$($Matches[1])</h5>" }
    if ($line -match '^#{4}\s+(.+)$') { return "<h4>$($Matches[1])</h4>" }
    if ($line -match '^#{3}\s+(.+)$') { return "<h3>$($Matches[1])</h3>" }
    if ($line -match '^#{2}\s+(.+)$') { return "<h2>$($Matches[1])</h2>" }
    if ($line -match '^#\s+(.+)$') { return "<h1>$($Matches[1])</h1>" }
    if ($line -match '^>\s*(.+)$') { return "<blockquote>$($Matches[1])</blockquote>" }
    if ([string]::IsNullOrWhiteSpace($line)) { return "" }
    $t = [System.Net.WebUtility]::HtmlEncode($line)
    $t = $t -replace '\*\*(.+?)\*\*', '<strong>$1</strong>'
    $t = $t -replace '`([^`]+)`', '<code>$1</code>'
    return "<p>$t</p>"
}

function Convert-MdTable($tableLines) {
    $rows = @()
    foreach ($ln in $tableLines) {
        $cells = ($ln.Trim('|') -split '\|') | ForEach-Object { $_.Trim() }
        $rows += ,$cells
    }
    if ($rows.Count -lt 2) { return "" }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<table class="md-table">')
    [void]$sb.AppendLine('<thead><tr>')
    foreach ($c in $rows[0]) {
        $hc = [System.Net.WebUtility]::HtmlEncode($c) -replace '\*\*(.+?)\*\*','<strong>$1</strong>'
        [void]$sb.AppendLine("<th>$hc</th>")
    }
    [void]$sb.AppendLine('</tr></thead><tbody>')
    $start = 1
    if ($rows[1] -join '' -match '^[-:|\s]+$') { $start = 2 }
    for ($i = $start; $i -lt $rows.Count; $i++) {
        [void]$sb.AppendLine('<tr>')
        foreach ($c in $rows[$i]) {
            $tc = [System.Net.WebUtility]::HtmlEncode($c) -replace '\*\*(.+?)\*\*','<strong>$1</strong>' -replace '`([^`]+)`','<code>$1</code>'
            [void]$sb.AppendLine("<td>$tc</td>")
        }
        [void]$sb.AppendLine('</tr>')
    }
    [void]$sb.AppendLine('</tbody></table>')
    return $sb.ToString()
}

$body = New-Object System.Text.StringBuilder
$lines = $md -split "`r?`n"
$i = 0
while ($i -lt $lines.Count) {
    $line = $lines[$i]
    if ($line -match '^\|') {
        $tbl = @()
        while ($i -lt $lines.Count -and $lines[$i] -match '^\|') {
            $tbl += $lines[$i]
            $i++
        }
        [void]$body.AppendLine((Convert-MdTable $tbl))
        continue
    }
    if ($line -match '^<div class="chart') {
        while ($i -lt $lines.Count -and $lines[$i] -notmatch '</div>') { $i++ }
        if ($i -lt $lines.Count) {
            [void]$body.AppendLine($chartBlock)
            $i++
        }
        continue
    }
    $html = Convert-MdLineToHtml $line
    if ($null -ne $html) { [void]$body.AppendLine($html) }
    $i++
}

$swatchCss = @'
.swatch { display:inline-block; width:12px; height:12px; border:1px solid #999; vertical-align:middle; margin-right:4px; }
'@

$html = @"
<!DOCTYPE html>
<html lang="zh-Hant">
<head>
<meta charset="utf-8"/>
<title>對戰場景 UI 配色規格 — 列印版 v1.2</title>
<style>
@page { size: A4; margin: 14mm 12mm; }
@media print {
  .no-print { display: none !important; }
  h2 { page-break-before: always; }
  h2:first-of-type { page-break-before: avoid; }
  table { page-break-inside: avoid; }
}
body { font-family: "Microsoft JhengHei UI", "Microsoft JhengHei", "PingFang TC", sans-serif; font-size: 9.5pt; line-height: 1.45; color: #222; max-width: 190mm; margin: 0 auto; padding: 8mm; }
.no-print { background: #f5e6c8; border: 1px solid #8b7355; padding: 10px 14px; margin-bottom: 16px; border-radius: 6px; }
h1 { font-size: 18pt; border-bottom: 2px solid #9a7a55; padding-bottom: 6px; }
h2 { font-size: 13pt; color: #5c4033; margin-top: 1.2em; }
h3 { font-size: 11pt; color: #493f3b; }
h4 { font-size: 10pt; }
code { font-family: Consolas, monospace; font-size: 8.5pt; background: #f5f0e8; padding: 0 3px; }
table.md-table { width: 100%; border-collapse: collapse; margin: 8px 0 12px; font-size: 8pt; }
table.md-table th, table.md-table td { border: 1px solid #bbb; padding: 3px 5px; text-align: left; vertical-align: top; }
table.md-table th { background: #e8dfd0; }
table.md-table tr:nth-child(even) td { background: #faf8f4; }
blockquote { margin: 8px 0; padding: 6px 12px; border-left: 3px solid #9a7a55; background: #faf6ef; font-size: 9pt; }
.chart-replacement { background: #f0ebe3; padding: 8px; margin: 10px 0; font-size: 9pt; }
.chart-replacement table { width: auto; min-width: 40%; }
p { margin: 0.35em 0; }
</style>
</head>
<body>
<div class="no-print">
<strong>列印說明</strong>：按 <kbd>Ctrl+P</kbd>（Mac：<kbd>⌘P</kbd>）→ 印表機或「另存 PDF」→ 建議 A4、邊界「預設」、勾選<strong>背景圖形</strong>（以印出表格底色）。列印後可關閉此頁。<br/>
來源檔：<code>BATTLE_UI_COLOR_SPEC.md</code> v1.2 · 產生時間：$(Get-Date -Format 'yyyy-MM-dd HH:mm')
</div>
$($body.ToString())
</body>
</html>
"@

[System.IO.File]::WriteAllText($htmlPath, $html, [System.Text.UTF8Encoding]::new($false))
Write-Host "OK: $htmlPath"
Write-Host "Open in browser, then Ctrl+P to print or Save as PDF."
