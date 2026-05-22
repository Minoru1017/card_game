# 場地牌狀態索引

> 對戰中**場上區域**（怪物區／咒術區）之規則狀態、UI 回饋與程式對照。  
> 色票見 `BATTLE_FX_COLOR_SPEC.md` §6.3；攻擊／反擊動畫見 `BattleSimulationDebugUI` 攻擊 FX 區塊。

---

## 1. 場地分區

| 代碼 | 區域 | 資料來源 | UI 根物件 | 備註 |
| ---- | ---- | -------- | --------- | ---- |
| `P_MON` | 我方怪物區 | `playerField` | `playerFieldCardObj` | 可攻擊／反擊、選取光暈 |
| `E_MON` | 敵方怪物區 | `enemyField` | `enemyFieldCardObj` | 同上 |
| `P_SPL` | 我方咒術區 | `playerFieldSpell` | `playerSpellFieldCardObj` | 林可的凝視等 |
| `E_SPL` | 敵方咒術區 | `enemyFieldSpell` | `enemySpellFieldCardObj` | 同上 |

---

## 2. 狀態總表（依 `FieldCardStatusIndex.StatusId`）

| ID | 名稱 | 分區 | 顯示型態 | 位置 | 優先序 |
| -- | ---- | ---- | -------- | ---- | ------ |
| `S01` | 凝視封攻擊（敵方凝視→我方怪） | `P_MON` | 持續徽章 | 卡面中央 | 1 |
| `S02` | 凝視封攻擊（我方凝視→敵方怪） | `E_MON` | 持續徽章 | 卡面中央 | 1 |
| `S03` | 首回合不可攻擊 | `P_MON`／`E_MON` | 持續徽章 | 卡面中央 | 2 |
| `S04` | 本回合已攻擊 | `P_MON` | 持續徽章 | 卡面中央 | 3 |
| `S05` | 本回合不可反擊（我方） | `P_MON` | 持續徽章 | 卡面中央 | 4 |
| `S06` | 本回合不可反擊（敵方） | `E_MON` | 持續徽章 | 卡面中央 | 4 |
| `S07` | 主動攻擊傷害浮字 | `P_MON`／`E_MON` | 瞬時 | 卡面右上方 | — |
| `S08` | 反擊傷害浮字 | `P_MON`／`E_MON` | 瞬時 | 卡面右上方 | — |
| `S09` | 反擊行為標籤 | 反擊方怪獸 | 瞬時 | 反擊方卡上方 | — |
| `S10` | 可攻擊選取光暈 | `P_MON`／`E_MON` | 持續 | 卡面全幅 | — |
| `S11` | 林可的凝視護盾 | `P_SPL`／`E_SPL` | 持續動畫 | 咒術卡全幅 | — |
| `S12` | 場怪 HP 受傷色 | `P_MON`／`E_MON` | 持續 | `healthText` | — |

同一張**怪物牌**上，持續徽章（S01～S06）**僅顯示一則**，依 §3 優先序決定。

---

## 3. 怪物區 — 持續狀態徽章（`FieldRestrictionBadge`）

### 3.1 文案 Token

| Token | 字串 | 用途 |
| ----- | ---- | ---- |
| `BADGE_CANNOT_ATTACK` | 不可攻擊 | 主標 |
| `BADGE_CANNOT_COUNTER` | 不可反擊 | 主標 |
| `BADGE_SEC_ROUNDS` | 剩{N}回合 | 副標（凝視剩餘） |
| `BADGE_SEC_OPENING` | 首回合 | 副標 |
| `BADGE_SEC_THIS_TURN` | 本回合 | 副標（已攻擊／已反擊） |

### 3.2 我方場怪（`GetPlayerFieldMonsterStatusBadge`）

| 優先 | 條件（規則） | 主標 | 副標 | 規則變數 |
| ---- | ------------ | ---- | ---- | -------- |
| 1 | 敵方林可的凝視生效 | 不可攻擊 | 剩{N}回合 | `EnemyLinGazeActive()`、`enemyLinGazeRoundsRemaining` |
| 2 | 第 1 回合禁止攻擊 | 不可攻擊 | 首回合 | `currentRound <= 1` |
| 3 | 我方本回合已發動攻擊且仍為我方回合 | 不可攻擊 | 本回合 | `playerHasAttackedThisTurn && playerTurn` |
| 4 | 我方本回合已反擊過 | 不可反擊 | 本回合 | `playerCounterUsedThisRound` |

### 3.3 敵方場怪（`GetEnemyFieldMonsterStatusBadge`）

| 優先 | 條件（規則） | 主標 | 副標 | 規則變數 |
| ---- | ------------ | ---- | ---- | -------- |
| 1 | 我方林可的凝視生效 | 不可攻擊 | 剩{N}回合 | `PlayerLinGazeActive()`、`playerLinGazeRoundsRemaining` |
| 2 | 第 1 回合禁止攻擊 | 不可攻擊 | 首回合 | `currentRound <= 1` |
| 3 | 敵方本回合已反擊過 | 不可反擊 | 本回合 | `enemyCounterUsedThisRound` |

