# card-game 程式碼品質分級掃描報告

> **掃描日期**: 2026-06-21  
> **掃描範圍**: 243 .cs  
> **來源**: E:\School\Grade_5_2\card-game\Assets\Scripts  
> **用途**: 僅供學習與架構研究

---

## Summary

| Tier | Count | Pct |
|:----:|:-----:|:---:|
| Excellent (優秀) | 0 | 0% |
| Good (不錯) | 6 | 2.5% |
| Fair (尚可) | 218 | 89.7% |
| Poor (不太好) | 8 | 3.3% |
| Bad (差勁) | 11 | 4.5% |

---

## Subsystems

| Subsystem | Tier | Files |
|-----------|:----:|:-----:|
| 其他 | Fair (尚可) | 99 | avg 2.98 |
| 戰鬥 / 回合 | Fair (尚可) | 65 | avg 2.82 |
| 卡牌 / 牌組 | Fair (尚可) | 58 | avg 2.97 |
| 存檔 | Fair (尚可) | 6 | avg 3 |
| Manager | Bad (差勁) | 6 | avg 1.83 |
| 資料載入 | Fair (尚可) | 4 | avg 3 |
| UI 控制器 | Fair (尚可) | 3 | avg 3 |
| 地圖 / 關卡 | Poor (不太好) | 2 | avg 2.5 |

---

## Top modules (Good/Excellent)

| Class | Lines | Tier |
|-------|:-----:|:----:|
| `CardArtLibrary` | 75 | Good (不錯) |
| `UiFontLibrary` | 50 | Good (不錯) |
| `DeckManager.ScenePersistence` | 44 | Good (不錯) |
| `BirdDuelRhythmSync` | 218 | Good (不錯) |
| `Audio/AudioLibrary` | 157 | Good (不錯) |
| `UiSpriteLibrary` | 156 | Good (不錯) |

---

## Watch list (Poor/Bad)

| Class | Lines | Empty | Tier |
|-------|:-----:|:-----:|:----:|
| `PlayerProfileCsvService` | 951 | 0 | Poor (不太好) |
| `HallSceneFeatureBinder` | 92 | 0 | Poor (不太好) |
| `StoryProgressWorldMapRuntime` | 890 | 0 | Poor (不太好) |
| `PlotUiOverlayCleanup` | 71 | 0 | Poor (不太好) |
| `LoginSceneController` | 699 | 1 | Poor (不太好) |
| `HallSceneAutoLayout` | 238 | 0 | Poor (不太好) |
| `TutorialPlotStarterDeckNotify` | 214 | 0 | Poor (不太好) |
| `ClickCard` | 191 | 0 | Poor (不太好) |
| `MainPlotSceneController` | 936 | 0 | Bad (差勁) |
| `TutorialBattleCoachUi` | 897 | 0 | Bad (差勁) |
| `SettingsSceneController` | 877 | 0 | Bad (差勁) |
| `HarborCombatCoachUi` | 823 | 0 | Bad (差勁) |

---

## Largest files

- `BattleSimulationManager` 4134 lines (Bad (差勁))
- `BattleSimulationDebugUI` 3867 lines (Bad (差勁))
- `DeckManager` 3863 lines (Bad (差勁))
- `SceneLoader.BattlePreview` 2458 lines (Bad (差勁))
- `BattleSimulationDebugUI.Settlement` 1553 lines (Bad (差勁))
- `StoryProgressSceneController` 1358 lines (Bad (差勁))
- `GlobalNavRuntime` 1341 lines (Bad (差勁))
- `PlayerProfileCsvService` 951 lines (Poor (不太好))

---

*Auto report from tools/Scan-CodeQuality.ps1*
