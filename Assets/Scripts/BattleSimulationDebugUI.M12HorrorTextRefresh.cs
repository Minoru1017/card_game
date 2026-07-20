using TMPro;
using UnityEngine;

public partial class BattleSimulationDebugUI
{
    /// <summary>恐怖亂碼 UI 收集目標用；與 <see cref="FindCanvas2"/> 同源。</summary>
    internal Canvas ResolveBattleUiCanvas()
    {
        Transform root = FindCanvas2();
        return root != null ? root.GetComponent<Canvas>() : null;
    }

    /// <summary>恐怖亂碼結束後，略過快取比對並從戰鬥狀態重寫所有可見文字。</summary>
    public void ForceRefreshAllBattleTextAfterHorrorScramble()
    {
        if (battleManager == null)
            return;

        nextRefreshTime = 0f;
        lastStatusStr = null;
        lastDeckStr = null;
        lastFieldStr = null;
        lastShownPlayerHeroHp = int.MinValue;
        lastShownEnemyHeroHp = int.MinValue;
        lastHandSignature = int.MinValue;
        lastRoundInitiativeSyncRound = -1;

        RefreshHeroHpHud();

        if (roundText != null)
            roundText.text = "Round " + battleManager.GetCurrentRound();

        SyncRoundInitiativeHud();

        if (weatherBadgeTmp != null)
            weatherBadgeTmp.text = "天氣：" + battleManager.GetCurrentWeatherLabelForUi();
        if (weatherRemainTmp != null)
            weatherRemainTmp.text = "效果剩餘回合：" + battleManager.GetCurrentWeatherRemainingRoundsForUi();
        if (weatherHintTmp != null)
            weatherHintTmp.text = "下一次天氣預報：" + battleManager.GetNextWeatherForecastHintForUi();
        if (activeWeatherEffectPanelRt != null && activeWeatherEffectPanelRt.gameObject.activeSelf)
            RefreshActiveWeatherEffectPanelText();

        bool debugPanelVisible = debugUiRoot == null || debugUiRoot.activeSelf;
        if (debugPanelVisible && statusText != null)
        {
            string statusStr = battleManager.GetBattleStateText();
            statusText.text = statusStr;
            lastStatusStr = statusStr;
        }

        if (debugPanelVisible && deckText != null)
        {
            string deckStr =
                "Player deck: " + battleManager.GetPlayerDeckCount() +
                "  Enemy deck: " + battleManager.GetEnemyDeckCount() +
                "\nPlayer discard: " + battleManager.GetPlayerDiscardCount() + "（" + battleManager.GetPlayerDiscardTopName() + "）" +
                "  Enemy discard: " + battleManager.GetEnemyDiscardCount() + "（" + battleManager.GetEnemyDiscardTopName() + "）";
            deckText.text = deckStr;
            lastDeckStr = deckStr;
        }

        if (debugPanelVisible && fieldText != null)
        {
            string toast = battleManager.GetBattleToastMessage();
            string toastLine = string.IsNullOrEmpty(toast)
                ? string.Empty
                : "\n<color=#AAFFCC>▶ " + toast + "</color>";
            string aiQuantLine =
                "\n<color=#FFD580>" + battleManager.GetEnemyAiQuantifiedTextForPlayerView() + "</color>";
            string fieldStr =
                battleManager.GetPlayerFieldText() + "\n" +
                battleManager.GetEnemyFieldText() +
                aiQuantLine +
                toastLine;
            fieldText.text = fieldStr;
            lastFieldStr = fieldStr;
        }

        RefreshExistingFieldCardDisplayTextOnly();

        if (handArea != null)
        {
            RebuildHandButtons();
            RebuildEnemyHandCards();
            lastHandSignature = ComputeHandSignature();
        }

        lastFieldSignature = ComputeFieldSignature();
        RefreshTurnBannerTextAfterHorrorScramble();

        M12BattleMissionBarUi missionBar = GetComponent<M12BattleMissionBarUi>();
        missionBar?.ForceRefreshNow();
    }

    private void RefreshTurnBannerTextAfterHorrorScramble()
    {
        if (turnBannerPanelRt == null || turnBannerTmp == null || !turnBannerPanelRt.gameObject.activeSelf)
            return;

        if (battleManager.IsPlayerTurn())
        {
            turnBannerTmp.text = "你的回合";
            turnBannerTmp.color = BattleUiColors.TurnPlayer;
        }
        else
        {
            turnBannerTmp.text = "敵方操作中...";
            turnBannerTmp.color = BattleUiColors.TurnEnemy;
        }
    }
}
