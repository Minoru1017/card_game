# Cursor Cloud Agent 專案規範

本文件適用於整個 `card_game` repository。所有在本專案工作的 Cursor Agent，包含從手機啟動的 Cloud Agent，都必須遵守。

## 一、Git 與交付流程

1. 一律從獨立的 `cursor/...` feature branch 工作，不得直接修改或推送 `main`。
2. 開始前先確認目前 branch、working tree 與遠端狀態；Cloud Agent 只能取得已推送到遠端的內容。
3. 只修改任務要求的範圍，不得順手進行無關重構、清理或格式化。
4. 完成後執行可用驗證，再 commit、push，並建立 draft PR。
5. 未經使用者明確指示，不得 force push、rebase 已共享分支、刪除 branch、合併 PR、啟用 auto-merge 或直接部署。
6. 發現遠端分支已前進、merge conflict 或來源不明的既有變更時，停止破壞性操作並回報。

## 二、Unity 資產安全

1. 專案 Unity 版本為 `2023.1.22f1`；不得擅自升級 Unity、套件或序列化格式。
2. 不得刪除或重新產生既有 `.meta` 檔與 GUID。
3. 移動、重新命名或刪除 `Assets/` 內容時，必須連同對應 `.meta`，且先確認所有引用。
4. 不得提交 `Library/`、`Temp/`、`Logs/`、`obj/`、建置輸出或其他本機快取。
5. Scene、Prefab、ScriptableObject 與其他 Unity YAML 檔應避免大規模手動改寫；無法確認序列化正確性時，只提出方案，不得猜測修改。
6. 大型音訊、影片、PSD、模型等二進位資產必須遵循 `.gitattributes` 的 Git LFS 規則。提交前確認 LFS 物件可用，避免再次產生 `GH008 unknown Git LFS objects`。

## 三、Cloud Agent 可執行範圍

適合直接處理：

- C# 程式分析、修改、重構與測試。
- Markdown 企劃、規格、架構與操作文件。
- CSV、JSON 等文字資料。
- 靜態檢查、可重現的命令列驗證及 CI。
- 明確、範圍有限且可由 diff 審查的 Unity YAML 修改。

必須標記為「待 Windows Unity Editor 驗收」：

- Scene、Prefab、Inspector 設定。
- UI 排版、解析度與 Safe Area。
- 動畫、粒子、材質、燈光。
- 音效音量、播放時機與混音。
- 遊戲手感、效能、實機操作。
- Android／Windows 實際打包結果。

Cloud Agent 不得因文字檢查通過，就宣稱上述視覺或執行結果已驗證。

## 四、測試與完成條件

1. 先找出專案既有測試與驗證方式，再選擇與修改範圍相符的命令。
2. 不得宣稱未實際執行的測試已通過。
3. 若雲端缺少 Unity Editor、合法授權、依賴或平台工具，需明確列出：
   - 已執行的檢查。
   - 無法執行的檢查及原因。
   - 使用者回到 Windows 後應驗證的步驟。
4. 測試失敗不得隱藏；需說明失敗是否由本次變更造成。
5. 交付摘要至少包含：修改內容、驗證結果、尚待本機驗收項目與風險。

## 五、安全與資料保護

1. 不得提交密碼、API key、存取 token、Unity 授權資料、個人憑證或本機 MCP 設定。
2. `.cursor/mcp.json` 等本機設定預設不納入版本控制。
3. 不得在輸出中顯示 secrets；需要憑證時使用 Cursor Secrets 或要求使用者自行設定。
4. 未經明確確認，不得執行不可逆資料遷移、大量刪除、正式環境部署或外部服務寫入。

## 六、手機委派的工作方式

收到手機端任務時：

1. 先重述任務範圍與可驗收成果；資訊不足且會影響實作方向時，提出一個聚焦問題。
2. 對高風險或跨系統任務先提出方案，不直接執行。
3. 將工作保留在 feature branch 與 draft PR，方便使用者回電腦後以 Unity Editor 驗收。
4. 若使用者要求違反本文件的操作，需指出具體風險；只有使用者清楚理解並再次明確授權時，才可執行可逆且合法的例外操作。

手機快速提醒請見 [`MOBILE_CLOUD_AGENT_NOTE.md`](./MOBILE_CLOUD_AGENT_NOTE.md)。
