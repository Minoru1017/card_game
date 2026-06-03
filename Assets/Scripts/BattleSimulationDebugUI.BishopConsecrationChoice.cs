using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    private RectTransform consecrationChoicePanelRt;
    private TextMeshProUGUI consecrationChoiceTitleTmp;
    private Button consecrationBindBishopButton;
    private Button consecrationBindNextMonsterButton;

    private void EnsureBishopConsecrationChoiceUi(Transform parent)
    {
        if (uiRoot == null || consecrationChoicePanelRt != null) return;

        GameObject panelObj = new GameObject("BishopConsecrationChoicePanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(uiRoot, false);
        consecrationChoicePanelRt = panelObj.GetComponent<RectTransform>();
        consecrationChoicePanelRt.anchorMin = new Vector2(0.5f, 0.5f);
        consecrationChoicePanelRt.anchorMax = new Vector2(0.5f, 0.5f);
        consecrationChoicePanelRt.pivot = new Vector2(0.5f, 0.5f);
        consecrationChoicePanelRt.sizeDelta = new Vector2(980f, 320f);
        panelObj.GetComponent<Image>().color = BattleUiColors.PanelCream96;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.55f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(24f, 0f);
        titleRt.offsetMax = new Vector2(-24f, -16f);
        consecrationChoiceTitleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyBattleRichTextTmpFont(consecrationChoiceTitleTmp);
        consecrationChoiceTitleTmp.fontSize = 38f;
        consecrationChoiceTitleTmp.alignment = TextAlignmentOptions.Center;
        consecrationChoiceTitleTmp.color = BattleUiColors.Ink;
        consecrationChoiceTitleTmp.enableWordWrapping = true;
        consecrationChoiceTitleTmp.richText = true;
        consecrationChoiceTitleTmp.raycastTarget = false;

        consecrationBindBishopButton = CreateConsecrationChoiceButton(
            panelObj.transform,
            "BindCurrentBishopButton",
            "綁定場上主教",
            new Vector2(-250f, -72f),
            OnConsecrationChoiceBindCurrentBishop);
        consecrationBindNextMonsterButton = CreateConsecrationChoiceButton(
            panelObj.transform,
            "BindNextMonsterButton",
            "綁定下一張場怪",
            new Vector2(250f, -72f),
            OnConsecrationChoiceBindNextMonster);

        panelObj.SetActive(false);
    }

    private Button CreateConsecrationChoiceButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPos,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(420f, 88f);

        Image image = buttonObj.GetComponent<Image>();
        image.color = BattleUiColors.BtnPrimary;
        image.raycastTarget = true;

        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(action);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(buttonObj.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 8f);
        labelRect.offsetMax = new Vector2(-12f, -8f);

        TextMeshProUGUI tmp = labelObj.GetComponent<TextMeshProUGUI>();
        ApplyBattleRichTextTmpFont(tmp);
        tmp.text = label;
        tmp.fontSize = 34f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = BattleUiColors.BtnPrimaryText;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        BattleUiColors.ApplyButtonStyle(button, name);
        return button;
    }

    private void OnBishopConsecrationBindChoiceRequested(BishopConsecrationBindChoiceRequest request)
    {
        TickBishopConsecrationChoiceUi();
    }

    private void OnConsecrationChoiceBindCurrentBishop()
    {
        if (battleManager == null) return;
        battleManager.PlayerChooseConsecrationBindToCurrentBishop();
        TickBishopConsecrationChoiceUi();
        RefreshFieldCards();
    }

    private void OnConsecrationChoiceBindNextMonster()
    {
        if (battleManager == null) return;
        battleManager.PlayerChooseConsecrationBindToNextMonster();
        TickBishopConsecrationChoiceUi();
        RefreshFieldCards();
    }

    private void TickBishopConsecrationChoiceUi()
    {
        if (battleManager == null) return;
        bool active = battleManager.IsPlayerAwaitingConsecrationBindChoice();
        if (consecrationChoicePanelRt != null)
            consecrationChoicePanelRt.gameObject.SetActive(active);
        if (!active || consecrationChoiceTitleTmp == null) return;

        ApplyBattleRichTextTmpFont(consecrationChoiceTitleTmp);

        Card fieldCard = battleManager.GetPlayerFieldCard();
        string bishopName = fieldCard != null ? fieldCard.cardName : "主教";
        if (string.IsNullOrWhiteSpace(bishopName))
            bishopName = "主教";
        consecrationChoiceTitleTmp.text =
            "<size=46><b>主教 祝聖預留</b></size>\n" +
            "<size=34>請選擇祝聖綁定對象(本局僅1次)</size>\n" +
            "<size=30>場上 <color=#5A3D8C>" + bishopName + "</color></size>";
    }
}
