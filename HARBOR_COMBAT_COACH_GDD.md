# 港灣實戰區：戰術教練 GDD（Harbor Combat Coach）

> **狀態**：企劃定案（2026-05-31）  
> **範圍**：1-1 **港灣訓練場實戰**（`BattleLaunchContext.IsHarborTrainingGroundBattle`）  
> **非範圍**：學院入門教學戰（`IsIntroTutorialBattle` · 見 `TutorialBattleCoachUi`）、Buildbeck 一般對戰、M-1-2 以後  
> **相關文件**：[`PLANNING_DOCS_INDEX.md`](PLANNING_DOCS_INDEX.md) · [`LEVEL_DESIGN_GDD.md`](LEVEL_DESIGN_GDD.md) · [`TUTORIAL_PLOT_SCRIPT.md`](TUTORIAL_PLOT_SCRIPT.md) §五 · [`GAMEPLAY_AND_RULES.md`](GAMEPLAY_AND_RULES.md) · [`DIFFICULTY_AND_AI_DESIGN.md`](DIFFICULTY_AND_AI_DESIGN.md)

---

## 一、設計目標

| 項目 | 定義 |
|------|------|
| **玩家狀態** | 已完成入門劇情與入門教學戰，**會基本出牌、攻擊、結束回合** |
| **教練角色** | 仍為**林可姐**，但語氣從「帶做」改為**戰術參謀** |
| **觸發哲學** | **關鍵時刻救場**（contextual / danger coaching），非每回合教學 |
| **頻率目標** | 單局約 **2～5 次**有效提示；同一觸發鍵每局有冷卻 |
| **市場對照** | 類似手遊 TCG 的致死預警、環境生效提醒、危機時建議解場／囤牌；**不是**全程新手教程 |

**一句話**

> 入門教會你怎麼玩；港灣實戰在「可能要輸」或「環境改變決策」時才開口。

---

## 二、與入門 Coach 差異（必讀）

| 維度 | 入門 `TutorialBattleCoachUi` | 港灣實戰 `HarborCombatCoachUi`（本文件） |
|------|------------------------------|------------------------------------------|
| **啟用條件** | `IsIntroTutorialBattle` | `IsHarborTrainingGroundBattle` |
| **提示密度** | 高（幾乎每回合可操作窗口） | 低（僅 P0～P1 觸發） |
| **預設面板** | 可收起；常態有未讀提示 | **預設收起**；有未讀時**僅邊框脈動**，玩家**點開**才展開（見 §3.1） |
| **手牌高亮** | 建議出牌／棄牌常開（見 `TutorialHandPlayAdvisor`） | 簡單／普通：提示觸發時可高亮；**困難：完全關閉**（見 §3.2、§六） |
| **劇情台詞** | 無長篇；但會反覆講規則 | **不講**基礎規則（火球用法、結束回合等） |
| **天氣** | 入門戰**關閉**天氣系統 | 港灣實戰**啟用**天氣（非 `IntroGreedy`） |
| **敵方 AI** | `IntroGreedy` | 港灣多為 `FastAttack`（見關卡 GDD） |
| **文案標點** | 入門戰慣例不用標點 | 戰術提示**不用標點**，與 Story progress 一致 |
| **強調字** | `StoryTextStyle.Em` / `Hi` 可選 | 僅關鍵數字與牌名用 `Em` |
| **立繪表情** | 單張（凝視卡圖 fallback） | **多表情**依 `hintKey` 切換（§3.5） |

**禁止重複（實戰區不再出現）**

- 「這是你的回合試著出一張怪獸或法術」
- 「第一回合火球還不能用先出怪獸吧」
- 「場上有怪獸了按結束回合就會自動攻擊」（除非 P1 且玩家連續漏攻擊節奏 N 次，見 §4.3）

---

## 三、UI 與互動規格

### 3.1 呈現

