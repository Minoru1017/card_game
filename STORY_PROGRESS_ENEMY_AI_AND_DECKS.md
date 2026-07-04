# Story Progress 敵方戰鬥類型與牌組表

> **用途**：1-1 入門／實戰、1-2 雙階段之敵方 AI 風格、數值與固定牌組之**單一權威對照**。  
> **卡牌 id**：`Assets/Assets/Datas/CardList.csv`  
> **法術 Key**：`-1`＝火球術（ordinal 0）、`-2`＝初級治療（ordinal 1）、`-3`＝林可的凝視（ordinal 2）  
> **敵方英雄（1-1 實戰／1-2）**：`EnemyHeroCatalog.ResolveForHarbor()` → **熱血同學**（`harbor_hot_blood_classmate`）  
> **最後同步**：程式碼快照 2026-07-05

---

## 一、總覽

| 關卡 | 模式旗標 | 難度標籤 | 敵 AI | 敵牌組來源 | 敵牌張數（典型） | 敵英雄 |
|------|----------|----------|-------|------------|------------------|--------|
| **M-1-1 入門** | `IsIntroTutorialBattle` | 入門級 | **綜合型** Balanced（教學偏置） | `IntroTutorialBattleRules.WeakEnemyDeckCardIds` | 30（池 20 張循環） | 場景預設／教學流程 |
| **M-1-1 港灣 · 簡單** | `IsHarborTrainingGroundBattle` | 簡單 | **快攻型** FastAttack | `HarborTrainingEasyBattleRules.EasyEnemyDeckCardIds` | 30（池 18 張循環） | 熱血同學 |
| **M-1-1 港灣 · 普通** | 同上 | 普通 | **快攻型** FastAttack | `HarborTrainingNormalBattleRules.NormalEnemyDeckCardIds` | 30（固定） | 熱血同學 |
| **M-1-1 港灣 · 困難** | 同上 | 困難 | **快攻型** FastAttack | `HarborTrainingHardBattleRules.HardEnemyDeckCardIds` | 30（固定） | 熱血同學 |
| **M-1-2 階段 A** | `IsM12TrioTutorialBattle` | 段考A | **綜合型** Balanced（教學偏置） | `M12PhaseABattleRules.EnemyDeckCardIds`（**鏡像玩家 15 張**） | 15 | 熱血同學 |
| **M-1-2 階段 B** | `IsM12CoachPracticeBattle` | 簡單（港灣簡單檔） | **快攻型** FastAttack | `HarborTrainingEasyBattleRules.EasyEnemyDeckCardIds` | 30（池 18 張循環） | 熱血同學 |

> **敵牌張數**：固定池會依玩家牌組張數 **循環填充**（`BattleSimulationManager` 建構敵牌堆時 `i % fixedPool.Count`）。入門／港灣／B 段玩家通常為 **30 張**；A 段鎖 **15 張**。

### AI 風格說明

| 風格 | 枚舉 | 行為摘要 |
|------|------|----------|
| **綜合型** | `Balanced` | 可出牌中選評分最高者；無明顯快攻／防禦偏置 |
| **快攻型** | `FastAttack` | 強烈優先出場怪與直傷法術，壓迫血線 |
| **防禦型** | `Defensive` | Story progress **本表關卡未使用**（自由對戰／Buildbeck 用） |

**教學偏置**（入門、M-1-2 A）：`Balanced` 時法術評分 **-26**、怪物 **-12**，降低 AI 搶攻強度（`BattleSimulationManager.ApplyEnemyAiStylePriorityTweak`）。

**快攻偏置**（港灣三檔、M-1-2 B）：依回合給怪物額外出牌加分；港灣各檔前段「軟壓力」回合數不同（見 §三）。

---

## 二、M-1-1 入門教學戰

**程式**：`IntroTutorialBattleRules` · `SceneLoader.TutorialBattle.cs`  
**玩家牌組**：`TutorialDeckApplicator` 鎖定 30 張入門牌（含修女／主教／城堡等，與敵牌不同）。

### 2.1 敵方戰鬥參數

| 項目 | 數值 |
|------|------|
| AI | **Balanced**（教學偏置） |
| 敵起始 HP | 16 |
| 敵每回合抽牌 | 1 |
| 敵傷害倍率 | ×0.72 |
| 回合上限 | 第 **10** 回合結束 → **強制玩家獲勝** |
| 超牌容許（over limit） | 0 |
| 最少法術（構築下限） | ≤2（依 Intro 難度檔） |
| 天氣 | 無 |

