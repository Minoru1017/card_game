# 遊戲數值設計與 AI／難度強度 — 參考文獻與網路資源目錄

| 項目 | 內容 |
|------|------|
| **用途** | 畢業專題報告「文獻探討／相關研究／設計依據」之**可查證外部來源**清單 |
| **本專案對照** | [`DIFFICULTY_AND_AI_DESIGN.md`](DIFFICULTY_AND_AI_DESIGN.md)（本遊戲實作）· [`ENEMY_AI_DECISION_TREE.md`](ENEMY_AI_DECISION_TREE.md) |
| **收錄原則** | 公開網頁或學術 PDF；與**數值平衡、難度曲線、敵方 AI、動態難度**相關；附簡述與本專題關聯 |
| **最後更新** | 2026-05-20 |

> **免責**：連結為檢索當下可達之公開資源；GDC Vault 多為付費會員完整影片，表中仍列標題供正式引用。引用前請自行確認學校引用格式（APA／IEEE 等）。

---

## 1. 建議閱讀順序（給報告執筆）

| 順序 | 類別 | 目的 |
|:----:|------|------|
| 1 | §3 難度設計觀念 | 界定「困難 vs 不公平」「挑戰 vs 懲罰」 |
| 2 | §2 數值／平衡方法 | 支撐「設計指數 + 公式映射」非隨機調參 |
| 3 | §4 卡牌／TCG | 對標組牌、曲線、稀有度 |
| 4 | §5 AI 與難度強度 | 對標 Greedy／囤牌、DDA、敵 AI 設計 |
| 5 | §6 學術與百科 | 文獻回顧一節的英文來源 |

---

## 2. 數值設計與遊戲平衡（通用）

