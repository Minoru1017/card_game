# 企劃文件索引

> **狀態**：維護中（2026-05-30）  
> **用途**：專案內所有企劃／GDD／規格文件之**單一入口**；新成員或 Agent 請由此開始。  
> **總覽表**：[`PLANNING_MASTER_TABLE.md`](PLANNING_MASTER_TABLE.md)  
> **待定與未定義項**：[`PLANNING_OPEN_ITEMS.md`](PLANNING_OPEN_ITEMS.md)

---

## 建議閱讀順序（主線 1-1）

1. [`PLANNING_MASTER_TABLE.md`](PLANNING_MASTER_TABLE.md) — 各領域狀態與主文件對照  
2. [`STORY_PROGRESS_WORLDVIEW.md`](STORY_PROGRESS_WORLDVIEW.md) — 空間、敘事、三層命名  
3. [`LEVEL_DESIGN_GDD.md`](LEVEL_DESIGN_GDD.md) — 1-1 流程、獎勵、地圖解鎖（**進度語意以此為準**）  
4. [`TUTORIAL_PLOT_SCRIPT.md`](TUTORIAL_PLOT_SCRIPT.md) — Main Plot 台詞與步驟索引  
5. [`GAMEPLAY_AND_RULES.md`](GAMEPLAY_AND_RULES.md) — 對戰規則、卡牌表、Settings  
6. 實作對照：`Assets/Resources/StoryProgressNodeDatabase.json`、`Assets/Scripts/PROJECT_CODE_INDEX_v2.md`

---

## 一、入口與總覽

| 文件 | 說明 |
|------|------|
| [`README.md`](README.md) | 玩家向功能說明、執行環境 |
| [`PLANNING_MASTER_TABLE.md`](PLANNING_MASTER_TABLE.md) | **企劃總表**（領域 × 狀態 × 主文件） |
| [`PLANNING_OPEN_ITEMS.md`](PLANNING_OPEN_ITEMS.md) | **待定／未定義**集中清單 |
| [`ARCHITECTURE_OVERVIEW.md`](ARCHITECTURE_OVERVIEW.md) | 場景流程、存檔、DataManager（英文架構圖） |

---

## 二、主線、關卡、劇情

| 文件 | 說明 |
|------|------|
| [`STORY_PROGRESS_WORLDVIEW.md`](STORY_PROGRESS_WORLDVIEW.md) | 港灣／學院分層、文案圓法、常見誤解 |
| [`LEVEL_DESIGN_GDD.md`](LEVEL_DESIGN_GDD.md) | **關卡設計 GDD**（目前僅 1-1 港灣訓練場定案） |
| [`HARBOR_1-1_VS_TRAINING_GROUND_DIFFICULTY.md`](HARBOR_1-1_VS_TRAINING_GROUND_DIFFICULTY.md) | **1-1 實戰 vs Buildbeck 訓練場** 簡單／普通難度對照表 |
| [`HARBOR_COMBAT_COACH_GDD.md`](HARBOR_COMBAT_COACH_GDD.md) | **港灣實戰區戰術教練**（關鍵時刻提示；與入門 coach 分離） |
| [`Assets/Scripts/HARBOR_COMBAT_COACH_IMPLEMENTATION.md`](Assets/Scripts/HARBOR_COMBAT_COACH_IMPLEMENTATION.md) | 港灣教練**程式實作**（類別、API、資源路徑） |
| [`TUTORIAL_PLOT_SCRIPT.md`](TUTORIAL_PLOT_SCRIPT.md) | 新手教學劇本 steps（`MainPlotSceneController`） |
| `Assets/Resources/StoryProgressNodeDatabase.json` | 大地圖節點資料（`M-1-1`、`M-1-2` 等） |

---

## 三、玩法、卡牌、牌組

| 文件 | 說明 |
|------|------|
| [`GAMEPLAY_AND_RULES.md`](GAMEPLAY_AND_RULES.md) | 卡牌 CSV、組牌、對戰流程、天氣、Settings |
| [`CARD_PROFICIENCY_GDD.md`](CARD_PROFICIENCY_GDD.md) | 熟練度、A/B/C 解鎖、存檔欄位 |
| [`卡牌技能階段式揭露.md`](卡牌技能階段式揭露.md) | 單卡三階戰技文案範例 |
| [`DECK_SAVE_IMPLEMENTATION.md`](DECK_SAVE_IMPLEMENTATION.md) | 牌組槽、CSV 鍵、Buildbeck 索引對照 |

