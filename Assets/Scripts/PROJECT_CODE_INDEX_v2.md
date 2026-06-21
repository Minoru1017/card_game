# 專案程式索引（v2）

> 整合舊版索引（v1）與 2026-04-15 新增之「天氣機制／天氣視覺特效」索引。  
> **企劃文件**：[PLANNING_DOCS_INDEX.md](../../PLANNING_DOCS_INDEX.md) · [PLANNING_MASTER_TABLE.md](../../PLANNING_MASTER_TABLE.md) · [PLANNING_OPEN_ITEMS.md](../../PLANNING_OPEN_ITEMS.md)

## A. 核心玩法系統

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 關卡戰鬥系統 | 回合制核心流程（出牌、攻擊、法術、回合推進、勝負）；結束時 `BattleEnded` 事件 | `BattleSimulationManager` / `CompleteBattle()` / `ChooseEnemyHandCardToPlayIndex()` | 玩家操作、手牌/牌組/場面狀態 | 戰鬥狀態變更、事件通知、勝敗結果 |
| 關卡 AI 系統 | 敵方自動決策與執行行動 | `EnemyAI` / `ExecutePlay()` / `ExecuteAttack()`（及 `BattleSimulationManager` 內敵方流程） | 當前戰場、敵方手牌 | 敵方出牌/攻擊行為 |
| 抽卡系統 | 開包、播放流程、抽卡結果落盤 | `OpenPackge` / `PackVideoController` / `OnClickOpen()` / `OnVideoFinished()` / `SaveCardData()` | 開包事件、卡池資料 | 抽卡結果、玩家收藏更新 |
| 背包系統 | 牌庫與牌組編輯、卡片檢視、重置流程 | `DeckManager`（partial）/ `DeckManager.ScenePersistence` / `UpdateLibrary()` / `ShowBackpackCardInspect()` | 收藏數量、UI操作 | 牌組資料變更、畫面更新 |
| 卡牌熟練度 | 對戰累積、A/B/C 階段、背包進度條 | `CardSkillProficiencyService` / `BackpackInspectMasteryLayout` | 勝敗、難度標籤 | 階段與進度條 |

---

## B. 資料與內容系統

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 玩家資料庫 | 玩家金幣/收藏/牌組槽位讀寫 | `PlayerData` / `LoadPlayerData()` / `SavePlayerData()` / `GetCollectionCount()` / `SetDeckCount()` | 卡牌ID、槽位、存檔資料 | 經 `PlayerSaveCoordinator` 寫入 `playerdata.csv` |
| 牌組顯示名稱 | 3 玩家槽 × 5 槽 raw 名、CSV、污染清理、Buildbeck 確定 | `PlayerDeckSlotNameStorage` / `ConfirmBuildbeckRename()` / `RepairPersistedDeckSlotNamePollutionIfNeeded()` | 自訂名、解散還原 | `deck_slot_name` 列；UI fallback「牌組n」 |
| 玩家 profile 摘要 | 戰績 CSV；牌組摘要來自 runtime | `PlayerProfileCsvService` / `LoadProfileForPlayerInfoDisplay()` / `SyncDeckSummaryFromRuntime()` | 開面板（只讀）vs 持久化 | `profile_decks` 列 |
| 存檔協調 | 主檔唯一寫入、旗標 Upsert、離場前 flush | `PlayerSaveCoordinator` / `PlayerSaveDebouncer` | — | 勿直接 `PlayerPersistSafeIO.Write…` 寫主檔 |
| 卡牌資料庫 | 載入卡牌主資料、查詢、隨機抽取 | `CardStore` / `Card` `MonsterCard` `SpellCard` / `GetCardById()` / `LoadCardData()` / `RandomCard()` | 卡牌CSV/ID | `Card` 物件與卡牌清單 |
| 劇本資料庫 | 劇情步驟、分支選項與跳轉 | `MainPlotSceneController` / `PlotStep` / `ShowStep()` / `OnChoiceClicked()` | 步驟索引、玩家選項 | 劇情畫面切換、下一步更新 |
| 卡牌ID轉換層 | 舊ID與法術Key對應 | `DeckCardId` / `NormalizeLegacyUnifiedId()` / `SpellKeyFromOrdinal()` | 舊版ID、法術序號 | 正規化後Key/ID |