### 2.2 敵方牌組池（`WeakEnemyDeckCardIds`，20 張）

| id | 卡名 | 池內張數 |
|----|------|----------|
| 4 | 民兵 | 4 |
| 5 | 長弓兵 | 4 |
| 22 | 教徒 | 3 |
| 17 | 修女 | 3 |
| -2 | 初級治療 | 3 |
| 4 / 5 / 22 | 填充 | 各 1 |

**湊滿 30 張後（循環）**

| id | 卡名 | 張數 |
|----|------|------|
| 4 | 民兵 | 9 |
| 5 | 長弓兵 | 9 |
| 22 | 教徒 | 6 |
| 17 | 修女 | 3 |
| -2 | 初級治療 | 3 |

**特點**：**無火球**、無高費怪、無 SSR；以初級治療與低費怪構成最溫和壓力。

---

## 三、M-1-1 港灣實戰區

**程式**：`HarborTrainingEasy/Normal/HardBattleRules` · `HarborTrainingTierConfig` · `SceneLoader.HarborTraining.cs`  
**三檔 AI 皆預設** `FastAttack`（`HarborTrainingTierConfig` 建構子預設值）。

### 3.1 三檔數值對照

| 項目 | 簡單 | 普通 | 困難 |
|------|------|------|------|
| AI | FastAttack | FastAttack | FastAttack |
| 敵起始 HP | 17 | 15 | 18 |
| 敵傷害倍率 | ×0.78 | ×0.66 | ×0.74 |
| 前段慢抽（每回合 1 張） | 至第 4 回合 | 至第 5 回合 | 至第 3 回合 |
| 之後每回合抽牌 | 2 | 2 | 2 |
| 快攻怪物加分（前段→後段） | +6 → +16 | +3 → +6 | +5 → +12 |
| 第 10 回合必勝 | **有** | 無 | 無 |
| 超牌容許 | 2 | 2 | 3 |
| 最少法術 | 1 | 1 | 2 |

### 3.2 簡單 · 敵牌組

**池（18 張）** — `EasyEnemyDeckCardIds`

| id | 卡名 | 池內張數 |
|----|------|----------|
| 4 | 民兵 | 4 |
| 5 | 長弓兵 | 3 |
| 22 | 教徒 | 3 |
| 17 | 修女 | 2 |
| -2 | 初級治療 | 2 |
| -1 | 火球術 | 1 |
| 4 / 5 / 22 | 填充 | 各 1 |

**湊滿 30 張後**

| id | 卡名 | 張數 |
|----|------|------|
| 4 | 民兵 | 9 |
| 5 | 長弓兵 | 7 |
| 22 | 教徒 | 7 |
| 17 | 修女 | 4 |
| -2 | 初級治療 | 2 |
| -1 | 火球術 | 1 |

法術比例 **10%**；**無 SSR**。

### 3.3 普通 · 敵牌組（固定 30 張）

`NormalEnemyDeckCardIds`

| id | 卡名 | 張數 |
|----|------|------|
| 4 | 民兵 | 7 |
| 5 | 長弓兵 | 8 |
| 22 | 教徒 | 7 |
| 17 | 修女 | 2 |
| 14 | 主教 | 2 |
| 6 | 王國騎兵 | 1 |
| -2 | 初級治療 | 2 |
| -1 | 火球術 | 1 |

法術比例 **10%**；**無 SSR 四騎（8～11）**。

### 3.4 困難 · 敵牌組（固定 30 張）

`HardEnemyDeckCardIds`

| id | 卡名 | 張數 |
|----|------|------|
| 4 | 民兵 | 6 |
| 5 | 長弓兵 | 6 |
| 22 | 教徒 | 5 |
| 17 | 修女 | 2 |
| 14 | 主教 | 3 |
| 6 | 王國騎兵 | 3 |
| 16 | 宗教審判官 | 1 |
| -2 | 初級治療 | 2 |
| -1 | 火球術 | 2 |

法術比例 **13%**；**無 SSR 四騎**；畢業證僅困難首通發放。

---

## 四、M-1-2 海牆巡邏 · 階段 A（御三家應用）

**程式**：`M12PhaseDeckCatalog.PhaseADeckCardIds` · `M12PhaseABattleRules` · `SceneLoader.M12SeawallPatrol.ConfigureM12PhaseABattlePending`  
**旗標**：`IsM12TrioTutorialBattle`

### 4.1 敵方戰鬥參數