---

## 四、對戰、難度、AI、戰前預覽

| 文件 | 說明 |
|------|------|
| [`DIFFICULTY_AND_AI_DESIGN.md`](DIFFICULTY_AND_AI_DESIGN.md) | 難度五檔、設計指數、AI 摘要（報告用） |
| [`ENEMY_AI_DECISION_TREE.md`](ENEMY_AI_DECISION_TREE.md) | 敵方出牌決策樹細節 |
| [`BATTLE_PREVIEW_PUZZLE_INDEX.md`](BATTLE_PREVIEW_PUZZLE_INDEX.md) | 戰前預覽謎題 PZ01、PZ02 |
| [`BALANCE_AND_AI_BIBLIOGRAPHY.md`](BALANCE_AND_AI_BIBLIOGRAPHY.md) | 平衡／AI 外部文獻連結 |

---

## 五、UI／美術／特效規格

| 文件 | 說明 |
|------|------|
| [`BATTLE_UI_COLOR_SPEC.md`](BATTLE_UI_COLOR_SPEC.md) | 對戰 UI 色票與腳本修改索引 |
| [`BATTLE_FX_COLOR_SPEC.md`](BATTLE_FX_COLOR_SPEC.md) | 對戰特效色票 |
| [`BACKPACK_INSPECT_UI_COLOR_SPEC.md`](BACKPACK_INSPECT_UI_COLOR_SPEC.md) | 背包檢視 UI 色票 |
| [`FIELD_CARD_STATUS_INDEX.md`](FIELD_CARD_STATUS_INDEX.md) | 場上狀態圖示索引 |

---

## 六、市場與研究（策略參考，非實作規格）

| 文件 | 說明 |
|------|------|
| [`MARKET_ANALYSIS_SOURCES.md`](MARKET_ANALYSIS_SOURCES.md) | 竞品資料來源 |
| [`MARKET_ANALYSIS_FIVE_GAMES.md`](MARKET_ANALYSIS_FIVE_GAMES.md) | 五款竞品與熟練度策略建議 |

---

## 七、測試、優化、程式索引（關聯維護）

| 文件 | 說明 |
|------|------|
| [`P0_MANUAL_REGRESSION.md`](P0_MANUAL_REGRESSION.md) | P0 手動回歸清單 |
| [`OPTIMIZATION_CHECKLIST.md`](OPTIMIZATION_CHECKLIST.md) | 效能與維護待辦 |
| [`Assets/Scripts/PROJECT_CODE_INDEX_v2.md`](Assets/Scripts/PROJECT_CODE_INDEX_v2.md) | **程式模組索引**（Story、Harbor、Battle 錨點） |

---

## 八、依讀者角色快速導航

| 角色 | 優先閱讀 |
|------|----------|
| **企劃／文案** | 本索引 §二 → `STORY_PROGRESS_WORLDVIEW` → `LEVEL_DESIGN_GDD` → `TUTORIAL_PLOT_SCRIPT` → `PLANNING_OPEN_ITEMS` |
| **關卡／數值** | `LEVEL_DESIGN_GDD` → `DIFFICULTY_AND_AI_DESIGN` → `GAMEPLAY_AND_RULES` → `PLANNING_OPEN_ITEMS` §港灣 |
| **程式（主線）** | `LEVEL_DESIGN_GDD` §程式對照 → `PROJECT_CODE_INDEX_v2` → `TutorialProgressState` / `HarborTrainingProgressState` |
| **程式（對戰）** | `GAMEPLAY_AND_RULES` → `ENEMY_AI_DECISION_TREE` → `BATTLE_UI_COLOR_SPEC` |
| **美術／UI** | `STORY_PROGRESS_WORLDVIEW` §五 → 各 `*_COLOR_SPEC` → `LEVEL_DESIGN_GDD` §體驗檢查 |
| **口試／報告** | `DIFFICULTY_AND_AI_DESIGN` → `BALANCE_AND_AI_BIBLIOGRAPHY` → `MARKET_ANALYSIS_FIVE_GAMES` |

---

## 九、修訂紀錄

| 日期 | 說明 |
|------|------|
| 2026-05-30 | 初版：建立企劃索引，串接總表與待定文件；補主線 1-1 閱讀路徑 |
