using UnityEngine;

/// <summary>Story progress 場景用 UI 圖（返回鍵、地圖節點圖示，自 UiSpriteLibrary 直接引用取得）。</summary>
public static class StoryProgressUiSprites
{
    private const string Intro11InstructionResourcesPath = "UI/1-1 Instruction";
    private const string Intro11InstructionSliceName = "1-1 Instruction_0";
    private const string Intro11PracticalApplicationResourcesPath = "UI/1-1 Practical Application";
    private const string Intro11PracticalApplicationSliceName = "1-1 Practical Application_0";
    private const string ClearResourcesPath = "UI/Clear";
    private const string ClearSliceName = "Clear_0";

    private static Sprite cachedReturnButton;
    private static Sprite cachedIntro11InstructionIcon;
    private static Sprite cachedIntro11PracticalApplicationIcon;
    private static Sprite cachedClearNodeIcon;

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

    /// <summary>1-1 教學關通關前 M-1-1 節點圖示。</summary>
    public static Sprite GetIntro11InstructionIcon()
    {
        if (cachedIntro11InstructionIcon != null)
            return cachedIntro11InstructionIcon;

        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        if (library != null && library.Intro11InstructionIcon != null)
        {
            cachedIntro11InstructionIcon = library.Intro11InstructionIcon;
            return cachedIntro11InstructionIcon;
        }

        cachedIntro11InstructionIcon = ResolveIntro11InstructionSlice(
            Resources.LoadAll<Sprite>(Intro11InstructionResourcesPath));

        if (cachedIntro11InstructionIcon == null)
        {
            Debug.LogWarning(
                "StoryProgressUiSprites: 1-1 Instruction 不在 UiSpriteLibrary／Resources，" +
                "請重跑 Tools/UI/Create or Refresh UI Sprite Library。");
        }

        return cachedIntro11InstructionIcon;
    }

    /// <summary>1-1 教學畢業後 M-1-1 實戰區節點圖示。</summary>
    public static Sprite GetIntro11PracticalApplicationIcon()
    {
        if (cachedIntro11PracticalApplicationIcon != null)
            return cachedIntro11PracticalApplicationIcon;

        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        if (library != null && library.Intro11PracticalApplicationIcon != null)
        {
            cachedIntro11PracticalApplicationIcon = library.Intro11PracticalApplicationIcon;
            return cachedIntro11PracticalApplicationIcon;
        }

        cachedIntro11PracticalApplicationIcon = ResolveIntro11PracticalApplicationSlice(
            Resources.LoadAll<Sprite>(Intro11PracticalApplicationResourcesPath));

        if (cachedIntro11PracticalApplicationIcon == null)
        {
            Debug.LogWarning(
                "StoryProgressUiSprites: 1-1 Practical Application 不在 UiSpriteLibrary／Resources，" +
                "請重跑 Tools/UI/Create or Refresh UI Sprite Library。");
        }

        return cachedIntro11PracticalApplicationIcon;
    }

    /// <summary>1-1／1-2 完全通關節點圖示。</summary>
    public static Sprite GetClearNodeIcon()
    {
        if (cachedClearNodeIcon != null)
            return cachedClearNodeIcon;

        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        if (library != null && library.StoryProgressClearIcon != null)
        {
            cachedClearNodeIcon = library.StoryProgressClearIcon;
            return cachedClearNodeIcon;
        }

        cachedClearNodeIcon = ResolveClearSlice(Resources.LoadAll<Sprite>(ClearResourcesPath));

        if (cachedClearNodeIcon == null)
        {
            Debug.LogWarning(
                "StoryProgressUiSprites: Clear 不在 UiSpriteLibrary／Resources，" +
                "請重跑 Tools/UI/Create or Refresh UI Sprite Library。");
        }

        return cachedClearNodeIcon;
    }

    private static Sprite ResolveClearSlice(Sprite[] slices)
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
            if (string.Equals(slice.name, ClearSliceName, System.StringComparison.Ordinal) ||
                string.Equals(slice.name, "Clear", System.StringComparison.Ordinal))
                return slice;
        }

        return fallback;
    }

    private static Sprite ResolveIntro11PracticalApplicationSlice(Sprite[] slices)
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
            if (string.Equals(slice.name, Intro11PracticalApplicationSliceName, System.StringComparison.Ordinal) ||
                string.Equals(slice.name, "1-1 Practical Application", System.StringComparison.Ordinal))
                return slice;
        }

        return fallback;
    }

    private static Sprite ResolveIntro11InstructionSlice(Sprite[] slices)
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
            if (string.Equals(slice.name, Intro11InstructionSliceName, System.StringComparison.Ordinal) ||
                string.Equals(slice.name, "1-1 Instruction", System.StringComparison.Ordinal))
                return slice;
        }

        return fallback;
    }
}
