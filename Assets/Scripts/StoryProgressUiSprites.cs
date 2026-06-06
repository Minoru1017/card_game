using UnityEngine;

/// <summary>Story progress 場景用 UI 圖（返回鍵，自 UiSpriteLibrary 直接引用取得）。</summary>
public static class StoryProgressUiSprites
{
    private static Sprite cachedReturnButton;

    public static Sprite GetReturnButtonSprite()
    {
        if (cachedReturnButton != null)
            return cachedReturnButton;

        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        if (library != null && library.ReturnButton != null)
        {
            cachedReturnButton = library.ReturnButton;
            return cachedReturnButton;
        }

        Debug.LogWarning(
            "StoryProgressUiSprites: 返回鍵不在 UiSpriteLibrary，" +
            "請重跑 Tools/UI/Create or Refresh UI Sprite Library。");
        return null;
    }
}