| 標題 | 作者／來源 | 類型 | 重點摘要 | 與本專題關聯 |
|------|------------|------|----------|--------------|
| [Balancing Your Game: A Formula-Driven Approach](https://gdcvault.com/play/1023865/Balancing-Your-Game-A-Formula) | GDC Vault | 演講 | 用公式處理任務長度、物價、敵人血量等，減少盲目試錯 | 呼應 `BuildDifficultyConfig` 的 Lerp 映射 |
| [Fun or Frustration: Game Balance Pitfalls and Recipes](https://gdcvault.com/play/1021413/Fun-or-Frustration-Game-Balance) | GDC Vault | 演講 | 難度、節奏、成長曲線常見陷阱與解法 | 報告「設計限制」可對照 |
| [Slay the Spire: Metrics Driven Design and Balance](https://gdcvault.com/play/1026309/-Slay-the-Spire-Metrics) | GDC Vault | 演講 | 早期即用指標驅動平衡，EA 期持續數據迭代 | 未實作 DDA；可作未來工作對照 |
| [Design Tips: Power Curves](https://www.cloudfallstudios.com/blog/2018/5/14/design-tips-power-curves-i) | Cloudfall Studios | 部落格 | 資源—效益曲線；獨立強度 vs 情境強度 | 稀有度加權 + 牌組協同 |
| [筆記：基礎遊戲設計 - 遊戲平衡](https://nagachiang.github.io/notes-design-101-balancing-games-chinese) | Nagachiang（中譯筆記） | 中文筆記 | 強度曲線、費米估計、三連擊法（overshoot） | 實務調參方法論 |
| [直接設計體驗的數值平衡](https://blog.chosenconcept.dev/tw/posts/2022/12/0016-direct-balancing) | Chosen Concept | 中文部落格 | 以**擊殺時間 K** 等體感指標反推 HP／攻擊／間隔 | 怪物 ATK／HP 設計可引用 |
| [Game balance (Wikipedia)](https://en.wikipedia.org/wiki/Game_balance) | Wikipedia | 百科 | 平衡定義、PvE／PvP、雪崩效應概論 | 文獻回顧入門 |

---

## 3. 難度設計與玩家體感（非純調數值）

| 標題 | 作者／來源 | 類型 | 重點摘要 | 與本專題關聯 |
|------|------------|------|----------|--------------|
| [Difficulty Curves](https://www.gamedeveloper.com/design/difficulty-curves) | Toby Schadt, *Game Developer* | 文章 | 線性／波浪／對數等難度曲線類型與取捨 | 五檔離散難度可畫成階梯曲線 |
| [UX Summit: Difficult Games by Data and Design](https://www.gdcvault.com/play/1034838/UX-Summit-Difficult-Games-by) | GDC Vault | 演講 | 測試、分析、用研綜合找「剛剛好」難度 | 口試：目前缺玩家實證 |
| [1000 Hours of Difficulty: How Destiny Builds Systemic Challenge](https://www.gdcvault.com/play/1028239/1000-Hours-of-Difficulty-How) | GDC Vault | 演講 | 多類別難度、系統性挑戰、長期調校 | 長期營運遊戲對照 |
| [遊戲誌 LV33｜難度設計：讓玩家痛苦但又不放棄](https://madefrom.hk/%e9%81%8a%e6%88%b2%e8%aa%8c-lv33%ef%bd%9c%e9%9b%a3%e5%ba%a6%e8%a8%ad%e8%a8%88%ef%bc%9a%e8%ae%93%e7%8e%a9%e5%ae%b6%e7%97%9b%e8%8b%a6%e4%bd%86%e5%8f%88%e4%b8%8d%e6%94%be%e6%a3%84-difficulty-design-ma/) | 遊戲誌／madefrom.hk | 中文專欄 | 可學習性、回饋、短循環、隱性輔助 | 報告「體驗目標」 |
| [游戏难度设计深度解析：为何“困难”不等于“不公平”](https://indiegamed.com/indiegame-news/game-difficulty-design-fairness-analysis) | indiegamed.com | 中文文章 | 反對純堆血量；應改機制與 AI | 呼應 Scheming AI 與斬殺規則 |
| [When Difficult Is Fun（Extra Credits）](https://www.youtube.com/watch?v=ea6UuRTjkKs) | Extra Credits | 影片 | **Challenging vs Punishing** 區分 | 口試好用的一頁概念 |
| [《游戏难度评估进阶指南…》](https://juejin.cn/post/7583933107689193535) | 掘金社群 | 中文長文 | 通關率以外：策略多樣性、容錯、難度—沉浸協同 | 若做問卷／實驗可參考指標 |

---

## 4. 卡牌／TCG 數值與牌組強度

| 標題 | 作者／來源 | 類型 | 重點摘要 | 與本專題關聯 |
|------|------------|------|----------|--------------|
| [Hearthstone Card Balance Philosophy](https://www.engadget.com/2014-01-20-hearthstone-card-balance-philosophy.html) | Engadget（訪談 Blizzard） | 新聞／訪談 | 互動性、OTK、環境多樣性 | 產業對照（非獨立） |
| [The Importance of Mana Curve in Arena](https://dotesports.com/hearthstone/news/importance-mana-curve-arena-29855) | Dot Esports | 文章 | 法力曲線與構築 | 本專題 30 張牌組上限 |
| [HearthMath: Making Curves](https://dotesports.com/hearthstone/news/hearthmath-making-curves-29955) | Dot Esports | 文章 | 曲線與數學直觀 | 數值章節附錄 |
| [Proposed Balance Model for Card Deck Measurement in Hearthstone](https://link.springer.com/article/10.1007/s40869-018-0072-9) | *The Computer Games Journal* (Springer) | **學術論文** | 牌組強度量化模型 | 碩士論文可引英文期刊 |
| [GAMEPLAY_AND_RULES.md](./GAMEPLAY_AND_RULES.md) | 本專題 | 內部文件 | CSV 卡牌、組牌規則、天氣 | 實作依據 |

---

## 5. 敵方 AI、強度與動態難度（DDA）

| 標題 | 作者／來源 | 類型 | 重點摘要 | 與本專題關聯 |
|------|------------|------|----------|--------------|
| [Enemy design and enemy AI for melee combat systems](https://www.gamedeveloper.com/design/enemy-design-and-enemy-ai-for-melee-combat-systems) | *Game Developer* | 文章 | 敵人設計與近戰 AI 行為、可讀性 | 回合制 TCG 可類比「可讀意圖」 |
| [How to Balance Enemy AI…?](https://gamedev.stackexchange.com/questions/213036/how-to-balance-enemy-ai-to-provide-a-challenge-without-frustrating-the-player) | Game Development SE | 問答 | 挑戰但不挫折；預告（tell）與公平感 | 斬殺／囤牌需可理解 |
| [Dynamic game difficulty balancing (Wikipedia)](https://en.wikipedia.org/wiki/Dynamic_game_difficulty_balancing) | Wikipedia | 百科 | DDA 定義、啟發式指標、參數調整、橡皮筋效應 | 本專題**非**即時 DDA |
| [AI for Dynamic Difficulty Adjustment in Games (Hamlet)](https://users.cs.northwestern.edu/~hunicke/pubs/Hamlet.pdf) | Robin Hunicke | **學術 PDF** | 經典 DDA／玩家建模思路 | 文獻回顧：與本專題五檔靜態對比 |
| [DIFFICULTY_AND_AI_DESIGN.md](./DIFFICULTY_AND_AI_DESIGN.md) | 本專題 | 內部文件 | 五檔難度、Greedy／Scheming、回合制決策 | **本系統實作說明** |
| [ENEMY_AI_DECISION_TREE.md](./ENEMY_AI_DECISION_TREE.md) | 本專題 | 內部文件 | `ChooseEnemyHandCardToPlayIndex` 決策樹 | 附錄／實作細節 |

### 5.1 檢索用關鍵字（自行擴充）

**英文**：`game balance formula`, `difficulty curve design`, `dynamic difficulty adjustment`, `enemy AI heuristic`, `card game power curve`, `metrics driven balance`  

**中文**：`遊戲數值平衡`, `難度曲線`, `動態難度`, `敵人 AI 設計`, `卡牌遊戲 平衡`

---

## 6. 學術檢索建議（圖書館／Google Scholar）

| 資料庫 | 建議查詢式 |
|--------|------------|
| Google Scholar | `"dynamic difficulty adjustment" video game` |
| Google Scholar | `"game balance" "card game" OR TCG` |
| IEEE Xplore | `NPC difficulty scaling game AI` |
| 華藝／Airiti | `遊戲 難度 調整 人工智慧` |

可與已收錄之 Springer 爐石牌組模型、Hamlet PDF 一併列入參考文獻。

---

## 7. 報告引用範例（APA 7 示意，請依系所格式修改）

**GDC 演講（影片）**

> GDC Vault. (n.d.). *Balancing your game: A formula-driven approach* [Video]. Game Developers Conference. https://gdcvault.com/play/1023865/Balancing-Your-Game-A-Formula

**期刊論文**

> Author. (2018). Proposed balance model for card deck measurement in Hearthstone. *The Computer Games Journal*. https://doi.org/10.1007/s40869-018-0072-9

**技術部落格**

> Chosen Concept. (2022, December). 直接設計體驗的數值平衡. https://blog.chosenconcept.dev/tw/posts/2022/12/0016-direct-balancing

**本專題實作（建議標為「系統文件」或「未出版技術報告」）**

> [Your Team]. (2026). *Difficulty and enemy AI design* (Internal technical report). Graduation project repository.

---

## 8. 與本專題定位的對照表（寫討論章用）

| 外部常見做法 | 本專題現況 | 文獻依據方向 |
|--------------|------------|--------------|
| 動態難度（DDA）依勝率調參 | 開戰前五檔手選 | Hamlet；Wikipedia DDA |
| 數據驅動長期調平衡 | 設計指數 + 固定 Profile | Slay the Spire GDC |
| 深度搜尋／學習型 AI | 啟發式決策樹 + 評分 | Game Developer 敵 AI 文 |
| 卡牌強度模型 | CSV + 稀有度加權 | Springer 爐石模型；Power Curves |
| 體感指標（TTK 等） | 部分（斬殺、血線門檻） | Chosen Concept 擊殺時間 |

---

## 9. 相關專題文件

| 文件 | 說明 |
|------|------|
| [MARKET_ANALYSIS_SOURCES.md](./MARKET_ANALYSIS_SOURCES.md) | 商業化獨立卡牌遊戲作品來源 |
| [MARKET_ANALYSIS_FIVE_GAMES.md](./MARKET_ANALYSIS_FIVE_GAMES.md) | 五款遊戲分析（若有） |
| [CARD_PROFICIENCY_GDD.md](./CARD_PROFICIENCY_GDD.md) | 熟練度／技能揭露 GDD |