| 項目 | 規格 |
|------|------|
| **元件** | 復用林可姐教練面板骨架（與入門相同 Prefab／程式結構），**獨立元件** `HarborCombatCoachUi` |
| **預設狀態** | 收起（僅頭像／小圓點） |
| **未讀提示** | 邊框／頭像**脈動**；**不**自動展開、不搶焦點 |
| **展開** | **僅玩家點擊**教練面板後展開並播放該則戰術文案（打字機效果） |
| **收起** | 玩家再次點擊收起；切換提示時若已展開則更新文案 |
| **位置** | 與入門相同：一般回合**左側**；棄牌階段**右側**（不遮棄牌區） |
| **遮罩** | 半透明暗化；**不擋**手牌與結束回合 |
| **打字速度** | 與入門相同 9 字／秒；戰術句短，總字數 ≤ 40 字為宜 |
| **立繪表情** | **定案（HARBOR-COACH-004）**：與台詞 **UI 分開**（頭像 `Image` + 文案 `TMP`）；依 `hintKey` **切換不同表情圖**，見 §3.5 |

### 3.2 玩家設定（建議）

| 設定項 | 預設 | 說明 |
|--------|------|------|
| **實戰戰術提示** | 開 | 關閉後教練面板不評估、不脈動 |
| **戰術手牌高亮** | 開（簡單／普通） | 關閉則只顯示文字；**困難級永遠不高亮**（定案，不受此開關影響） |

### 3.3 已定案互動（HARBOR-COACH-002）

| 項目 | 定案 |
|------|------|
| 自動展開 | **不做** |
| 脈動 | 有未讀戰術提示時脈動 |
| 閱讀 | 玩家**主動點開**才看到完整句子 |
| 理由 | 實戰區不搶操作節奏；進階玩家可忽略脈動繼續出牌 |

### 3.5 立繪表情（定案 · HARBOR-COACH-004）

**不是**把文字畫進立繪圖裡，而是：**同一套教練面板裡，頭像區與台詞區本來就分開**；實戰區在顯示／排隊提示時，依戰術類型換**不同林可立繪 Sprite**（入門教練仍維持單張立繪，不共用表情表）。

| 項目 | 規格 |
|------|------|
| **表情枚舉** | `neutral` 平常 · `alert` 警示 · `serious` 嚴肅 · `encourage` 鼓勵 |
| **切換時機** | 佇列有新 `hintKey` 時更新頭像（收起態也換，配合脈動）；玩家點開後展開文案時頭像與該鍵一致 |
| **缺圖 fallback** | 該表情資源不存在 → `neutral`；`neutral` 仍無 → `TutorialPlotScriptFactory.GetLinKePortraitSprite()` |
| **資源路徑（建議）** | `Resources/UI/LinKeCoach/linke_{expression}.png`（例：`linke_alert.png`） |
| **美術備註** | 與入門卡圖裁切可不同：教練頭像建議 **1:1、臉部居中**，四張表情構圖一致以利切換 |

#### 產出尺寸（1:1，四張相同）

| 項目 | 數值 | 說明 |
|------|------|------|
| **標準交件** | **512 × 512 px** | 建議四張皆以此尺寸匯出；Canvas 參考 1920×1080，頭像最大顯示約 **152×152**（收起），512 約 **3.3×** 超採樣，手機／PC 皆夠銳 |
| **可選母稿** | **1024 × 1024 px** | 僅在需要更大臉部細節時；匯入 Unity 前縮成 512 或保留 1024（檔案較大） |
| **比例** | **1:1** | 寬高必須一致，四張像素尺寸必須相同 |
| **格式** | PNG（透明或淺底皆可） | 與現有 UI 圖一致；`preserveAspect` 顯示 |
| **檔名** | `linke_neutral.png` · `linke_alert.png` · `linke_serious.png` · `linke_encourage.png` | 四檔皆 **512×512**（或四檔皆 1024×1024，勿混用） |

**構圖安全區（512 畫布上）**

- 臉部與雙肩落在畫面**正中**；四張表情的**臉大小、眼睛高度、下巴位置**對齊（切換時才不跳）。
- 建議以中心 **360 × 360 px** 方框（約 70%）放臉部關鍵特徵；外圈留髮飾／肩線，避免貼邊被圓角框裁切（入門框內緒約 **4 px**，框線約 **152 + 12** 外框）。

**Unity 匯入（建議）**

