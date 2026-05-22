using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>卡牌詳情面板宿主（背包 DeckManager、商店開包 CardInspectPanelHost 等）。</summary>
public interface ICardInspectPanelHost
{
    void EnsureCoreRefsForInspect();
    Canvas BackpackInspectResolveCanvas();
    TMP_FontAsset BackpackInspectResolveFont();
    string BackpackInspectDeckInclusionText(int cardId);
    int BackpackInspectCollectionCount(int cardId);
    void BackpackInspectFillCollectionIds(List<int> ids);
    Card BackpackInspectGetCard(int cardId);
    void ShowBackpackProficiencyHelp();
    void ShowCardInspect(Card card, CardDisplay sourceDisplay = null);
    void HideCardInspect();
}
