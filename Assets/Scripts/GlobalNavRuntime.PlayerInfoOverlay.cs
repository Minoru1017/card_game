using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class GlobalNavRuntime : MonoBehaviour
{
    private void TogglePlayerInfoPanel()
    {
        EnsurePlayerInfoOverlay();
        if (playerInfoOverlayRoot == null) return;
        if (playerInfoOverlayRoot.activeSelf)
        {
            playerInfoOverlayRoot.SetActive(false);
            RefreshBuildbeckDeckNameLabelsAfterPlayerInfo();
        }
        else OpenPlayerInfoOverlay();
    }

    private bool OpenPlayerInfoOverlay()
    {
        Input.imeCompositionMode = IMECompositionMode.On;
        CloseValuablesVaultOverlay();
        EnsurePlayerInfoOverlay();
        if (playerInfoOverlayRoot == null) return false;
        RefreshPlayerInfoOverlayContent();
        playerInfoOverlayRoot.transform.SetAsLastSibling();
        playerInfoOverlayRoot.SetActive(true);
        return true;
    }

    private static void RefreshBuildbeckDeckNameLabelsAfterPlayerInfo()
    {
        Scene s = SceneManager.GetActiveScene();
        if (!s.IsValid() || !s.name.Equals("Buildbeck", StringComparison.OrdinalIgnoreCase))
            return;
        GameObject dm = GameObject.Find("DataManager");
        if (dm == null) return;
        DeckManager deck = dm.GetComponent<DeckManager>();
        if (deck != null)
            deck.RefreshBuildbeckDeckNameDisplayFromMemory();
    }

    private void RefreshPlayerInfoOverlayContent()
    {
        PlayerProfileCsvService.PlayerProfile p = PlayerProfileCsvService.LoadProfileForPlayerInfoDisplay();
        PlayerData pd = PlayerData.ResolveCanonical();
        int coins = pd != null ? pd.playerCoins : (PlayerData.TryGetActiveSlotCoinsFromSave(out int coinsFromSave) ? coinsFromSave : 0);
        string slotName = PlayerData.GetActivePlayerSlotName();
        int slot = pd != null ? Mathf.Clamp(pd.activePlayerSlot, 1, PlayerData.MaxPlayerSlots) : 1;

        if (playerSlotNameInput != null) playerSlotNameInput.text = slotName;

        string uuidShort = string.IsNullOrWhiteSpace(p.uuid)
            ? "-"
            : (p.uuid.Length > 12 ? p.uuid.Substring(0, 8) + "..." : p.uuid);
        if (playerInfoUuidText != null)
            playerInfoUuidText.text = uuidShort;
        if (playerInfoRoleText != null)
            playerInfoRoleText.text = PlayerInfoProgressCopy.FormatRoleWithSlot(p.role, slot);
        if (playerInfoProgressText != null)
        {
            TutorialProgressState.SyncActiveSlotGraduationFromCollection();
            playerInfoProgressText.text = PlayerInfoProgressCopy.BuildSummary(slot);
            FitProfileValueTextHeight(playerInfoProgressText, 26f, 28f);
        }
        if (playerInfoStartDateText != null)
            playerInfoStartDateText.text = string.IsNullOrWhiteSpace(p.startDate) ? "-" : p.startDate;
        if (playerInfoCoinsText != null)
            playerInfoCoinsText.text = coins.ToString("N0");
        if (playerInfoDeckSummaryText != null)
        {
            playerInfoDeckSummaryText.text = FormatDeckSummaryForDisplay(p.decks);
            FitProfileValueTextHeight(playerInfoDeckSummaryText, 26f, 30f);
        }
        if (playerInfoHeroSummaryText != null)
            playerInfoHeroSummaryText.text = "無";
        if (playerInfoLastResultText != null)
            playerInfoLastResultText.text = string.IsNullOrWhiteSpace(p.lastResult) ? "-" : p.lastResult;
        RefreshPlayerInfoRecordPanel(p);
        RefreshBuildbeckDeckNameLabelsAfterPlayerInfo();
    }

    private static string FormatDeckSummaryForDisplay(string decks)
    {
        if (string.IsNullOrWhiteSpace(decks)) return "-";
        string[] parts = decks.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1) return decks.Trim();
        for (int i = 0; i < parts.Length; i++)
            parts[i] = parts[i].Trim();
        return string.Join("\n", parts);
    }

    private static void FitProfileValueTextHeight(TextMeshProUGUI valueField, float minHeight, float perLineHeight)
    {
        if (valueField == null) return;
        valueField.ForceMeshUpdate();
        int lineCount = valueField.textInfo != null ? Mathf.Max(1, valueField.textInfo.lineCount) : 1;
        float height = Mathf.Max(minHeight, lineCount * perLineHeight);
        RectTransform rt = valueField.rectTransform;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    private TextMeshProUGUI CreateInfoText(Transform parent, string name, Vector2 anchoredPos, Vector2 size, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (navLabelFont != null) tmp.font = navLabelFont;
        tmp.fontSize = fontSize;
        tmp.color = new Color(0.2f, 0.16f, 0.12f, 1f);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.text = string.Empty;
        return tmp;
    }
}
