/// <summary>主教戰技 UI 特效請求（祝聖預留／綁定／首傷減傷）。</summary>
public enum BishopConsecrationVisualKind
{
    /// <summary>本局首次將主教置場，授予祝聖預留。</summary>
    ReserveGranted,
    /// <summary>祝聖綁定至下一隻場上怪獸。</summary>
    BoundToField,
    /// <summary>綁定怪獸首次受傷觸發祝聖／宗教連攜減傷。</summary>
    FirstHitReduced
}

public readonly struct BishopConsecrationVisualRequest
{
    public readonly BishopConsecrationVisualKind kind;
    /// <summary>true=特效目標為我方場上怪；false=敵方場上怪。</summary>
    public readonly bool onPlayerSide;
    public readonly int reductionAmount;
    public readonly bool religiousSynergy;
    public readonly bool holyTherapyLinkOnNun;

    public BishopConsecrationVisualRequest(
        BishopConsecrationVisualKind kind,
        bool onPlayerSide,
        int reductionAmount = 0,
        bool religiousSynergy = false,
        bool holyTherapyLinkOnNun = false)
    {
        this.kind = kind;
        this.onPlayerSide = onPlayerSide;
        this.reductionAmount = reductionAmount > 0 ? reductionAmount : 3;
        this.religiousSynergy = religiousSynergy;
        this.holyTherapyLinkOnNun = holyTherapyLinkOnNun;
    }
}
