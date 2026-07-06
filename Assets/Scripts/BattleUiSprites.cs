using UnityEngine;

/// <summary>對戰 UI 圖（暫停鍵等，自 UiSpriteLibrary 直接引用取得）。</summary>
public static class BattleUiSprites
{
    private const string PauseButtonResourcesPath = "UI/pause-button";
    private const string PauseButtonSliceName = "pause-button_0";

    private static Sprite cachedPauseButton;

    public static Sprite GetPauseButtonSprite()
    {
        if (cachedPauseButton != null)
            return cachedPauseButton;

        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        if (library != null && library.BattlePauseButton != null)
        {
            cachedPauseButton = library.BattlePauseButton;
            return cachedPauseButton;
        }

        cachedPauseButton = ResolvePauseButtonSlice(Resources.LoadAll<Sprite>(PauseButtonResourcesPath));

        if (cachedPauseButton == null)
        {
            Debug.LogWarning(
                "BattleUiSprites: pause-button 不在 UiSpriteLibrary／Resources，" +
                "請重跑 Tools/UI/Create or Refresh UI Sprite Library。");
        }

        return cachedPauseButton;
    }

    private static Sprite ResolvePauseButtonSlice(Sprite[] slices)
    {
        if (slices == null || slices.Length == 0)
            return null;

        Sprite fallback = null;
        for (int i = 0; i < slices.Length; i++)
        {
            Sprite slice = slices[i];
            if (slice == null)
                continue;
            fallback ??= slice;
            if (string.Equals(slice.name, PauseButtonSliceName, System.StringComparison.Ordinal) ||
                string.Equals(slice.name, "pause-button", System.StringComparison.Ordinal))
                return slice;
        }

        return fallback;
    }
}
