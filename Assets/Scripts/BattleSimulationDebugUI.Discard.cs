using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    private RectTransform discardDropZoneRt;
    private TextMeshProUGUI discardDropZoneText;
    private RectTransform discardPromptPanelRt;
    private TextMeshProUGUI discardPromptText;
    private Image discardDropZoneImage;

    private void EnsureDiscardSelectionUi(Transform parent)
    {
        if (uiRoot == null || discardDropZoneRt != null) return;

        GameObject zoneObj = new GameObject("DiscardDropZone", typeof(RectTransform), typeof(Image));
        zoneObj.transform.SetParent(parent, false);
        discardDropZoneRt = zoneObj.GetComponent<RectTransform>();
        discardDropZoneRt.anchorMin = new Vector2(0f, 0f);
        discardDropZoneRt.anchorMax = new Vector2(0f, 1f);
        discardDropZoneRt.pivot = new Vector2(0f, 0.5f);
        discardDropZoneRt.offsetMin = new Vector2(0f, 0f);
        discardDropZoneRt.offsetMax = new Vector2(420f, 0f);
        Image zoneImg = zoneObj.GetComponent<Image>();
        discardDropZoneImage = zoneImg;
        zoneImg.sprite = null;
        zoneImg.type = Image.Type.Simple;
        zoneImg.preserveAspect = false;
        zoneImg.color = new Color(0.52f, 0.43f, 0.34f, 1f);
        zoneImg.raycastTarget = true;

        GameObject zoneLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        zoneLabelObj.transform.SetParent(zoneObj.transform, false);
        RectTransform labelRt = zoneLabelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(20f, 20f);
        labelRt.offsetMax = new Vector2(-20f, -20f);
        discardDropZoneText = zoneLabelObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) discardDropZoneText.font = sharedUIFont;
        discardDropZoneText.alignment = TextAlignmentOptions.Center;
        discardDropZoneText.fontSize = 26f;
        discardDropZoneText.color = new Color(0.98f, 0.94f, 0.86f, 1f);
        discardDropZoneText.enableWordWrapping = true;
        discardDropZoneText.text = "棄牌區";
        discardDropZoneText.raycastTarget = false;
        zoneObj.SetActive(false);

        GameObject promptObj = new GameObject("DiscardPromptPanel", typeof(RectTransform), typeof(Image));
        promptObj.transform.SetParent(uiRoot, false);
        discardPromptPanelRt = promptObj.GetComponent<RectTransform>();
        discardPromptPanelRt.anchorMin = new Vector2(0.5f, 0.83f);
        discardPromptPanelRt.anchorMax = new Vector2(0.5f, 0.83f);
        discardPromptPanelRt.pivot = new Vector2(0.5f, 0.5f);
        discardPromptPanelRt.sizeDelta = new Vector2(920f, 170f);
        promptObj.GetComponent<Image>().color = new Color(0.93f, 0.89f, 0.82f, 0.96f);

        GameObject promptTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        promptTextObj.transform.SetParent(promptObj.transform, false);
        RectTransform promptTextRt = promptTextObj.GetComponent<RectTransform>();
        promptTextRt.anchorMin = Vector2.zero;
        promptTextRt.anchorMax = Vector2.one;
        promptTextRt.offsetMin = new Vector2(14f, 8f);
        promptTextRt.offsetMax = new Vector2(-14f, -8f);
        discardPromptText = promptTextObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) discardPromptText.font = sharedUIFont;
        discardPromptText.fontSize = 42f;
        discardPromptText.alignment = TextAlignmentOptions.Center;
        discardPromptText.color = new Color(0.29f, 0.23f, 0.17f, 1f);
        discardPromptText.enableWordWrapping = true;
        discardPromptText.richText = true;
        discardPromptText.raycastTarget = false;
        promptObj.SetActive(false);
    }

    private void TickDiscardSelectionUi()
    {
        if (battleManager == null) return;
        bool active = battleManager.IsPlayerInDiscardSelection() && battleManager.IsPlayerTurn();
        int pending = battleManager.GetPlayerPendingDiscardCount();

        if (discardDropZoneRt != null)
        {
            discardDropZoneRt.gameObject.SetActive(active);
            if (!active) SetDiscardDropZoneHover(false);
            if (active && discardDropZoneText != null)
            {
                discardDropZoneText.text =
                    "<size=48><b>棄牌區</b></size>\n" +
                    "<size=34>長按拖曳手牌到此</size>";
            }
        }

        if (discardPromptPanelRt != null)
        {
            discardPromptPanelRt.gameObject.SetActive(active);
            if (active && discardPromptText != null)
            {
                string title = pending == 1 ? "手牌超過上限1張" : ("手牌超過上限" + pending + "張");
                discardPromptText.text =
                    "<size=52><b>" + title + "</b></size>\n" +
                    "<size=38>長按卡牌拖至棄牌區</size>";
            }
        }
    }

    public bool TryDropPlayerCardToDiscardByScreenPoint(Card card, Vector2 screenPoint, Camera eventCamera)
    {
        if (battleManager == null || card == null) return false;
        if (!battleManager.IsPlayerInDiscardSelection()) return false;
        if (discardDropZoneRt == null) return false;
        if (!RectTransformUtility.RectangleContainsScreenPoint(discardDropZoneRt, screenPoint, eventCamera))
            return false;
        int index = battleManager.GetPlayerHandCardIndex(card);
        if (index < 0) return false;
        return battleManager.PlayerDiscardCardFromHand(index);
    }

    public bool IsScreenPointOverDiscardZone(Vector2 screenPoint, Camera eventCamera)
    {
        if (discardDropZoneRt == null || !discardDropZoneRt.gameObject.activeSelf) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(discardDropZoneRt, screenPoint, eventCamera);
    }

    public void SetDiscardDropZoneHover(bool hovering)
    {
        if (discardDropZoneImage != null)
        {
            discardDropZoneImage.color = hovering
                ? new Color(0.96f, 0.86f, 0.52f, 1f)
                : new Color(0.52f, 0.43f, 0.34f, 1f);
        }
        if (discardDropZoneRt != null)
        {
            discardDropZoneRt.localScale = hovering ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
        }
        if (discardDropZoneText != null)
        {
            discardDropZoneText.color = hovering
                ? new Color(0.22f, 0.17f, 0.12f, 1f)
                : new Color(0.98f, 0.94f, 0.86f, 1f);
        }
    }
}
