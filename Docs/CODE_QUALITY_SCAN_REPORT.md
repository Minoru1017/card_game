# card-game 程式碼品質分級掃描報告

> **掃描日期**: 2026-07-04  
> **掃描範圍**: 273 .cs  
> **來源**: E:\School\Grade_5_2\card-game\Assets\Scripts  
> **用途**: 僅供學習與架構研究

---

## Summary

| Tier | Count | Pct |
|:----:|:-----:|:---:|
| Excellent (優秀) | 0 | 0% |
| Good (不錯) | 6 | 2.2% |
| Fair (尚可) | 245 | 89.7% |
| Poor (不太好) | 12 | 4.4% |
| Bad (差勁) | 10 | 3.7% |

---

## Subsystems

| Subsystem | Tier | Files |
|-----------|:----:|:-----:|
| 其他 | Fair (尚可) | 113 | avg 2.99 |
| 戰鬥 / 回合 | Fair (尚可) | 74 | avg 2.82 |
| 卡牌 / 牌組 | Fair (尚可) | 62 | avg 2.95 |
| Manager | Bad (差勁) | 7 | avg 1.86 |
| 存檔 | Fair (尚可) | 6 | avg 3 |
| 資料載入 | Fair (尚可) | 5 | avg 3 |
| 地圖 / 關卡 | Fair (尚可) | 3 | avg 2.67 |
| UI 控制器 | Fair (尚可) | 3 | avg 3 |

---

## Top modules (Good/Excellent)

| Class | Lines | Tier |
|-------|:-----:|:----:|
| `CardArtLibrary` | 82 | Good (不錯) |
| `UiFontLibrary` | 54 | Good (不錯) |
| `DeckManager.ScenePersistence` | 44 | Good (不錯) |
| `BirdDuelRhythmSync` | 221 | Good (不錯) |
| `Audio/AudioLibrary` | 165 | Good (不錯) |
| `UiSpriteLibrary` | 161 | Good (不錯) |

---

## Watch list (Poor/Bad)

| Class | Lines | Empty | Tier |
|-------|:-----:|:-----:|:----:|
| `PlayerProfileCsvService` | 958 | 0 | Poor (不太好) |
| `StoryProgressWorldMapRuntime` | 926 | 0 | Poor (不太好) |
| `PlotUiOverlayCleanup` | 71 | 0 | Poor (不太好) |
| `LoginSceneController` | 699 | 1 | Poor (不太好) |
| `DeckPackSceneController` | 595 | 0 | Poor (不太好) |
| `FreeBattleSceneController` | 59 | 0 | Poor (不太好) |
| `SettingsSceneController.SoundVolume` | 262 | 0 | Poor (不太好) |
| `HallSceneAutoLayout` | 238 | 0 | Poor (不太好) |
| `TutorialPlotStarterDeckNotify` | 214 | 0 | Poor (不太好) |
| `ClickCard` | 191 | 0 | Poor (不太好) |
| `GlobalNavRuntime.PlayerInfoOverlay` | 116 | 0 | Poor (不太好) |
| `HallSceneFeatureBinder` | 110 | 0 | Poor (不太好) |

---

## Largest files

- `BattleSimulationManager` 4215 lines (Bad (差勁))
- `BattleSimulationDebugUI` 3971 lines (Bad (差勁))
- `DeckManager` 3944 lines (Bad (差勁))
- `SceneLoader.BattlePreview` 2455 lines (Bad (差勁))
- `BattleSimulationDebugUI.Settlement` 1573 lines (Bad (差勁))
- `StoryProgressSceneController` 1404 lines (Bad (差勁))
- `MainPlotSceneController` 974 lines (Bad (差勁))
- `PlayerProfileCsvService` 958 lines (Poor (不太好))

---

*Auto report from tools/Scan-CodeQuality.ps1*
