using UnityEditor;
using UnityEngine;

/// <summary>
/// 建立 / 重新整理 Assets/Resources/UiSpriteLibrary.asset：
/// 從現有 Resources 路徑載入教練表情與返回鍵，存成直接 Sprite 引用。
/// 美術更新後重跑本選單即可同步。
/// 選單：Tools/UI/Create or Refresh UI Sprite Library
/// </summary>
public static class UiSpriteLibraryPopulator
{
    private const string LibraryAssetPath = "Assets/Resources/UiSpriteLibrary.asset";
    private const string CoachPrefix = "UI/LinKeCoach/linke_";
    private const string ReturnPath = "UI/return";
    private const string HarborBayPath = "UI/Level background/bay";
    private const string BattlePreviewPanelPath = "UI/pre-war preview";
    private const string DifficultyRoot = "UI/Difficulty level";
    private const string RarityRoot = "UI/Rarity";
    private const string CdRoot = "Assets/UI/CD";

    [MenuItem("Tools/UI/Create or Refresh UI Sprite Library")]
    public static void CreateOrRefresh()
    {
        UiSpriteLibrary library = AssetDatabase.LoadAssetAtPath<UiSpriteLibrary>(LibraryAssetPath);
        if (library == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            library = ScriptableObject.CreateInstance<UiSpriteLibrary>();
            AssetDatabase.CreateAsset(library, LibraryAssetPath);
            Debug.Log($"UiSpriteLibraryPopulator: 已建立新資產 {LibraryAssetPath}");
        }

        // 第一個表情（Neutral）定案用 Linkk_Smile.jpeg.png；其餘表情仍等 Resources/UI/LinKeCoach 美術。
        Sprite neutral = LoadCoachNeutralSpriteFromAsset();
        if (neutral == null)
            neutral = LoadSprite(CoachPrefix + "neutral");
        Sprite alert = LoadSprite(CoachPrefix + "alert");
        Sprite serious = LoadSprite(CoachPrefix + "serious");
        Sprite encourage = LoadSprite(CoachPrefix + "encourage");
        library.EditorSetCoach(neutral, alert, serious, encourage);

        Sprite returnButton = LoadReturnSprite(ReturnPath);
        library.EditorSetReturnButton(returnButton);

        Sprite battlePauseButton = LoadBattlePauseButtonSpriteFromAsset();
        library.EditorSetBattlePauseButton(battlePauseButton);

        Sprite basePlate = LoadBasePlateSprite();
        library.EditorSetResponsiveBasePlate(basePlate);

        Sprite harborBay = LoadSprite(HarborBayPath);
        Sprite previewPanel = LoadPreviewPanelSprite(BattlePreviewPanelPath);
        Sprite classroom = LoadSpriteFromAssetPath(ClassroomAssetPath);
        Sprite classroomHorror = LoadSpriteFromAssetPath(ClassroomHorrorAssetPath);
        library.EditorSetBattleScene(harborBay, previewPanel, classroom, classroomHorror);

        Sprite dIntro = LoadSprite(DifficultyRoot + "/Basics");
        Sprite dEasy = LoadSprite(DifficultyRoot + "/Easy");
        Sprite dNormal = LoadSprite(DifficultyRoot + "/Normal");
        Sprite dHard = LoadSprite(DifficultyRoot + "/Hard");
        Sprite dBoss = LoadSprite(DifficultyRoot + "/Boss");
        library.EditorSetDifficulty(dIntro, dEasy, dNormal, dHard, dBoss);

        Sprite rN = LoadSprite(RarityRoot + "/稀有度N");
        Sprite rR = LoadRaritySprite(RarityRoot + "/稀有度R", RarityRoot + "/R");
        Sprite rSr = LoadSprite(RarityRoot + "/稀有度SR");
        Sprite rSsr = LoadRaritySprite(RarityRoot + "/稀有度SSR", RarityRoot + "/SSR");
        Sprite rUr = LoadRaritySprite(RarityRoot + "/稀有度UR", RarityRoot + "/UR");
        library.EditorSetRarityFrames(rN, rR, rSr, rSsr, rUr);

        Sprite cdHarborPractice = LoadCdCoverSprite(BirdDuelCdCatalog.DefaultCoverAssetKey);
        Sprite cdCourtMarch = LoadCdCoverSprite(BirdDuelCdCatalog.CourtMarchCoverAssetKey);
        library.EditorSetBirdDuelCdCovers(cdHarborPractice, cdCourtMarch);

        Sprite youWillDieSprite = LoadYouWillDieSpriteFromAsset();
        library.EditorSetCombatStatus(youWillDieSprite);

        Sprite intro11Instruction = LoadIntro11InstructionSpriteFromAsset();
        Sprite intro11PracticalApplication = LoadIntro11PracticalApplicationSpriteFromAsset();
        Sprite storyProgressClear = LoadStoryProgressClearSpriteFromAsset();
        library.EditorSetStoryProgressNodeIcons(intro11Instruction, intro11PracticalApplication, storyProgressClear);

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"UiSpriteLibraryPopulator: coach(neutral={neutral != null}, alert={alert != null}, " +
            $"serious={serious != null}, encourage={encourage != null}), return={returnButton != null}, " +
            $"battlePause={battlePauseButton != null}, " +
            $"basePlate={basePlate != null}, " +
            $"harborBay={harborBay != null}, previewPanel={previewPanel != null}, classroom={classroom != null}, classroomHorror={classroomHorror != null}, " +
            $"difficulty(intro={dIntro != null}, easy={dEasy != null}, normal={dNormal != null}, " +
            $"hard={dHard != null}, boss={dBoss != null}), " +
            $"rarity(N={rN != null}, R={rR != null}, SR={rSr != null}, SSR={rSsr != null}, UR={rUr != null}), " +
            $"cdHarborPractice={cdHarborPractice != null}, cdCourtMarch={cdCourtMarch != null}, " +
            $"youWillDie={youWillDieSprite != null}, intro11Instruction={intro11Instruction != null}, " +
            $"intro11PracticalApplication={intro11PracticalApplication != null}, " +
            $"storyProgressClear={storyProgressClear != null} " +
            $"→ {LibraryAssetPath}");
    }

    // 主檔載入；多 Slice 時退回 LoadAll 取第一個有效 Sprite。
    private static Sprite LoadSprite(string resourcesPath)
    {
        Sprite direct = Resources.Load<Sprite>(resourcesPath);
        if (direct != null)
            return direct;

        Sprite[] slices = Resources.LoadAll<Sprite>(resourcesPath);
        if (slices != null)
        {
            for (int i = 0; i < slices.Length; i++)
                if (slices[i] != null)
                    return slices[i];
        }
        return null;
    }

    // 返回鍵：與 StoryProgressUiSprites 相同規則，優先 return_0 / return slice。
    private static Sprite LoadReturnSprite(string resourcesPath)
    {
        Sprite direct = Resources.Load<Sprite>(resourcesPath);
        if (direct != null)
            return direct;

        Sprite[] slices = Resources.LoadAll<Sprite>(resourcesPath);
        if (slices != null)
        {
            for (int i = 0; i < slices.Length; i++)
            {
                Sprite s = slices[i];
                if (s != null && (s.name == "return_0" || s.name == "return"))
                    return s;
            }
            for (int i = 0; i < slices.Length; i++)
                if (slices[i] != null)
                    return slices[i];
        }
        return null;
    }

    // 稀有度框：主檔路徑優先，無則退回備援路徑（與 CardDisplay 相同規則）。
    private static Sprite LoadRaritySprite(string primaryPath, string fallbackPath)
    {
        Sprite primary = LoadSprite(primaryPath);
        return primary != null ? primary : LoadSprite(fallbackPath);
    }

    // 對戰預覽面板：與 ResolveBattlePreviewPanelSprite 相同規則，優先含 "pre-war" 的 slice。
    private static Sprite LoadPreviewPanelSprite(string resourcesPath)
    {
        Sprite direct = Resources.Load<Sprite>(resourcesPath);
        if (direct != null)
            return direct;

        Sprite[] slices = Resources.LoadAll<Sprite>(resourcesPath);
        if (slices != null)
        {
            for (int i = 0; i < slices.Length; i++)
            {
                Sprite s = slices[i];
                if (s != null && s.name.IndexOf("pre-war", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return s;
            }
            for (int i = 0; i < slices.Length; i++)
                if (slices[i] != null)
                    return slices[i];
        }
        return null;
    }

    private const string BasePlateAssetPath = "Assets/UI/base plate.png";
    private const string ClassroomAssetPath = "Assets/UI/Level background/Classroom_FE.png";
    private const string ClassroomHorrorAssetPath = "Assets/UI/Level background/Classroom_FR.png";
    private const string CoachNeutralAssetPath = "Assets/UI/NPC/Linkk_Smile.jpeg.png";
    private const string YouWillDieAssetPath = "Assets/UI/Combat status/You will die.png";
    private const string Intro11InstructionAssetPath = "Assets/UI/1-1 Instruction.png";
    private const string Intro11PracticalApplicationAssetPath = "Assets/UI/1-1 Practical Application.png";
    private const string StoryProgressClearAssetPath = "Assets/UI/Clear.png";
    private const string BattlePauseButtonAssetPath = "Assets/UI/pause-button.png";

    private static Sprite LoadIntro11InstructionSpriteFromAsset()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(Intro11InstructionAssetPath);
        if (assets == null)
            return null;

        Sprite preferred = null;
        Sprite fallback = null;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite sprite)
                continue;
            fallback ??= sprite;
            if (string.Equals(sprite.name, "1-1 Instruction_0", System.StringComparison.Ordinal) ||
                string.Equals(sprite.name, "1-1 Instruction", System.StringComparison.Ordinal))
                preferred = sprite;
        }

        return preferred != null ? preferred : fallback;
    }

    private static Sprite LoadIntro11PracticalApplicationSpriteFromAsset()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(Intro11PracticalApplicationAssetPath);
        if (assets == null)
            return null;

        Sprite preferred = null;
        Sprite fallback = null;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite sprite)
                continue;
            fallback ??= sprite;
            if (string.Equals(sprite.name, "1-1 Practical Application_0", System.StringComparison.Ordinal) ||
                string.Equals(sprite.name, "1-1 Practical Application", System.StringComparison.Ordinal))
                preferred = sprite;
        }

        return preferred != null ? preferred : fallback;
    }

    private static Sprite LoadStoryProgressClearSpriteFromAsset()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(StoryProgressClearAssetPath);
        if (assets == null)
            return null;

        Sprite preferred = null;
        Sprite fallback = null;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite sprite)
                continue;
            fallback ??= sprite;
            if (string.Equals(sprite.name, "Clear_0", System.StringComparison.Ordinal) ||
                string.Equals(sprite.name, "Clear", System.StringComparison.Ordinal))
                preferred = sprite;
        }

        return preferred != null ? preferred : fallback;
    }

    private static Sprite LoadBattlePauseButtonSpriteFromAsset()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BattlePauseButtonAssetPath);
        if (assets == null)
            return null;

        Sprite preferred = null;
        Sprite fallback = null;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite sprite)
                continue;
            fallback ??= sprite;
            if (string.Equals(sprite.name, "pause-button_0", System.StringComparison.Ordinal) ||
                string.Equals(sprite.name, "pause-button", System.StringComparison.Ordinal))
                preferred = sprite;
        }

        return preferred != null ? preferred : fallback;
    }

    private static Sprite LoadYouWillDieSpriteFromAsset()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(YouWillDieAssetPath);
        if (assets == null)
            return null;

        Sprite preferred = null;
        Sprite fallback = null;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite sprite)
                continue;
            fallback ??= sprite;
            if (string.Equals(sprite.name, "You will die_0", System.StringComparison.Ordinal))
                preferred = sprite;
        }
        return preferred != null ? preferred : fallback;
    }

    private static Sprite LoadCoachNeutralSpriteFromAsset()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(CoachNeutralAssetPath);
        if (assets == null)
            return null;

        Sprite preferred = null;
        Sprite fallback = null;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite sprite)
                continue;
            fallback ??= sprite;
            if (string.Equals(sprite.name, "Linkk_Smile.jpeg_0", System.StringComparison.Ordinal) ||
                string.Equals(sprite.name, "Linkk_Smile.jpeg", System.StringComparison.Ordinal))
                preferred = sprite;
        }

        return preferred != null ? preferred : fallback;
    }

    // 不在 Resources 下的貼圖：直接以 AssetDatabase 載入第一個 Sprite 子資產。
    private static Sprite LoadSpriteFromAssetPath(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null)
            return null;

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }
        return null;
    }

    private static Sprite LoadBasePlateSprite()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BasePlateAssetPath);
        if (assets == null)
            return null;

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }
        return null;
    }

    private static Sprite LoadCdCoverSprite(string cdFileName)
    {
        if (string.IsNullOrWhiteSpace(cdFileName))
            return null;

        string[] extensions = { ".jpg", ".jpeg", ".png" };
        for (int i = 0; i < extensions.Length; i++)
        {
            string path = CdRoot + "/" + cdFileName + extensions[i];
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
                continue;

            string preferredSlice = cdFileName + "_0";
            for (int a = 0; a < assets.Length; a++)
            {
                if (assets[a] is Sprite sprite &&
                    string.Equals(sprite.name, preferredSlice, System.StringComparison.Ordinal))
                    return sprite;
            }

            for (int a = 0; a < assets.Length; a++)
            {
                if (assets[a] is Sprite sprite)
                    return sprite;
            }
        }

        return null;
    }
}
