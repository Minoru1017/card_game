# 戰前預覽謎題索引

> **企劃索引**：[PLANNING_DOCS_INDEX.md](./PLANNING_DOCS_INDEX.md) · **1-1 關卡**：[LEVEL_DESIGN_GDD.md](./LEVEL_DESIGN_GDD.md)  
> Buildbeck **戰前預覽**（`pre-war preview` 美術稿）內之謎題、解鎖條件與 UI 對照。  
> 程式常數見 `BattlePreviewPuzzleIndex.cs`；實作見 `SceneLoader.BattlePreview.cs`、`SceneLoader.BattlePreview.BossUnlockFx.cs`。

---

## 1. 謎題總表

| ID | 名稱 | 標籤 | 場景 | 持久化 | 狀態 |
| -- | ---- | ---- | ---- | ------ | ---- |
| `PZ01` | 訓練場魔王解謎 | `#訓練場` | 戰前預覽（美術模式） | 僅本場預覽開啟期間 | 已實作 |
| `PZ02` | 找出困難級 | `#找出困難級` | 戰前預覽（美術模式） | 僅本場預覽開啟期間 | 已實作 |

> 新增謎題時：先在本表與 `BattlePreviewPuzzleIndex` 登記 `PZ**`，再於 `SceneLoader.BattlePreview`（或專用 partial）實作邏輯。  
> **隨機出題**：每次開啟戰前預覽時 `RollRandomPreviewPuzzleId()`，PZ01／PZ02 各 **50%**（`RandomPreviewPuzzlePz01Weight = 0.5`）。謎題變更時會重建拱門 UI。

---

## 2. PZ01 — 訓練場魔王解謎

### 2.1 目的

玩家在**未顯示魔王級拱門**前，依隱藏順序點選四種難度（須**選取成功**，即按鈕出現選中回饋）；完成後顯示**魔王級**並可開始該難度對戰。

### 2.2 解鎖點擊序（`BossUnlockClickSequence`）

| 步驟 | 索引 | 須點選難度 | 拱門 UI 標籤 | `BattleDifficultyTier` |
| ---- | ---- | ---------- | ------------ | ---------------------- |
| 1 | 0 | 簡單 | 簡單級 | `Easy` |
| 2 | 1 | 普通 | 普通級 | `Normal` |
| 3 | 2 | 入門 | 入門級 | `Intro` |
| 4 | 3 | 困難 | 困難級 | `Hard` |

- **錯序或跳步**：任一步點到非當前步驟的難度 → `battlePreviewBossUnlockStep` 歸 **0**（從頭計）。
- **計步條件**：僅在 `ToggleAuthoredDifficultyFeedback` 回傳 **true**（本次為「選取」、非取消選取）時推進。
- **完成**：第 4 步成功後呼叫 `UnlockBossTierForPreview()`。

### 2.3 畫面狀態

| 狀態 | 四拱門列 | 魔王揭示區 | 謎題標題 | 謎題提示 |
| ---- | -------- | ---------- | -------- | -------- |
| 鎖定（預設） | 顯示 | 隱藏 | `謎題` + `#訓練場` | 請找出魔王級並通關一次 |
| 已解鎖 | 隱藏 | 顯示魔王級按鈕 | `魔王級` | 隱藏難度已顯現／目前選擇 |
| 對戰情報開啟 | 隱藏 | 隱藏 | （文案層隱藏） | — |

### 2.4 文案 Token

| Token | 內容 |
| ----- | ---- |
| `TITLE_LOCKED` | 謎題 #訓練場（Rich Text） |
| `HINT_LOCKED` | 請找出魔王級並通關一次 |
| `TITLE_UNLOCKED` | 魔王級 |
| `HINT_UNLOCKED` | 隱藏難度已顯現；目前選擇: {難度中文} |
| `HEADER_SELECT` | 選擇難易度 |
| `LEFT_TITLE` | 初次通關獎勵 |
| `RIGHT_TITLE` | 放棄解謎 |

### 2.5 版面錨點（美術稿比例 1524×883）

| 區塊 | 錨點 X | 錨點 Y | 備註 |
| ---- | ------ | ------ | ---- |
| 謎題標題／提示 | 0.28–0.72 | 標題帶 0.66–0.74；提示 0.56–0.64 | `AuthoredPuzzleCenter*` |
| 四拱門列 | 0.11–0.89 | 0.10–0.48 | `DifficultyArchRow` |
| 魔王揭示 | 0.11–0.89 | 0.14–0.52 | `BossTierReveal` |
| 對戰情報捲動 | 0.28–0.72 | 0.36–0.80 | `AuthoredDetailLayer` |

### 2.6 固定版面參數（非謎題邏輯）

| 參數 | 值 | 常數 |
| ---- | -- | ---- |
| 拱門間距 | 2 | `AuthoredArchRowSpacing` |
| 拱門大小倍率 | 2.50× | `AuthoredArchButtonHeightScale` |

### 2.7 解鎖演出（`CoUnlockBossTierRevealFx`）

完成 PZ01 點擊序後觸發（約 2.3s，`Time.unscaledDeltaTime`）：

