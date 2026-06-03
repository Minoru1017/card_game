/// <summary>城堡戰技「堅城駐守」首次受傷減傷 UI 特效請求。</summary>
public readonly struct CastleFortressStandVisualRequest
{
    /// <summary>true=我方場上城堡；false=敵方場上城堡。</summary>
    public readonly bool onPlayerSide;
    public readonly int reductionAmount;

    public CastleFortressStandVisualRequest(bool onPlayerSide, int reductionAmount)
    {
        this.onPlayerSide = onPlayerSide;
        this.reductionAmount = reductionAmount > 0 ? reductionAmount : 5;
    }
}
