# Unity MCP 用法範例（card-game 專案）

本文件說明如何在 Cursor 透過 **CoplayDev MCP for Unity** 操作本專案，以及 **`DevAutomation`** 測試 API 的 `execute_code` 範例。

---

## 前置條件

1. Unity 2023.1 已開啟本專案  
2. **Window → MCP for Unity** → **Start Server**（狀態 Session connected）  
3. Cursor **Settings → MCP** 中 `unityMCP` 已啟用  
4. 專案根目錄已有 `.cursor/mcp.json`：

```json
{
  "mcpServers": {
    "unityMCP": {
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

5. 需 Play Mode 的操作：先在 Unity 按 **Play**，或由 MCP 的 `manage_editor` 執行 `play`

---

## 在 Cursor 怎麼下指令

### 方式 A：自然語言（推薦）

直接對 AI 說，例如：

- 「Read the Unity console messages and summarize any warnings or errors.」
- 「用 execute_code 跑 `DevAutomation.GetStatus()`」
- 「Play 模式下解鎖 M-1-2 並 LaunchM12FromStoryProgress」

### 方式 B：請 AI 呼叫 MCP 工具

常見內建工具：

| 工具 | 用途 |
|------|------|
| `manage_editor` | `play` / `stop` / `pause` |
| `read_console` | 讀 Console（error / warning / log） |
| `manage_scene` | 查作用中場景、Hierarchy |
| `find_gameobjects` | 依名稱／Component 找物件 |
| `execute_code` | 在 Editor 內執行 C#（見下文） |
| `refresh_unity` | 強制重新編譯腳本 |

---

## `execute_code` 基本格式

`execute_code` 的 `code` 參數是**方法本體**（不要寫 `using`、不要包 `class`），用 `return` 回傳字串結果。

### 查狀態

```csharp
return DevAutomation.GetStatus();
```

### 讀場景名稱

```csharp
return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
```

### 觸發按鈕（通用）

```csharp
return DevAutomation.InvokeButton("遊戲進度", requireInteractable: true);
```

```csharp
return DevAutomation.InvokeButtonExact("EnterStageButton", requireInteractable: true);
```

---

## DevAutomation API 一覽

腳本位置：`Assets/Scripts/DevAutomation.cs`（**僅 Unity Editor Play Mode**）

| 方法 | 說明 |
|------|------|
| `GetStatus()` | 場景、M-1-2 旗標、對戰狀態 |
| `GoToHall()` | 載入 hall |
| `GoToStoryProgress()` | 載入 Story progress |
| `UnlockM12OnMap()` | 測試用：解鎖地圖 M-1-2（港灣首通 + 學院畢業） |
| `SelectStoryNode("M-1-2")` | 選地圖節點 |
| `EnterSelectedStoryStage()` | 按 EnterStageButton |
| `LaunchM12FromStoryProgress(unlockIfNeeded)` | 進 M-1-2 完整流程入口 |
| `ResetM12MidRunProgress()` | 清 M-1-2 中途旗標（保留通關獎勵） |
| `InvokeButton` / `InvokeButtonExact` | 觸發 uGUI Button |
| `SkipPlot()` | 略過劇情或點一下繼續 |
| `AdvancePlotTap()` | 點 PlotTapToContinue |
| `AdvanceM12Stroll()` | 海牆散策：熱區或「前往加練」 |
| `ForceBattleWin()` | 測試用：強制本局勝利 |
| `StartM12PhaseAWinRateSim(games, seed)` | **段考 A 批次模擬**（Editor：開場景 + Play，結束寫 `Assets/SimResults/m12_phase_a_exam_winrate.md`） |
| `GetM12PhaseAWinRateSimStatus()` | 查段考 A 批次是否在跑 |
| `TryAdvanceStep()` | **自動推進一步**（見下） |
| *對戰操作* | 見下方 **「對戰自動化 API」**（`GetBattleStatus`、`PlayHandCard`、`TryBattleStep` 等） |

---

## 常用流程範例

### 1. 讀 Console

對 AI 說：

> Read the Unity console messages and summarize any warnings or errors.

或 MCP `read_console`：

- `action`: `get`
- `types`: `["error", "warning"]`
- `count`: `"20"`

### 2. 進 Play Mode

MCP `manage_editor`：

- `action`: `play`

停止：

- `action`: `stop`

### 3. 從 hall 進 Story progress

Play 後執行：

```csharp
return DevAutomation.InvokeButton("遊戲進度", requireInteractable: true);
```

或：

```csharp
DevAutomation.GoToStoryProgress();
return DevAutomation.GetStatus();
```

### 4. 測試用：解鎖並進 M-1-2

```csharp
DevAutomation.UnlockM12OnMap();
return DevAutomation.LaunchM12FromStoryProgress(unlockIfNeeded: false);
```

### 5. 自動推進一步（煙霧測試）

`TryAdvanceStep()` 會依目前畫面嘗試：

1. 進關演出進行中 → 回傳 `waiting`
2. 對戰中 → `ForceBattleWin()`
3. 結算按鈕「繼續／繼續散策」→ 點擊
4. 海牆散策 → 點熱區或 ContinueButton
5. Main Plot → 略過或點繼續
6. hall → 點「遊戲進度」

單次：

```csharp
return DevAutomation.TryAdvanceStep();
```

在 Cursor 可反覆請 AI「再執行一次 TryAdvanceStep」，直到回傳 `idle:` 或通關。

### 6. M-1-2 煙霧測試建議序列

1. `manage_editor` → `play`  
2. `return DevAutomation.UnlockM12OnMap();`  
3. `return DevAutomation.LaunchM12FromStoryProgress(false);`  
4. 重複 `return DevAutomation.TryAdvanceStep();`（劇情、段考 A、散策、加練 B、終幕）  
5. `read_console` 確認無 error  
6. `manage_editor` → `stop`

### 7. 重跑 M-1-2 中途（不重發首通獎）

```csharp
DevAutomation.ResetM12MidRunProgress();
DevAutomation.SelectStoryNode("M-1-2");
return DevAutomation.LaunchM12FromStoryProgress(false);
```

### 8. M-1-2 段考 A 勝率／通關率批次（Editor）

**須先退出 Play Mode**（批次會自動 Enter Play、跑完自動 Stop）。

Editor 選單：**Tools → M-1-2 → Win Rate Sim (Phase A Exam, …)**

MCP `execute_code`（50 局快速測）：

```csharp
return DevAutomation.StartM12PhaseAWinRateSim(50, M12PhaseAWinRateSimBootstrap.DefaultBaseSeed);
```

200 局：

```csharp
return DevAutomation.StartM12PhaseAWinRateSim(200);
```

跑完後讀 Console 摘要，或開啟報告：

`Assets/SimResults/m12_phase_a_exam_winrate.md`

（含 **勝率** 與 **段考通過率**＝勝利且御三家戰技皆觸發）

### 套用戰鬥卡牌調校（Card Tuning）

**不要用** `execute_menu_item("Card Tuning")` 或 `Tools/Battle/Card Tuning`（只是子選單，Unity 會報錯）。

**建議（MCP `execute_code`）**：

```csharp
return DevAutomation.ApplyBattleCardTuningPreset1ToOpenScene();
```

**或**完整 Editor 選單路徑（擇一）：

- `Tools/Battle/Apply Card Tuning Preset 1 to Open Scene`
- `Tools/Battle/Card Tuning/Apply Preset 1 (預設一) to Open Scene`

---

## 內建 MCP 工具範例（非 DevAutomation）

### 查作用中場景

`manage_scene`：

- `action`: `get_active`

### 找場景內所有 Button

`find_gameobjects`：

- `search_method`: `by_component`
- `search_term`: `Button`
- `include_inactive`: `true`

### 強制重新編譯（新增腳本後）

`refresh_unity`：

- `mode`: `force`
- `scope`: `scripts`
- `compile`: `request`
- `wait_for_ready`: `true`

若 `execute_code` 報 `DevAutomation does not exist`，先跑上述 refresh，等 Unity 編譯完成再試。

---

## 限制（重要）

| 能做 | 不能做 |
|------|--------|
| Play / Stop、讀 Console、查 Hierarchy | 模擬滑鼠在 Game 視窗座標點擊 |
| `Button.onClick.Invoke()` | 拖曳手牌、鬥鳥手勢 |
| **`DevAutomation` 對戰 API**（見下） | 代替玩家正常出牌通關（無 API 時） |
| 改 Component、載場景 | 操作 build 後的 standalone 玩家版 |

卡牌對戰自動化請用 **`DevAutomation` 對戰 API**（直接呼叫 `BattleSimulationManager`），不要依賴「假滑鼠」。

---

## 對戰自動化 API（`DevAutomation.Battle`）

腳本：`Assets/Scripts/DevAutomation.Battle.cs`（**僅 Unity Editor Play Mode，且場景內有 `BattleSimulationManager`**）

| 方法 | 說明 |
|------|------|
| `GetBattleStatus()` | 回合、HP、場怪、手牌索引（`*`=可出、`-`=不可） |
| `DescribePlayerHand()` | 僅手牌列表 |
| `PlayHandCard(handIndex)` | 打出指定索引（0-based） |
| `GetRecommendedPlayHandIndex()` | 查入門級 Greedy 建議出牌索引 |
| `PlayBestHandCard()` | 入門級 Greedy 自動選牌（`GetRecommendedPlayerPlayHandIndex`） |
| `EndBattleTurn()` | 結束我方回合（會自動攻擊） |
| `EndBattleTurnIfFieldOccupied()` | **場上已有怪獸**則結束回合；空場則提示先出牌 |
| `PlayerAttackNow()` | 立即場怪攻擊／直擊（測試用） |
| `AutoDiscardOnce()` | 自動棄一張（引擎建議索引） |
| `AutoDiscardAllPending()` | **自動棄完**所有待棄牌 |
| `TryBattleDiscardStep(discardAll?)` | 棄牌一步；`discardAll:true` 一次棄完 |
| `GetRecommendedDiscardHandIndex()` | 查下一張建議棄牌索引 |
| `DiscardHandCard(handIndex)` | 棄指定手牌 |
| `ResolveConsecrationChoice(bindToCurrentBishop)` | 祝聖綁定選目標（唯一需「選目標」的 UI） |
| `TryBattleStep(handIndex?, endTurnIfNoPlay?)` | **推進一步**（含祝聖→棄一張→出牌） |
| `TryBattlePumpOnce()` | **完整我方回合**（祝聖→棄光→出一張→結束） |
| `TryBattleCompleteTurn(preferredHandIndex?)` | 等同 `TryBattlePumpOnce` |
| `PlayBattleToEnd(maxPumps?)` | **背景協程依規則打完本局**（不作弊；**不再因動畫等待 timeout 中斷**；結束後自動匯出戰報） |
| `GetBattleOutcomeAnalysis()` | 分析勝負關鍵（須對戰已結束） |
| `ExportBattleRecord(filePath?)` | 匯出本局紀錄至 `Assets/SimResults/`（須對戰已結束） |
| `StartBattleAutoPlayRoutine(maxPumps?)` | `PlayBattleToEnd` 別名 |

**目標選擇說明：** 出牌／棄牌邏輯對標**入門檔敵 AI**；**每回合最多從手牌打出 1 張**（出牌前驗證索引與 `IsPlayerHandCardPlayableNow`，已出牌或場上有怪獸則直接結束回合）；怪物打出會寫入對戰歷史；結束後自動匯出戰報。

### 範例：手牌超上限棄牌

```csharp
return DevAutomation.GetBattleStatus();
return DevAutomation.AutoDiscardAllPending();
return DevAutomation.TryBattleDiscardStep();          // 棄一張
return DevAutomation.TryBattleDiscardStep(true);       // 一次棄完
```

### 範例：查狀態並打出一張牌

```csharp
return DevAutomation.GetBattleStatus();
```

```csharp
return DevAutomation.PlayHandCard(0);
```

### 範例：逐步自動對戰（MCP 反覆執行）

```csharp
return DevAutomation.TryBattleStep();
```

無牌可出且要結束回合：

```csharp
return DevAutomation.TryBattleStep(endTurnIfNoPlay: true);
```

### 範例：一整個我方回合

```csharp
return DevAutomation.TryBattleCompleteTurn();
```

### 範例：自動打完本局（不作弊）

```csharp
return DevAutomation.PlayBattleToEnd();
```

Play Mode 內背景跑；查狀態直到 `battle over`（結束後 status 會含 `exported=Assets/SimResults/...md`）：

```csharp
return DevAutomation.GetBattleAutoPlayRoutineStatus();
```

對戰結束後查勝負點／手動匯出：

```csharp
return DevAutomation.GetBattleOutcomeAnalysis();
return DevAutomation.ExportBattleRecord();
```

逐步手動推（每 call 一個完整我方回合）：

```csharp
return DevAutomation.TryBattlePumpOnce();
```

### 範例：背景自動打完本局（舊名稱仍可用）

```csharp
return DevAutomation.StartBattleAutoPlayRoutine(5000);
```

查協程狀態：

```csharp
return DevAutomation.GetBattleAutoPlayRoutineStatus();
```

---

## 疑難排解

| 現象 | 處理 |
|------|------|
| Cursor 的 unityMCP 紅點 / errored | Unity 重開 Start Server；Cursor Reload Window |
| `8080` 連不上 | MCP for Unity 視窗確認 Server 已 Start |
| `DevAutomation` 找不到 | `refresh_unity` 等編譯；確認在 **Editor Play Mode** |
| M-1-2 節點灰的 | Play 下執行 `DevAutomation.UnlockM12OnMap()` |
| `TryAdvanceStep` 一直 `idle` | `GetStatus()` 看場景；可能需手動等進關演出結束再執行 |
| `ExecuteMenuItem target for Card Tuning does not exist` | **子選單不能執行**。用完整葉節點路徑，或 `execute_code`：`return DevAutomation.ApplyBattleCardTuningPreset1ToOpenScene();` |

---

## 相關檔案

- MCP 設定：`.cursor/mcp.json`
- Unity 套件：`Packages/manifest.json` → `com.coplaydev.unity-mcp`
- 自動化 API：`Assets/Scripts/DevAutomation.cs`、`Assets/Scripts/DevAutomation.Battle.cs`