---

## C. 顯示與互動系統（UI/UX）

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 使用者介面（UI） | 戰鬥HUD、手牌區、場面區、結算/暫停；除錯半屏預設關閉 | `BattleSimulationDebugUI`（partial：Settlement、WeatherRuntime、FieldCards…）/ `BattleEnded` 訂閱結算 | 戰鬥狀態事件 | UI元件顯示與動畫 |
| 場地牌狀態 | 場上怪獸徽章、傷害浮字、凝視護盾等狀態對照 | `FIELD_CARD_STATUS_INDEX.md` / `FieldCardStatusIndex` / `GetPlayerFieldMonsterStatusBadge()` | 規則旗標、回合數 | 中央徽章與瞬時 FX |
| 戰前預覽謎題 | 訓練場魔王解謎（PZ01）、難度拱門與解鎖序（**謎題已停用**） | `BATTLE_PREVIEW_PUZZLE_INDEX.md` / `BattlePreviewPuzzleIndex` / `SceneLoader.BattlePreview` | 難度點選、預覽開關 | 預覽 UI；開戰前改走鬥鳥（見 §I） |
| 使用者體驗（UX） | 縮放、懸浮、長按、拖曳、點擊回饋 | `ZoomUI` / `BattleHandHoverPreview` / `BattleHandLongPressTooltip` / `BattleHandDiscardDrag` / `ClickCard` | Pointer事件 | 視覺回饋、互動狀態 |
| 卡牌顯示 | 將卡牌資料映射成畫面元素 | `CardDisplay` / `SetCard()` / `ShowCard()` / `CardCounter` | `Card` 物件、數值 | 卡面文字/圖像更新 |

---

## G. 港灣實戰戰術教練（Harbor Combat Coach）

> 企劃：[HARBOR_COMBAT_COACH_GDD.md](../../HARBOR_COMBAT_COACH_GDD.md) · 實作：[HARBOR_COMBAT_COACH_IMPLEMENTATION.md](HARBOR_COMBAT_COACH_IMPLEMENTATION.md)

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 教練 UI | 林可姐戰術面板；未讀脈動、點擊展開、棄牌階段右側 | `HarborCombatCoachUi` / `EnsureHarborCombatCoach()` / `ShouldAllowHandHighlight()` | 港灣開戰、`HarborCombatCoachHint` | 面板、立繪、打字機文案 |
| 觸發評估 | P0/P1 提示、冷卻、難度分流 | `HarborCombatCoachAdvisor.TryEvaluate()` / `HarborCombatCoachAdvisorSession` | `BattleSimulationManager` 盤面 | 單則 `HarborCombatCoachHint` |
| 致死預警 | 下回合敵方傷害上界（含火球意圖） | `HarborCombatLethalThreatEstimator.Evaluate()` / `BattleSimulationManager.EstimateHarborCoach*` | 難度檔、手牌/場面 | `ShouldWarn`、傷害值 |
| 立繪表情 | hintKey → 四表情 Sprite | `HarborCombatCoachExpressionCatalog.ApplyToPortrait()` | `hintKey` | `Image.sprite` |
| 手牌高亮 | 戰術建議牌索引（困難關閉） | `HarborCombatHandHighlightAdvisor` / `RequestHarborHandPlayHighlights()` | hintKey、手牌 | 高亮 `HandCard_*` |
| 玩家設定 | 戰術提示／高亮開關 | `HarborCombatCoachPrefs` | PlayerPrefs | bool |

**啟用**：`BattleLaunchContext.IsHarborTrainingGroundBattle`（與 `TutorialBattleCoachUi` 互斥）。

---

## I. 鬥鳥暖身賽（戰前節奏小遊戲）

