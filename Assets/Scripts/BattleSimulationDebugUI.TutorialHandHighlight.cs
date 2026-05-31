using System.Collections.Generic;
using UnityEngine;

public partial class BattleSimulationDebugUI
{
    private readonly List<int> tutorialHandHintIndices = new List<int>(8);
    private readonly HashSet<int> tutorialHandHintIndexSet = new HashSet<int>();
    private bool tutorialHandPlayHighlightRequested;
    private bool tutorialHandDiscardHighlightRequested;
    private bool harborHandPlayHighlightRequested;
    private bool harborHandDiscardHighlightRequested;
    private string harborHandHintKey = string.Empty;

    /// <summary>「你的回合」橫幅正在顯示（教學手牌引導須與此同步）。</summary>
    public bool IsYourTurnPromptBannerVisible() => IsPlayerTurnBannerVisuallyShowing();

    /// <summary>林可姐建議出牌時呼叫；實際高亮會等到「你的回合」橫幅出現。</summary>
    public void RequestTutorialHandPlayHighlights()
    {
        if (!TutorialBattleCoachUi.IsActiveForCurrentBattle) return;
        tutorialHandPlayHighlightRequested = true;
        tutorialHandDiscardHighlightRequested = false;
        RefreshTutorialHandPlayHighlights();
    }

    /// <summary>棄牌階段：高亮建議棄掉的手牌（與自動棄牌邏輯一致）。</summary>
    public void RequestTutorialHandDiscardHighlights()
    {
        if (!TutorialBattleCoachUi.IsActiveForCurrentBattle) return;
        tutorialHandDiscardHighlightRequested = true;
        tutorialHandPlayHighlightRequested = false;
        RefreshTutorialHandPlayHighlights();
    }

    public void RefreshTutorialHandPlayHighlights()
    {
        if (handArea == null || battleManager == null)
        {
            ClearTutorialHandPlayHighlights();
            return;
        }

        if (!TutorialBattleCoachUi.IsActiveForCurrentBattle)
        {
            ApplyTutorialHandPlayHighlightVisuals(null);
            return;
        }

        if (tutorialHandDiscardHighlightRequested
            && battleManager.IsPlayerTurn()
            && battleManager.IsPlayerInDiscardSelection())
        {
            if (!TutorialHandDiscardAdvisor.TryGetRecommendedDiscardHandIndices(battleManager, tutorialHandHintIndices))
            {
                ApplyTutorialHandPlayHighlightVisuals(null);
                return;
            }

            tutorialHandHintIndexSet.Clear();
            for (int i = 0; i < tutorialHandHintIndices.Count; i++)
                tutorialHandHintIndexSet.Add(tutorialHandHintIndices[i]);
            ApplyTutorialHandPlayHighlightVisuals(tutorialHandHintIndexSet);
            return;
        }

        if (!tutorialHandPlayHighlightRequested)
        {
            ApplyTutorialHandPlayHighlightVisuals(null);
            return;
        }

        // 教學戰：林可姐建議出牌時直接高亮，不依賴「你的回合」橫幅（該橫幅每回合僅閒置顯示一次；
        // 場上怪被擊殺後同一回合需再出牌時，橫幅不會重現會導致高亮永遠不出現）。
        bool requireYourTurnBanner = !TutorialBattleCoachUi.IsActiveForCurrentBattle;
        if (requireYourTurnBanner && !IsYourTurnPromptBannerVisible())
        {
            ApplyTutorialHandPlayHighlightVisuals(null);
            return;
        }

        if (!TutorialHandPlayAdvisor.TryGetRecommendedHandIndices(battleManager, tutorialHandHintIndices))
        {
            ApplyTutorialHandPlayHighlightVisuals(null);
            return;
        }

        tutorialHandHintIndexSet.Clear();
        for (int i = 0; i < tutorialHandHintIndices.Count; i++)
            tutorialHandHintIndexSet.Add(tutorialHandHintIndices[i]);

        ApplyTutorialHandPlayHighlightVisuals(tutorialHandHintIndexSet);
    }

    public void ClearTutorialHandPlayHighlights()
    {
        tutorialHandPlayHighlightRequested = false;
        tutorialHandDiscardHighlightRequested = false;
        tutorialHandHintIndices.Clear();
        tutorialHandHintIndexSet.Clear();
        ApplyTutorialHandPlayHighlightVisuals(null);
    }

    public void RequestHarborHandPlayHighlights(string hintKey)
    {
        if (!HarborCombatCoachUi.IsActiveForCurrentBattle || !HarborCombatCoachUi.ShouldAllowHandHighlight())
            return;
        harborHandPlayHighlightRequested = true;
        harborHandDiscardHighlightRequested = false;
        harborHandHintKey = hintKey ?? string.Empty;
        RefreshHarborHandPlayHighlights();
    }

    public void RequestHarborHandDiscardHighlights(string hintKey)
    {
        if (!HarborCombatCoachUi.IsActiveForCurrentBattle || !HarborCombatCoachUi.ShouldAllowHandHighlight())
            return;
        harborHandDiscardHighlightRequested = true;
        harborHandPlayHighlightRequested = false;
        harborHandHintKey = hintKey ?? string.Empty;
        RefreshHarborHandPlayHighlights();
    }

