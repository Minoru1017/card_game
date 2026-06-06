# 資源讀取清單（自動產生）

> 由 `Tools/Resources/Generate Resource Usage Report` 產生於 2026-06-05 05:25。
> 請勿手動編輯；重跑選單即可更新。

## 摘要

- 掃描腳本數：188
- 讀取點總數：53
- 路徑可解析但磁碟找不到：1（潛在錯誤，建議檢查）
- 路徑含變數無法靜態解析：17（需看程式邏輯）

| 分類 | 數量 |
|---|---|
| UI（Prefab） | 8 |
| UI（圖像） | 8 |
| UI（字型/材質） | 1 |
| 美術 | 14 |
| 資料設定 | 5 |
| 音頻 | 17 |

## 美術（14）

| 型別 | 載入方式 | Key / 路徑 | 磁碟位置 | 狀態 | 來源 |
|---|---|---|---|---|---|
| Sprite | Resources.Load | `CardArt/{id}` | `—` | 變數 | Assets/Scripts/TutorialBattleRewardService.cs:214 |
| Sprite | Resources.Load | `{$"CardArt/{card.id}"}` | `—` | 變數 | Assets/Scripts/BackpackCardInspectPanel.cs:520 |
| Sprite | AssetDatabase.LoadAssetAtPath | `{assetPath}` | `—` | 變數 | Assets/Editor/CardArtworkAutoBinder.cs:203 |
| Sprite | Resources.Load | `{card.artworkResourcePath.Trim(}` | `—` | 變數 | Assets/Scripts/BackpackCardInspectPanel.cs:511 |
| Sprite | Resources.Load | `{card.artworkResourcePath.Trim(}` | `—` | 變數 | Assets/Scripts/TutorialBattleRewardService.cs:188 |
| Sprite | Resources.Load | `{card.deckThumbResourcePath.Trim(}` | `—` | 變數 | Assets/Scripts/BackpackCardInspectPanel.cs:516 |
| Sprite | Resources.Load | `{card.deckThumbResourcePath.Trim(}` | `—` | 變數 | Assets/Scripts/TutorialBattleRewardService.cs:198 |
| Sprite | Resources.Load | `{path.Trim(}` | `—` | 變數 | Assets/Scripts/CardStore.cs:188 |
| Sprite | Resources.Load | `{path}` | `—` | 變數 | Assets/Scripts/SceneLoader.BattlePreview.cs:872 |
| Sprite | Resources.LoadAll | `{path}` | `—` | 變數 | Assets/Scripts/SceneLoader.BattlePreview.cs:875 |
| Sprite | Resources.Load | `{resourcePath}` | `—` | 變數 | Assets/Scripts/TutorialPlotScriptFactory.cs:190 |
| Sprite | Resources.LoadAll | `{resourcePath}` | `—` | 變數 | Assets/Scripts/TutorialPlotScriptFactory.cs:193 |
| Sprite | Resources.Load | `{resourcesPath}` | `—` | 變數 | Assets/Scripts/CardDisplay.cs:227 |
| Sprite | Resources.LoadAll | `{resourcesPath}` | `—` | 變數 | Assets/Scripts/CardDisplay.cs:230 |

## UI（圖像）（8）

| 型別 | 載入方式 | Key / 路徑 | 磁碟位置 | 狀態 | 來源 |
|---|---|---|---|---|---|
| Sprite | AssetDatabase.LoadAssetAtPath | `Assets/UI/Card preset images.png` | `Assets/UI/Card preset images.png` | 缺檔! | Assets/Editor/CardArtworkAutoBinder.cs:905 |
| Sprite | AssetDatabase.LoadAssetAtPath | `Assets/UI/Level background/bay.png` | `Assets/UI/Level background/bay.png` | OK | Assets/Scripts/HarborTrainingBattleBackground.cs:99 |
| Sprite | Resources.Load | `UI/Level background/bay` | `Assets/Resources/UI/Level background/bay.png` | OK | Assets/Scripts/HarborTrainingBattleBackground.cs:96 |
| Sprite | Resources.Load | `UI/LinKeCoach/linke_{fileName}` | `—` | 變數 | Assets/Scripts/HarborCombatCoachExpressionCatalog.cs:74 |
| Sprite | Resources.Load | `UI/pre-war preview` | `Assets/Resources/UI/pre-war preview.png` | OK | Assets/Scripts/SceneLoader.BattlePreview.cs:916 |
| Sprite | Resources.LoadAll | `UI/pre-war preview` | `Assets/Resources/UI/pre-war preview.png` | OK | Assets/Scripts/SceneLoader.BattlePreview.cs:920 |
| Sprite | Resources.Load | `UI/return` | `Assets/Resources/UI/return.png` | OK | Assets/Scripts/StoryProgressUiSprites.cs:15 |
| Sprite | Resources.LoadAll | `UI/return` | `Assets/Resources/UI/return.png` | OK | Assets/Scripts/StoryProgressUiSprites.cs:19 |