> 企劃：[`Docs/鬥鳥手勢小遊戲企劃.md`](../../Docs/鬥鳥手勢小遊戲企劃.md) · 節奏資料：`Assets/Resources/BirdDuelRhythmSync.asset`

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 核心規則 | 鳥勢反制、計分、看破、PASS、勝負與情報層級（純 C#） | `BirdDuelCore` / `ResolveJudgement()` / `ResolveIntelTier()` | 對手鳥勢、玩家輸入、時序 | `BirdBeatJudgement`、分數／看破增量 |
| 場景控制器 | BGM 鼓點 UI、判定、假 scare、加成 draft；**partial** 分檔 | `FightingBirdGameSceneController`（`.UiBuild` `.MatchFlow` `.Draft` `.Audio` `.Visuals` `.Input` …） | `PreBattleDuelContext`、NPC profile | `PreBattleBonusContext`、返回戰鬥 |
| 節奏同步 | BGM BPM、首拍偏移、CD 難度 grid | `BirdDuelRhythmSync` / `ResolveForCd()` | `cdId`、grid mode | 判定窗口、步距 |
| 戰前上下文 | 開戰難度、魔王級、港灣英雄 | `PreBattleDuelContext` / `PreBattleBonusContext` / `PreBattleCdContext` | 預覽 modal 選項 | 鬥鳥→戰鬥帶入 |
| 場景載入 | 預覽後進鬥鳥、結束接戰 | `SceneLoader.BirdDuel` / `LaunchBirdDuelThenBattle()` / `ResumeBattleAfterBirdDuel()` | 難度檔、港灣旗標 | `Fighting bird game` → `BattleSimulation` |
| 判定音效 | Hi-Hat 母帶依 Perfect/Good/Guard/Miss 切片 | `BirdDuelHitSfxBank` / `AudioLibrary.birdDuelHitSfxSource` | 判定結果 | 單次 SFX |
| CD 資料 | 光碟檔、陣營、勝利 draft 池 | `BirdDuelCdCatalog` / `BirdDuelCdSelectOverlayUi` | CD id | draft 白名單 |
| Editor 量測 | BGM BPM 寫入 RhythmSync | `BirdDuelBgmTempoAnalyzer` | `Assets/Music/*.mp3` | `BirdDuelRhythmSync.asset` |

**流程摘要**：戰前預覽確認難度 → `LaunchBirdDuelThenBattle` → 鬥鳥（情報／加成 draft）→ `ResumeBattleAfterBirdDuel` → 正式對戰。

## H. 港灣訓練場難度（1-1 實戰三檔）

| 職責 | 類別／API | 備註 |
|------|-----------|------|
| 三檔常數 | `HarborTrainingEasyBattleRules` / `Normal` / `Hard` | 牌組、傷害、快攻、抽牌 |
| 開戰與戰中統一入口 | `HarborTrainingDifficultyRuntime` / `HarborTrainingTierConfig` | `SceneLoader.HarborTraining`、`BattleSimulationManager` |
| 回合上限勝利 | `BattleSimulationManager.HarborTraining.cs` | 僅簡單第 10 回合 |
| 對照文件 | `HARBOR_1-1_VS_TRAINING_GROUND_DIFFICULTY.md` | 與 Buildbeck 訓練場分離 |

---

## D. 場景與維運工具

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 場景流程/導航 | 場景切換與前置條件檢查 | `SceneLoader` / `SceneLoader.BattlePreview` / `SceneLoader.BirdDuel` / `SceneLoader.HarborTraining` / `BattleSceneBootstrap` / `EnterBattle()` | 切換請求、組牌狀態 | 場景載入、戰前預覽→鬥鳥→戰鬥 |
| 開發日誌 | Editor 保留、Release 不洗版 | `GameDevLog` | 訊息字串 | Console 輸出 |
| EditMode 測試 | 熟練度／牌組名稱煙霧測試 | `Assets/Editor/CardGameEditModeTests.cs` | NUnit | 斷言通過 |
| Bug 場景（牌組名） | 存檔／跨槽／污染手動驗收 | `BugHandlingDeckSlotNameScenario` / `Bug handling scenarios` | Play Mode UI | 對照 `Docs/DECK_SLOT_NAME_BUG_CHECKLIST.md` |
| 存檔重置/維運 | 由全域玩家資訊面板執行重置流程 | `GlobalNavRuntime` / `LoadProfileForPlayerInfoDisplay()` | 開面板只讀；關閉 Buildbeck 刷新名稱 | 勿用 `RefreshProfileFromRuntime` 開面板 |
| 貴重品庫 | 4×6 儲存格、每槽存檔、全局選單視窗 | `ValuablesVaultState` / `GlobalNavValuablesVaultOverlay` / `TryOpenValuablesVaultOverlay()` | 格子索引、definitionId | playerdata `slot,N,valuable,...` |
| 自動模擬/測試 | 批次自動對戰與勝率統計 | `BattleAutoSimPlugin` / `Run()` / `EnsureProgressUi()` / `TryAutoPlayOneCard()` | 模擬參數、回合數 | 勝率統計、進度與結果 |

