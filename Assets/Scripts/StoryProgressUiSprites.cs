using UnityEngine;

/// <summary>Story progress 場景用 UI 圖（<c>Assets/UI/return.png</c> 執行期自 Resources 載入）。</summary>
public static class StoryProgressUiSprites
{
    private const string ReturnButtonResourcesPath = "UI/return";

    private static Sprite cachedReturnButton;

    public static Sprite GetReturnButtonSprite()
    {
        if (cachedReturnButton != null)
            return cachedReturnButton;

        cachedReturnButton = Resources.Load<Sprite>(ReturnButtonResourcesPath);
        if (cachedReturnButton != null)
            return cachedReturnButton;

        Sprite[] slices = Resources.LoadAll<Sprite>(ReturnButtonResourcesPath);
        if (slices != null)
        {
            for (int i = 0; i < slices.Length; i++)
            {
                Sprite s = slices[i];
                if (s == null) continue;
                if (s.name == "return_0" || s.name == "return")
                {
                    cachedReturnButton = s;
                    return cachedReturnButton;
                }
            }

            if (slices.Length > 0 && slices[0] != null)
            {
                cachedReturnButton = slices[0];
                return cachedReturnButton;
            }
        }

        return null;
    }
}