### 3.4 UI 行為

| 項目 | 值 |
| ---- | -- |
| GameObject | `FieldRestrictionBadge` |
| 錨點 | 卡面中央 `(0.5, 0.5)` |
| 動畫 | `FieldRestrictionBadgePulse`（輕微縮放脈動） |
| 更新 | 每幀 `UpdateFieldRestrictionBadges()`；重建場牌時 `ApplyFieldRestrictionBadge` |
| 邊框色 | 不可攻擊 → `RestrictionBadgeBorderAttack`；不可反擊 → `RestrictionBadgeBorderCounter` |

### 3.5 反擊／攻擊次數重置

| 事件 | 重置欄位 |
| ---- | -------- |
| 換局（`TryAdvanceRound`） | `playerCounterUsedThisRound`、`enemyCounterUsedThisRound` |
| 敵方回合開始（`RunEnemyTurn` 開頭） | 同上（防殘留） |

---

## 4. 怪物區 — 瞬時浮動回饋

| ID | 內容 | 觸發 | GameObject | 位置 | 色系 |
| -- | ---- | ---- | ---------- | ---- | ---- |
| S07 | `-{傷害}` | 主動攻擊命中 | `FloatingDamageLabel` | 右上方 | `DamageLabel*` |
| S08 | `-{傷害}` | 反擊命中 | `FloatingDamageLabel` | 右上方 | `CounterDamageLabel*` |
| S09 | `反擊` | 反擊動畫 | `CounterAttackLabel` | 反擊方卡上方 | `CounterLabel*` |

持續時間約：傷害浮字 **1.05s**、反擊標籤 **1.18s**（`unscaledDeltaTime`）。

---

## 5. 咒術區 — 林可的凝視（S11）

| 項目 | 說明 |
| ---- | ---- |
| 卡牌 | 咒術序號 `SpellOrdinal == 2` |
| 視覺 | `LinGazeShieldRoot` 護盾脈衝 |
| 規則 | 敵方凝視：擋**敵方**攻擊我方；我方凝視：擋**我方**攻擊敵方 |
| 剩餘回合 | 怪物區徽章 S01／S02 副標顯示；咒術區本體不顯示文字徽章 |

---

## 6. 選取光暈（S10）

| 項目 | 說明 |
| ---- | ---- |
| GameObject | `FieldSelectHaloRoot` |
| 顯示條件 | 我方回合 **且** 雙方場上都有怪獸 |
| 方法 | `UpdateFieldSelectHaloVisibility` |

---

## 7. 刷新抑制旗標（非玩家可見狀態）

攻擊動畫期間避免銷毀場牌物件，否則浮動字／徽章會失效。

| 旗標 | 用途 |
| ---- | ---- |
| `deferFieldRefreshDuringAttack` | 延後 `RefreshFieldCards` |
| `deferEnemyFieldRefresh` | 敵方場怪不重建 |
| `pendingFieldRefreshAfterAttack` | 攻擊 FX 結束後補刷 |
| `holdEnemyFieldCardUntilFireballHit` | 火球擊殺前保留敵場怪顯示 |
| `holdPlayerFieldCardUntilFireballHit` | 火球擊殺前保留我場怪顯示 |

---

## 8. 程式索引（維護用）

| 職責 | 符號 | 檔案 |
| ---- | ---- | ---- |
| 狀態 ID／文案常數 | `FieldCardStatusIndex` | `FieldCardStatusIndex.cs` |
| 徽章資料結構 | `FieldMonsterStatusBadge` | `BattleSimulationManager.cs` |
| 我方／敵方徽章查詢 | `GetPlayerFieldMonsterStatusBadge`、`GetEnemyFieldMonsterStatusBadge` | 同上 |
| 場牌重建 | `RefreshFieldCards`、`RebuildSingleFieldCard` | `BattleSimulationDebugUI.FieldCards.cs` |
| 徽章 UI | `ApplyFieldRestrictionBadge`、`UpdateFieldRestrictionBadges` | 同上 |
| 傷害浮字 | `PlayFloatingDamageNumber` | `BattleSimulationDebugUI.cs` |
| 反擊標籤 | `PlayCounterAttackLabel` | 同上 |
| 攻擊 FX 串接 | `PlayAttackFx`、`PlayCounterAttackFx` | 同上 |
| 色票 | `RestrictionBadge*`、`DamageLabel*`、`CounterLabel*` | `BattleFxColors.cs` |

---

## 9. 版本紀錄

| 日期 | 說明 |
| ---- | ---- |
| 2026-05-16 | 初版：S01～S12 總表、徽章優先序、浮動傷害／反擊、凝視護盾、程式索引 |

---

*新增場地狀態時：先於 `FieldCardStatusIndex` 加 `StatusId` 與文案常數，再實作 `BattleSimulationManager` 查詢與 `FieldCards` UI，並更新本表 §2。*