- Texture Type：Sprite (2D and UI) · Mesh Type：Full Rect · Pixels Per Unit：**100**（與多數 UI 圖一致即可）
- Max Size：**512**（交 1024 母稿時可設 1024，顯示仍縮到 ~152）
- Filter Mode：Bilinear · Compression：依專案 UI 慣例（與 `Assets/UI/` 其他 PNG 相同）

#### `hintKey` → 表情對照（首版）

| `hintKey` | 表情 | 理由 |
|-----------|------|------|
| `lethal_next_turn` | `alert` | 致死／斷血預警 |
| `discard_required` | `neutral` | 操作說明，語氣平穩 |
| `weather_fire_rain` | `serious` | 環境傷害壓力 |
| `weather_holy_light` | `encourage` | 正面時機 |
| `weather_fog` | `neutral` | 戰術取捨說明 |
| `weather_gale` | `encourage` | 法術加成機會 |
| `hand_near_cap` | `serious` | 資源壓力 |
| `threat_field` | `alert` | 場面威脅 |
| `no_field_before_end` | `encourage` | 建議補場，非驚嚇 |
| `heal_before_end` | `encourage` | 防守恢復 |
| `harbor_pressure` | `serious` | 訓練強度提醒 |

新增提示鍵時須同步補此表與資源檔名。

### 3.6 與其他 HUD 關係

- **回合／先攻擊 HUD**（右下角）：常駐，不取代教練。
- **天氣 Badge**（除錯／戰鬥 UI）：教練在天氣**首次影響決策**時補一句，不重複念天氣表。
- **「你的回合」橫幅**：教練**不**因回合開始自動展開。

---

## 四、觸發表

優先級：**數字越小越優先**；同幀只顯示**一則**。評估時機：我方回合、非開場演出、非回合動畫中、非棄牌動畫鎖定（與入門 `EvaluatePlayerTurnHints` 相同窗口）。

**冷卻欄位說明**

| 欄位 | 含義 |
|------|------|
| **每局** | 本局對戰最多觸發 1 次 |
| **每回合窗口** | 每次 `PlayerTurnActionWindowOpened` 最多 1 次 |
| **每 N 回合** | 距上次同鍵觸發至少 N 個 `currentRound` |

---

### 4.1 P0（首版必做）

| 優先 | 鍵名 `hintKey` | 觸發條件（程式可檢） | 冷卻 | 手牌高亮 |
|:----:|----------------|-------------------|------|----------|
| 1 | `lethal_next_turn` | **致死預警**：模擬敵方下回合行動後對英雄的**最大合理傷害**（§4.4；含場攻、法術／火球意圖、直傷） | 每局 2 | 簡單／普通：可高亮 1～2 張；**困難：否** |
| 2 | `discard_required` | `GetPlayerPendingDiscardCount() > 0` 或進入棄牌選擇 | 每回合窗口 1 | 簡單／普通：棄牌 Advisor 1 張；**困難：否** |
| 3 | `weather_active` | 本回合天氣生效且 `IsWeatherSystemEnabledForBattle()`；依 `currentWeather` 子鍵（見 §4.2） | 每種天氣每局 1 | 簡單／普通：視子鍵；**困難：否** |
| 4 | `hand_near_cap` | 手牌 ≥ 6 且未在棄牌階段 | 每局 1 | 否 |

---

### 4.2 天氣子鍵（`weather_active` 分流）

| 子鍵 | 天氣（UI 名） | 觸發補充 | 戰術重點 |
|------|---------------|----------|----------|
| `weather_fire_rain` | 緋焰時雨 | 回合結束場上怪各 -5 | 解場或接受換血 |
| `weather_holy_light` | 月華聖祈 | 治療 +10 | 有治療／場怪時值得現在打 |
| `weather_fog` | 蒼潮夜湧 | 直傷英雄 -50% | 可考慮直傷而非執著解怪 |
| `weather_gale` | 朔風森詠 | 首張法術 +20% | 可現在打法術或囤關鍵法術 |

**觸發時機**：天氣生效後**第一次**進入我方回合且該天氣仍有效；或天氣預報剛結束進入我方回合（二擇一，實作取較不吵者）。

---

### 4.3 P1（次版）

