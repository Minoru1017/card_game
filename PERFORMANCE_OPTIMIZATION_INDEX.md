# 效能優化索引（FPS 60 落地指南）

> 目標：專案在桌機與行動裝置運行時穩定維持 **60 FPS**。
> 本文件是「之後要再做同一件事，照表索引即可」的速查表：每個因素都列出**症狀 → 原因 → 解法 → 本專案實作位置 → 如何驗證**。
>
> 最後更新：2026-06-06

---

## 0. 心法（先讀）

- **先量測再優化**：用 Unity Profiler（Window ▸ Analysis ▸ Profiler）看是 CPU bound 還是 GPU bound。
  - CPU 高：腳本 `Update`、UI Canvas rebuild、GC、實例化。
  - GPU 高：overdraw（半透明堆疊）、解析度、shader 複雜度、batch 過多。
- **手機要看 frame time，不只看 FPS**：16.6ms = 60fps、33.3ms = 30fps。盯著各區塊的 ms 找最肥的。
- **每幀都跑的東西最貴**：`Update()`、`LateUpdate()` 內任何 `Find*`、字串組合、`ForceRebuildLayout`、`SetAsLastSibling` 都要盡量移除或節流。
- 參考來源：Unity 官方〈Optimizing Unity UI〉、〈Optimize your mobile game performance〉、〈Fixing performance problems〉等最佳實踐。

---

## 拖累 FPS 的 5 個主因（本次已實作）

### 因素 1｜沒有全平台鎖定目標 FPS
- **症狀**：手機預設 `Application.targetFrameRate = 30`，桌機被 vSync 綁在螢幕更新率，不是穩定 60。
- **原因**：Unity 行動平台預設 30fps；vSync 開啟時 `targetFrameRate` 被忽略。
- **解法**：啟動時 `QualitySettings.vSyncCount = 0` + `Application.targetFrameRate = 60`（或玩家設定值），且**所有平台**都套用。
- **實作位置**：
  - `Assets/Scripts/MobileRuntimePerformanceBootstrap.cs`（`BeforeSceneLoad` 對全平台套用 FPS，行動平台再加上 GPU/CPU 省電開關）
  - `Assets/Scripts/BattleCardTuningUserSettings.cs`（`SetTargetFps` / `ApplySavedTargetFps`，PlayerPrefs 記住 30/60）
  - Settings 場景提供 30/60 切換。
- **驗證**：Settings 切 60 → 進任一場景，Stats 面板（Game 視窗右上）FPS 應接近 60；切 30 應掉到 30。

### 因素 2｜非互動 UI 開著 raycastTarget
- **症狀**：場景內大量裝飾文字／圖示開著 `Raycast Target`，每次指標事件都被 `GraphicRaycaster` 逐一測試，浪費 CPU 並可能擋住真正的按鈕。
- **原因**：Unity 的 Text/Image 預設 `raycastTarget = true`，但標題、說明、背板、圖示根本不需要被點。
- **解法**：把不在任何 `Selectable`（Button/Toggle/Slider…）底下的 Graphic 關閉 `raycastTarget`。
- **實作位置**：`Assets/Scripts/UiRaycastTargetOptimizer.cs`
  - `AfterSceneLoad` + `sceneLoaded` 每次場景載入掃描一次。
  - 只關「祖先沒有 `Selectable`」的 `TMP_Text` / `Text`，互動元件與其子物件一律保留，**不會破壞點擊**。
- **驗證**：執行後看 Console 的 `UiRaycastTargetOptimizer: 關閉 N 個…`；按鈕、捲動、輸入框仍可正常操作。
- **延伸（手動）**：純裝飾的 `Image`（背景、邊框）也可在 Inspector 取消勾選 Raycast Target；但若是 Button 背板或 ScrollRect 區域請保留。

### 因素 3｜每幀重建 UI 字串 / TMP 文字
- **症狀**：對戰 HUD 每次刷新都重組字串並寫入 TMP，即使內容沒變也觸發 mesh 重建與 GC。
- **原因**：`tmp.text = "..."` 會重算文字網格；字串串接會配置暫時記憶體 → GC spike → 卡頓。
- **解法**：(1) 刷新節流（桌機 0.2s / 手機 0.33s）；(2) 內容**變更偵測**，相同就跳過賦值。
- **實作位置**：`Assets/Scripts/BattleSimulationDebugUI.cs`
  - `DesktopRefreshInterval` / `MobileRefreshInterval` 節流。
  - `lastStatusStr` / `lastDeckStr` / `lastFieldStr` 快取，內容相同則不寫入 TMP。
