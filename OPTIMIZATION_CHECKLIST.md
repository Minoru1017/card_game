# 程式優化清單

> 追蹤畢業專題程式端待優化項目。完成後將 `[ ]` 改為 `[x]`，並在項目末尾加上完成日期（例：`（2026-05-20 完成）`）。
>
> **最後更新**：2026-05-20

---

## 使用方式

1. 優先從 **P0** 開始處理；P1、P2 可在 demo 前依時間取捨。
2. 若項目拆成多個 PR／提交，可在同一項下用子項目 `- [ ]` 細分。
3. 不確定是否算完成時，以「可重現驗證通過」為準（手動測試步驟寫在驗證欄）。
4. P0 手動回歸表：[P0_MANUAL_REGRESSION.md](./P0_MANUAL_REGRESSION.md)

---

## P0 — 立即建議（功能風險／維護）

_（P0 已全部實作；見下方「已完成」。）_

---

## P1 — 短期優化（demo 前建議）

_（P1 已全部實作；見下方「已完成」。）_

---

## P2 — 可延後（品質／答辯加分）

_（P2 已全部實作；見下方「已完成」。）_

---

## 已完成

- [x] **結算觸發改為事件驅動**（2026-05-20 完成）  
  `BattleSimulationManager.BattleEnded`／`BattleRuleMessageChanged`；`BattleSimulationDebugUI` 不再於 `Update` 每幀呼叫 `UpdateBattleResultText()`。

- [x] **拆分 `BattleSimulationDebugUI` 職責**（2026-05-20 完成）  
  天氣執行期邏輯移至 `BattleSimulationDebugUI.WeatherRuntime.cs`（主檔約 4700 行）。

- [x] **拆分 `DeckManager` 職責**（2026-05-20 完成）  
  場景存檔鉤子移至 `DeckManager.ScenePersistence.cs`；主檔維持 `partial`。

- [x] **拆分 `SceneLoader` 戰前預覽 UI**（2026-05-20 完成）  
  戰前預覽與難度設定移至 `SceneLoader.BattlePreview.cs`；載入／進戰流程留在 `SceneLoader.cs`。

- [x] **結算熟練度區改用 TMP**（2026-05-20 完成）  
  結算五欄列狀態文字改為 `TextMeshProUGUI`，沿用 `sharedUIFont`。

- [x] **背包熟練度列排版常數化**（2026-05-20 完成）  
  集中於 `BackpackInspectMasteryLayout.cs`。

- [x] **統一 PlayerData 權威來源**（2026-05-23 完成）  
  全專案改為 `PlayerData.ResolveCanonical()`；移除 `FindFirstObjectByType<PlayerData>` 後備。

- [x] **Git／存檔衛生**（2026-05-23 完成）  
  `.gitignore` 新增 `debug-*.log`、`Assets/PlayerDataSnapshots/*.csv`；資料夾內 [README](./Assets/PlayerDataSnapshots/README.md) 說明勿提交試玩快照。

- [x] **結算與熟練度回歸驗證（手動 checklist）**（2026-05-20 完成）  
  [P0_MANUAL_REGRESSION.md](./P0_MANUAL_REGRESSION.md) 表內 **A～D 已全部通過**。

- [x] **凍結結算面板 Layout Version**（2026-05-23 完成）  
  `EndBattlePanelLayoutVersion = 6` 並加註解：僅整批重建時才 +1。

- [x] **EditMode 小型測試**（2026-05-20 完成）  
  `Assets/Editor/CardGameEditModeTests.cs`：熟練度 fill、結算 entries、五槽名稱、普通以上勝場計入。

- [x] **敵方 AI 與五段難度行為**（2026-05-20 完成）  
  `IntroGreedy`／`EasySpellLean`／`SchemingHard`／`SchemingBoss`；`MapDifficultyToEnemyAiPlayStyle` 與設計文件已同步。

- [x] **減少執行期 Debug.Log 噪音**（2026-05-20 完成）  
  `GameDevLog`（`UNITY_EDITOR`）；`BattleVerbose`、Buildbeck 診斷 log 改用之。

- [x] **文件與程式索引同步**（2026-05-20 完成）  
  `PROJECT_CODE_INDEX_v2.md`、`ENEMY_AI_DECISION_TREE.md`、`DIFFICULTY_AND_AI_DESIGN.md`。

- [x] **戰鬥 UI 與除錯 UI 分離**（2026-05-20 完成）  
  `debugPanelVisibleOnPlay` 預設關閉；正式 HUD／結算不依除錯半屏。

---

## 相關文件

| 文件 | 說明 |
|------|------|
| [PLANNING_DOCS_INDEX.md](./PLANNING_DOCS_INDEX.md) | 企劃文件索引 |
| [PLANNING_MASTER_TABLE.md](./PLANNING_MASTER_TABLE.md) | 企劃總表 |
| [PLANNING_OPEN_ITEMS.md](./PLANNING_OPEN_ITEMS.md) | 待定與未定義項 |
| [DECK_SAVE_IMPLEMENTATION.md](./DECK_SAVE_IMPLEMENTATION.md) | 牌組名稱／槽位存檔 |
| [ARCHITECTURE_OVERVIEW.md](./ARCHITECTURE_OVERVIEW.md) | DataManager／場景流程 |
| [CARD_PROFICIENCY_GDD.md](./CARD_PROFICIENCY_GDD.md) | 熟練度規則 |
| [P0_MANUAL_REGRESSION.md](./P0_MANUAL_REGRESSION.md) | P0 手動回歸表 |
| [PROJECT_CODE_INDEX_v2.md](./Assets/Scripts/PROJECT_CODE_INDEX_v2.md) | 程式模組索引 |
