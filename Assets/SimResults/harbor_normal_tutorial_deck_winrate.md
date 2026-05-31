# Harbor Normal Win Rate (Tutorial default deck)

## 企劃 KPI

- **目標**：入門預設 30 張牌組，港灣 **普通** 首通約 **60%**
- **回合**：不限（無簡單檔第 10 回合必勝）

## 程式定案（`HarborTrainingNormalBattleRules.cs`）

| 項目 | 普通實戰 |
|------|----------|
| 敵 HP | 15 |
| 敵傷害倍率 | 0.66 |
| 敵抽牌 | 第 1～5 回合 1 張／回合，之後 2 張 |
| 快攻 | 前 5 回合 +3，之後 +6 |
| 敵牌組 | 簡單弱牌 + 主教、騎兵（無 SSR） |

## 批次模擬校準（自動出牌 AI）

| 版本 | 勝率 (200 局) | 備註 |
|------|---------------|------|
| 舊 Buildbeck Normal 池 | 0% | 含 SSR 四騎 |
| 首版港灣專用規則 | 7～10% | 仍偏難 |
| **定案數值 (0.66 / 15 HP)** | **~15.5%** | 種子 20260531 |

自動 AI 不會保留治療／拆場時機，**顯著低於真人**。請以 5～10 局真人試玩調整 `EnemyDamageMultiplier`（建議區間 0.62～0.70）。

## 重跑模擬

`Tools/Harbor/Win Rate Sim (Normal, Tutorial Deck, 200 games)`