1. **全螢幕閃光**：紫／金混合色淡入淡出  
2. **四拱門列**：`CanvasGroup` 淡出並略縮小  
3. **魔王區爆發**：擴散光環、旋轉光芒、軌道火花（錨點同 `BossTierReveal`）  
4. **魔王級按鈕**：彈出＋短暫金色脈動  
5. **謎題標題**：切換為 `TITLE_UNLOCKED` 後 punch；提示淡入  

演出期間 `battlePreviewBossUnlockAnimating` 為 true，難度拱門點擊無效。

---

## 3. PZ02 — 找出困難級

### 3.1 目的

拱門標籤**未出現困難級**；玩家須依隱藏順序點選難度（須**選取成功**），完成後**第 4 欄**拱門變為**困難級**並可對戰。

### 3.2 解鎖點擊序（`Pz02HardUnlockClickSequence`）

| 步驟 | 須點選難度 | 對應拱門（由左至右） |
| ---- | ---------- | -------------------- |
| 1 | 入門 | 第 3 欄（入門） |
| 2 | 簡單 | 第 2 欄（簡單） |
| 3 | 普通 | 第 1 欄（普通） |
| 4 | 普通 | 第 1 欄（普通，再點一次） |

- **錯序**：`battlePreviewBossUnlockStep` 歸 **0**。  
- **完成**：第 4 步成功後，第 4 欄（原兩顆入門中之最右）圖示改為**困難級**（`ApplyHardDifficultyToFourthArchSlot`）。

### 3.3 拱門由左至右（`Pz02ArchSlotsLeftToRight`）

| 位置 | 顯示 | 解鎖前實際 | 解鎖後第 4 欄 |
| ---- | ---- | ---------- | ------------- |
| 1 | 普通 | `Normal` | — |
| 2 | 簡單 | `Easy` | — |
| 3 | 入門 | `Intro` | — |
| 4 | 入門 | `Intro` | **`Hard`** |

### 3.4 文案 Token

| Token | 內容 |
| ----- | ---- |
| `TITLE_LOCKED` | 謎題 #找出困難級 |
| `HINT_LOCKED` | 請找出困難級 |
| `TITLE_UNLOCKED` | 困難級 |

### 3.5 解鎖演出

`CoUnlockPz02HardFourthArchFx`：短閃光 → 第 4 欄彈跳變色 → 換上困難級圖；**不移除**四拱門列（與 PZ01 大揭示區不同）。

---

## 4. 互動與變數對照

| 玩家操作 | 處理函式 | 主要變數 |
| -------- | -------- | -------- |
| 點難度拱門 | `OnAuthoredDifficultyTierClicked` | `battlePreviewBossUnlockStep`、`battlePreviewFeedbackDifficultyTier` |
| 開／關戰前預覽 | `ShowBattlePreviewModal` / `HideBattlePreviewModal` | `battlePreviewActivePuzzleId`（每次開啟重抽） |
| 重開預覽 | `ResetBossTierUnlockPuzzle` | `battlePreviewBossTierUnlocked = false`、步驟歸 0 |
| 點對戰情報 | `OnBattlePreviewIntelClicked` | `battlePreviewDetailVisible`、隱藏拱門區 |
| 開始對戰 | `OnBattlePreviewStartClicked` | `selectedDifficultyTier` → `BattleLaunchContext` |

---

## 5. 與難度／戰鬥的銜接

| 項目 | 說明 |
| ---- | ---- |
| 預覽選擇 | `selectedDifficultyTier`（解鎖後可為 `Boss`） |
| 進戰標籤 | `BattleLaunchContext.SetPendingDifficultyLabelZh` |
| 對戰紀錄 | `BattleDifficultyRuntime` / `GetBattleDifficultyLabelForRecord()` |
| 標準中文難度 | 入門、簡單、普通、困難、魔王（見 `PlayerProfileCsvService.StandardDifficultyLabelsZh`） |

---

## 6. 程式索引（維護用）

| 職責 | 符號 | 檔案 |
| ---- | ---- | ---- |
| 謎題 ID／序／文案 | `BattlePreviewPuzzleIndex` | `BattlePreviewPuzzleIndex.cs` |
| UI 建置與解謎流程 | `CreateAuthoredPuzzle*`、`OnAuthoredDifficultyTierClicked` | `SceneLoader.BattlePreview.cs` |
| 難度拱門資源 | `UI/Difficulty level/{Basics,Easy,Normal,Hard,Boss}` | `Resources` |
| 預覽底圖 | `UI/pre-war preview` | `SceneLoader.ResolveBattlePreviewPanelSprite` |

---

## 7. 版本紀錄

| 日期 | 說明 |
| ---- | ---- |
| 2026-05-20 | 初版：PZ01 訓練場魔王解謎、點擊序、狀態表、程式對照 |
| 2026-05-20 | PZ01 魔王解鎖華麗演出（`SceneLoader.BattlePreview.BossUnlockFx.cs`） |
| 2026-05-20 | PZ02 找出困難級；點擊序 入門→簡單→普通→普通，第 4 欄顯現困難 |
| 2026-05-20 | 開啟預覽時 PZ01／PZ02 各 50% 隨機（`RollRandomPreviewPuzzleId`） |

---

*擴充謎題時：更新 §1 總表 → `BattlePreviewPuzzleIndex` 常數 → 實作與本文件 §2 子章。*
