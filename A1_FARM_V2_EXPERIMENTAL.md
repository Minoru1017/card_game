# A-1 三畦耕作 Experimental V2

> 狀態：實驗代碼，預設停用，不取代現行 A-1 流程。

## 目的

現行 `SideQuestA1TideIslandFarmOverlay` 同時負責農事規則、步驟狀態、輸入判定、UI 建構、動畫、Coroutine、文案與完成回呼。V2 先把「農事規則與狀態」抽成不依賴 Unity 的純 C# 狀態機，供後續測試、效能比較與漸進式整合。

## 隔離保證

- 舊檔案 `Assets/Scripts/SideQuestA1TideIslandFarmOverlay.cs` 未修改。
- `SideQuestA1Flow`、獎勵、存檔與 Scene 未引用 V2。
- V2 assembly 的 `autoReferenced` 為 `false`。
- V2 assembly 需要 `CARDGAME_A1_FARM_V2` define；專案目前沒有啟用此 define。
- 沒有修改 `ProjectSettings`，所以一般 Editor、Play、Build 仍只使用舊程式。
- V2 沒有 MonoBehaviour、Scene、Prefab、UI、音效、存檔或獎勵副作用。

## 新增內容

| 檔案 | 用途 |
|------|------|
| `Assets/Experimental/A1FarmV2/A1FarmStateMachineV2.cs` | 純 C# 農事狀態機、集中式閾值與快照 |
| `Assets/Experimental/A1FarmV2/CardGame.A1Farm.Experimental.asmdef` | 預設停用且不自動引用的 assembly |
| `Assets/Editor/Experimental/A1FarmV2/A1FarmStateMachineV2Tests.cs` | Legacy MVP 行為對照測試 |
| `Assets/Editor/Experimental/A1FarmV2/CardGame.A1Farm.Experimental.Tests.asmdef` | 僅 Editor、僅 define 開啟時編譯的測試 assembly |

## 舊版與 V2 差異

| 項目 | 舊版 | Experimental V2 |
|------|------|-----------------|
| 核心結構 | 約 1,780 行 MonoBehaviour | 獨立純 C# 狀態機 |
| UI／規則 | 同一類別 | 完全分離；V2 無 UI |
| Unity 相依 | `Time`、Coroutine、RectTransform、TMP、Image | 無 UnityEngine 相依 |
| 閾值 | 分散於多個 private handler | 集中於 `A1FarmConfig.LegacyMvp` |
| 進度儲存 | 多組 bool、陣列、HashSet、counter | `ulong` bit mask、counter 與 snapshot |
| 每次輸入成本 | Drag 時可能遍歷並重繪 20 格 | 狀態更新為 O(1)，不產生 UI |
| 時間輸入 | `Time.unscaledTime`／Coroutine | 呼叫端傳入 beat offset／elapsed seconds |
| 隨機性 | Unity Random 與動態熱區 | Core 不管理視覺位置與隨機熱區 |
| 測試 | 無 A-1 農事單元測試 | 閾值、節拍、鐮刀、跳過、完整流程測試 |
| 執行狀態 | 正式使用 | 預設不編譯、不接線 |

## Legacy 行為對照

V2 首版刻意保留現行 MVP 規則，方便比較：

- 黑麥犁溝：20 格中完成 16 格。
- 壓土節拍：誤差不超過 0.35 秒，成功 2 次。
- 鐮刀：左右各跨越 80 單位，可跨多次輸入累積。
- 休耕土塊：3 個；鹽網：14 格；海蓬：5 叢。
- 泡種：目前維持點擊 3 次。
- 點種：6 格；引水：6 個不重複格；除藤：2 處；收莢：3 處。
- 等待動畫規則：累計 1.6 秒。
- 任意未結束步驟都可 Skip；終止後拒絕其他輸入。

目前企劃中的「泡種長按」及「L 形連通水路」與舊 MVP 實作不同。V2 不擅自改玩法，未來應另加 Spec-Aligned config，而不是偷偷改變 Legacy parity。

## 暫時啟用測試

僅在獨立測試 branch 或本機 Unity Editor：

1. 在 Player Settings 的 Scripting Define Symbols 暫時加入 `CARDGAME_A1_FARM_V2`。
2. 開啟 Unity Test Runner 的 EditMode 測試。
3. 執行 `CardGame.Experimental.Tests.A1.A1FarmStateMachineV2Tests`。
4. 測試後移除 define，且不要把 ProjectSettings 的 define 變更提交到正式 branch。

## 尚未完成

- 沒有 V2 Overlay adapter。
- 沒有替換 `SideQuestA1Flow` 的入口。
- 沒有 Scene／Prefab 變更。
- 沒有將狀態機結果轉換為正式獎勵或存檔。
- 尚未在 Unity 2023.1.22f1 Test Runner 實際編譯與執行。
- 尚未量測手機裝置上的 GC、CPU 或 UI rebuild。

在上述項目完成且通過 Windows Unity Editor 驗收前，V2 不得取代舊程式。
