/// <summary>玩家打出主教後，選擇祝聖綁定對象的 UI 請求。</summary>
public readonly struct BishopConsecrationBindChoiceRequest
{
    public readonly string bishopDisplayName;

    public BishopConsecrationBindChoiceRequest(string bishopDisplayName)
    {
        this.bishopDisplayName = string.IsNullOrWhiteSpace(bishopDisplayName) ? "主教" : bishopDisplayName.Trim();
    }
}
