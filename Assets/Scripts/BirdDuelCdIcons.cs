using UnityEngine;

/// <summary>鬥鳥 CD 光碟封面圖（Assets/UI/CD/，經 <see cref="UiSpriteLibrary"/> 註冊）。</summary>
public static class BirdDuelCdIcons
{
    public static Sprite Resolve(string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId))
            return null;

        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        if (library == null)
            return null;

        return library.GetBirdDuelCdCover(cdId.Trim());
    }
}
