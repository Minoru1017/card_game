# Cursor 自訂 Skills 說明書

> 本專案 Skills 目錄：`.cursor/skills/`  
> 最後更新：2026-06-21

本文件說明 card-game 專案內**自訂 Agent Skills** 的用途、Agent 自動流程、適用時機與注意事項。  
Skills 不會在每次對話自動載入；需由你或 Agent **明確選用**後才會依流程執行。

---

## 一、通用使用方式

| 方式 | 操作 |
|------|------|
| Cursor UI | Agent 聊天 **+** → **Skills** → 選擇 skill 名稱 |
| 斜線指令 | 在聊天輸入 **`/skill-name`**（例如 `/major-bug-resolution`） |
| 自然語言 | 描述任務並提到關鍵字（見各 skill「適用時機」）；Agent 應選對應 skill |

**共通特性**

- 所有本專案 skill 皆設 `disable-model-invocation: true`：**不會**在背景自動套用，避免誤觸重型流程。
- Agent 被 skill 引導後，應**實際執行**腳本／改檔／驗收，而非只口頭描述步驟。
- 詳細參數與範例在各 skill 資料夾內的 `reference.md` 或 `checklist-template.md`。

---

## 二、Skill 一覽

| Skill | 指令 | 一句話 |
|-------|------|--------|
| [major-bug-resolution](#三major-bug-resolution) | `/major-bug-resolution` | 重大 bug：驗收先行 → 隔離 → Bug 場景 → 整合 |
| [code-quality-scan](#四code-quality-scan) | `/code-quality-scan` | 靜態掃描 C#，產生分級報告與 Watch list |
| [partial-refactor](#五partial-refactor) | `/partial-refactor` | 超大 `.cs` 拆成 partial，行為不變 |
| [planning-doc-sync](#六planning-doc-sync) | `/planning-doc-sync` | 企劃／規格／索引與程式同步 |
| [unity-library-refresh](#七unity-library-refresh) | `/unity-library-refresh` | 刷新 Audio／Font／Sprite／CardArt Library |
| [bird-duel-rhythm](#八bird-duel-rhythm) | `/bird-duel-rhythm` | 鬥鳥 BGM 節奏、BPM、難度資料 |

---

## 三、major-bug-resolution

**功能**：處理跨 **記憶體 × CSV 存檔 × UI 綁定 × Profile 摘要** 的重大 bug，避免「改 A 壞 B、驗收後又回溯」。

**參考案例**：牌組名稱（`PlayerDeckSlotNameStorage.cs`、`Docs/DECK_SLOT_NAME_BUG_CHECKLIST.md`）。

### Agent 自動流程（六階段）

```
Phase 0  撰寫 Docs/{功能}_BUG_CHECKLIST.md（驗收標準 + 合法流程 + 待解清單）
    ↓
Phase 1  建立單一 canonical 模組；主檔僅 thin wrapper
    ↓
Phase 2  Bug handling scenarios 場景 + 手動測試 UI
    ↓
Phase 3  逐項修復 + Save/Load 路徑稽核（display vs persist）
    ↓
Phase 4  EditMode 測試 + Bug 場景 + 主場景驗收
    ↓
Phase 5  驗收通過後才整合進 PlayerData / DeckManager 等主檔
    ↓
Phase 6  使用者「驗收成功」→ 勾選 [x]、可選 commit
```

**Phase 3 必做稽核**：追蹤所有 `LoadPlayerData`、`SavePlayerData`、`RefreshProfileFromRuntime`、`SyncProfile*`、`cachedOtherSlotRows` 路徑。

| 路徑類型 | 規則 |
|----------|------|
| 使用者按「確定」持久化 | memory → CSV → profile 摘要 |
| 僅開面板顯示 | 只讀 runtime；**不可**用會觸發 Load 的 full profile Save |
| CSV merge | 以 runtime 重寫 domain 列；merge 內 **禁止** `LoadPlayerData()` |

### 適合使用時機

- 改名／存檔後資料回溯、跨玩家槽污染、存檔亂碼
- 開啟某 UI（如玩家資訊）後主畫面狀態被洗掉
- 需在 **Bug handling scenarios** 隔離驗收後再併入主遊戲
- 使用者提到：重大 bug、驗收清單、隔離、存檔污染

### 不適合

- 單行 typo、null ref、純排版 — 直接改即可

### 要注意的點

- **Phase 0 未完成不要寫程式** — 沒有驗收文件就會再度漏測
- **Bug 場景未過不要整合主檔** — 避免半成品進 Buildbeck
- **勿把 `[x]` 當裝飾** — 必須複測後才勾
- **勿混大 refactor 與 bug fix** — 同一 PR 難 review、難 bisect
- 附檔：`.cursor/skills/major-bug-resolution/checklist-template.md`、`unity-patterns.md`

---

## 四、code-quality-scan

**功能**：對 Unity／反編譯 C# 專案跑 **CODE_QUALITY_SCAN**，輸出 CSV 指標與 Markdown 分級報告（Excellent → Bad）。

### Agent 自動流程

1. 解析專案根目錄（workspace 或使用者指定路徑）
2. 執行 `scripts/Run-CodeQualityScan.ps1`
3. 讀取 `.code-quality-scan/code_scan_ratings.csv` 與 `Docs/CODE_QUALITY_SCAN_REPORT.md`
4. 摘要：各 tier 數量、**Watch list**（Poor/Bad）、前 8 大檔案

### 適合使用時機

- 新功能合併前想看技術債
- 決定哪些 class 要 `/partial-refactor`
- 使用者說：程式碼品質、一鍵掃描、分級

### 要注意的點

- 需 **PowerShell**；掃 DLL 需 dnSpy（見 skill `reference.md`）
- 評分為 **heuristic**，非編譯器或測試結果
- 預設輸出在本專案 `Docs/CODE_QUALITY_SCAN_REPORT.md`，非 Desktop 舊版路徑

---

## 五、partial-refactor

**功能**：將 **800+ 行** MonoBehaviour 拆成 `partial class`，對齊本專案命名（`.UiBuild`、`.MatchFlow`、`.HarborTraining` 等），**行為不變**。

### Agent 自動流程

1. 鎖定目標 class（使用者指定或 scan Watch list）
2. 讀完整檔 → 提出 topic 拆分表（超大檔先徵求同意）
3. 主檔改 `partial` + MASTER INDEX；抽出 partial 並補齊 `using`
4. 編譯檢查（無 CS0246 等）
5. 建議再跑 `/code-quality-scan` 確認單檔 ≤800 行趨勢

可選： `scripts/Split-PartialClass.ps1` + plan.json（見 `reference.md`）。

### 適合使用時機

- 掃描結果 Poor/Bad
- 上帝類難維護但**尚不宜改邏輯**
- 使用者說：partial 拆分、上帝類

### 要注意的點

- **refactor-only PR** — 不與玩法規則修改混在同一 PR
- **共用欄位留主檔** — partial 之間不複製 field
- `DeckManager`、`BattleSimulationManager` 等 3000+ 行：**先 plan**，需使用者明確同意再拆
- 拆完若 public API 變了 → 失敗

---

## 六、planning-doc-sync

**功能**：功能／規則變更後，同步 **企劃書、GDD、索引、Open Items**，維持「定案 ↔ 程式」一致。

### Agent 自動流程

1. 從 git diff 或使用者描述判斷 **變更域**
2. 查 `reference.md` 的「Change → documents」表
3. **先改主規格**（GDD／規則），再改索引
4. 更新 `PLANNING_DOCS_INDEX.md` **§九修訂紀錄**
5. 關閉或新增 `PLANNING_OPEN_ITEMS.md` 條目
6. 回報：改了哪些檔、剩哪些 TBD

### 適合使用時機

- 實作完成後要更新企劃
- 鬥鳥／港灣／存檔／新場景等規格定案
- 使用者說：同步企劃、定案、更新索引

### 要注意的點

- **勿擅自改** `LEVEL_DESIGN_GDD.md` 進度語意（Clear／入門畢業／港灣畢業證）除非 intentional
- **勿為實作任務改市場分析 md**
- **勿無請求新增頂層 md** — 優先擴充既有 GDD
- 詳細路由表在 `.cursor/skills/planning-doc-sync/reference.md`

---

## 七、unity-library-refresh

**功能**：新增或變更 BGM、SFX、字型、UI 圖、卡面圖後，刷新 `Assets/Resources/*Library.asset`，避免 runtime 缺引用。

### Agent 自動流程

1. 依 git diff 或描述選 **target**：`all` | `audio` | `ui-font` | `ui-sprite` | `card-art` | `fonts-full`
2. 優先執行 `scripts/Run-UnityLibraryRefresh.ps1`（Unity batchmode）
3. 失敗則列出 **Editor 選單路徑** 請使用者在 Unity 手動執行
4. 摘要：更新了哪些 `.asset`、Console 是否有 null 警告

### 適合使用時機

- pull 別人 art/audio commit 後
- 新增 `Assets/Music/`、卡面 PNG、Noto 字型
- Play Mode 出現 Library 缺 clip／sprite

### 要注意的點

- 需 **Windows + Unity Editor**（版本與專案一致）
- 卡面必走 **`card-art`**（Rescan + Library），不要只跑 `ui-sprite`
- 專案需有 `Assets/Editor/UnityLibraryRefreshBatch.cs`
- 可設環境變數 `UNITY_EDITOR_PATH`

---

## 八、bird-duel-rhythm

**功能**：鬥鳥小遊戲 **BGM 節拍同步**：BPM、downbeat、`BirdDuelRhythmSync.asset` 各 CD 難度係數；對齊企劃 §12.4.1。

### Agent 自動流程

依任務分支：

| 分支 | 流程 |
|------|------|
| **A 重測 BPM** | 確認 clip 路徑 → 請使用者在 Unity 跑 **Tools → Audio → Analyze…** → 驗 Console → 確認 asset 更新且難度係數未被 analyzer 洗掉 |
| **B 新 CD** | 註冊 catalog → library refresh → 新增 `CdEntry` → 量測或手調 → planning-doc-sync |
| **C 調難度** | 只改 `CdEntry` 的 mul 欄位，**不改** controller 內 Base 視窗常數 |
| **D 除錯 desync** | 跑 skill 內 checklist（cdId、offset、loop 長度、AudioLibrary） |

### 適合使用時機

- 新增／替換鬥鳥 BGM
- 節奏感不對、count-in 偏移、loop 接點錯
- 調整入門／陣營 CD 難度（資料面）

### 要注意的點

- **Analyzer 只更新 tempo** — 既有 difficulty mul 應保留（`EditorSetCdEntry` 行為）
- **節奏調參與玩法規則分 PR**
- 新音檔先 `/unity-library-refresh` `audio`
- 規格表變更需 `/planning-doc-sync`

---

## 九、Skills 組合建議

| 情境 | 建議順序 |
|------|----------|
| 技術債 → 拆分 | `/code-quality-scan` → `/partial-refactor` → 再 scan |
| 新 BGM → 鬥鳥 | `/unity-library-refresh` → `/bird-duel-rhythm` → `/planning-doc-sync` |
| 重大存檔 bug | `/major-bug-resolution`（全程）；完成後可 `/planning-doc-sync` 更新 checklist 連結 |
| 功能上線 | 實作 → `/planning-doc-sync` → 必要時 scan |

**避免同時觸發**

- `/partial-refactor` + `/major-bug-resolution` 在同一變更混用（一個要行為不變 refactor，一個要改行為修 bug）
- `/bird-duel-rhythm` BPM 重測 + 玩法規則修改同一 PR

---

## 十、檔案位置速查

```
.cursor/skills/
├── major-bug-resolution/
│   ├── SKILL.md
│   ├── checklist-template.md
│   └── unity-patterns.md
├── code-quality-scan/
│   ├── SKILL.md
│   ├── reference.md
│   └── scripts/Run-CodeQualityScan.ps1
├── partial-refactor/
│   ├── SKILL.md
│   ├── reference.md
│   └── scripts/Split-PartialClass.ps1
├── planning-doc-sync/
│   ├── SKILL.md
│   └── reference.md
├── unity-library-refresh/
│   ├── SKILL.md
│   ├── reference.md
│   └── scripts/Run-UnityLibraryRefresh.ps1
└── bird-duel-rhythm/
    ├── SKILL.md
    └── reference.md
```

---

## 十一、維護說明

- 新增 skill：在 `.cursor/skills/{name}/SKILL.md` 建立，並**更新本說明書 §二、§十**。
- 個人全域 skill：可複製資料夾到 `%USERPROFILE%\.cursor\skills\`（全 workspace 可用）。
- **勿**在 `~/.cursor/skills-cursor/` 新增 — 該目錄為 Cursor 內建保留。

若 skill 行為與本說明書不一致，以各 skill 目錄內 **`SKILL.md`** 為準，並應同步修訂本文件。
