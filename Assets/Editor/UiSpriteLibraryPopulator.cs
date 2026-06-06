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

        Sprite neutral = LoadSprite(CoachPrefix + "neutral");
        Sprite alert = LoadSprite(CoachPrefix + "alert");
        Sprite serious = LoadSprite(CoachPrefix + "serious");
        Sprite encourage = LoadSprite(CoachPrefix + "encourage");
        library.EditorSetCoach(neutral, alert, serious, encourage);

        Sprite returnButton = LoadReturnSprite(ReturnPath);
        library.EditorSetReturnButton(returnButton);

        Sprite harborBay = LoadSprite(HarborBayPath);
        Sprite previewPanel = LoadPreviewPanelSprite(BattlePreviewPanelPath);
        library.EditorSetBattleScene(harborBay, previewPanel);

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

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"UiSpriteLibraryPopulator: coach(neutral={neutral != null}, alert={alert != null}, " +
            $"serious={serious != null}, encourage={encourage != null}), return={returnButton != null}, " +
            $"harborBay={harborBay != null}, previewPanel={previewPanel != null}, " +
            $"difficulty(intro={dIntro != null}, easy={dEasy != null}, normal={dNormal != null}, " +
            $"hard={dHard != null}, boss={dBoss != null}), " +
            $"rarity(N={rN != null}, R={rR != null}, SR={rSr != null}, SSR={rSsr != null}, UR={rUr != null}) " +
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
}
