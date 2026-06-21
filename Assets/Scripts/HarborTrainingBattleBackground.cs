using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 港灣訓練場（1-1 實戰）對戰場景背景：將 <c>戰鬥背景</c> 換為 bay 圖（自 UiSpriteLibrary 直接引用）；其餘對戰維持場景預設。
/// </summary>
public static class HarborTrainingBattleBackground
{
    public const string BattleBackgroundObjectName = "戰鬥背景";

    private static Sprite cachedDefaultSprite;
    private static Sprite cachedHarborSprite;
    private static bool defaultCaptured;

    public static void ApplyForActiveBattleContext()
    {
        if (!TutorialBattleBackgroundMusicPlayer.IsSupportedBattleScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            return;

        if (BattleLaunchContext.IsHarborTrainingGroundBattle ||
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

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.SetAsFirstSibling();
    }
}
