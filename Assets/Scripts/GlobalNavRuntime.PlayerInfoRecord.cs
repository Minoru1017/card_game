using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class GlobalNavRuntime : MonoBehaviour
{
    private void BuildPlayerInfoRecordPanel(Transform recordBody, ref float rowY)
    {
        const float barHeight = 44f;
        const float columnAreaHeight = 118f;
        const float summaryHeight = 36f;
        float filterTop = rowY;

        GameObject filterBar = new GameObject("RecordFilterBar", typeof(RectTransform), typeof(Image));
        filterBar.transform.SetParent(recordBody, false);
        RectTransform filterBarRt = filterBar.GetComponent<RectTransform>();
        filterBarRt.anchorMin = new Vector2(0f, 1f);
        filterBarRt.anchorMax = new Vector2(1f, 1f);
        filterBarRt.pivot = new Vector2(0.5f, 1f);
        filterBarRt.anchoredPosition = new Vector2(0f, filterTop);
        filterBarRt.sizeDelta = new Vector2(0f, barHeight);
        filterBar.GetComponent<Image>().color = new Color(0.72f, 0.80f, 0.86f, 0.55f);

        int filterCount = PlayerInfoRecordFilterLabels.Length;
        for (int i = 0; i < filterCount; i++)
        {
            int filterCode = PlayerInfoRecordFilterCodes[i];
            string tabLabel = PlayerInfoRecordFilterLabels[i];
            float minX = (float)i / filterCount;
            float maxX = (float)(i + 1) / filterCount;

            GameObject tabObj = new GameObject("Filter_" + tabLabel, typeof(RectTransform), typeof(Image), typeof(Button));
            tabObj.transform.SetParent(filterBar.transform, false);
            RectTransform tabRt = tabObj.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(minX, 0f);
            tabRt.anchorMax = new Vector2(maxX, 1f);
            tabRt.offsetMin = new Vector2(5f, 5f);
            tabRt.offsetMax = new Vector2(-5f, -5f);

            Image tabBg = tabObj.GetComponent<Image>();
            tabBg.color = new Color(1f, 1f, 1f, 0f);
            Button tabBtn = tabObj.GetComponent<Button>();
            tabBtn.onClick.AddListener(() =>
            {
                playerInfoActiveRecordFilter = filterCode;
                RefreshPlayerInfoOverlayContent();
            });

            GameObject tabLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            tabLabelObj.transform.SetParent(tabObj.transform, false);
            RectTransform tabLabelRt = tabLabelObj.GetComponent<RectTransform>();
            tabLabelRt.anchorMin = Vector2.zero;
            tabLabelRt.anchorMax = Vector2.one;
            tabLabelRt.offsetMin = Vector2.zero;
            tabLabelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI tabTmp = tabLabelObj.GetComponent<TextMeshProUGUI>();
            if (navLabelFont != null) tabTmp.font = navLabelFont;
            tabTmp.text = tabLabel;
            tabTmp.fontSize = 24f;
            tabTmp.fontStyle = FontStyles.Bold;
            tabTmp.alignment = TextAlignmentOptions.Center;
            tabTmp.color = new Color(0.92f, 0.95f, 0.98f, 1f);
            tabTmp.raycastTarget = false;

            playerInfoRecordFilterButtons[i] = tabBtn;
            playerInfoRecordFilterButtonBgs[i] = tabBg;
            playerInfoRecordFilterLabels[i] = tabTmp;
        }

        rowY -= barHeight + 14f;
        float columnTop = rowY;

        GameObject columnsRoot = new GameObject("DifficultyColumns", typeof(RectTransform));
        columnsRoot.transform.SetParent(recordBody, false);
        RectTransform columnsRt = columnsRoot.GetComponent<RectTransform>();
        columnsRt.anchorMin = new Vector2(0f, 1f);
        columnsRt.anchorMax = new Vector2(1f, 1f);
        columnsRt.pivot = new Vector2(0.5f, 1f);
        columnsRt.anchoredPosition = new Vector2(0f, columnTop);
        columnsRt.sizeDelta = new Vector2(0f, columnAreaHeight);

        int columnCount = PlayerProfileCsvService.StandardDifficultyLabelsZh.Length;
        playerInfoRecordColumns = new PlayerInfoRecordColumnUi[columnCount];

        for (int i = 0; i < columnCount; i++)
        {
            string diffLabel = PlayerProfileCsvService.StandardDifficultyLabelsZh[i];
            float minX = (float)i / columnCount;
            float maxX = (float)(i + 1) / columnCount;

            GameObject colObj = new GameObject("Col_" + diffLabel, typeof(RectTransform));
            colObj.transform.SetParent(columnsRoot.transform, false);
            RectTransform colRt = colObj.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(minX, 0f);
            colRt.anchorMax = new Vector2(maxX, 1f);
            colRt.offsetMin = new Vector2(3f, 0f);
            colRt.offsetMax = new Vector2(-3f, 0f);

            GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(colObj.transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.06f, 1f);
            badgeRt.anchorMax = new Vector2(0.94f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f);
            badgeRt.anchoredPosition = new Vector2(0f, -4f);
            badgeRt.sizeDelta = new Vector2(0f, 36f);
            Image badgeImg = badgeObj.GetComponent<Image>();
            badgeImg.color = PlayerInfoDifficultyBadgeColors[i];

            GameObject badgeTextObj = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            RectTransform badgeTextRt = badgeTextObj.GetComponent<RectTransform>();
            badgeTextRt.anchorMin = Vector2.zero;
            badgeTextRt.anchorMax = Vector2.one;
            badgeTextRt.offsetMin = Vector2.zero;
            badgeTextRt.offsetMax = Vector2.zero;
            TextMeshProUGUI badgeTmp = badgeTextObj.GetComponent<TextMeshProUGUI>();
            if (navLabelFont != null) badgeTmp.font = navLabelFont;
            badgeTmp.text = diffLabel;
            badgeTmp.fontSize = 18f;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = Color.white;
            badgeTmp.raycastTarget = false;

            GameObject countObj = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(colObj.transform, false);
            RectTransform countRt = countObj.GetComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0f, 1f);
            countRt.anchorMax = new Vector2(1f, 1f);
            countRt.pivot = new Vector2(0.5f, 1f);
            countRt.anchoredPosition = new Vector2(0f, -50f);
            countRt.sizeDelta = new Vector2(0f, 44f);
            TextMeshProUGUI countTmp = countObj.GetComponent<TextMeshProUGUI>();
            if (navLabelFont != null) countTmp.font = navLabelFont;
            countTmp.text = "0";
            countTmp.fontSize = 26f;
            countTmp.fontStyle = FontStyles.Bold;
            countTmp.alignment = TextAlignmentOptions.Center;
            countTmp.color = new Color(0.35f, 0.38f, 0.42f, 1f);
            countTmp.raycastTarget = false;

            playerInfoRecordColumns[i] = new PlayerInfoRecordColumnUi
            {
                badgeImage = badgeImg,
                badgeText = badgeTmp,
                countText = countTmp
            };
        }

        rowY -= columnAreaHeight + 12f;
        playerInfoRecordTotalText = CreateProfileTextLine(
            recordBody,
            "RecordFilterTotal",
            ref rowY,
            summaryHeight,
            17f,
            PlayerInfoTextMuted,
            string.Empty,
            true,
            false,
            2f);
        rowY -= PlayerInfoLineGap;
    }

    private void RefreshPlayerInfoRecordPanel(PlayerProfileCsvService.PlayerProfile p)
    {
        if (playerInfoRecordColumns == null || playerInfoRecordColumns.Length == 0) return;

        int[] counts = PlayerProfileCsvService.GetDifficultyCountsForResult(p, playerInfoActiveRecordFilter);
        for (int i = 0; i < playerInfoRecordColumns.Length && i < counts.Length; i++)
        {
            if (playerInfoRecordColumns[i]?.countText != null)
                playerInfoRecordColumns[i].countText.text = Mathf.Max(0, counts[i]).ToString();
        }

        for (int i = 0; i < PlayerInfoRecordFilterCodes.Length; i++)
        {
            bool active = PlayerInfoRecordFilterCodes[i] == playerInfoActiveRecordFilter;
            if (playerInfoRecordFilterButtonBgs[i] != null)
                playerInfoRecordFilterButtonBgs[i].color = active ? Color.white : new Color(1f, 1f, 1f, 0f);
            if (playerInfoRecordFilterLabels[i] != null)
                playerInfoRecordFilterLabels[i].color = active
                    ? new Color(0.22f, 0.26f, 0.30f, 1f)
                    : new Color(0.92f, 0.95f, 0.98f, 1f);
        }

        if (playerInfoRecordTotalText != null)
            playerInfoRecordTotalText.text = PlayerProfileCsvService.BuildBattleRecordPanelSummary(p, playerInfoActiveRecordFilter);
    }
}
