# 港灣實戰戰術教練 — 程式實作說明

> 企劃規格：[HARBOR_COMBAT_COACH_GDD.md](../../HARBOR_COMBAT_COACH_GDD.md)  
> 索引：[PROJECT_CODE_INDEX_v2.md](PROJECT_CODE_INDEX_v2.md) §G

## 啟用條件

| 旗標 | 說明 |
|------|------|
| `BattleLaunchContext.IsHarborTrainingGroundBattle` | 港灣訓練場開戰時為 true；與入門教學戰互斥 |
| `HarborCombatCoachPrefs.AreTacticalHintsEnabled()` | PlayerPrefs `harbor_coach_tactical_hints`，預設開 |
| `HarborCombatCoachPrefs.IsHandHighlightEnabled()` | PlayerPrefs `harbor_coach_hand_highlight`，預設開；**困難**仍由程式強制關閉 |

開戰掛點：`BattleSimulationDebugUI` 初始化時 `EnsureHarborCombatCoach(canvas2)`。

## 類別職責

| 類別 | 職責 |
|------|------|
| `HarborCombatCoachUi` | 林可姐面板 UI（鏡像入門教練版面）；**僅脈動 + 點擊展開**；訂閱 `BattleSimulationManager` 玩家回合事件 |
| `HarborCombatCoachAdvisor` | P0/P1 觸發評估、冷卻、`HarborCombatCoachHint` 文案 |
| `HarborCombatCoachAdvisorSession` | 單局冷卻狀態（每局／每回合窗口／天氣／每 N 回合） |
| `HarborCombatLethalThreatEstimator` | 致死預警傷害上界（§4.4）；唯讀 |
| `HarborCombatCoachExpressionCatalog` | `hintKey` → 立繪表情；`Resources/UI/LinKeCoach/linke_*.png` |
| `HarborCombatHandHighlightAdvisor` | 依 hint 回傳手牌索引 |
| `HarborCombatCoachPrefs` | 玩家設定 PlayerPrefs |

## 與入門教練的邊界

- **勿**改 `TutorialBattleCoachUi` 觸發表。
- 手牌高亮共用 `BattleSimulationDebugUI.TutorialHandHighlight` 的視覺元件，港灣走 `RequestHarborHandPlayHighlights` / `ClearHarborHandPlayHighlights`。
- 入門：`TutorialHandPlayAdvisor`；港灣：`HarborCombatHandHighlightAdvisor`。

## 評估時機

與入門相同窗口：

- `Update` 內玩家回合、非 `IsTurnSequenceInProgress`、非法術演出
- `PlayerTurnActionWindowOpenedForPromptUi` 後 `ScheduleEvaluate`
- 週期重評：`ReEvaluateIntervalSeconds`（1.25s）

同幀只採用 **優先級最高且冷卻通過** 的一則（見 `HarborCombatCoachAdvisor.TryEvaluate`）。

**棄牌階段獨佔**：`IsPlayerInDiscardSelection()` 或 `GetPlayerPendingDiscardCount() > 0` 時僅評估 `discard_required`，不與致死／出牌提示並存；進入棄牌時清除出牌高亮與舊提示鍵。

## 難度

`HarborCombatCoachAdvisor.ResolveHarborTier()` ← `HarborTrainingBattleCopy.TierFromLabelZh(BattleLaunchContext.ResolveForBattleRecord())`。

| 難度 | P1 | 手牌高亮 |
|------|-----|----------|
| 簡單 | `harbor_pressure`、`heal_before_end` | 允許（可關 prefs） |
| 普通 | P1 全開 | 允許 |
| 困難 | 無 P1 | `ShouldAllowHandHighlight()` 恆 false |

## 致死估算 API（`BattleSimulationManager`）

港灣教練專用唯讀方法（`#region Harbor combat coach`）：

- `PeekPendingEnemyDirectAttackUnlockForCoach()`
- `EstimateHarborCoachEnemyFireballRawDamage()`
- `EstimateHarborCoachDamageToPlayerMonsterFromRaw(int)`
- `EstimateHarborCoachDirectDamageToPlayerHeroFromRaw(int)`
- `EstimateHarborCoachScaledEnemyAttackToPlayerHero(int)`

`HarborCombatLethalThreatEstimator` 另呼叫 `ChooseEnemyHandCardToPlayIndex()` 推測敵方出牌，**不**執行真實出牌。

## 立繪資源

路徑：`Assets/Resources/UI/LinKeCoach/`

| 檔名 | 表情 |
|------|------|
| `linke_neutral.png` | 平常 |
| `linke_alert.png` | 警示 |
| `linke_serious.png` | 嚴肅 |
| `linke_encourage.png` | 鼓勵 |

建議 **512×512** 1:1。缺檔 → neutral → `TutorialPlotScriptFactory.GetLinKePortraitSprite()`。

## 擴充提示鍵

1. `HarborCombatCoachAdvisor` 增加條件與 `HarborCombatCoachHintCooldown`
2. `HarborCombatCoachExpressionCatalog.HintToExpression` 增加對照
3. `HarborCombatHandHighlightAdvisor`（若需高亮）
4. GDD §5 文案

## 驗收對照

見 GDD §八；程式自測建議：港灣簡單開局、敵手火球 + 我方空場 → 致死脈動；困難 → 無手牌高亮；入門戰不出現 `HarborCombatCoach` 物件。
