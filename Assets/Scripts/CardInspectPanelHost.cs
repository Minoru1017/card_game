using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>商店等無 DeckManager 場景：提供卡牌詳情面板所需資料與 UI 解析。</summary>
[DisallowMultipleComponent]
public class CardInspectPanelHost : MonoBehaviour, ICardInspectPanelHost
{
    public PlayerData playerData;
    public CardStore cardStore;

    private BackpackCardInspectPanel inspectPanel;
    private BackpackProficiencyHelpDialog proficiencyHelpDialog;

    public void Assign(PlayerData pd, CardStore store)
    {
        if (pd != null) playerData = pd;
        if (store != null) cardStore = store;
    }

    private BackpackCardInspectPanel InspectPanel
    {
        get
        {
            if (inspectPanel == null)
            {
                inspectPanel = GetComponent<BackpackCardInspectPanel>();
                if (inspectPanel == null)
                    inspectPanel = gameObject.AddComponent<BackpackCardInspectPanel>();
                inspectPanel.BindHost(this);
            }
            return inspectPanel;
        }
    }

    public void EnsureCoreRefsForInspect()
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        if (cardStore == null) cardStore = Object.FindFirstObjectByType<CardStore>();
        if (cardStore == null && playerData != null) cardStore = playerData.CardStore;
    }

    public Canvas BackpackInspectResolveCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isActiveAndEnabled) continue;
            if (!c.gameObject.scene.IsValid()) continue;
            return c;
        }
        return null;
    }

    public TMP_FontAsset BackpackInspectResolveFont()
    {
        TMP_FontAsset buildbeck = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (buildbeck != null) return buildbeck;
        return TMP_Settings.defaultFontAsset;
    }

    public string BackpackInspectDeckInclusionText(int cardId)
    {
        if (playerData == null) return "尚未加入牌組";
        if (playerData.GetSelectedDeckCount(cardId) <= 0) return "尚未加入目前牌組";
        string deckName = playerData.GetDeckSlotDisplayName(playerData.selectedDeckSlot);
        if (string.IsNullOrWhiteSpace(deckName)) deckName = "牌組";
        return $"已含在牌組 {deckName.Trim()}";
    }

    public int BackpackInspectCollectionCount(int cardId) =>
        playerData != null ? playerData.GetCollectionCount(cardId) : 0;

    public void BackpackInspectFillCollectionIds(List<int> ids)
    {
        if (ids == null || playerData == null) return;
        foreach (var kv in playerData.playerCollection)
        {
            if (kv.Value <= 0) continue;
            if (cardStore != null && cardStore.GetCardById(kv.Key) == null) continue;
            ids.Add(kv.Key);
        }
    }

    public Card BackpackInspectGetCard(int cardId) =>
        cardStore != null ? cardStore.GetCardById(cardId) : null;

    public void ShowCardInspect(Card card, CardDisplay sourceDisplay = null)
    {
        if (card == null) return;
        EnsureCoreRefsForInspect();
        InspectPanel.Show(card, sourceDisplay);
    }

    public void HideCardInspect()
    {
        if (inspectPanel != null)
            inspectPanel.Hide();
    }

    public void ShowBackpackProficiencyHelp()
    {
        Canvas canvas = BackpackInspectResolveCanvas();
        if (canvas == null) return;
        if (proficiencyHelpDialog == null)
        {
            proficiencyHelpDialog = GetComponent<BackpackProficiencyHelpDialog>();
            if (proficiencyHelpDialog == null)
                proficiencyHelpDialog = gameObject.AddComponent<BackpackProficiencyHelpDialog>();
        }
        proficiencyHelpDialog.Show(canvas, BackpackInspectResolveFont());
    }

    void Update()
    {
        if (inspectPanel == null || !inspectPanel.IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            HideCardInspect();
        inspectPanel.TickSwipeInput();
    }
}
