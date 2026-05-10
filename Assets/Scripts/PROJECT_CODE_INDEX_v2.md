# 專案程式索引（v2）

> 整合舊版索引（v1）與 2026-04-15 新增之「天氣機制／天氣視覺特效」索引。

## A. 核心玩法系統

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 關卡戰鬥系統 | 回合制核心流程（出牌、攻擊、法術、回合推進、勝負） | `BattleSimulationManager` / `StartBattle()` / `PlayerPlayCardFromHand()` / `PlayerAttack()` / `EndPlayerTurn()` / `RunEnemyTurn()` | 玩家操作、手牌/牌組/場面狀態 | 戰鬥狀態變更、事件通知、勝敗結果 |
| 關卡 AI 系統 | 敵方自動決策與執行行動 | `EnemyAI` / `ExecutePlay()` / `ExecuteAttack()`（及 `BattleSimulationManager` 內敵方流程） | 當前戰場、敵方手牌 | 敵方出牌/攻擊行為 |
| 抽卡系統 | 開包、播放流程、抽卡結果落盤 | `OpenPackge` / `PackVideoController` / `OnClickOpen()` / `OnVideoFinished()` / `SaveCardData()` | 開包事件、卡池資料 | 抽卡結果、玩家收藏更新 |
| 背包系統 | 牌庫與牌組編輯、卡片檢視、重置流程 | `DeckManager` / `UpdateLibrary()` / `UpdateDeck()` / `CreateCard()` / `ShowBackpackCardInspect()` | 收藏數量、UI操作 | 牌組資料變更、畫面更新 |

---

## B. 資料與內容系統

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 玩家資料庫 | 玩家金幣/收藏/牌組槽位讀寫 | `PlayerData` / `LoadPlayerData()` / `SavePlayerData()` / `GetCollectionCount()` / `SetDeckCount()` | 卡牌ID、槽位、存檔資料 | 記憶體資料與存檔同步 |
| 卡牌資料庫 | 載入卡牌主資料、查詢、隨機抽取 | `CardStore` / `Card` `MonsterCard` `SpellCard` / `GetCardById()` / `LoadCardData()` / `RandomCard()` | 卡牌CSV/ID | `Card` 物件與卡牌清單 |
| 劇本資料庫 | 劇情步驟、分支選項與跳轉 | `MainPlotSceneController` / `PlotStep` / `ShowStep()` / `OnChoiceClicked()` | 步驟索引、玩家選項 | 劇情畫面切換、下一步更新 |
| 卡牌ID轉換層 | 舊ID與法術Key對應 | `DeckCardId` / `NormalizeLegacyUnifiedId()` / `SpellKeyFromOrdinal()` | 舊版ID、法術序號 | 正規化後Key/ID |

---

## C. 顯示與互動系統（UI/UX）

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 使用者介面（UI） | 戰鬥HUD、手牌區、場面區、結算/暫停等畫面 | `BattleSimulationDebugUI`（含 partial）/ `CreateDebugPanel()` / `RebuildHandButtons()` / `RefreshFieldCards()` / `EnsureEndBattlePanel()` | 戰鬥狀態事件 | UI元件顯示與動畫 |
| 使用者體驗（UX） | 縮放、懸浮、長按、拖曳、點擊回饋 | `ZoomUI` / `BattleHandHoverPreview` / `BattleHandLongPressTooltip` / `BattleHandDiscardDrag` / `ClickCard` | Pointer事件 | 視覺回饋、互動狀態 |
| 卡牌顯示 | 將卡牌資料映射成畫面元素 | `CardDisplay` / `SetCard()` / `ShowCard()` / `CardCounter` | `Card` 物件、數值 | 卡面文字/圖像更新 |

---

## D. 場景與維運工具

| 模組 | 功能描述 | 主要類別/函式 | 典型輸入 | 典型輸出 |
| --- | --- | --- | --- | --- |
| 場景流程/導航 | 場景切換與前置條件檢查 | `SceneLoader` / `BattleSceneBootstrap` / `EnterBattle()` | 切換請求、組牌狀態 | 場景載入、提示顯示 |
| 存檔重置/維運 | 由全域玩家資訊面板執行重置流程 | `GlobalNavRuntime` / `TryOpenPlayerInfoOverlay()` | 玩家資訊面板互動 | 玩家資料重置與刷新 |
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
| 天氣特效總調度 | 依當前天氣開關與更新特效層 | `BattleSimulationDebugUI` / `UpdateWeatherScreenEffects()` | 天氣名稱、剩餘回合 | 對應特效顯示/隱藏與更新 |
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