| 優先 | 鍵名 | 觸發條件 | 冷卻 | 手牌高亮 |
|:----:|------|----------|------|----------|
| 5 | `threat_field` | 敵方場怪存在，且其威脅（攻+血）明顯高於我方場怪或我方無怪；手上有可解場法術（如火球） | 每局 1 | 簡單／普通：火球等；**困難：否** |
| 6 | `no_field_before_end` | 我方回合、場上無怪、尚未結束回合、已過開局回合；手上有可出怪 | 每 2 回合 1 | 簡單／普通：怪 1 張；**困難：否** |
| 7 | `heal_before_end` | 場上有怪、HP ≤ 72% max、手有初級治療 | 每局 1 | 簡單／普通：治療；**困難：否** |
| 8 | `harbor_pressure` | 港灣簡單／普通：第 3～6 回合內玩家 HP 掉幅 ≥ 35% max | 每局 1 | 簡單／普通：防守牌；**困難：否** |

**P2（暫不做）**

- 敵方凝視、林可 gaze 專屬長說明（入門已教）
- 戰後復盤、AI 覆盤
- 依牌組熟練度動態調文案

---

### 4.4 致死預警演算法（定案：含法術／火球意圖 · HARBOR-COACH-001）

> 目標：**寧可早警告，不要漏警告**。  
> **定案**：不只算「場怪攻擊」，須**淺層模擬敵方下一回合**，納入敵方法術（含火球）與快攻 AI 出牌傾向。

#### 4.4.1 模擬範圍（敵方回合內順序）

在**當前盤面快照**上，保守估計敵方回合結束後對我方英雄造成的**最大合理傷害** `threatDamageMax`（取多條路徑中的最大值，非加總）：

| 步驟 | 模擬內容 | 程式錨點 |
|------|----------|----------|
| A. 出牌階段 | 若敵方可出牌：用 `ChooseEnemyHandCardToPlayIndex()`（或同等評分）取**最傷英雄**的一手；若為火球且我方有場怪，假設打場怪，**本回合不直傷**；若為火球且我方無場怪，算法術對英雄傷害 | `BattleSimulationManager.ChooseEnemyHandCardToPlayIndex` |
| B. 攻擊階段 | 敵方場怪攻擊：若我方無場怪或 `pendingEnemyDirectAttackUnlock`／可直傷，加上場攻（含反擊後仍可能對英雄的淨傷害，取保守上界） | `EnemyAI.ExecuteAttack` 規則對照 |
| C. 法術後盤面 | 若 A 步火球擊殺我方場怪且 B 未發生直傷，再評估**攻擊階段是否改打英雄**（無場即直傷） | 盤面狀態推演 |

**不模擬（首版）**

- 敵方多張牌連打、凝視週期傷、天氣結算（天氣另走 `weather_active`）。
- 精確 Monte Carlo 或完整回合克隆。

#### 4.4.2 火球／法術意圖（定案納入）

| 情境 | 是否計入對英雄威脅 | 說明 |
|------|-------------------|------|
| 敵手有火球、我方**無**場怪 | **是** | 取火球對英雄傷害（含減傷係數） |
| 敵手有火球、我方**有**場怪、場怪 HP ≤ 敵場攻擊 | **部分** | 假設敵方優先解場；`threatDamageMax` 取 `max(直傷機率加權, 場攻後直傷)`，簡單級直傷權重 0.35、普通 0.5、困難 0.65 |
| 敵手有火球、我方場怪可擋一輪 | **否（該路徑）** | 改提示「下回合先解場」分支，致死鍵仍可因 B+C 路徑觸發 |
| 敵手有初級治療 | **否** | 不計對英雄傷害 |
| `ChooseEnemyHandCardToPlayIndex` 選到非傷害牌 | **否** | 僅取傷害向出牌 |

#### 4.4.3 難度與安全邊際

| 難度 | `threatDamage` 安全邊際 | 直傷權重（有場怪時） |
|------|-------------------------|---------------------|
| 簡單 | `playerHp ≤ threatDamageMax × 1.20` | 0.35 |
| 普通 | `playerHp ≤ threatDamageMax × 1.15` | 0.50 |
| 困難 | `playerHp ≤ threatDamageMax × 1.10` | 0.65 |

