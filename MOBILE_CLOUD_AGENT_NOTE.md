# 手機 Cloud Agent 便利貼

> 手機只是下指令與審查；實際工作在雲端 Linux VM。
> Cloud 看不到未 push 的本機檔案，也不能取代 Windows Unity Editor 驗收。

## 可以放心交辦

- C# 程式、測試、錯誤分析與範圍有限的重構。
- Markdown 企劃、規格、架構文件。
- CSV、JSON 等文字資料。
- 搜尋引用、檢查 diff、執行可用 CI。
- 建立 feature branch、commit、push、draft PR。

## 先不要只靠手機完成

- Scene／Prefab／Inspector、UI 排版、動畫、材質、音效與遊戲手感。
- Windows／Android 最終打包與實機驗證。
- 大量移動或刪除 Assets、處理 `.meta` GUID。
- 大型二進位檔或 Git LFS 歷史操作。
- 直接推 `main`、force push、合併 PR、正式部署。
- 貼出密碼、token、Unity 授權或其他 secrets。

## 每次交辦可直接貼

```text
請從目前 base branch 建立 cursor/... feature branch。
先分析再修改，只處理本次指定範圍，不要直接推 main。
不得破壞 Unity .meta/GUID，不提交快取或 secrets。
執行所有可用驗證；無法在雲端驗證的 Unity 視覺、場景、
音效、手感與打包項目，請列為「待 Windows Unity Editor 驗收」。
完成後 commit、push 並建立 draft PR，不要自行合併。
```

完整強制規範：[`AGENTS.md`](./AGENTS.md)
