using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 港灣訓練場（1-1 實戰）對戰場景背景：將 <c>戰鬥背景</c> 換為 bay 圖（自 UiSpriteLibrary 直接引用）；
/// M-1-2 階段 A 段考換為教室圖（Classroom_FE；恐怖狀態為 Classroom_FR）；其餘對戰維持場景預設。
/// </summary>
public static class HarborTrainingBattleBackground
{
    public const string BattleBackgroundObjectName = "戰鬥背景";

    /// <summary>場景預設（自由對戰）「戰鬥背景」的版面：置中固定 2778×1284，超出畫布裁切（放大效果）。</summary>
    private static readonly Vector2 DefaultZoomedSize = new Vector2(2778f, 1284f);

    private static Sprite cachedDefaultSprite;
    private static Sprite cachedHarborSprite;
    private static bool defaultCaptured;

    public static void ApplyForActiveBattleContext()
    {
        if (!TutorialBattleBackgroundMusicPlayer.IsSupportedBattleScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            return;

        if (BattleLaunchContext.IsM12TrioTutorialBattle)
            ApplyClassroomBackground();
        else if (BattleLaunchContext.IsHarborTrainingGroundBattle ||
            BattleLaunchContext.IsM12CoachPracticeBattle)
            ApplyHarborBackground();
        else
            RestoreDefaultBackground();
    }

    private static void ApplyHarborBackground()
    {
        Image image = ResolveBattleBackgroundImage();
        if (image == null)
            return;

        CaptureDefaultIfNeeded(image);

        Sprite harbor = ResolveHarborSprite();
        if (harbor == null)
        {
            Debug.LogWarning("HarborTrainingBattleBackground: bay sprite not found.");
            return;
        }

        ApplySpriteToBackground(image, harbor);
    }

    public static void ApplyClassroomBackground() => ApplyClassroomSprite(resolveNormal: true);

    /// <summary>M-1-2 段考恐怖狀態：Classroom_FR。</summary>
    public static void ApplyM12PhaseAHorrorBackground() => ApplyClassroomSprite(resolveNormal: false);

    private static void ApplyClassroomSprite(bool resolveNormal)
    {
        Image image = ResolveBattleBackgroundImage();
        if (image == null)
            return;

        CaptureDefaultIfNeeded(image);

        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        Sprite classroom = library != null
            ? (resolveNormal ? library.ClassroomBackground : library.ClassroomHorrorBackground)
            : null;
        if (classroom == null)
        {
            Debug.LogWarning(
                "HarborTrainingBattleBackground: " +
                (resolveNormal ? "Classroom_FE" : "Classroom_FR") +
                " 不在 UiSpriteLibrary，請重跑 Tools/UI/Create or Refresh UI Sprite Library。");
            return;
        }

        ApplySpriteToBackground(image, classroom);
    }

    private static void RestoreDefaultBackground()
    {
        Image image = ResolveBattleBackgroundImage();
        if (image == null || !defaultCaptured || cachedDefaultSprite == null)
            return;

        ApplySpriteToBackground(image, cachedDefaultSprite);
    }

    private static Image ResolveBattleBackgroundImage()
    {
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Transform[] all = roots[r].GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || !string.Equals(t.name, BattleBackgroundObjectName, System.StringComparison.Ordinal))
                    continue;

                Image img = t.GetComponent<Image>();
                if (img != null)
                    return img;
            }
        }

        return null;
    }

    private static void CaptureDefaultIfNeeded(Image image)
    {
        if (defaultCaptured || image == null)
            return;

        cachedDefaultSprite = image.sprite;
        defaultCaptured = true;
    }

    private static Sprite ResolveHarborSprite()
    {
        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        if (library != null && library.HarborBayBackground != null)
        {
            cachedHarborSprite = library.HarborBayBackground;
            return cachedHarborSprite;
        }

        Debug.LogWarning(
            "HarborTrainingBattleBackground: bay 不在 UiSpriteLibrary，" +
            "請重跑 Tools/UI/Create or Refresh UI Sprite Library。");
        cachedHarborSprite = null;
        return cachedHarborSprite;
    }

    /// <summary>清除快取（例如更換 bay 貼圖後於編輯器呼叫）。</summary>
    public static void InvalidateCaches()
    {
        cachedHarborSprite = null;
        cachedDefaultSprite = null;
        defaultCaptured = false;
    }

    private static void ApplySpriteToBackground(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.type = Image.Type.Simple;

        RectTransform rt = image.rectTransform;
        if (rt == null)
            return;

        // 與自由對戰（場景預設）同一放大倍率：置中 2778×1284、超出畫布部分自然裁切。
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = DefaultZoomedSize;
        rt.localScale = Vector3.one;
        rt.SetAsFirstSibling();
    }
}