敵方傷害倍率：港灣簡單用 `HarborTrainingEasyBattleRules`；其餘用難度檔 `EnemyDamageMultiplier` 等現有係數。

#### 4.4.4 輸出與文案

- UI 顯示傷害值 = `Ceil(threatDamageMax)`，文案見 `lethal_next_turn`。
- **建議動作分支**（僅影響文案，不強制高亮）：
  - 模擬顯示下回合火球直傷為主 → 文案加「注意直傷」
  - 模擬顯示場攻為主 → 文案加「解場或出怪」
  - 有初級治療 + 場怪 → 偏治療

#### 4.4.5 實作備註

- 獨立類別 `HarborCombatLethalThreatEstimator`；**唯讀**盤面，不呼叫真實 `EnemyPlayCard`。
- 可複用 `ChooseEnemyHandCardToPlayIndex` 的評分結果作「敵方最可能出的傷害牌」；若該索引為 -1 則跳過 A 步。
- 單元測試建議：無場 + 敵手火球 → 必觸發；有場 + 僅場攻 → 依 HP 與攻擊力；與實際敵方回合不一致時以**預警偏早**為驗收通過。

---

## 五、文案草案

> 格式：無標點；數字與牌名用 `StoryTextStyle.Em`；必要時用 `Hi` 標「先攻」「解場」。

### 5.1 P0 文案

| 鍵名 | 文案草案 |
|------|----------|
| `lethal_next_turn` | 下回合敵方可能造成約 Em(傷害值) 點傷害 優先解場治療或出怪擋 |
| `discard_required` | 手牌超過上限 長按不要的牌拖到左側棄牌區（多張時帶剩餘張數，與入門相同生成規則） |
| `weather_fire_rain` | 緋焰時雨生效 回合結束場上怪各受 5 傷 先想清楚要不要解場 |
| `weather_holy_light` | 月華聖祈生效 本回合治療加 10 有治療或場怪時值得現在打 |
| `weather_fog` | 蒼潮夜湧生效 直傷英雄減半 有時打英雄比硬解怪划算 |
| `weather_gale` | 朔風森詠生效 本回合第一張法術加 20 可現在打或囤給關鍵法術 |
| `hand_near_cap` | 手牌接近上限 先打掉低價值牌 避免被迫棄掉關鍵牌 |

### 5.2 P1 文案

| 鍵名 | 文案草案 |
|------|----------|
| `threat_field` | 敵方場上怪壓力大 用火球術拆場或先出怪換血 |
| `no_field_before_end` | 場上還沒怪 先出一隻再結束回合 下回合才能穩定攻擊 |
| `heal_before_end` | 血量偏低 場上有怪時可先打初級治療 |
| `harbor_pressure` | 港灣訓練壓力上來了 多用防守牌和法術別只換血 |

### 5.3 簡單級補充（可併入 `harbor_pressure` 或獨立）

| 條件 | 文案 |
|------|------|
| 簡單級 + 連續 2 回合無法解場 | 對手快攻很兇 優先解場再談打臉 |

---

## 六、難度與 KPI 差異

| 難度 | 提示傾向 | 致死預警 | 手牌高亮（定案 HARBOR-COACH-003） |
|------|----------|----------|------------------------------|
| **簡單** | P0 全開；P1 僅 `harbor_pressure`、`heal_before_end` | 安全邊際 20% · 含法術模擬 | 允許（玩家可關） |
| **普通** | P0 全開；P1 全開 | 安全邊際 15% · 含法術模擬 | 允許（玩家可關） |
| **困難** | 僅 `lethal_next_turn`、`weather_active`、`discard_required` | 安全邊際 10% · 含法術模擬 | **完全關閉**（忽略設定） |

與 [`LEVEL_DESIGN_GDD.md`](LEVEL_DESIGN_GDD.md) 港灣簡單 KPI（約 70% 首通、約 10 回合）對齊：簡單可略多提示，困難減少「嘮叨」。

---

## 七、程式實作錨點（已實作 2026-05-31）