---

## E. 天氣系統（本次整合新增）

### E-1. 天氣規則與回合調度（Battle 層）

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 天氣命名與輪替 | 天氣名稱映射、輪替與首輪覆寫 | `BattleSimulationManager` / `GetWeatherLabel()` / `GetRotatingWeatherBySerial()` / `GetFirstWeatherOverrideIfAny()` | 回合序號、Inspector 勾選 | 當前天氣型別/名稱 |
| 天氣階段流程 | 預報觸發、持續回合、冷卻回合、恢復回合 | `CoPresentWeatherForecastForTurn()` / `TryEnterWeatherPhaseForCurrentRound()` | 回合開始事件 | 是否中斷、是否預報、回合狀態 |
| 天氣效果結算 | 依天氣對傷害/治療/法術倍率套用修正 | `ApplyWeatherSpellPowerBonus()` / `ApplyFogDirectDamageReductionIfNeeded()` / `ApplyHolyLightHealBonusIfNeeded()` / `ApplyFireRainEndTurnEffect()` | 戰鬥數值、當前天氣 | 修正後數值、戰鬥記錄 |
| 天氣 UI 資訊輸出 | 對 UI 提供天氣文本與倒數提示 | `GetCurrentWeatherForecastDetailsText()` / `GetWeatherPseudoCardText()` / `GetCurrentWeatherLabelForUi()` / `GetCurrentWeatherRemainingRoundsForUi()` / `GetNextWeatherForecastHintForUi()` | 當前天氣狀態 | 顯示文本、剩餘回合資訊 |

### E-2. 天氣視覺特效（UI 層）

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 天氣特效總調度 | 依當前天氣開關與更新特效層 | `BattleSimulationDebugUI.WeatherRuntime` / `UpdateWeatherScreenEffects()` | 天氣名稱、剩餘回合 | 對應特效顯示/隱藏與更新 |
| 天氣動畫迴圈 | 各天氣專屬動畫邏輯 | `AnimateFireRainFx()` / `AnimateHolyLightFx()` / `AnimateFogFx()`（海嘯視覺）/ `AnimateGaleFx()` | `deltaTime`、天氣狀態 | 粒子/遮罩/風場動畫 |
| 天氣預報與面板 | 全屏預報、場地效果面板與說明刷新 | `OnWeatherForecastStarted()` / `CoShowWeatherForecastOverlay()` / `RefreshActiveWeatherEffectPanelText()` | 預報事件、天氣文本 | UI 顯示更新、資訊排版 |
| 特效層建構 | 建立天氣覆蓋層與子元件 | `CreateWeatherScreenFx()` / `CreateWeatherFxLayer()` / `CreateHolyLightEdge()` / `AddHolyLightEdgeLayer()` / `AddFogEdgeLayer()` / `AddGaleNightLayer()` | 畫布/父節點 | 可動態更新的特效層級 |

---

## F. 天氣命名對照（世界觀文案）

| 天氣型別 | 當前顯示名（四字） | 規則摘要 |
| --- | --- | --- |
| FireRain | 緋焰時雨 | 回合結束雙方場上怪獸各受 5 點傷害 |
| HolyLight | 月華聖祈 | 治療效果增加 10 |
| Fog（視覺已改海嘯） | 蒼潮夜湧 | 直接攻擊英雄傷害減少 50% |
| Gale | 朔風森詠 | 雙方首張法術效果增加 20% |