## UI（Prefab）（8）

| 型別 | 載入方式 | Key / 路徑 | 磁碟位置 | 狀態 | 來源 |
|---|---|---|---|---|---|
| GameObject | AssetDatabase.LoadAssetAtPath | `Assets/prefabs/DataManager.prefab` | `Assets/prefabs/DataManager.prefab` | OK | Assets/Editor/CardArtworkAutoBinder.cs:480 |
| GameObject | AssetDatabase.LoadAssetAtPath | `Assets/prefabs/DataManager.prefab` | `Assets/prefabs/DataManager.prefab` | OK | Assets/Editor/CardArtworkAutoBinder.cs:506 |
| GameObject | AssetDatabase.LoadAssetAtPath | `Assets/prefabs/DataManager.prefab` | `Assets/prefabs/DataManager.prefab` | OK | Assets/Editor/CardArtworkAutoBinder.cs:530 |
| GameObject | Resources.Load | `Buildbeck/UI/BuildbeckScrollGrid` | `Assets/Resources/Buildbeck/UI/BuildbeckScrollGrid.prefab` | OK | Assets/Scripts/BuildbeckSceneAutoScaffold.cs:141 |
| GameObject | Resources.Load | `Buildbeck/UI/DeckSlotGuideDot` | `Assets/Resources/Buildbeck/UI/DeckSlotGuideDot.prefab` | OK | Assets/Scripts/DeckManager.cs:2319 |
| GameObject | Resources.Load | `Buildbeck/UI/DeckSlotGuideDotsRoot` | `Assets/Resources/Buildbeck/UI/DeckSlotGuideDotsRoot.prefab` | OK | Assets/Scripts/DeckManager.cs:2290 |
| GameObject | Resources.Load | `Buildbeck/UI/DeckSlotGuideNavButton` | `Assets/Resources/Buildbeck/UI/DeckSlotGuideNavButton.prefab` | OK | Assets/Scripts/DeckManager.cs:2370 |
| GameObject | AssetDatabase.LoadAssetAtPath | `{PrefabPath}` | `—` | 變數 | Assets/Editor/GlobalNavPrefabBuilder.cs:14 |

## UI（字型/材質）（1）

| 型別 | 載入方式 | Key / 路徑 | 磁碟位置 | 狀態 | 來源 |
|---|---|---|---|---|---|
| TMP_FontAsset | Resources.Load | `Fonts & Materials/LiberationSans SDF` | `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` | OK | Assets/Scripts/BuildbeckProficiencyDebugUi.cs:156 |

## 音頻（17）

