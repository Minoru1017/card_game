# 背包卡牌詳情 UI 配色規格（Buildbeck Inspect）

| 項目 | 內容 |
|------|------|
| **狀態** | v2 整合紙面（程式 token） |
| **場景** | `Buildbeck` 背包模式、`BackpackCardInspectPanel` 浮動詳情 |
| **世界觀** | 館藏／圖鑑：一張暖色紙上閱讀，天藍只點綴立繪井 |
| **方案** | **動森紙面（主）**＋巧克力字＋鈴錢金主標；冷灰／深灰藍大色塊**停用** |
| **關聯文件** | [`BATTLE_UI_COLOR_SPEC.md`](BATTLE_UI_COLOR_SPEC.md)（對照用，勿混用為背包主色） |
| **程式** | `BackpackInspectUiColors.cs`、`BackpackInspectVisualStyle.cs`、`BackpackCardInspectPanel.cs` |

---

## 1. 整合原則（v2）

| 原則 | 說明 |
|------|------|
| **一張紙** | 全頁與右欄主底皆 `PAGE_PAPER`，不再天藍底＋亞麻半透明疊加 |
| **凹區用暖色** | 數值列、戰技、熟練度用 `PAGE_PAPER_INSET` / `PAGE_PAPER_MUTED`，不用冷灰 |
| **天藍只留左井** | `ART_WELL_WASH` 僅立繪區淡洗，呼應館藏但不搶右欄 |
| **字色收斂** | 正文 `INK`；副標 `INK_SOFT`；主標 `MAIN_TITLE`；戰技欄不再白字＋深藍底 |
| **薄荷停用** | `MINT_LABEL` 改指向 `INK_SOFT`（戰技副標不再薄荷綠） |

---

## 2. Design Tokens（`BackpackInspectUiColors`）

### 2.1 紙面與立繪

| 代號 | Hex | 用途 |
|------|-----|------|
| **PAGE_PAPER** | `#FFF8E7` | 全頁面板、資訊區、數值晶片 |
| **PAGE_PAPER_INSET** | `#F5E6C8` | 數值列底、戰技區、凹層 |
| **PAGE_PAPER_MUTED** | `#E8DCC8` | 熟練度條底、分頁未選 |
| **PAGE_DIVIDER** | `#E0D4C4` | 分隔、返回鈕、分頁邊界 |
| **ART_WELL_WASH** | `#C5DCE8` @ **42%** | 立繪井淡天藍 |
| **ART_FRAME** | `#A8B5A8` | 立繪框（灰綠褐，貼紙面） |
| **DIM** | `#4A4038` @ **45%** | 遮罩 |

### 2.2 文字

| 代號 | Hex | 用途 |
|------|-----|------|
| **INK** | `#5C4033` | 內文、戰技正文、熟練度、按鈕字 |
| **INK_SOFT** | `#493F3B` | 副標 Rich Text、英文副標 |
| **INK_MUTED** | `#6B5F58` | 類型、提示、熟練度狀態 |
| **MAIN_TITLE** | `#F8D878` | 卡名 |

### 2.3 熟練度（暖紙上）

| 代號 | Hex | 用途 |
|------|-----|------|
| **PROFICIENCY_BG** | `#E8DCC8` | 條底 |
| **PROFICIENCY_TRACK** | `INK` @ **14%** | 軌道 |
| **PROFICIENCY_FILL** | `#D4A04A` | 進度（UR 金琥珀） |
| **PROFICIENCY_LABEL** | `INK` | 標題 |
| **PROFICIENCY_STATUS** | `INK_MUTED` | `C · 完整` |

### 2.4 戰技階段色（略降飽和，紙上可讀）

| 階段 | Hex |
|------|-----|
| A | `#6A9A82` |
| B | `#6A8FA8` |
| C | `#C49A4A` |

### 2.5 稀有度

| 稀有度 | Hex |
|--------|-----|
| N | `#8A939C` |
| R | `#6FA878` |
| SR | `#5A8FB8` |
| SSR | `#9A7AB8` |
| UR | `#D4A04A` |

---

## 3. 元件對照

| 元件 | 色票 |
|------|------|
| `Panel` / `InfoRegion` | `PAGE_PAPER` |
| `ArtWell` | `ART_WELL_WASH` |
| `ArtFrame` | `ART_FRAME` |
| `BackButton` | `PAGE_DIVIDER` / `INK` |
| `Title` | `MAIN_TITLE` |
| `Subtitle`（Rich） | `INK_SOFT` |
| `StatsStrip` / `StatChip` | `PAGE_PAPER_INSET` / `PAGE_PAPER` |
| `MasteryBar` | `PROFICIENCY_*`（暖紙系） |
| `SkillBg` / `Skill` | `PAGE_PAPER_INSET` / `INK` |
| `StageTab` | `PAGE_PAPER` / `PAGE_PAPER_MUTED` |

---

## 4. 字型（`BackpackInspectVisualStyle.Typography`）

| 層級 | 字級 | 用途 |
|------|------|------|
| 主標 | **68** | 卡名 |
| 副標 | **40** | 戰技名（Rich Text） |
| 內文 | **34** | 類型、數值晶片、戰技正文 |
| 提示 | **26** | 頁碼、返回、底部提示 |

---

## 5. 變更紀錄

| 日期 | 說明 |
|------|------|
| 2026-05-20 | 初版：背包詳情分冊 |
| 2026-05-20 | v2 **配色整合**：暖紙單面、冷灰數值列／深藍戰技欄移除；`UiBuildGeneration` **17** |