| 項目 | 數值 |
|------|------|
| AI | **Balanced**（教學偏置） |
| 敵起始 HP | 15 |
| 敵每回合抽牌 | 1 |
| 敵傷害倍率 | ×0.85 |
| 回合上限 | 第 **12** 回合（逾限邏輯見 `M12PhaseABattleRules.MaxRoundsInclusive`） |
| 超牌容許 | 0 |
| 最少法術 | 1 |
| 教練提示 | **無**（A 段不啟用 `M12BattleCoachUi`） |

### 4.2 敵方牌組（15 張 · **與玩家完全相同 · 鏡像對局**）

| id | 卡名 | 張數 | 備註 |
|----|------|------|------|
| 13 | 國王 | 2 | 庭訓號令 |
| 12 | 王后 | 2 | 王室庇護 |
| 4 | 民兵 | 4 | 列陣 |
| 5 | 長弓兵 | 2 | 低費鋪場 |
| 22 | 教徒 | 2 | 填場 |
| -2 | 初級治療 | 2 | 保命 |
| -1 | 火球術 | 1 | 解場 |

**合計**：怪物 12 ＋ 法術 3 ＝ **15**。  
**刻意不含**：修女、主教、城堡、騎兵、護林鹿。

---

## 五、M-1-2 海牆巡邏 · 階段 B（教會三張加練）

**程式**：`M12PhaseDeckCatalog.PhaseBDeckCardIds`（**玩家 20 張**）· 敵方沿用 **`HarborTrainingEasyBattleRules`** · `ConfigureM12PhaseBBattlePending`  
**旗標**：`IsM12CoachPracticeBattle`

### 5.1 敵方戰鬥參數

與 **§3.2 港灣簡單** 相同（敵 HP 17、傷害 ×0.78、FastAttack、第 10 回合必勝等）。  
**教練**：`M12BattleCoachUi` 啟用。

### 5.2 敵方牌組

同 **§3.2 港灣簡單敵牌**（`EasyEnemyDeckCardIds` 循環至 30 張）。  
**不是**玩家 B 段 20 張牌表。

### 5.3 玩家 B 段鎖定牌組（20 張 · 對照用）

敵方不用此表；僅供關卡設計對照。

| id | 卡名 | 張數 |
|----|------|------|
| 17 | 修女 | 2 |
| 14 | 主教 | 2 |
| 7 | 城堡 | 1 |
| 22 | 教徒 | 3 |
| -2 | 初級治療 | 3 |
| 13 | 國王 | 1 |
| 12 | 王后 | 1 |
| 4 | 民兵 | 2 |
| 5 | 長弓兵 | 2 |
| 6 | 王國騎兵 | 1 |
| -1 | 火球術 | 1 |

**合計**：怪物 14 ＋ 法術 6 ＝ **20**。

---

## 六、程式對照索引

| 關卡 | 敵 AI 注入 | 敵牌 id 陣列 | 執行期規則 |
|------|------------|--------------|------------|
| M-1-1 入門 | `SceneLoader.ConfigureIntroTutorialBattlePending` | `IntroTutorialBattleRules.WeakEnemyDeckCardIds` | `IntroTutorialBattleRules` |
| M-1-1 港灣 | `SceneLoader.ApplyHarborTrainingPendingConfig` | `HarborTraining*BattleRules.*EnemyDeckCardIds` | `HarborTrainingDifficultyRuntime` |
| M-1-2 A | `ConfigureM12PhaseABattlePending` | `M12PhaseABattleRules.EnemyDeckCardIds` | `M12PhaseABattleRules` |
| M-1-2 B | `ConfigureM12PhaseBBattlePending` | `HarborTrainingEasyBattleRules.EasyEnemyDeckCardIds` | `HarborTrainingEasyBattleRules`（經 `HarborTrainingDifficultyRuntime`） |

| AI 風格 enum | 定義 |
|--------------|------|
| `EnemyAiPlayStyle` | `Assets/Scripts/EnemyAiPlayStyle.cs` |
| 風格文案 | `EnemyAiPlayStyleCatalog` |

---

## 七、相關文件

| 文件 | 內容 |
|------|------|
| [`HARBOR_1-1_VS_TRAINING_GROUND_DIFFICULTY.md`](HARBOR_1-1_VS_TRAINING_GROUND_DIFFICULTY.md) | 港灣實戰 vs Buildbeck 訓練場、KPI 對照 |
| [`LEVEL_DESIGN_M-1-2.md`](LEVEL_DESIGN_M-1-2.md) | M-1-2 關卡目標、任務欄、中段散策 |
| [`LEVEL_DESIGN_GDD.md`](LEVEL_DESIGN_GDD.md) | 1-1 港灣三難度企劃總表 |
