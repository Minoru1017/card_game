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
    [Tooltip("對戰暫停鍵 — Assets/UI/pause-button.png（sprite: pause-button_0）")]
    [SerializeField] private Sprite battlePauseButton;
    [SerializeField] private Sprite responsiveBasePlate;

    [Header("對戰場景")]
    [SerializeField] private Sprite harborBayBackground;
    [SerializeField] private Sprite classroomBackground;
    [SerializeField] private Sprite classroomHorrorBackground;
    [SerializeField] private Sprite classroomCoachPracticeBackground;
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

    [Header("鬥鳥 CD 封面")]
    [Tooltip("《港灣練習帶》— Assets/UI/CD/CD_1")]
    [SerializeField] private Sprite birdDuelCdHarborPractice;
    [Tooltip("《庭訓進行曲》— Assets/UI/CD/CD_2")]
    [SerializeField] private Sprite birdDuelCdCourtMarch;

    [Header("戰鬥狀態演出")]
    [Tooltip("You will die 致死預警 — Assets/UI/Combat status/You will die.png（sprite: You will die_0，完整圖示）")]
    [SerializeField] private Sprite youWillDieIcon;

    [Header("Story progress 地圖節點")]
    [Tooltip("1-1 教學關通關前 M-1-1 節點圖示 — Assets/UI/1-1 Instruction.png")]
    [SerializeField] private Sprite intro11InstructionIcon;
    [Tooltip("1-1 教學畢業後 M-1-1 實戰區節點圖示 — Assets/UI/1-1 Practical Application.png")]
    [SerializeField] private Sprite intro11PracticalApplicationIcon;
    [Tooltip("1-1／1-2 完全通關節點圖示 — Assets/UI/Clear.png")]
    [SerializeField] private Sprite storyProgressClearIcon;

    public Sprite ReturnButton => returnButton;
    /// <summary>對戰暫停鍵圖示。</summary>
    public Sprite BattlePauseButton => battlePauseButton;
    /// <summary>響應式左右底板（361×1080）；右側以水平翻轉共用同一張。</summary>
    public Sprite ResponsiveBasePlate => responsiveBasePlate;
    public Sprite HarborBayBackground => harborBayBackground;
    /// <summary>教室背景（Classroom_FE）；M-1-2 階段 A 段考一般狀態。</summary>
    public Sprite ClassroomBackground => classroomBackground;
    /// <summary>教室恐怖背景（Classroom_FR）；M-1-2 階段 A 段考恐怖狀態。</summary>
    public Sprite ClassroomHorrorBackground => classroomHorrorBackground;
    /// <summary>教室加練背景（Classroom_DA）；M-1-2 階段 B 教練實戰。</summary>
    public Sprite ClassroomCoachPracticeBackground => classroomCoachPracticeBackground;
    public Sprite BattlePreviewPanel => battlePreviewPanel;
    /// <summary>「You will die」致死預警全屏圖示；白色線稿，需搭配暗色底顯示。</summary>
    public Sprite YouWillDieIcon => youWillDieIcon;

    /// <summary>1-1 教學關通關前，Story progress 地圖 M-1-1 節點圖示。</summary>
    public Sprite Intro11InstructionIcon => intro11InstructionIcon;

    /// <summary>1-1 教學畢業後（實戰區），Story progress 地圖 M-1-1 節點圖示。</summary>
    public Sprite Intro11PracticalApplicationIcon => intro11PracticalApplicationIcon;

    /// <summary>1-1／1-2 完全通關時，Story progress 地圖節點圖示。</summary>
    public Sprite StoryProgressClearIcon => storyProgressClearIcon;

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

    /// <summary>CD 光碟封面；未註冊的 cdId 回傳 null。</summary>
    public Sprite GetBirdDuelCdCover(string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId))
            return null;

        if (string.Equals(cdId.Trim(), BirdDuelCdCatalog.DefaultCdId, System.StringComparison.Ordinal))
            return birdDuelCdHarborPractice;

        if (string.Equals(cdId.Trim(), "court_march", System.StringComparison.OrdinalIgnoreCase))
            return birdDuelCdCourtMarch;

        return null;
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
    public void EditorSetBattlePauseButton(Sprite sprite)
    {
        battlePauseButton = sprite;
    }

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetResponsiveBasePlate(Sprite sprite)
    {
        responsiveBasePlate = sprite;
    }

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetBattleScene(
        Sprite harborBay,
        Sprite previewPanel,
        Sprite classroom,
        Sprite classroomHorror = null,
        Sprite classroomCoach = null)
    {
        harborBayBackground = harborBay;
        battlePreviewPanel = previewPanel;
        classroomBackground = classroom;
        classroomHorrorBackground = classroomHorror;
        classroomCoachPracticeBackground = classroomCoach;
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

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetBirdDuelCdCovers(Sprite harborPracticeTape, Sprite courtMarch = null)
    {
        birdDuelCdHarborPractice = harborPracticeTape;
        birdDuelCdCourtMarch = courtMarch;
    }

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetCombatStatus(Sprite youWillDie)
    {
        youWillDieIcon = youWillDie;
    }

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetStoryProgressNodeIcons(
        Sprite intro11Instruction,
        Sprite intro11PracticalApplication = null,
        Sprite storyProgressClear = null)
    {
        intro11InstructionIcon = intro11Instruction;
        intro11PracticalApplicationIcon = intro11PracticalApplication;
        storyProgressClearIcon = storyProgressClear;
    }
#endif
}
