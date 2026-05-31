# 企劃總表

> **狀態**：維護中（2026-05-30）  
> **用途**：一頁掌握各企劃領域的**主文件、定案程度、讀者與程式錨點**。細節請進主文件；未定義項見 [`PLANNING_OPEN_ITEMS.md`](PLANNING_OPEN_ITEMS.md)。  
> **文件索引**：[`PLANNING_DOCS_INDEX.md`](PLANNING_DOCS_INDEX.md)

---

## 狀態圖例

| 標記 | 意義 |
|------|------|
| **定案** | 文案／流程已對齊實作或為驗收基準 |
| **部分定案** | 主流程已定，子項仍見待定文件 |
| **草案** | 有文件但未與程式完全對齊 |
| **待開案** | 僅備註或 JSON 占位，尚無 GDD |

---

## 總表

| 領域 | 主文件 | 狀態 | 主要讀者 | 程式／資料錨點 | 待定摘要 |
|------|--------|------|----------|----------------|----------|
| **企劃入口** | `PLANNING_DOCS_INDEX.md` | 定案 | 全員 | — | — |
| **主線世界觀** | `STORY_PROGRESS_WORLDVIEW.md` | 定案 | 企劃、美術、文案 | `StoryProgressLevelCopy.cs`、`StoryProgressNodeDatabase.json` | 與關卡 GDD 的 **Clear 語意**需對齊（見待定 §DOC） |
| **關卡 1-1** | `LEVEL_DESIGN_GDD.md` | **部分定案** | 企劃、程式 | `HarborTrainingProgressState`、`HarborTrainingRewardService`、`SceneLoader.HarborTraining.cs` | 三難度數值表、平手／放棄、通關獎勵 UI 狀態（見待定 §L1-1） |
| **關卡 1-2+** | —（待 `LEVEL_DESIGN_M-1-2.md` 或擴章） | **待開案** | 企劃 | `M-1-2` 節點於 JSON；地圖點擊未接戰鬥 | 海牆巡邏全流程（見待定 §L1-2） |
| **入門劇本** | `TUTORIAL_PLOT_SCRIPT.md` | 定案 | 文案、程式 | `TutorialPlotScriptFactory.cs`、`MainPlotSceneController.cs` | 重溫入門是否跳劇情（見待定 §TUT） |
| **教學進度旗標** | `LEVEL_DESIGN_GDD.md` §二、`TUTORIAL_PLOT_SCRIPT.md` | 部分定案 | 程式 | `TutorialProgressState.cs` | 舊存檔遷移（見待定 §SAVE） |
| **對戰規則** | `GAMEPLAY_AND_RULES.md` | 定案 | 企劃、程式 | `BattleSimulationManager.cs`、`CardList.csv` | — |
| **難度與 AI** | `DIFFICULTY_AND_AI_DESIGN.md` | 定案（報告） | 程式、口試 | `BuildDifficultyConfig`、`EnemyAiPlayStyle` | 港灣三檔是否僅標籤差異（見待定 §L1-1） |
| **敵 AI 細節** | `ENEMY_AI_DECISION_TREE.md` | 定案 | 程式 | `EnemyAi*.cs` | — |
| **戰前預覽謎題** | `BATTLE_PREVIEW_PUZZLE_INDEX.md` | 定案 | 程式、美術 | `SceneLoader.BattlePreview.cs` | 港灣預覽與碼頭敘事一致性（見待定 §ART） |
| **卡牌熟練度** | `CARD_PROFICIENCY_GDD.md` | 部分定案（v1 暫採） | 企劃 | `proficiency` CSV 鍵、`MonsterSkillRegistry` | §7 八項待決；彙總見待定 §PROF |
| **單卡戰技文案** | `卡牌技能階段式揭露.md` | 草案 | 企劃 | `MonsterSkillRegistry` | 逐卡填表進度 |
| **牌組存檔** | `DECK_SAVE_IMPLEMENTATION.md` | 定案 | 程式 | `PlayerData`、`DeckManager` | — |
| **大地圖節點文案** | `StoryProgressNodeDatabase.json` + `StoryProgressLevelCopy.cs` | 部分定案 | 程式 | `StoryProgressWorldMapRuntime.cs` | 節點點擊→進關流程（見待定 §MAP） |
| **登入／大廳導流** | `TUTORIAL_PLOT_SCRIPT.md`（首段） | 部分定案 | 程式 | 登入場景、`hall` | 未寫入關卡 GDD（見待定 §FLOW） |
| **對戰 UI 色票** | `BATTLE_UI_COLOR_SPEC.md` | 定案 | 美術、程式 | `BattleUiColors.cs` 等 | — |
| **對戰 FX 色票** | `BATTLE_FX_COLOR_SPEC.md` | 定案 | 美術 | FX Token | — |
| **背包 UI 色票** | `BACKPACK_INSPECT_UI_COLOR_SPEC.md` | 定案 | 美術 | — | — |
| **市場研究** | `MARKET_ANALYSIS_FIVE_GAMES.md` | 參考 | 企劃 | — | 不直接驅動實作 |
| **系統架構** | `ARCHITECTURE_OVERVIEW.md` | 定案 | 程式 | 場景流程圖 | 英文；非劇情規格 |
| **P0 回歸** | `P0_MANUAL_REGRESSION.md` | 維護中 | QA | — | — |

---

## 主線 1-1 進度語意（定案摘要）

以下為**目前實作與 `LEVEL_DESIGN_GDD.md` 一致**的用語；勿與舊版「入門戰勝即 Clear」混淆。

| 用語 | 條件 | 玩家可見效果 |
|------|------|----------------|
| **入門進行中** | 未完成 `Main Plot` 教學戰勝 | Story progress：進入關卡 → 劇情 |
| **入門畢業** | 教學對戰勝利 | 可開港灣預覽；地圖副標多為「實戰區」 |
| **實戰 Clear（M-1-1）** | 港灣**任一難度首通勝利** | 節點 Clear；解鎖 `M-1-2` |
| **港灣畢業證** | 港灣**困難首通勝利**（一次） | 獲得 SR 聖院騎士（卡 id 18） |

完整流程與獎勵表見 [`LEVEL_DESIGN_GDD.md`](LEVEL_DESIGN_GDD.md)。`STORY_PROGRESS_WORLDVIEW.md` §七「通關＝入門畢業」指**敘事層**，與**地圖 Clear** 不同層級——對照說明見 [`PLANNING_OPEN_ITEMS.md`](PLANNING_OPEN_ITEMS.md) §DOC-001。

---

## 建議下一批企劃產出

| 優先 | 產出物 | 解除待定 |
|------|--------|----------|
| P0 | 修訂 `STORY_PROGRESS_WORLDVIEW.md` §三、§七 與本總表「進度語意」對齊 | §DOC |
| P0 | `LEVEL_DESIGN` 補港灣三難度數值表或「同配置僅標籤」聲明 | §L1-1-DIFF |
| P1 | `LEVEL_DESIGN_M-1-2.md`（或 GDD 第二章） | §L1-2 |
| P1 | 大地圖「點擊節點 → 進關」流程圖 | §MAP |
| P2 | 舊存檔遷移說明 | §SAVE |

---

## 修訂紀錄

| 日期 | 說明 |
|------|------|
| 2026-05-30 | 初版：建立企劃總表，彙整 1-1 進度語意與各領域狀態 |
