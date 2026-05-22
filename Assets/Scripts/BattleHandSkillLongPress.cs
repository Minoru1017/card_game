using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 手牌長按顯示完整戰技／效果浮窗。
/// 按住後可在手牌間滑動：停留在某張牌上滿觸發時間即顯示該牌浮窗，移到另一張牌則重新計時。
/// </summary>
public class BattleHandSkillLongPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler
{
    public float holdSeconds = 0.35f;

    private static readonly List<BattleHandSkillLongPress> Instances = new List<BattleHandSkillLongPress>();
    private static BattleHandSkillLongPress activeShown;
    private static bool globalPressing;
    private static BattleHandSkillLongPress hoverTarget;
    private static float hoverTargetEnterTime;
    private static int lastGlobalTickFrame = -1;

    private RectTransform cardRect;
    private Card boundCard;
    private BattleSimulationDebugUI host;
    private Func<bool> shouldSuppress;
    private bool hasTooltipContent;
    private bool shown;
    private int suppressClickUntilFrame = -1;

    public void Setup(
        RectTransform source,
        Card card,
        BattleSimulationDebugUI uiHost,
        Func<bool> suppressPredicate)
    {
        cardRect = source;
        boundCard = card;
        host = uiHost;
        shouldSuppress = suppressPredicate;
        hasTooltipContent = host != null && host.TryGetHandLongPressTooltipContent(card, out _);
        if (!Instances.Contains(this))
            Instances.Add(this);
    }

    public bool ShouldSuppressClickAfterLongPress() => Time.frameCount <= suppressClickUntilFrame;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!globalPressing) return;
        if (!hasTooltipContent) return;
        if (shouldSuppress != null && shouldSuppress()) return;
        SetHoverTarget(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (shouldSuppress != null && shouldSuppress()) return;
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (!hasTooltipContent) return;

        globalPressing = true;
        DismissAllShown();
        SetHoverTarget(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndGlobalPress(true);
    }

    private void Update()
    {
        if (Time.frameCount == lastGlobalTickFrame) return;
        lastGlobalTickFrame = Time.frameCount;
        TickGlobalLongPress();
    }

    private static bool IsPrimaryPointerHeld()
    {
        if (Input.touchCount > 0) return true;
        return Input.GetMouseButton(0);
    }

    private static void TickGlobalLongPress()
    {
        if (!globalPressing) return;
        if (!IsPrimaryPointerHeld())
        {
            EndGlobalPress(true);
            return;
        }

        BattleHandSkillLongPress under = ResolveTopmostCardUnderPointer();
        if (under == null)
        {
            if (activeShown != null)
                activeShown.DismissShownOnly();
            hoverTarget = null;
            return;
        }

        if (under.shouldSuppress != null && under.shouldSuppress())
        {
            EndGlobalPress(true);
            return;
        }

        SetHoverTarget(under);

        if (activeShown == hoverTarget) return;
        if (Time.unscaledTime - hoverTargetEnterTime < hoverTarget.holdSeconds) return;

        if (activeShown != null)
            activeShown.DismissShownOnly();
        hoverTarget.ShowTooltipNow();
    }

    private static void SetHoverTarget(BattleHandSkillLongPress card)
    {
        if (card == null || hoverTarget == card) return;
        if (activeShown != null && activeShown != card)
            activeShown.DismissShownOnly();
        hoverTarget = card;
        hoverTargetEnterTime = Time.unscaledTime;
    }

    private static BattleHandSkillLongPress ResolveTopmostCardUnderPointer()
    {
        BattleHandSkillLongPress best = null;
        int bestSibling = -1;
        Vector2 screen = GetPointerScreenPosition();
        for (int i = 0; i < Instances.Count; i++)
        {
            BattleHandSkillLongPress lp = Instances[i];
            if (lp == null || !lp.isActiveAndEnabled || !lp.hasTooltipContent) continue;
            if (!lp.ContainsScreenPoint(screen)) continue;
            int sib = lp.cardRect != null ? lp.cardRect.GetSiblingIndex() : 0;
            if (sib >= bestSibling)
            {
                bestSibling = sib;
                best = lp;
            }
        }
        return best;
    }

    private bool ContainsScreenPoint(Vector2 screen)
    {
        if (cardRect == null) return false;
        Camera cam = ResolveEventCamera();
        return RectTransformUtility.RectangleContainsScreenPoint(cardRect, screen, cam);
    }

    private Camera ResolveEventCamera()
    {
        Canvas c = cardRect.GetComponentInParent<Canvas>();
        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
            return c.worldCamera;
        return null;
    }

    private void ShowTooltipNow()
    {
        if (host == null || boundCard == null) return;
        host.ShowHandLongPressTooltip(cardRect, boundCard);
        shown = true;
        activeShown = this;
    }

    private void DismissShownOnly()
    {
        if (!shown) return;
        host?.HideHandLongPressTooltip();
        suppressClickUntilFrame = Time.frameCount + 1;
        shown = false;
        if (activeShown == this)
            activeShown = null;
    }

    private static void EndGlobalPress(bool hideTooltip)
    {
        globalPressing = false;
        hoverTarget = null;
        if (hideTooltip)
            DismissAllShown();
        else
        {
            for (int i = 0; i < Instances.Count; i++)
            {
                BattleHandSkillLongPress lp = Instances[i];
                if (lp != null) lp.shown = false;
            }
            activeShown = null;
        }
    }

    private static void DismissAllShown()
    {
        if (activeShown != null)
        {
            activeShown.DismissShownOnly();
            return;
        }
        for (int i = 0; i < Instances.Count; i++)
        {
            BattleHandSkillLongPress lp = Instances[i];
            if (lp != null && lp.shown)
                lp.DismissShownOnly();
        }
    }

    private static Vector2 GetPointerScreenPosition()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).position;
        return Input.mousePosition;
    }

    private void OnDisable()
    {
        if (globalPressing && hoverTarget == this)
            hoverTarget = null;
        DismissShownOnly();
        Instances.Remove(this);
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
        if (activeShown == this)
        {
            activeShown = null;
            if (globalPressing)
                EndGlobalPress(true);
        }
    }

    public static void DismissAllShownTooltips()
    {
        EndGlobalPress(true);
    }
}