    public void ClearHarborHandPlayHighlights()
    {
        harborHandPlayHighlightRequested = false;
        harborHandDiscardHighlightRequested = false;
        harborHandHintKey = string.Empty;
        if (HarborCombatCoachUi.IsActiveForCurrentBattle)
            ApplyTutorialHandPlayHighlightVisuals(null);
    }

    public void RefreshHarborHandPlayHighlights()
    {
        if (handArea == null || battleManager == null)
        {
            ClearHarborHandPlayHighlights();
            return;
        }

        if (!HarborCombatCoachUi.IsActiveForCurrentBattle || !HarborCombatCoachUi.ShouldAllowHandHighlight())
        {
            ApplyTutorialHandPlayHighlightVisuals(null);
            return;
        }

        bool inDiscardPhase = battleManager.IsPlayerInDiscardSelection()
            || battleManager.GetPlayerPendingDiscardCount() > 0;
        if (inDiscardPhase && !harborHandDiscardHighlightRequested)
        {
            ApplyTutorialHandPlayHighlightVisuals(null);
            return;
        }

        if (harborHandDiscardHighlightRequested
            && battleManager.IsPlayerTurn()
            && (battleManager.IsPlayerInDiscardSelection() || battleManager.GetPlayerPendingDiscardCount() > 0))
        {
            if (!HarborCombatHandHighlightAdvisor.TryGetHighlightedHandIndices(
                    battleManager, harborHandHintKey, tutorialHandHintIndices))
            {
                ApplyTutorialHandPlayHighlightVisuals(null);
                return;
            }

            tutorialHandHintIndexSet.Clear();
            for (int i = 0; i < tutorialHandHintIndices.Count; i++)
                tutorialHandHintIndexSet.Add(tutorialHandHintIndices[i]);
            ApplyTutorialHandPlayHighlightVisuals(tutorialHandHintIndexSet);
            return;
        }

        if (!harborHandPlayHighlightRequested)
        {
            ApplyTutorialHandPlayHighlightVisuals(null);
            return;
        }

        if (!HarborCombatHandHighlightAdvisor.TryGetHighlightedHandIndices(
                battleManager, harborHandHintKey, tutorialHandHintIndices))
        {
            ApplyTutorialHandPlayHighlightVisuals(null);
            return;
        }

        tutorialHandHintIndexSet.Clear();
        for (int i = 0; i < tutorialHandHintIndices.Count; i++)
            tutorialHandHintIndexSet.Add(tutorialHandHintIndices[i]);
        ApplyTutorialHandPlayHighlightVisuals(tutorialHandHintIndexSet);
    }

    private void ApplyTutorialHandPlayHighlightVisuals(HashSet<int> highlightedIndices)
    {
        if (handArea == null) return;

        for (int i = 0; i < handArea.childCount; i++)
        {
            Transform child = handArea.GetChild(i);
            if (child == null) continue;
            if (!TryParsePlayerHandCardIndex(child.name, out int handIndex))
            {
                SetTutorialHandCardHighlighted(child.gameObject, false);
                continue;
            }

            bool on = highlightedIndices != null && highlightedIndices.Contains(handIndex);
            SetTutorialHandCardHighlighted(child.gameObject, on);
        }
    }

    private static void SetTutorialHandCardHighlighted(GameObject handCardRoot, bool highlighted)
    {
        if (handCardRoot == null) return;

        TutorialBattleHandPlayHighlight fx = handCardRoot.GetComponent<TutorialBattleHandPlayHighlight>();
        if (highlighted)
        {
            if (fx == null) fx = handCardRoot.AddComponent<TutorialBattleHandPlayHighlight>();
            fx.SetHighlighted(true);
        }
        else if (fx != null)
        {
            fx.SetHighlighted(false);
        }
    }

    private static bool TryParsePlayerHandCardIndex(string objectName, out int handIndex)
    {
        handIndex = -1;
        if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith("HandCard_")) return false;
        return int.TryParse(objectName.Substring("HandCard_".Length), out handIndex);
    }

    private bool tutorialPlayerHadFieldMonsterLastFrame;

    /// <summary>入門教學：場上怪獸被擊殺後，重新觸發出牌高亮與林可姐提示。</summary>
    private void TryHandleTutorialPlayerFieldMonsterLost()
    {
        if (!TutorialBattleCoachUi.IsActiveForCurrentBattle || battleManager == null)
        {
            tutorialPlayerHadFieldMonsterLastFrame = battleManager != null && battleManager.PlayerHasFieldMonster();
            return;
        }

        bool hasField = battleManager.PlayerHasFieldMonster();
        if (tutorialPlayerHadFieldMonsterLastFrame
            && !hasField
            && battleManager.IsPlayerTurn()
            && !battleManager.IsBattleOver()
            && !battleManager.IsTurnSequenceInProgress()
            && !battleManager.IsSpellCastPresentationActive())
        {
            tutorialHandPlayHighlightRequested = true;
            tutorialHandDiscardHighlightRequested = false;
            TutorialBattleCoachUi.NotifyPlayerFieldEmptiedDuringPlayerTurn(battleManager);
            RefreshTutorialHandPlayHighlights();
        }

        tutorialPlayerHadFieldMonsterLastFrame = hasField;
    }
}
