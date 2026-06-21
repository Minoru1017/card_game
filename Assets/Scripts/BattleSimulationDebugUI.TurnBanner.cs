using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    // --- TurnBanner ---
    private void CreateBattleTurnBanner(Transform canvasParent)
    {
        if (canvasParent == null) return;

        GameObject go = new GameObject("BattleTurnBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Shadow));
        go.transform.SetParent(canvasParent, false);
        turnBannerPanelRt = go.GetComponent<RectTransform>();
        turnBannerPanelRt.anchorMin = new Vector2(0.5f, 0.5f);
        turnBannerPanelRt.anchorMax = new Vector2(0.5f, 0.5f);
        turnBannerPanelRt.pivot = new Vector2(0.5f, 0.5f);
        turnBannerPanelRt.anchoredPosition = Vector2.zero;
        turnBannerPanelRt.sizeDelta = new Vector2(540f, 112f);

        Image bg = go.GetComponent<Image>();
        bg.color = BattleUiColors.TurnBg;
        bg.raycastTarget = false;

        Shadow sh = go.GetComponent<Shadow>();
        sh.effectColor = BattleUiColors.ShadowUi;
        sh.effectDistance = new Vector2(6f, -7f);

        turnBannerCg = go.GetComponent<CanvasGroup>();
        turnBannerCg.alpha = 0f;
        turnBannerCg.blocksRaycasts = false;
        turnBannerCg.interactable = false;
        go.SetActive(false);

        GameObject textGo = new GameObject("TurnBannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20f, 14f);
        trt.offsetMax = new Vector2(-20f, -14f);
        turnBannerTmp = textGo.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) turnBannerTmp.font = sharedUIFont;
        turnBannerTmp.fontSize = 42f;
        turnBannerTmp.alignment = TextAlignmentOptions.Center;
        turnBannerTmp.color = BattleUiColors.TurnBannerText;
        turnBannerTmp.raycastTarget = false;
        turnBannerTmp.enableWordWrapping = true;
        turnBannerTmp.text = string.Empty;

        Outline ol = textGo.AddComponent<Outline>();
        BattleUiColors.ApplyHpOutline(ol);
    }

    /// <summary>將「你的回合／敵方操作中…」置於同 Canvas 下其他戰鬥 UI 之上，並維持在除錯面板之下（除錯為最後子物件）。</summary>
    private void ApplyBattleTurnBannerStackOrder()
    {
        if (turnBannerPanelRt == null || debugUiRoot == null) return;
        Transform bannerParent = turnBannerPanelRt.parent;
        if (bannerParent == null || debugUiRoot.transform.parent != bannerParent) return;

        turnBannerPanelRt.SetAsLastSibling();
        debugUiRoot.transform.SetAsLastSibling();
    }

    private void ForceHideTurnBanner()
    {
        if (turnBannerRoutine != null)
        {
            StopCoroutine(turnBannerRoutine);
            turnBannerRoutine = null;
        }
        if (turnBannerCg != null) turnBannerCg.alpha = 0f;
        if (turnBannerPanelRt != null) turnBannerPanelRt.gameObject.SetActive(false);
        if (TutorialBattleCoachUi.IsActiveForCurrentBattle)
            RefreshTutorialHandPlayHighlights();
    }

    private void SyncTurnBannerWithBattleState()
    {
        if (battleManager == null) return;
        if (BattleAutoSimPlugin.IsRunning) return;
        if (battleManager.IsBattleOver())
        {
            ForceHideTurnBanner();
            return;
        }
        if (battleManager.IsOpeningPresentationInProgress())
        {
            ForceHideTurnBanner();
            return;
        }
        if (battleManager.IsPlayerTurn())
        {
            ForceHideTurnBanner();
            DisarmYourTurnBannerAllIdlePromptClocks();
            yourTurnBannerTurnStartIdleClockStartUnscaled = Time.unscaledTime;
            return;
        }
        OnTurnBannerRequested(BattleTurnBannerKind.EnemyTurn);
    }

    private void OnTurnBannerRequested(BattleTurnBannerKind kind)
    {
        if (turnBannerCg == null || turnBannerPanelRt == null || turnBannerTmp == null) return;

        if (kind == BattleTurnBannerKind.PlayerTurn && !turnBannerPlayerFromIdleTimeout)
            return;

        if (kind == BattleTurnBannerKind.Hidden || kind == BattleTurnBannerKind.EnemyTurn)
            DisarmYourTurnBannerAllIdlePromptClocks();

        if (turnBannerRoutine != null)
        {
            StopCoroutine(turnBannerRoutine);
            turnBannerRoutine = null;
        }

        switch (kind)
        {
            case BattleTurnBannerKind.Hidden:
                turnBannerRoutine = StartCoroutine(TurnBannerFadeOutRoutine());
                break;
            case BattleTurnBannerKind.PlayerTurn:
                turnBannerPlayerFromIdleTimeout = false;
                turnBannerTmp.text = "你的回合";
                turnBannerTmp.color = BattleUiColors.TurnPlayer;
                turnBannerPanelRt.anchoredPosition = Vector2.zero;
                turnBannerPanelRt.gameObject.SetActive(true);
                turnBannerCg.alpha = 0f;
                ApplyBattleTurnBannerStackOrder();
                turnBannerRoutine = StartCoroutine(TurnBannerFadeInAndFloatRoutine());
                if (playerHandPressDepth > 0)
                    ForceHideTurnBanner();
                break;
            case BattleTurnBannerKind.EnemyTurn:
                turnBannerTmp.text = "敵方操作中...";
                turnBannerTmp.color = BattleUiColors.TurnEnemy;
                turnBannerPanelRt.anchoredPosition = Vector2.zero;
                turnBannerPanelRt.gameObject.SetActive(true);
                turnBannerCg.alpha = 0f;
                ApplyBattleTurnBannerStackOrder();
                turnBannerRoutine = StartCoroutine(TurnBannerFadeInAndFloatRoutine());
                break;
        }
    }

    private IEnumerator TurnBannerFadeOutRoutine()
    {
        if (turnBannerCg == null) yield break;
        float dur = 0.2f;
        float t = 0f;
        float start = turnBannerCg.alpha;
        while (t < dur && turnBannerCg != null)
        {
            t += Time.unscaledDeltaTime;
            turnBannerCg.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / dur));
            yield return null;
        }
        if (turnBannerCg != null) turnBannerCg.alpha = 0f;
        if (turnBannerPanelRt != null) turnBannerPanelRt.gameObject.SetActive(false);
        turnBannerRoutine = null;
    }

    private IEnumerator TurnBannerFadeInAndFloatRoutine()
    {
        if (turnBannerCg == null || turnBannerPanelRt == null) yield break;
        const float fadeIn = 0.24f;
        float t = 0f;
        while (t < fadeIn && turnBannerCg != null)
        {
            t += Time.unscaledDeltaTime;
            turnBannerCg.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / fadeIn));
            yield return null;
        }
        if (turnBannerCg != null) turnBannerCg.alpha = 1f;

        if (turnBannerTmp != null && turnBannerTmp.text == "你的回合")
            RefreshTutorialHandPlayHighlights();

        while (turnBannerPanelRt != null && turnBannerPanelRt.gameObject.activeInHierarchy)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 2.55f) * 8f;
            turnBannerPanelRt.anchoredPosition = new Vector2(0f, bob);
            yield return null;
        }
        turnBannerRoutine = null;
    }

    /// <summary>其他 UI 用：已達任一「你的回合」閒置提示條件（與浮窗顯示併用）。</summary>
    public bool IsPlayerTurnUiIdleStandbyMode => playerTurnUiIdleStandbyMode;

    private void StopYourTurnBannerHandTouchNoPlayArmDeferRoutine()
    {
        if (yourTurnBannerHandTouchNoPlayArmDeferRoutine == null) return;
        StopCoroutine(yourTurnBannerHandTouchNoPlayArmDeferRoutine);
        yourTurnBannerHandTouchNoPlayArmDeferRoutine = null;
    }

    private void DisarmYourTurnBannerTurnStartAndHandTouchClocksOnly()
    {
        yourTurnBannerTurnStartIdleClockStartUnscaled = -1f;
        yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled = -1f;
        StopYourTurnBannerHandTouchNoPlayArmDeferRoutine();
    }

    private void DisarmYourTurnBannerAllIdlePromptClocks()
    {
        yourTurnBannerTurnStartIdleClockStartUnscaled = -1f;
        yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled = -1f;
        yourTurnBannerAfterFieldPlayClockStartUnscaled = -1f;
        yourTurnBannerIdlePromptShownThisWindow = false;
        playerTurnUiIdleStandbyMode = false;
        yourTurnBannerHandTouchSessionLedToPlay = false;
        StopYourTurnBannerHandTouchNoPlayArmDeferRoutine();
    }

    private void ClearYourTurnBannerIdlePromptClockArmsOnly()
    {
        yourTurnBannerTurnStartIdleClockStartUnscaled = -1f;
        yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled = -1f;
        yourTurnBannerAfterFieldPlayClockStartUnscaled = -1f;
        StopYourTurnBannerHandTouchNoPlayArmDeferRoutine();
    }

    private void OnPlayerTurnActionWindowOpenedForPromptUi()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        DisarmYourTurnBannerAllIdlePromptClocks();
        yourTurnBannerTurnStartIdleClockStartUnscaled = Time.unscaledTime;
        ForceHideTurnBanner();
    }

    private void OnPlayerCommittedHandCardToFieldFromHand()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        DisarmYourTurnBannerTurnStartAndHandTouchClocksOnly();
        yourTurnBannerAfterFieldPlayClockStartUnscaled = Time.unscaledTime;
        yourTurnBannerIdlePromptShownThisWindow = false;
        playerTurnUiIdleStandbyMode = false;
        ForceHideTurnBanner();
    }

    private void OnPlayerPressedEndTurnForPromptUi()
    {
        DisarmYourTurnBannerAllIdlePromptClocks();
        ForceHideTurnBanner();
    }

    private float GetYourTurnIdlePromptThresholdSeconds()
    {
        return Mathf.Max(10f, playerTurnIdlePromptSeconds);
    }

    private IEnumerator YourTurnBannerHandTouchNoPlayMaybeArmNextFrameRoutine()
    {
        yield return null;
        yourTurnBannerHandTouchNoPlayArmDeferRoutine = null;
        if (BattleAutoSimPlugin.IsRunning) yield break;
        if (battleManager == null || !battleManager.IsPlayerTurn() || battleManager.IsBattleOver()) yield break;
        if (battleManager.IsOpeningPresentationInProgress()) yield break;
        if (battleManager.IsTurnSequenceInProgress()) yield break;
        if (battleManager.IsSpellCastPresentationActive()) yield break;
        if (isGamePaused) yield break;
        if (yourTurnBannerHandTouchSessionLedToPlay) yield break;
        if (playerHandPressDepth > 0) yield break;

        yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled = Time.unscaledTime;
        yourTurnBannerIdlePromptShownThisWindow = false;
    }

    /// <summary>回合開始無操作／觸碰手牌後未出牌／手牌上場後未結束回合，逾時顯示「你的回合」。</summary>
    private void TickYourTurnBannerIdlePrompts()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (battleManager == null) return;
        if (!battleManager.IsPlayerTurn() || battleManager.IsBattleOver()) return;
        if (battleManager.IsOpeningPresentationInProgress()) return;
        if (battleManager.IsTurnSequenceInProgress()) return;
        if (battleManager.IsSpellCastPresentationActive()) return;
        if (isGamePaused) return;

        bool anyClockArmed =
            yourTurnBannerTurnStartIdleClockStartUnscaled >= 0f ||
            yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled >= 0f ||
            yourTurnBannerAfterFieldPlayClockStartUnscaled >= 0f;
        if (!anyClockArmed) return;
        if (playerHandPressDepth > 0) return;
        if (isPlayingCardAnimation) return;

        float threshold = GetYourTurnIdlePromptThresholdSeconds();
        float deadline = float.MaxValue;
        if (yourTurnBannerTurnStartIdleClockStartUnscaled >= 0f)
            deadline = Mathf.Min(deadline, yourTurnBannerTurnStartIdleClockStartUnscaled + threshold);
        if (yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled >= 0f)
            deadline = Mathf.Min(deadline, yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled + threshold);
        if (yourTurnBannerAfterFieldPlayClockStartUnscaled >= 0f)
            deadline = Mathf.Min(deadline, yourTurnBannerAfterFieldPlayClockStartUnscaled + threshold);

        if (Time.unscaledTime <= deadline) return;

        playerTurnUiIdleStandbyMode = true;
        if (yourTurnBannerIdlePromptShownThisWindow) return;
        if (IsPlayerTurnBannerVisuallyShowing()) return;

        yourTurnBannerIdlePromptShownThisWindow = true;
        ClearYourTurnBannerIdlePromptClockArmsOnly();
        turnBannerPlayerFromIdleTimeout = true;
        OnTurnBannerRequested(BattleTurnBannerKind.PlayerTurn);
    }

    private void NotifyTurnIdlePromptPlayerTookPlayOrAttackIntent()
    {
        yourTurnBannerHandTouchSessionLedToPlay = true;
        DisarmYourTurnBannerTurnStartAndHandTouchClocksOnly();
    }

    /// <summary>我方手牌按下（由 <see cref="BattlePlayerHandCardPressNotifier"/> 呼叫）。</summary>
    public void NotifyPlayerHandCardPressBegan()
    {
        if (battleManager == null || !battleManager.IsPlayerTurn()) return;
        if (BattleAutoSimPlugin.IsRunning) return;
        playerHandPressDepth++;
        if (playerHandPressDepth == 1)
        {
            yourTurnBannerHandTouchSessionLedToPlay = false;
            ForceHideTurnBanner();
        }
    }

    /// <summary>我方手牌放開或指標離開手牌（由 <see cref="BattlePlayerHandCardPressNotifier"/> 呼叫）。</summary>
    public void NotifyPlayerHandCardPressEnded()
    {
        if (playerHandPressDepth <= 0) return;
        playerHandPressDepth--;
        if (playerHandPressDepth > 0) return;
        if (BattleAutoSimPlugin.IsRunning) return;
        if (battleManager == null || !battleManager.IsPlayerTurn()) return;
        StopYourTurnBannerHandTouchNoPlayArmDeferRoutine();
        yourTurnBannerHandTouchNoPlayArmDeferRoutine = StartCoroutine(YourTurnBannerHandTouchNoPlayMaybeArmNextFrameRoutine());
    }

    private bool IsPlayerTurnBannerVisuallyShowing()
    {
        return turnBannerPanelRt != null &&
               turnBannerPanelRt.gameObject.activeSelf &&
               turnBannerCg != null &&
               turnBannerCg.alpha > 0.08f &&
               turnBannerTmp != null &&
               turnBannerTmp.text == "你的回合";
    }

    private static void AttachPlayerHandPressNotifier(GameObject cardRoot, BattleSimulationDebugUI host)
    {
        if (cardRoot == null || host == null) return;
        BattlePlayerHandCardPressNotifier n = cardRoot.GetComponent<BattlePlayerHandCardPressNotifier>();
        if (n == null) n = cardRoot.AddComponent<BattlePlayerHandCardPressNotifier>();
        n.Init(host);
    }
}