| 型別 | 載入方式 | Key / 路徑 | 磁碟位置 | 狀態 | 來源 |
|---|---|---|---|---|---|
| AudioClip | AssetDatabase.LoadAssetAtPath | `Assets/Music/Battle failed.mp3` | `Assets/Music/Battle failed.mp3` | OK | Assets/Scripts/TutorialBattleDefeatSfx.cs:131 |
| AudioClip | AssetDatabase.LoadAssetAtPath | `Assets/Music/Battle victory.mp3` | `Assets/Music/Battle victory.mp3` | OK | Assets/Scripts/TutorialBattleVictorySfx.cs:131 |
| AudioClip | AssetDatabase.LoadAssetAtPath | `Assets/Music/lhzrw-t5u2p.mp3` | `Assets/Music/lhzrw-t5u2p.mp3` | OK | Assets/Scripts/PlotMenuClickSfx.cs:109 |
| AudioClip | AssetDatabase.LoadAssetAtPath | `Assets/Music/Master Minded - Amazonian Grounding.mp3` | `Assets/Music/Master Minded - Amazonian Grounding.mp3` | OK | Assets/Scripts/StoryProgressBackgroundMusicPlayer.cs:242 |
| AudioClip | AssetDatabase.LoadAssetAtPath | `Assets/Music/Roie Shpigler - Enchanted Valley.mp3` | `Assets/Music/Roie Shpigler - Enchanted Valley.mp3` | OK | Assets/Scripts/PlotBackgroundMusicPlayer.cs:227 |
| AudioClip | AssetDatabase.LoadAssetAtPath | `Assets/Music/typewriter-typing-.mp3` | `Assets/Music/typewriter-typing-.mp3` | OK | Assets/Scripts/PlotDialogueTypewriterSfx.cs:123 |
| AudioClip | AssetDatabase.LoadAssetAtPath | `Assets/Music/Ziv Moran - Shades - Mysterious.mp3` | `Assets/Music/Ziv Moran - Shades - Mysterious.mp3` | OK | Assets/Scripts/TutorialBattleBackgroundMusicPlayer.cs:291 |
| AudioClip | AssetDatabase.LoadAssetAtPath | `Assets/Resources/Music/Aves - Forgotten Dreams.mp3` | `Assets/Resources/Music/Aves - Forgotten Dreams.mp3` | OK | Assets/Scripts/TutorialBattleBackgroundMusicPlayer.cs:279 |
| AudioClip | Resources.Load | `Music/Aves - Forgotten Dreams` | `Assets/Resources/Music/Aves - Forgotten Dreams.mp3` | OK | Assets/Scripts/TutorialBattleBackgroundMusicPlayer.cs:276 |
| AudioClip | Resources.Load | `Music/Battle failed` | `Assets/Resources/Music/Battle failed.mp3` | OK | Assets/Scripts/TutorialBattleDefeatSfx.cs:128 |
| AudioClip | Resources.Load | `Music/Battle victory` | `Assets/Resources/Music/Battle victory.mp3` | OK | Assets/Scripts/TutorialBattleVictorySfx.cs:128 |
| AudioClip | Resources.Load | `Music/lhzrw-t5u2p` | `Assets/Resources/Music/lhzrw-t5u2p.mp3` | OK | Assets/Scripts/PlotMenuClickSfx.cs:106 |
| AudioClip | Resources.Load | `Music/Master Minded - Amazonian Grounding` | `Assets/Resources/Music/Master Minded - Amazonian Grounding.mp3` | OK | Assets/Scripts/StoryProgressBackgroundMusicPlayer.cs:239 |
| AudioClip | Resources.Load | `Music/Roie Shpigler - Enchanted Valley` | `Assets/Resources/Music/Roie Shpigler - Enchanted Valley.mp3` | OK | Assets/Scripts/PlotBackgroundMusicPlayer.cs:224 |
| AudioClip | Resources.Load | `Music/typewriter-typing-` | `Assets/Resources/Music/typewriter-typing-.mp3` | OK | Assets/Scripts/PlotDialogueTypewriterSfx.cs:120 |
| AudioClip | Resources.Load | `Music/Ziv Moran - Shades - Mysterious` | `Assets/Resources/Music/Ziv Moran - Shades - Mysterious.mp3` | OK | Assets/Scripts/TutorialBattleBackgroundMusicPlayer.cs:288 |
| AudioClip | Resources.Load | `{path}` | `—` | 變數 | Assets/Scripts/PlotNpcVoicePlayer.cs:107 |

## 資料設定（5）

| 型別 | 載入方式 | Key / 路徑 | 磁碟位置 | 狀態 | 來源 |
|---|---|---|---|---|---|
| TextAsset | AssetDatabase.LoadAssetAtPath | `Assets/Assets/Datas/CardList.csv` | `Assets/Assets/Datas/CardList.csv` | OK | Assets/Editor/CardArtworkAutoBinder.cs:358 |
| TextAsset | AssetDatabase.LoadAssetAtPath | `Assets/Assets/Datas/CardList.csv` | `Assets/Assets/Datas/CardList.csv` | OK | Assets/Editor/CardArtworkAutoBinder.cs:417 |
| TextAsset | Resources.Load | `GlobalNavConfig` | `Assets/Resources/GlobalNavConfig.json` | OK | Assets/Scripts/GlobalNavRuntime.cs:122 |
| TextAsset | Resources.Load | `StoryProgressNodeDatabase` | `Assets/Resources/StoryProgressNodeDatabase.json` | OK | Assets/Scripts/BattleCardTuningPreset.cs:92 |
| TextAsset | Resources.Load | `StoryProgressNodeDatabase` | `Assets/Resources/StoryProgressNodeDatabase.json` | OK | Assets/Scripts/StoryProgressNodeDatabase.cs:53 |

## 已偵測到的路徑常數（命名慣例參考）