- **驗證**：對戰中 Profiler 的 UI/GC 區塊在「沒有狀態變化」的幀應明顯下降。

### 因素 4｜`Update`/`LateUpdate` 內每幀做階層操作或掃描
- **症狀**：每幀 `SetAsLastSibling()`、`FindObjectsByType`、同步 I/O，造成持續 CPU 開銷。
- **原因**：階層改動會使 Canvas dirty 觸發 rebuild；`Find*` 是線性掃描；磁碟讀取會 stall。
- **解法**：先判斷「需要才做」、把結果快取、把一次性讀取移出每幀。
- **實作位置**：
  - `Assets/Scripts/StoryProgressWorldMapRuntime.cs`（`LateUpdate` 只在不是最後一個 sibling 時才 `SetAsLastSibling`；log 改 `GameDevLog`）。
  - `Assets/Scripts/HallSceneFeatureBinder.cs`（資源顯示改用記憶體資料，移除進場同步 `LoadPlayerData()`）。
- **驗證**：Hall 進場無磁碟 stall；Story progress 滑動順暢、Console 不再每幀刷 log。

### 因素 5｜Canvas rebuild 過大 + 半透明 overdraw
- **症狀**：一個元素變動就讓整張大 Canvas 重建；多層全螢幕半透明圖造成 GPU overdraw。
- **原因**：同一 Canvas 內任一元件 dirty，整個 Canvas 的 batch 都會重算；overdraw 在手機 fill-rate 受限時特別痛。
- **解法**：
  - **拆分 Canvas**：把「會頻繁變動」（HUD、計時、血量）與「幾乎不動」（背景、邊框）分到不同 Canvas，縮小 rebuild 範圍。
  - **動態解析度**：GPU 吃緊時降算繪解析度撐住 60fps。
  - **降 overdraw**：減少全螢幕半透明堆疊；不需要的全螢幕 `Image` 關掉或縮小。
- **實作位置**：`Assets/Scripts/MobileAdaptiveResolutionController.cs`（依即時 FPS 用 `ScalableBufferManager.ResizeBuffers` 在 0.72~1.0 間調整算繪解析度，`DontDestroyOnLoad`）。
- **待手動處理（成本較高，建議 demo 前依時間取捨）**：
  - 把對戰 HUD 與靜態背景拆成不同 Canvas。
  - 用 Scene 視窗的 **Overdraw** 繪製模式找最紅的區域，刪掉不必要的半透明層。
- **驗證**：Profiler GPU 區塊在低階機降解析度後回到 16ms 內；Overdraw 視圖紅區減少。

---

## 通用檢查清單（之後照表套用）

- [ ] 啟動有 `vSyncCount = 0` 且 `targetFrameRate = 60`（或玩家設定）。
- [ ] 新場景的裝飾文字/圖片沒必要的 `Raycast Target` 已關（或交給 `UiRaycastTargetOptimizer`）。
- [ ] `Update/LateUpdate` 內沒有 `Find*`、沒有無條件 `SetAsLastSibling`、沒有同步 I/O。
- [ ] 每幀更新的文字有節流 + 變更偵測。
- [ ] 大 Canvas 已依「動/靜」拆分；全螢幕半透明層數量受控。
- [ ] 量測：Profiler 確認 frame time < 16.6ms；CPU/GPU 哪邊肥就針對哪邊。

## 相關檔案速查
| 主題 | 檔案 |
| --- | --- |
| 啟動 FPS / 品質預設 | `Assets/Scripts/MobileRuntimePerformanceBootstrap.cs` |
| FPS 設定（30/60、PlayerPrefs） | `Assets/Scripts/BattleCardTuningUserSettings.cs` |
| raycastTarget 自動最佳化 | `Assets/Scripts/UiRaycastTargetOptimizer.cs` |
| 動態解析度 | `Assets/Scripts/MobileAdaptiveResolutionController.cs` |
| 對戰 HUD 節流/快取 | `Assets/Scripts/BattleSimulationDebugUI.cs` |
| Story progress 階層/log | `Assets/Scripts/StoryProgressWorldMapRuntime.cs` |
| Hall 進場 I/O | `Assets/Scripts/HallSceneFeatureBinder.cs` |