> 程式細節見 [`Assets/Scripts/HARBOR_COMBAT_COACH_IMPLEMENTATION.md`](Assets/Scripts/HARBOR_COMBAT_COACH_IMPLEMENTATION.md)、[`PROJECT_CODE_INDEX_v2.md`](Assets/Scripts/PROJECT_CODE_INDEX_v2.md) §G。

| 元件 | 建議路徑 |
|------|----------|
| 教練 UI | `HarborCombatCoachUi.cs`（鏡像 `TutorialBattleCoachUi`，獨立 `ShouldRun()`） |
| 立繪表情 | `HarborCombatCoachExpressionCatalog.cs`（`hintKey` → `HarborCoachExpression`，`Resources.Load` 四張圖 + fallback） |
| 觸發評估 | `HarborCombatCoachAdvisor.cs`（靜態或服務類；輸入 `BattleSimulationManager`） |
| 致死估算 | `HarborCombatLethalThreatEstimator.cs`（唯讀盤面 + `ChooseEnemyHandCardToPlayIndex`） |
| 手牌高亮 | 復用 `TutorialBattleHandPlayHighlight`；`HarborCombatCoachUi.ShouldAllowHandHighlight()`：**困難永遠 false** |
| 啟動掛點 | `BattleSimulationDebugUI.EnsureTutorialBattleCoach` 旁新增 `EnsureHarborCombatCoach` |
| 開關 | `BattleLaunchContext.IsHarborTrainingGroundBattle` |

**不得修改**

- 入門 `TutorialBattleCoachUi` 的觸發表與優先級（避免回歸教學戰）。

---

## 八、驗收清單

- [ ] 入門教學戰**不會**出現港灣戰術提示。
- [ ] 港灣實戰單局提示 ≤ 7 次（含重複鍵冷卻後），多數局 2～5 次。
- [ ] 致死預警在「下回合可能斷血」場景有觸發；滿血虐菜局不洗版。
- [ ] 天氣生效首回合有對應一句；同天氣不重複洗版。
- [ ] 棄牌階段提示與入門棄牌邏輯一致，但不講「什麼是棄牌」教學。
- [ ] 關閉「實戰戰術提示」後行為符合設定。
- [ ] 有未讀提示時**僅脈動**，不自動展開；點開後才顯示文案。
- [ ] 困難級**無**手牌高亮（含致死／棄牌／天氣觸發）。
- [ ] 致死預警在敵手持有火球且可能直傷時會觸發（含模擬）。
- [ ] 與右下角回合／先攻擊 HUD、天氣 UI、你的回合橫幅不重疊遮擋。
- [ ] 各 `hintKey` 觸發時頭像切到對應表情；缺圖時 fallback 不崩潰。
- [ ] 入門教練仍為單立繪，不受港灣表情表影響。

---

## 九、已定案項（原待定）

| ID | 定案 |
|----|------|
| **HARBOR-COACH-001** | **納入**敵方法術／火球意圖；淺層模擬敵方回合（出牌評分 + 攻擊 + 盤面推演），見 §4.4 |
| **HARBOR-COACH-002** | **僅脈動**；玩家**點開**才展開，不自動展開，見 §3.3 |
| **HARBOR-COACH-003** | **困難級完全關閉**手牌高亮；簡單／普通仍可依設定與觸發鍵高亮，見 §六 |
| **HARBOR-COACH-004** | **要**獨立立繪表情：頭像與台詞 UI 分開；依 `hintKey` 換圖（四表情 + fallback），見 §3.5 |

## 十、待定項（`PLANNING_OPEN_ITEMS` 候選）

| ID | 項目 |
|----|------|
| — | （港灣教練企劃項暫無） |

---

## 十一、修訂紀錄

| 日期 | 說明 |
|------|------|
| 2026-05-31 | 初版：觸發表 P0/P1、文案草案、與入門 coach 差異、UI／難度／實作錨點 |
| 2026-05-31 | 定案 HARBOR-COACH-001～003：致死含法術模擬、僅脈動點開、困難關高亮 |
| 2026-05-31 | 定案 HARBOR-COACH-004：實戰多表情立繪 + hintKey 對照表（§3.5） |
| 2026-05-31 | 程式實作：`HarborCombatCoachUi`、Advisor、LethalThreatEstimator、ExpressionCatalog、DebugUI 掛點 |
