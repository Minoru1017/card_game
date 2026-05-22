using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CardState
{
    Library, Deck
}
public class ClickCard : MonoBehaviour, IPointerClickHandler
{
    public CardState state;

    public DeckManager deckManager;
    private bool isAnimating;

    void Start()
    {
        // Prefer injected reference from DeckManager.CreateCard().
        if (deckManager == null)
        {
            var dm = GameObject.Find("DataManager");
            if (dm != null) deckManager = dm.GetComponent<DeckManager>();
        }
        if (deckManager == null)
        {
            deckManager = Object.FindFirstObjectByType<DeckManager>();
        }

        if (deckManager == null)
        {
            Debug.LogError("ClickCard: DeckManager not found.");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (eventData.dragging) return;
        if (!enabled) return;
        if (isAnimating) return;
        if (deckManager == null) return;

        var display = GetComponentInParent<CardDisplay>();
        if (display == null || display.card == null) return;

        if (state == CardState.Library)
        {
            if (!deckManager.showDeck)
            {
                deckManager.ShowBackpackCardInspect(display.card, display);
                return;
            }

            RectTransform cardRoot = display.GetComponentInParent<RectTransform>();
            int libraryCountBefore = deckManager.GetLibraryCardCount(display.card.id);
            bool willRemove = libraryCountBefore <= 1;
            StartCoroutine(PlayLibraryClickFxThenMove(cardRoot, display.card.id, willRemove, libraryCountBefore));
            return;
        }

        deckManager.UpdataCard(state, display.card.id);
    }

    private IEnumerator PlayLibraryClickFxThenMove(RectTransform cardRoot, int cardId, bool willRemove, int libraryCountBefore)
    {
        isAnimating = true;
        CanvasGroup cg = null;
        Vector2 startPos = Vector2.zero;
        Vector3 startScale = Vector3.one;
        GameObject underCardGhost = null;
        CanvasGroup underGhostGroup = null;
        RectTransform parent = null;
        GridLayoutGroup grid = null;
        int originalSiblingIndex = -1;
        Transform originalParent = null;
        List<RectTransform> rightCards = new List<RectTransform>();
        List<Vector2> rightStartPos = new List<Vector2>();
        if (cardRoot != null)
        {
            cg = cardRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = cardRoot.gameObject.AddComponent<CanvasGroup>();
            startPos = cardRoot.anchoredPosition;
            startScale = cardRoot.localScale;
            originalParent = cardRoot.parent;
            originalSiblingIndex = cardRoot.GetSiblingIndex();

            // Library stacked-card effect:
            // when count >= 2, show a faint "card underneath" while top card is being pulled.
            if (!willRemove && libraryCountBefore >= 2 && cardRoot.parent != null)
            {
                underCardGhost = Instantiate(cardRoot.gameObject, cardRoot.parent);
                underCardGhost.name = cardRoot.gameObject.name + "_UnderGhost";
                RectTransform ghostRt = underCardGhost.GetComponent<RectTransform>();
                if (ghostRt != null)
                {
                    ghostRt.SetSiblingIndex(cardRoot.GetSiblingIndex());
                    ghostRt.anchoredPosition = startPos + new Vector2(12f, -10f);
                    ghostRt.localScale = startScale * 0.97f;
                }

                ClickCard ghostClick = underCardGhost.GetComponentInChildren<ClickCard>(true);
                if (ghostClick != null) ghostClick.enabled = false;
                ZoomUI ghostZoom = underCardGhost.GetComponentInChildren<ZoomUI>(true);
                if (ghostZoom != null) ghostZoom.enabled = false;
                Button ghostButton = underCardGhost.GetComponentInChildren<Button>(true);
                if (ghostButton != null) ghostButton.interactable = false;

                underGhostGroup = underCardGhost.GetComponent<CanvasGroup>();
                if (underGhostGroup == null) underGhostGroup = underCardGhost.AddComponent<CanvasGroup>();
                underGhostGroup.alpha = 0.72f;
                underGhostGroup.blocksRaycasts = false;
                underGhostGroup.interactable = false;

                // Temporarily draw this card on top for pull-out FX.
                cardRoot.SetAsLastSibling();
            }

            if (willRemove)
            {
                parent = cardRoot.parent as RectTransform;
                grid = parent != null ? parent.GetComponent<GridLayoutGroup>() : null;
                int removedIndex = cardRoot.GetSiblingIndex();
                if (parent != null && removedIndex >= 0)
                {
                    for (int i = removedIndex + 1; i < parent.childCount; i++)
                    {
                        RectTransform child = parent.GetChild(i) as RectTransform;
                        if (child == null) continue;
                        rightCards.Add(child);
                        rightStartPos.Add(child.anchoredPosition);
                    }
                }
                if (grid != null) grid.enabled = false;
            }

            Vector2 endPos = startPos + new Vector2(0f, 30f);
            float startAlpha = cg.alpha <= 0f ? 1f : cg.alpha;
            float shiftX = 0f;
            if (willRemove)
            {
                if (grid != null) shiftX = grid.cellSize.x + grid.spacing.x;
                else shiftX = cardRoot.rect.width + 20f;
            }
            float t = 0f;
            const float duration = 0.14f;
            while (t < duration && cardRoot != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = p * p * (3f - 2f * p);
                cardRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                cg.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                if (underCardGhost != null)
                {
                    RectTransform ghostRt = underCardGhost.GetComponent<RectTransform>();
                    if (ghostRt != null)
                    {
                        ghostRt.anchoredPosition = Vector2.Lerp(startPos + new Vector2(12f, -10f), startPos, eased);
                    }
                    if (underGhostGroup != null)
                    {
                        underGhostGroup.alpha = Mathf.Lerp(0.72f, 0.96f, eased);
                    }
                }
                if (willRemove)
                {
                    for (int i = 0; i < rightCards.Count; i++)
                    {
                        RectTransform c = rightCards[i];
                        if (c == null) continue;
                        Vector2 to = rightStartPos[i] + new Vector2(-shiftX, 0f);
                        c.anchoredPosition = Vector2.Lerp(rightStartPos[i], to, eased);
                    }
                }
                yield return null;
            }
        }

        deckManager.UpdataCard(state, cardId);

        // If object still exists (count > 0), restore visual state for next interactions.
        if (cardRoot != null)
        {
            cardRoot.anchoredPosition = startPos;
            cardRoot.localScale = startScale;
            if (cg != null) cg.alpha = 1f;
            if (originalParent != null && cardRoot.parent == originalParent && originalSiblingIndex >= 0)
            {
                int maxIndex = Mathf.Max(0, cardRoot.parent.childCount - 1);
                cardRoot.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, maxIndex));
            }
        }
        if (underCardGhost != null)
        {
            // Remove ghost immediately to avoid short residual frame at bottom-right.
            Destroy(underCardGhost);
        }
        if (willRemove && grid != null && parent != null)
        {
            grid.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
        isAnimating = false;
    }
}
