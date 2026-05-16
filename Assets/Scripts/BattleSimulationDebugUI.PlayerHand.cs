using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    private int ComputeHandSignature()
    {
        int count = battleManager.GetPlayerHandCount();
        int hash = count * 486187739;
        for (int i = 0; i < count; i++)
        {
            Card c = battleManager.GetPlayerHandCard(i);
            int id = c == null ? -1 : c.id;
            hash = hash * 31 + id;
        }
        int enemyCount = battleManager.GetEnemyHandCount();
        hash = hash * 31 + enemyCount;
        for (int i = 0; i < enemyCount; i++)
        {
            Card c = battleManager.GetEnemyHandCard(i);
            int id = c == null ? -1 : c.id;
            hash = hash * 31 + id;
        }
        // 場上是否有怪／凝視會影響林可的凝視手牌灰階與可點性，需觸發手牌重建。
        if (battleManager != null)
        {
            hash = hash * 31 + (battleManager.PlayerHasFieldMonster() ? 1 : 0);
            hash = hash * 31 + (battleManager.EnemyHasFieldMonster() ? 1 : 0);
            hash = hash * 31 + (battleManager.PlayerLinGazeActive() ? 1 : 0);
            hash = hash * 31 + (battleManager.EnemyLinGazeActive() ? 1 : 0);
            hash = hash * 31 + battleManager.GetPlayerPendingDiscardCount();
            hash = hash * 31 + battleManager.GetCurrentRound();
        }
        return hash;
    }

    private void RebuildHandButtons()
    {
        if (playerOpeningHandFlyRoutine != null)
        {
            StopCoroutine(playerOpeningHandFlyRoutine);
            playerOpeningHandFlyRoutine = null;
        }
        HideTooltip();
        playerHandPressDepth = 0;
        for (int i = handArea.childCount - 1; i >= 0; i--)
        {
            Destroy(handArea.GetChild(i).gameObject);
        }

        int count = battleManager.GetPlayerHandCount();
        if (count <= 0)
        {
            GameObject emptyObj = new GameObject("EmptyHandText", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyObj.transform.SetParent(handArea, false);
            RectTransform emptyRect = emptyObj.GetComponent<RectTransform>();
            emptyRect.anchorMin = Vector2.zero;
            emptyRect.anchorMax = Vector2.one;
            emptyRect.offsetMin = Vector2.zero;
            emptyRect.offsetMax = Vector2.zero;
            TextMeshProUGUI emptyText = emptyObj.GetComponent<TextMeshProUGUI>();
            emptyText.text = "Hand: no cards";
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.fontSize = 24f;
            emptyText.color = Color.white;
            SetHandButtonsInteractable();
            return;
        }

        float cardWidth = GetBattleHandDisplayedWidth();
        // Symmetric fan layout with wider spacing for better readability/selectability.
        float stackStep = Mathf.Max(32f, cardWidth * 0.42f + GetHandCardSpacing());
        float centerX = handArea.rect.width * 0.5f;
        float center = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Card card = battleManager.GetPlayerHandCard(i);
            if (battleCardPrefab != null)
            {
                CreateHandCardFromPrefab(handArea, "HandCard_" + i, card, centerX + (i - center) * stackStep);
            }
            else
            {
                CreateHandCardButton(handArea, "HandCard_" + i, card, centerX + (i - center) * stackStep, () => OnPlayerCardClicked(card, null));
            }

            RectTransform cardRect = handArea.GetChild(handArea.childCount - 1) as RectTransform;
            if (cardRect != null)
            {
                float fan = (i - center) * -6f;
                cardRect.localRotation = Quaternion.Euler(0f, 0f, fan);
                float curveY = -Mathf.Pow(Mathf.Abs(i - center), 1.35f) * 4.2f;
                cardRect.anchoredPosition += new Vector2(0f, curveY);
            }
        }

        SetHandButtonsInteractable();
        TrySchedulePlayerOpeningHandFlyFromDeck();
    }

    private void RebuildEnemyHandCards()
    {
        if (enemyHandArea == null) return;
        if (enemyOpeningHandFlyRoutine != null)
        {
            StopCoroutine(enemyOpeningHandFlyRoutine);
            enemyOpeningHandFlyRoutine = null;
        }
        for (int i = enemyHandArea.childCount - 1; i >= 0; i--)
        {
            Destroy(enemyHandArea.GetChild(i).gameObject);
        }

        int count = battleManager.GetEnemyHandCount();
        if (count <= 0 || battleCardPrefab == null) return;

        float scale = 0.75f;
        float cardWidth = GetBattleHandDisplayedWidth(scale);
        float stackStep = Mathf.Max(18f, cardWidth * 0.33f);
        float centerX = enemyHandArea.rect.width * 0.5f;
        float center = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Card card = battleManager.GetEnemyHandCard(i);
            if (card == null) continue;

            GameObject cardObj = Instantiate(battleCardPrefab, enemyHandArea);
            cardObj.name = "EnemyHandCard_" + i;
            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if (rect == null) rect = cardObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(centerX + (i - center) * stackStep, -8f);
            ApplyBattleHandCardRectLayout(rect, scale);
            float fan = (i - center) * 6f;
            rect.localRotation = Quaternion.Euler(0f, 0f, fan);
            float curveY = Mathf.Pow(Mathf.Abs(i - center), 1.35f) * 3.6f;
            rect.anchoredPosition += new Vector2(0f, curveY);

            CardDisplay display = cardObj.GetComponentInChildren<CardDisplay>();
            if (display != null)
            {
                display.SetCard(card);
                if (card is SpellCard && display.effectText != null)
                {
                    display.effectText.gameObject.SetActive(false);
                }
                ApplyPrefabVisualTuning(display);
            }

            Button b = cardObj.GetComponent<Button>();
            if (b != null) b.interactable = false;

            bool enemyLinLocked = IsEnemyLinGazeHandCardLocked(card);
            ApplyLinGazeHandCardLockedVisual(cardObj, enemyLinLocked);
        }

        TryScheduleEnemyOpeningHandFlyFromDeck();
    }
    private void CreateHandCardButton(Transform parent, string name, Card card, float x, UnityEngine.Events.UnityAction action)
    {
        GameObject cardObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        cardObj.transform.SetParent(parent, false);

        RectTransform rect = cardObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, -8f);
        rect.sizeDelta = new Vector2(170f, 210f);

        bool handLocked = IsPlayerHandCardLockedByFieldRule(card);
        Image image = cardObj.GetComponent<Image>();
        image.color = handLocked ? new Color(0.62f, 0.58f, 0.53f, 0.9f) : new Color(0.97f, 0.94f, 0.88f, 0.96f);

        Button button = cardObj.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            action?.Invoke();
        });
        ApplyLinGazeHandCardLockedVisual(cardObj, handLocked);
        button.interactable = battleManager.IsPlayerTurn() && !handLocked;

        GameObject txtObj = new GameObject("CardText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(cardObj.transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0f, 0f);
        txtRect.anchorMax = new Vector2(1f, 1f);
        txtRect.offsetMin = new Vector2(8f, 8f);
        txtRect.offsetMax = new Vector2(-8f, -8f);

        TextMeshProUGUI cardLabel = txtObj.GetComponent<TextMeshProUGUI>();
        cardLabel.text = battleManager != null ? battleManager.GetCardHandPreviewText(card) : string.Empty;
        cardLabel.fontSize = 16f * GetHandCardTextScale();
        cardLabel.color = handLocked ? new Color(0.42f, 0.37f, 0.33f, 1f) : new Color(0.2f, 0.16f, 0.12f, 1f);
        cardLabel.enableWordWrapping = true;
        cardLabel.alignment = TextAlignmentOptions.TopLeft;

        RectTransform bgRect = cardObj.GetComponent<RectTransform>();
        bgRect.localScale = Vector3.one * GetHandCardBackplateScale();
        string skill = battleManager != null ? battleManager.GetCardSkillDescription(card) : string.Empty;
        ConfigureHandHoverPreview(cardObj, rect, skill, null);
        PrepareHandCardPointerRouting(cardObj);
        AttachDiscardDragBehavior(cardObj, card);
        AttachPlayerHandPressNotifier(cardObj, this);
    }

    private void CreateHandCardFromPrefab(Transform parent, string name, Card card, float x)
    {
        GameObject cardObj = Instantiate(battleCardPrefab, parent);
        cardObj.name = name;

        RectTransform rect = cardObj.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = cardObj.AddComponent<RectTransform>();
        }
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, -8f);
        ApplyBattleHandCardRectLayout(rect);

        CardDisplay display = cardObj.GetComponentInChildren<CardDisplay>();
        if (display != null)
        {
            display.SetCard(card);
            if (card is SpellCard && display.effectText != null)
            {
                display.effectText.gameObject.SetActive(false);
            }
            ApplyPrefabVisualTuning(display);
            display.RefreshCardArtRarityOverlayExternal();
        }
        else
        {
            Debug.LogWarning("BattleSimulationDebugUI: prefab missing CardDisplay.");
        }

        Button button = cardObj.GetComponent<Button>();
        if (button == null) button = cardObj.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            OnPlayerCardClicked(card, rect);
        });
        bool handLocked = IsPlayerHandCardLockedByFieldRule(card);
        ApplyLinGazeHandCardLockedVisual(cardObj, handLocked);
        button.interactable = battleManager.IsPlayerTurn() && !handLocked;
        string skillMessage = battleManager != null ? battleManager.GetCardSkillDescription(card) : string.Empty;
        ConfigureHandHoverPreview(cardObj, rect, skillMessage, null);
        PrepareHandCardPointerRouting(cardObj);
        AttachDiscardDragBehavior(cardObj, card);
        AttachPlayerHandPressNotifier(cardObj, this);
    }

    private void AttachDiscardDragBehavior(GameObject cardObj, Card card)
    {
        if (cardObj == null) return;
        BattleHandDiscardDrag drag = cardObj.GetComponent<BattleHandDiscardDrag>();
        if (drag == null) drag = cardObj.AddComponent<BattleHandDiscardDrag>();
        drag.Setup(
            () => battleManager != null && battleManager.IsPlayerInDiscardSelection() && battleManager.IsPlayerTurn(),
            (screenPos, eventCamera) => IsScreenPointOverDiscardZone(screenPos, eventCamera),
            (screenPos, eventCamera) => TryDropPlayerCardToDiscardByScreenPoint(card, screenPos, eventCamera),
            hovering => SetDiscardDropZoneHover(hovering),
            () =>
            {
                if (battleManager == null) return;
                lastHandSignature = int.MinValue;
                RebuildHandButtons();
            });
    }

    /// <summary>
    /// 子物件上的 TMP／Image 若開啟 Raycast，會成為射線目標而讓根節點的 BattleHandLongPressTooltip／Button 收不到事件。
    /// 改為僅由根節點接收指標。
    /// </summary>
    private static void PrepareHandCardPointerRouting(GameObject cardRoot)
    {
        if (cardRoot == null) return;
        Graphic rootGraphic = cardRoot.GetComponent<Graphic>();
        if (rootGraphic == null)
        {
            var img = cardRoot.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.001f);
            rootGraphic = img;
        }
        rootGraphic.raycastTarget = true;

        Graphic[] graphics = cardRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g.gameObject == cardRoot) continue;
            g.raycastTarget = false;
        }

        Button b = cardRoot.GetComponent<Button>();
        if (b != null) b.targetGraphic = rootGraphic;
    }

    /// <param name="hoverDelayedTooltipMessage">滑鼠停在上半部一段時間後的提示（與怪物牌相同觸發邏輯）。</param>
    /// <param name="longPressTooltipMessage">長按浮窗；手牌一律 null（與怪物牌一致）。</param>
    private void ConfigureHandHoverPreview(GameObject cardObj, RectTransform cardRect, string hoverDelayedTooltipMessage, string longPressTooltipMessage)
    {
        if (cardObj == null) return;
        if (cardRect == null) cardRect = cardObj.GetComponent<RectTransform>();
        if (cardRect == null || uiRoot == null) return;

        GameObject handCardRoot = cardObj;
        System.Func<bool> suppressWhenHandCardDimmed = () =>
            IsPlayerHandCardRootVisuallyDimmed(handCardRoot) ||
            (battleManager != null && battleManager.IsPlayerInDiscardSelection()) ||
            (battleManager != null && battleManager.IsSpellCastPresentationActive()) ||
            isPlayingCardAnimation ||
            (spellCastOverlayRoot != null && spellCastOverlayRoot.gameObject.activeSelf);

        ZoomUI[] zooms = cardObj.GetComponentsInChildren<ZoomUI>(true);
        for (int zi = 0; zi < zooms.Length; zi++)
        {
            ZoomUI z = zooms[zi];
            if (z == null) continue;
            z.shouldSuppressScaleEffects = suppressWhenHandCardDimmed;
        }

        ZoomUI zoom = cardObj.GetComponent<ZoomUI>();
        if (zoom != null) zoom.enabled = false;

        BattleHandHoverPreview preview = cardObj.GetComponent<BattleHandHoverPreview>();
        if (preview == null) preview = cardObj.AddComponent<BattleHandHoverPreview>();
        preview.shouldSuppressHeavyHoverEffects = suppressWhenHandCardDimmed;
        preview.Setup(cardRect, uiRoot, 1.08f, 0.5f, hoverDelayedTooltipMessage, ShowTooltip, HideTooltip);

        BattleHandLongPressTooltip lp = cardObj.GetComponent<BattleHandLongPressTooltip>();
        if (!string.IsNullOrWhiteSpace(longPressTooltipMessage))
        {
            if (lp == null) lp = cardObj.AddComponent<BattleHandLongPressTooltip>();
            lp.enabled = true;
            lp.Setup(longPressTooltipMessage, ShowTooltip, HideTooltip);
        }
        else if (lp != null)
        {
            lp.enabled = false;
        }
    }

    private void OnPlayerCardClicked(Card card, RectTransform cardRect)
    {
        if (isPlayingCardAnimation) return;
        if (battleManager == null || !battleManager.IsPlayerTurn()) return;
        if (card == null) return;
        if (battleManager.IsPlayerInDiscardSelection())
        {
            // Discard phase uses long-press drag into discard zone only.
            return;
        }
        if (battleManager.PlayerHasFieldMonster())
        {
            if (card is MonsterCard) return;
            if (!(card is SpellCard spGate && spGate.SpellOrdinal == 1)) return;
        }
        if (IsLinGazeSpellCard(card) && !battleManager.CanPlayerCastLinGazeNow())
            return;

        int index = battleManager.GetPlayerHandCardIndex(card);
        if (index < 0) return;

        NotifyTurnIdlePromptPlayerTookPlayOrAttackIntent();

        if (card is SpellCard sp && sp.SpellOrdinal == 1)
        {
            battleManager.PlayerPlayCardFromHand(index);
            return;
        }

        if (cardRect != null) StartCoroutine(PlayCardAnimationThenCast(card, cardRect));
        else battleManager.PlayerPlayCardFromHand(index);
    }

    private IEnumerator PlayCardAnimationThenCast(Card card, RectTransform cardRect)
    {
        if (BattleAutoSimPlugin.IsRunning) yield break;
        isPlayingCardAnimation = true;

        Vector2 start = cardRect.anchoredPosition;
        Quaternion startRot = cardRect.localRotation;
        Vector2 target = new Vector2(0f, -20f);
        float t = 0f;
        const float dur = 0.2f;

        while (t < dur && cardRect != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            cardRect.anchoredPosition = Vector2.Lerp(start, target, p);
            cardRect.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, p);
            cardRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.08f, p);
            yield return null;
        }

        int index = battleManager.GetPlayerHandCardIndex(card);
        if (index >= 0)
        {
            bool asyncSpell = battleManager.PlayerPlayCardFromHand(index);
            if (!asyncSpell) isPlayingCardAnimation = false;
        }
        else isPlayingCardAnimation = false;
    }

    private void OnSpellCastAsyncPresentationFinished()
    {
        isPlayingCardAnimation = false;
    }
    private void SetHandButtonsInteractable()
    {
        bool debugPanelOpen = debugUiRoot != null && debugUiRoot.activeSelf;
        bool spellPresenting = battleManager != null && battleManager.IsSpellCastPresentationActive();
        bool canPlay = battleManager.IsPlayerTurn() && !isPlayingCardAnimation && !spellPresenting && !debugPanelOpen;
        bool inDiscardSelection = battleManager.IsPlayerInDiscardSelection();
        for (int i = 0; i < handArea.childCount; i++)
        {
            Transform t = handArea.GetChild(i);
            Button b = t.GetComponent<Button>();
            if (b == null) continue;
            Card c = i < battleManager.GetPlayerHandCount() ? battleManager.GetPlayerHandCard(i) : null;
            bool handLocked = !inDiscardSelection && IsPlayerHandCardLockedByFieldRule(c);
            b.interactable = canPlay && !handLocked;
            ApplyLinGazeHandCardLockedVisual(t.gameObject, handLocked);

            GameObject handRoot = t.gameObject;
            System.Func<bool> suppressWhenDimmed = () =>
                IsPlayerHandCardRootVisuallyDimmed(handRoot) ||
                (battleManager != null && battleManager.IsPlayerInDiscardSelection()) ||
                (battleManager != null && battleManager.IsSpellCastPresentationActive()) ||
                isPlayingCardAnimation ||
                (spellCastOverlayRoot != null && spellCastOverlayRoot.gameObject.activeSelf) ||
                (debugUiRoot != null && debugUiRoot.activeSelf);

            BattleHandHoverPreview hoverPreview = t.GetComponent<BattleHandHoverPreview>();
            if (hoverPreview != null)
                hoverPreview.shouldSuppressHeavyHoverEffects = suppressWhenDimmed;

            ZoomUI[] zooms = t.GetComponentsInChildren<ZoomUI>(true);
            for (int zi = 0; zi < zooms.Length; zi++)
            {
                if (zooms[zi] != null)
                    zooms[zi].shouldSuppressScaleEffects = suppressWhenDimmed;
            }
        }
    }

    /// <summary>與 <see cref="ApplyLinGazeHandCardLockedVisual"/> 調低透明度對齊；此狀態下手牌關閉 hover／Zoom 以省效能。</summary>
    private static bool IsPlayerHandCardRootVisuallyDimmed(GameObject handCardRoot)
    {
        if (handCardRoot == null) return false;
        CanvasGroup cg = handCardRoot.GetComponent<CanvasGroup>();
        if (cg == null) return false;
        return cg.alpha < 0.99f;
    }

    private static bool IsLinGazeSpellCard(Card c) => c is SpellCard sp && sp.SpellOrdinal == 2;

    /// <summary>我方場上有怪時僅初級治療（SpellOrdinal 1）可打；其餘手牌灰階鎖定。</summary>
    private bool IsPlayerHandCardLockedByFieldRule(Card card)
    {
        if (battleManager != null &&
            battleManager.IsOpeningRoundFireballBlockedForPlayer() &&
            card is SpellCard fireball &&
            fireball.SpellOrdinal == 0)
            return true;

        if (battleManager == null || !battleManager.PlayerHasFieldMonster()) return false;
        if (card is SpellCard sp && sp.SpellOrdinal == 1) return false;
        return true;
    }

    private bool IsEnemyLinGazeHandCardLocked(Card card) =>
        battleManager != null && IsLinGazeSpellCard(card) && !battleManager.CanEnemyCastLinGazeNow();

    private static void ApplyLinGazeHandCardLockedVisual(GameObject cardRoot, bool locked)
    {
        if (cardRoot == null) return;
        CanvasGroup cg = cardRoot.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardRoot.AddComponent<CanvasGroup>();
        cg.alpha = locked ? 0.5f : 1f;
        cg.interactable = !locked;
    }

    private void TrySchedulePlayerOpeningHandFlyFromDeck()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (battleManager == null || handArea == null || playerDeckPileRt == null) return;
        if (battleManager.GetPlayerHandCount() <= 0) return;
        // If UI initializes slightly after opening presentation, still allow fly-in during round 1.
        if (!battleManager.IsOpeningPresentationInProgress() && battleManager.GetCurrentRound() > 1) return;
        int sid = battleManager.GetBattleSessionId();
        if (playerOpeningHandFlySessionDone == sid) return;
        if (playerOpeningHandFlyRoutine != null) return;
        playerOpeningHandFlyRoutine = StartCoroutine(CoPlayerOpeningHandFlyFromDeckRoutine(sid));
    }

    private IEnumerator CoPlayerOpeningHandFlyFromDeckRoutine(int battleSessionId)
    {
        if (handArea == null || playerDeckPileRt == null || battleManager == null)
        {
            playerOpeningHandFlyRoutine = null;
            yield break;
        }

        int n = handArea.childCount;
        if (n <= 0)
        {
            playerOpeningHandFlyRoutine = null;
            yield break;
        }

        Vector3 deckWorld = playerDeckPileRt.TransformPoint(playerDeckPileRt.rect.center);
        Vector3 deckLocal3 = handArea.InverseTransformPoint(deckWorld);
        Vector2 deckLocal = new Vector2(deckLocal3.x, deckLocal3.y);

        var rects = new System.Collections.Generic.List<RectTransform>(n);
        var startPos = new System.Collections.Generic.List<Vector2>(n);
        var endPos = new System.Collections.Generic.List<Vector2>(n);
        var endRot = new System.Collections.Generic.List<Quaternion>(n);
        var endScale = new System.Collections.Generic.List<Vector3>(n);

        for (int i = 0; i < n; i++)
        {
            RectTransform rt = handArea.GetChild(i) as RectTransform;
            if (rt == null) continue;
            int idx = rects.Count;
            Vector2 stackOffset = new Vector2(idx * 2.2f, -idx * 1.4f);
            Vector2 sp = deckLocal + stackOffset;

            rects.Add(rt);
            startPos.Add(sp);
            endPos.Add(rt.anchoredPosition);
            endRot.Add(rt.localRotation);
            endScale.Add(rt.localScale);

            Button b = rt.GetComponent<Button>();
            if (b != null) b.interactable = false;

            rt.anchoredPosition = sp;
            rt.localRotation = Quaternion.identity;
            rt.localScale = endScale[idx] * 0.82f;
        }

        if (rects.Count == 0)
        {
            playerOpeningHandFlyRoutine = null;
            yield break;
        }

        const float stagger = 0.055f;
        const float flyDur = 0.38f;
        float total = stagger * (rects.Count - 1) + flyDur;
        float t = 0f;
        while (t < total && handArea != null)
        {
            t += Time.unscaledDeltaTime;
            for (int i = 0; i < rects.Count; i++)
            {
                RectTransform rt = rects[i];
                if (rt == null) continue;
                float u = Mathf.Clamp01((t - i * stagger) / flyDur);
                float eased = u * u * (3f - 2f * u);
                rt.anchoredPosition = Vector2.Lerp(startPos[i], endPos[i], eased);
                rt.localRotation = Quaternion.Slerp(Quaternion.identity, endRot[i], eased);
                rt.localScale = Vector3.Lerp(endScale[i] * 0.82f, endScale[i], eased);
            }
            yield return null;
        }

        for (int i = 0; i < rects.Count; i++)
        {
            if (rects[i] == null) continue;
            rects[i].anchoredPosition = endPos[i];
            rects[i].localRotation = endRot[i];
            rects[i].localScale = endScale[i];
        }

        SetHandButtonsInteractable();
        playerOpeningHandFlySessionDone = battleSessionId;
        playerOpeningHandFlyRoutine = null;
    }

    private void TryScheduleEnemyOpeningHandFlyFromDeck()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (battleManager == null || enemyHandArea == null || enemyDeckPileRt == null) return;
        if (battleManager.GetEnemyHandCount() <= 0) return;
        // If UI initializes slightly after opening presentation, still allow fly-in during round 1.
        if (!battleManager.IsOpeningPresentationInProgress() && battleManager.GetCurrentRound() > 1) return;
        int sid = battleManager.GetBattleSessionId();
        if (enemyOpeningHandFlySessionDone == sid) return;
        if (enemyOpeningHandFlyRoutine != null) return;
        enemyOpeningHandFlyRoutine = StartCoroutine(CoEnemyOpeningHandFlyFromDeckRoutine(sid));
    }

    private IEnumerator CoEnemyOpeningHandFlyFromDeckRoutine(int battleSessionId)
    {
        if (enemyHandArea == null || enemyDeckPileRt == null || battleManager == null)
        {
            enemyOpeningHandFlyRoutine = null;
            yield break;
        }

        int n = enemyHandArea.childCount;
        if (n <= 0)
        {
            enemyOpeningHandFlyRoutine = null;
            yield break;
        }

        Vector3 deckWorld = enemyDeckPileRt.TransformPoint(enemyDeckPileRt.rect.center);
        Vector3 deckLocal3 = enemyHandArea.InverseTransformPoint(deckWorld);
        Vector2 deckLocal = new Vector2(deckLocal3.x, deckLocal3.y);

        var rects = new System.Collections.Generic.List<RectTransform>(n);
        var startPos = new System.Collections.Generic.List<Vector2>(n);
        var endPos = new System.Collections.Generic.List<Vector2>(n);
        var endRot = new System.Collections.Generic.List<Quaternion>(n);
        var endScale = new System.Collections.Generic.List<Vector3>(n);

        for (int i = 0; i < n; i++)
        {
            RectTransform rt = enemyHandArea.GetChild(i) as RectTransform;
            if (rt == null) continue;
            int idx = rects.Count;
            Vector2 stackOffset = new Vector2(idx * 2.2f, -idx * 1.4f);
            Vector2 sp = deckLocal + stackOffset;

            rects.Add(rt);
            startPos.Add(sp);
            endPos.Add(rt.anchoredPosition);
            endRot.Add(rt.localRotation);
            endScale.Add(rt.localScale);

            rt.anchoredPosition = sp;
            rt.localRotation = Quaternion.identity;
            rt.localScale = endScale[idx] * 0.82f;
        }

        if (rects.Count == 0)
        {
            enemyOpeningHandFlyRoutine = null;
            yield break;
        }

        const float stagger = 0.055f;
        const float flyDur = 0.38f;
        float total = stagger * (rects.Count - 1) + flyDur;
        float t = 0f;
        while (t < total && enemyHandArea != null)
        {
            t += Time.unscaledDeltaTime;
            for (int i = 0; i < rects.Count; i++)
            {
                RectTransform rt = rects[i];
                if (rt == null) continue;
                float u = Mathf.Clamp01((t - i * stagger) / flyDur);
                float eased = u * u * (3f - 2f * u);
                rt.anchoredPosition = Vector2.Lerp(startPos[i], endPos[i], eased);
                rt.localRotation = Quaternion.Slerp(Quaternion.identity, endRot[i], eased);
                rt.localScale = Vector3.Lerp(endScale[i] * 0.82f, endScale[i], eased);
            }
            yield return null;
        }

        for (int i = 0; i < rects.Count; i++)
        {
            if (rects[i] == null) continue;
            rects[i].anchoredPosition = endPos[i];
            rects[i].localRotation = endRot[i];
            rects[i].localScale = endScale[i];
        }

        enemyOpeningHandFlySessionDone = battleSessionId;
        enemyOpeningHandFlyRoutine = null;
    }

}
