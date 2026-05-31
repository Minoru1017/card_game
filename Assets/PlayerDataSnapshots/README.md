# PlayerDataSnapshots（本機試玩用）

此資料夾內的 `*.csv` 為 **Unity Editor／本機試玩** 產生的玩家資料快照，**不應提交至 Git**。

| 檔案 | 說明 |
|------|------|
| `playerdata.profile_mirror.csv` | 與執行期 `playerdata.csv` 同步的鏡像（開發除錯） |
| `player_profile.csv` | 玩家資訊摘要鏡像 |

正式存檔路徑為 `Application.persistentDataPath` 下的 `playerdata.csv`（見 `DECK_SAVE_IMPLEMENTATION.md`）。

若需團隊共用範例存檔，請另建 `*.csv.example` 並手動複製，勿直接 commit 含個人進度的 csv。