| 常數名 | 值 |
|---|---|
| IntroPlotVoiceIdPrefix | `1-1` |
| colorClose | `</color>` |
| PreviewRightTitleRich | `<b>敵方特色</b>` |
| PreviewLeftTitleRich | `<b>訓練提示</b>` |
| Pz02PuzzleTitleLockedRich | `<b>謎題</b> <color=#8A6B3A>#找出困難級</color>` |
| Pz01PuzzleTitleLockedRich | `<b>謎題</b> <color=#8A6B3A>#訓練場</color>` |
| PreviewRightDetailRich | `<color=#43573A><b>快攻型</b> 傾向早出場怪與直傷 壓迫感強 需善用防守與法術化解</color>` |
| PreviewLeftDetailRich | `<color=#43573A>簡單級前段較緩 約 10 回合內收尾 普通級可練可過、節奏略升 仍宜保留治療與拆場法術 穩血線再反擊</color>` |
| PreviewGoalRich | `<color=#6C533D>練習目標 運用<color=#43573A><b>防守牌</b></color>與<color=#43573A><b>法術</b></color>擊敗對手</color>` |
| Pz02PuzzleTitleUnlockedRich | `<size=110%><b>困難級</b></size>` |
| Pz01PuzzleTitleUnlockedRich | `<size=110%><b>魔王級</b></size>` |
| PreviewHeaderRich | `<size=115%><b>港灣訓練場 選擇難易度</b></size>` |
| HeaderSelectDifficultyRich | `<size=115%><b>選擇難易度</b></size>` |
| CardCsvPath | `Assets/Assets/Datas/CardList.csv` |
| DefeatClipAssetPath | `Assets/Music/Battle failed.mp3` |
| VictoryClipAssetPath | `Assets/Music/Battle victory.mp3` |
| MenuClickClipAssetPath | `Assets/Music/lhzrw-t5u2p.mp3` |
| AmazonianGroundingAssetPath | `Assets/Music/Master Minded - Amazonian Grounding.mp3` |
| EnchantedValleyAssetPath | `Assets/Music/Roie Shpigler - Enchanted Valley.mp3` |
| TypingClipAssetPath | `Assets/Music/typewriter-typing-.mp3` |
| HarborTrainingBgmAssetPath | `Assets/Music/Ziv Moran - Shades - Mysterious.mp3` |
| DataManagerPrefabPath | `Assets/prefabs/DataManager.prefab` |
| JsonAssetPath | `Assets/Resources/BattleCardTuningPresets.json` |
| UiFolder | `Assets/Resources/Buildbeck/UI` |
| ForgottenDreamsAssetPath | `Assets/Resources/Music/Aves - Forgotten Dreams.mp3` |
| PrefabFolder | `Assets/Resources/prefabs` |
| BattleScenePath | `Assets/Scenes/BattleSimulation.unity` |
| CardStoreScenePath | `Assets/Scenes/CardStore.unity` |
| UiFolderPrefix | `Assets/UI/` |
| DefaultFallbackArtPath | `Assets/UI/Card preset images.png` |
| CardArtFolderPrefix | `Assets/UI/CardArt/` |
| DeckThumbFolderPrefix | `Assets/UI/DeckThumb/` |
| HarborBayAssetPath | `Assets/UI/Level background/bay.png` |
| ScrollGrid | `Buildbeck/UI/BuildbeckScrollGrid` |
| GuideDot | `Buildbeck/UI/DeckSlotGuideDot` |
| GuideDotsRoot | `Buildbeck/UI/DeckSlotGuideDotsRoot` |
| GuideNavButton | `Buildbeck/UI/DeckSlotGuideNavButton` |
| LogPrefix | `CardArt AutoBinder` |
| LinKeGazeCardArtResourcePath | `CardArt/林可的凝視` |
| ConfigResourcePath | `GlobalNavConfig` |
| ForgottenDreamsResourcesPath | `Music/Aves - Forgotten Dreams` |
| DefeatClipResourcesPath | `Music/Battle failed` |
| VictoryClipResourcesPath | `Music/Battle victory` |
| MenuClickClipResourcesPath | `Music/lhzrw-t5u2p` |
| AmazonianGroundingResourcesPath | `Music/Master Minded - Amazonian Grounding` |
| EnchantedValleyResourcesPath | `Music/Roie Shpigler - Enchanted Valley` |
| TypingClipResourcesPath | `Music/typewriter-typing-` |
| HarborTrainingBgmResourcesPath | `Music/Ziv Moran - Shades - Mysterious` |
| VoiceResourcesFolder | `NPC voice` |
| ResourcePath | `StoryProgressNodeDatabase` |
| BattleDonePrefix | `tutorial_battle_done_v1_slot_` |
| PlotDonePrefix | `tutorial_plot_done_v1_slot_` |
| DifficultyLevelResourceRoot | `UI/Difficulty level` |
| HarborBayResourcesPath | `UI/Level background/bay` |
| ResourcePrefix | `UI/LinKeCoach/linke_` |
| DefaultBattlePreviewPanelResourcePath | `UI/pre-war preview` |
| ReturnButtonResourcesPath | `UI/return` |
| FooterHint | `左側上下捲動瀏覽 / 右側顯示物品資訊` |

