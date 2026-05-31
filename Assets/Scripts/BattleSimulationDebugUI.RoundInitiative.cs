using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI
{
    private const float RoundInitiativeHudMarginRight = 40f;
    private const float RoundInitiativeHudMarginBottom = 36f;
    private const float RoundInitiativeHudWidth = 380f;
    private const float RoundInitiativeHudHeight = 96f;
    private const float RoundInitiativeHudPadH = 14f;
    private const float RoundInitiativeHudPadV = 8f;
    private const float RoundInitiativeRoundFontSize = 28f;
    private const float RoundInitiativeDetailFontSize = 24f;
    private const float RoundInitiativeLegendFontSize = 20f;
    private const float RoundInitiativeLegendWidth = 52f;
    private const float RoundInitiativeStrikeSwatchSize = 30f;
    private const float RoundInitiativePlayerSwatchX = 56f;
    private const float RoundInitiativeEnemySwatchX = 96f;

    private RectTransform roundInitiativeHudRt;
    private TMP_Text roundInitiativeRoundTmp;
    private TMP_Text roundInitiativeDetailTmp;
    private Image roundInitiativePlayerStrikeSwatch;
    private Image roundInitiativeEnemyStrikeSwatch;
    private Outline roundInitiativePlayerStrikeOutline;
    private Outline roundInitiativeEnemyStrikeOutline;
    private GameObject roundInitiativeTextRow;
    private GameObject roundInitiativeSwatchRow;
    private int lastRoundInitiativeSyncRound = -1;
    private bool lastRoundInitiativePlayerStrikesFirst;
    private bool lastRoundInitiativeHudVisible;
    private bool lastRoundInitiativeIntroLayout;

    private void CreateRoundInitiativeHud(Transform canvasParent)
    {
        if (canvasParent == null || roundInitiativeHudRt != null) return;

        GameObject root = new GameObject("BattleRoundInitiativeHud", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasParent, false);
        roundInitiativeHudRt = root.GetComponent<RectTransform>();
        roundInitiativeHudRt.anchorMin = new Vector2(1f, 0f);
        roundInitiativeHudRt.anchorMax = new Vector2(1f, 0f);
        roundInitiativeHudRt.pivot = new Vector2(1f, 0f);
        roundInitiativeHudRt.anchoredPosition =
            new Vector2(-RoundInitiativeHudMarginRight, RoundInitiativeHudMarginBottom);
        roundInitiativeHudRt.sizeDelta = new Vector2(RoundInitiativeHudWidth, RoundInitiativeHudHeight);

        Image bg = root.GetComponent<Image>();
        bg.color = BattleUiColors.TurnBg;
        bg.raycastTarget = false;

        Shadow sh = root.AddComponent<Shadow>();
        sh.effectColor = BattleUiColors.ShadowUi;
        sh.effectDistance = new Vector2(3f, -4f);

        GameObject roundRow = new GameObject("RoundRow", typeof(RectTransform));
        roundRow.transform.SetParent(root.transform, false);
        RectTransform roundRowRt = roundRow.GetComponent<RectTransform>();
        roundRowRt.anchorMin = new Vector2(0f, 0.55f);
        roundRowRt.anchorMax = new Vector2(1f, 1f);
        roundRowRt.offsetMin = new Vector2(RoundInitiativeHudPadH, 0f);
        roundRowRt.offsetMax = new Vector2(-RoundInitiativeHudPadH, -6f);

        GameObject roundObj = new GameObject("RoundLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        roundObj.transform.SetParent(roundRow.transform, false);
        RectTransform roundRt = roundObj.GetComponent<RectTransform>();
        roundRt.anchorMin = Vector2.zero;
        roundRt.anchorMax = Vector2.one;
        roundRt.offsetMin = Vector2.zero;
        roundRt.offsetMax = Vector2.zero;
        roundInitiativeRoundTmp = roundObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) roundInitiativeRoundTmp.font = sharedUIFont;
        roundInitiativeRoundTmp.fontSize = RoundInitiativeRoundFontSize;
        roundInitiativeRoundTmp.alignment = TextAlignmentOptions.Left;
        roundInitiativeRoundTmp.color = BattleUiColors.TurnPlayer;
        roundInitiativeRoundTmp.raycastTarget = false;
        roundInitiativeRoundTmp.text = "第 1 回合";

        roundInitiativeTextRow = new GameObject("FirstStrikeTextRow", typeof(RectTransform));
        roundInitiativeTextRow.transform.SetParent(root.transform, false);
        RectTransform textRowRt = roundInitiativeTextRow.GetComponent<RectTransform>();
        textRowRt.anchorMin = new Vector2(0f, 0f);
        textRowRt.anchorMax = new Vector2(1f, 0.52f);
        textRowRt.offsetMin = new Vector2(RoundInitiativeHudPadH, RoundInitiativeHudPadV + 2f);
        textRowRt.offsetMax = new Vector2(-RoundInitiativeHudPadH, 0f);

        GameObject detailObj = new GameObject("FirstStrikeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        detailObj.transform.SetParent(roundInitiativeTextRow.transform, false);
        RectTransform detailRt = detailObj.GetComponent<RectTransform>();
        detailRt.anchorMin = Vector2.zero;
        detailRt.anchorMax = Vector2.one;
        detailRt.offsetMin = Vector2.zero;
        detailRt.offsetMax = Vector2.zero;
        roundInitiativeDetailTmp = detailObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) roundInitiativeDetailTmp.font = sharedUIFont;
        roundInitiativeDetailTmp.fontSize = RoundInitiativeDetailFontSize;
        roundInitiativeDetailTmp.alignment = TextAlignmentOptions.Left;
        roundInitiativeDetailTmp.color = BattleUiColors.TurnBannerText;
        roundInitiativeDetailTmp.raycastTarget = false;
        roundInitiativeDetailTmp.text = "先攻 我方";

        roundInitiativeSwatchRow = new GameObject("FirstStrikeSwatchRow", typeof(RectTransform));
        roundInitiativeSwatchRow.transform.SetParent(root.transform, false);
        RectTransform swatchRowRt = roundInitiativeSwatchRow.GetComponent<RectTransform>();
        swatchRowRt.anchorMin = new Vector2(0f, 0f);
        swatchRowRt.anchorMax = new Vector2(1f, 0.52f);
        swatchRowRt.offsetMin = new Vector2(RoundInitiativeHudPadH, RoundInitiativeHudPadV + 2f);
        swatchRowRt.offsetMax = new Vector2(-RoundInitiativeHudPadH, 0f);

        GameObject swatchLegendObj = new GameObject("SwatchLegend", typeof(RectTransform), typeof(TextMeshProUGUI));
        swatchLegendObj.transform.SetParent(roundInitiativeSwatchRow.transform, false);
        RectTransform legendRt = swatchLegendObj.GetComponent<RectTransform>();
        legendRt.anchorMin = new Vector2(0f, 0.5f);
        legendRt.anchorMax = new Vector2(0f, 0.5f);
        legendRt.pivot = new Vector2(0f, 0.5f);
        legendRt.anchoredPosition = Vector2.zero;
        legendRt.sizeDelta = new Vector2(RoundInitiativeLegendWidth, 32f);
        TMP_Text legendTmp = swatchLegendObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) legendTmp.font = sharedUIFont;
        legendTmp.fontSize = RoundInitiativeLegendFontSize;
        legendTmp.alignment = TextAlignmentOptions.Left;

        roundInitiativePlayerStrikeSwatch = CreateFirstStrikeSwatch(
            roundInitiativeSwatchRow.transform,
            "PlayerFirstStrikeSwatch",
            new Vector2(RoundInitiativePlayerSwatchX, 0f),
            BattleUiColors.AllyHp);
        roundInitiativeEnemyStrikeSwatch = CreateFirstStrikeSwatch(
            roundInitiativeSwatchRow.transform,
            "EnemyFirstStrikeSwatch",
            new Vector2(RoundInitiativeEnemySwatchX, 0f),
            BattleUiColors.FoeHp);
        legendTmp.color = BattleUiColors.TurnBannerText;
        legendTmp.text = "先攻";
        legendTmp.raycastTarget = false;

        root.SetActive(false);
        lastRoundInitiativeHudVisible = false;
    }

    private Image CreateFirstStrikeSwatch(Transform parent, string name, Vector2 anchoredPos, Color baseColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(RoundInitiativeStrikeSwatchSize, RoundInitiativeStrikeSwatchSize);

        Image img = go.GetComponent<Image>();
        img.sprite = GetUnitWhiteSprite();
        img.color = baseColor;
        img.raycastTarget = false;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = BattleUiColors.TurnPlayer;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.enabled = false;

        if (name.Contains("Player"))
            roundInitiativePlayerStrikeOutline = outline;
        else
            roundInitiativeEnemyStrikeOutline = outline;

        return img;
    }

    private void SyncRoundInitiativeHud()
    {
        if (roundInitiativeHudRt == null || battleManager == null) return;

        bool visible = !BattleAutoSimPlugin.IsRunning
                       && !battleManager.IsBattleOver()
                       && !battleManager.IsOpeningPresentationInProgress();

        if (roundInitiativeHudRt.gameObject.activeSelf != visible)
            roundInitiativeHudRt.gameObject.SetActive(visible);

        lastRoundInitiativeHudVisible = visible;
        if (!visible) return;

        int round = battleManager.GetCurrentRound();
        bool playerStrikesFirst = battleManager.DoesPlayerStrikeFirstThisRound();
        bool introLayout = BattleLaunchContext.IsIntroTutorialBattle;

        if (round == lastRoundInitiativeSyncRound
            && playerStrikesFirst == lastRoundInitiativePlayerStrikesFirst
            && introLayout == lastRoundInitiativeIntroLayout
            && visible == lastRoundInitiativeHudVisible)
            return;

        lastRoundInitiativeSyncRound = round;
        lastRoundInitiativePlayerStrikesFirst = playerStrikesFirst;
        lastRoundInitiativeIntroLayout = introLayout;

        if (roundInitiativeRoundTmp != null)
            roundInitiativeRoundTmp.text = "第 " + round + " 回合";

        if (roundInitiativeTextRow != null)
            roundInitiativeTextRow.SetActive(!introLayout);
        if (roundInitiativeSwatchRow != null)
            roundInitiativeSwatchRow.SetActive(introLayout);

        if (!introLayout)
        {
            if (roundInitiativeDetailTmp != null)
            {
                roundInitiativeDetailTmp.text = playerStrikesFirst ? "先攻 我方" : "先攻 敵方";
                roundInitiativeDetailTmp.color = playerStrikesFirst
                    ? BattleUiColors.AllyLabel
                    : BattleUiColors.TurnEnemy;
            }
        }
        else
        {
            ApplyFirstStrikeSwatchHighlight(playerStrikesFirst);
        }
    }

    private void ApplyFirstStrikeSwatchHighlight(bool playerStrikesFirst)
    {
        ApplySwatchState(
            roundInitiativePlayerStrikeSwatch,
            roundInitiativePlayerStrikeOutline,
            playerStrikesFirst,
            BattleUiColors.AllyHp);
        ApplySwatchState(
            roundInitiativeEnemyStrikeSwatch,
            roundInitiativeEnemyStrikeOutline,
            !playerStrikesFirst,
            BattleUiColors.FoeHp);
    }

    private static void ApplySwatchState(Image img, Outline outline, bool active, Color baseColor)
    {
        if (img == null) return;

        img.color = active
            ? baseColor
            : new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f);
        img.transform.localScale = active ? Vector3.one * 1.1f : Vector3.one;

        if (outline != null)
            outline.enabled = active;
    }
}
