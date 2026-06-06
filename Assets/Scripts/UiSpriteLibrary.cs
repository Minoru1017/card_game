using UnityEngine;

/// <summary>
/// UI 圖像註冊表（直接引用，取代字串式 Resources.Load）。
/// A 類：港灣教練表情、返回鍵。
/// B 類：港灣對戰背景（bay）、對戰預覽面板、難度分級圖（入門～魔王）。
/// A 類進階：CardArt 稀有度框（N/R/SR/SSR/UR）。
///
/// 取得方式：UiSpriteLibrary.Instance（一次性從 Resources/UiSpriteLibrary.asset 載入並快取）。
/// </summary>
[CreateAssetMenu(fileName = "UiSpriteLibrary", menuName = "Card Game/UI Sprite Library", order = 2)]
public sealed class UiSpriteLibrary : ScriptableObject
{
    public const string ResourcesPath = "UiSpriteLibrary";

    [Header("港灣教練表情")]
    [SerializeField] private Sprite coachNeutral;
    [SerializeField] private Sprite coachAlert;
    [SerializeField] private Sprite coachSerious;
    [SerializeField] private Sprite coachEncourage;

    [Header("通用 UI")]
    [SerializeField] private Sprite returnButton;

    [Header("對戰場景")]
    [SerializeField] private Sprite harborBayBackground;
    [SerializeField] private Sprite battlePreviewPanel;

    [Header("難度分級圖")]
    [SerializeField] private Sprite difficultyIntro;
    [SerializeField] private Sprite difficultyEasy;
    [SerializeField] private Sprite difficultyNormal;
    [SerializeField] private Sprite difficultyHard;
    [SerializeField] private Sprite difficultyBoss;

    [Header("CardArt 稀有度框")]
    [SerializeField] private Sprite rarityFrameN;
    [SerializeField] private Sprite rarityFrameR;
    [SerializeField] private Sprite rarityFrameSr;
    [SerializeField] private Sprite rarityFrameSsr;
    [SerializeField] private Sprite rarityFrameUr;

    public Sprite ReturnButton => returnButton;
    public Sprite HarborBayBackground => harborBayBackground;
    public Sprite BattlePreviewPanel => battlePreviewPanel;

    public Sprite GetCoachExpression(HarborCoachExpression expression)
    {
        switch (expression)
        {
            case HarborCoachExpression.Alert: return coachAlert;
            case HarborCoachExpression.Serious: return coachSerious;
            case HarborCoachExpression.Encourage: return coachEncourage;
            default: return coachNeutral;
        }
    }

    public Sprite GetDifficultyTier(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Easy: return difficultyEasy;
            case BattleDifficultyTier.Normal: return difficultyNormal;
            case BattleDifficultyTier.Hard: return difficultyHard;
            case BattleDifficultyTier.Boss: return difficultyBoss;
            default: return difficultyIntro;
        }
    }

    public Sprite GetRarityFrame(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.R: return rarityFrameR;
            case CardRarity.SR: return rarityFrameSr;
            case CardRarity.SSR: return rarityFrameSsr;
            case CardRarity.UR: return rarityFrameUr;
            default: return rarityFrameN;
        }
    }

    private static UiSpriteLibrary instance;
    private static bool instanceLoaded;

    /// <summary>一次性載入並快取的單例；找不到資產時回傳 null（呼叫端應退回舊載入方式）。</summary>
    public static UiSpriteLibrary Instance
    {
        get
        {
            if (!instanceLoaded)
            {
                instance = Resources.Load<UiSpriteLibrary>(ResourcesPath);
                instanceLoaded = true;
                if (instance == null)
                {
                    Debug.LogWarning(
                        $"UiSpriteLibrary: 找不到 Resources/{ResourcesPath}.asset，" +
                        "請執行 Tools/UI/Create or Refresh UI Sprite Library；暫時回退舊載入方式。");
                }
            }
            return instance;
        }
    }

#if UNITY_EDITOR
    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetCoach(Sprite neutral, Sprite alert, Sprite serious, Sprite encourage)
    {
        coachNeutral = neutral;
        coachAlert = alert;
        coachSerious = serious;
        coachEncourage = encourage;
    }

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetReturnButton(Sprite sprite)
    {
        returnButton = sprite;
    }

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetBattleScene(Sprite harborBay, Sprite previewPanel)
    {
        harborBayBackground = harborBay;
        battlePreviewPanel = previewPanel;
    }

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetDifficulty(Sprite intro, Sprite easy, Sprite normal, Sprite hard, Sprite boss)
    {
        difficultyIntro = intro;
        difficultyEasy = easy;
        difficultyNormal = normal;
        difficultyHard = hard;
        difficultyBoss = boss;
    }

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetRarityFrames(Sprite n, Sprite r, Sprite sr, Sprite ssr, Sprite ur)
    {
        rarityFrameN = n;
        rarityFrameR = r;
        rarityFrameSr = sr;
        rarityFrameSsr = ssr;
        rarityFrameUr = ur;
    }
#endif
}
