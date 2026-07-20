using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>恐怖狀態期間：LateUpdate 將戰鬥場景內文字／數字亂碼化並持續跳動。</summary>
public sealed class M12PhaseAHorrorTextScrambleUi : MonoBehaviour
{
    private const float ScrambleIntervalSeconds = 0.07f;

    private static M12PhaseAHorrorTextScrambleUi instance;
    private float nextScrambleUnscaled;
    private readonly Dictionary<TextMeshProUGUI, string> originalTmpTexts = new Dictionary<TextMeshProUGUI, string>();
    private readonly Dictionary<Text, string> originalLegacyTexts = new Dictionary<Text, string>();
    private readonly List<TextMeshProUGUI> scrambleTmpTargets = new List<TextMeshProUGUI>(128);
    private readonly List<Text> scrambleLegacyTargets = new List<Text>(16);
    private Canvas battleCanvas;
    private bool targetsDirty = true;

    public static bool IsActive => instance != null && instance.enabled;

    public static void SetActiveForBattle(bool active)
    {
        if (!BattleLaunchContext.IsM12TrioTutorialBattle)
            return;

        M12PhaseAHorrorTextScrambleUi ui = EnsureInstance();
        if (ui == null)
            return;

        if (!active)
        {
            ui.enabled = false;
            ui.RestoreAllOriginalText();
            ForceRefreshBattleUi();
            return;
        }

        ui.originalTmpTexts.Clear();
        ui.originalLegacyTexts.Clear();
        ui.battleCanvas = null;
        ui.targetsDirty = true;
        ui.enabled = true;
        ui.nextScrambleUnscaled = 0f;
    }

    /// <summary>手牌重建等動態 UI 變更後，重新收集可亂碼文字（僅限對戰 Canvas 子樹）。</summary>
    public static void NotifyBattleTextTargetsChanged()
    {
        if (instance == null || !instance.enabled)
            return;
        instance.targetsDirty = true;
    }

    private static M12PhaseAHorrorTextScrambleUi EnsureInstance()
    {
        if (instance != null)
            return instance;

        BattleSimulationDebugUI debugUi = Object.FindFirstObjectByType<BattleSimulationDebugUI>();
        if (debugUi == null)
            return null;

        instance = debugUi.GetComponent<M12PhaseAHorrorTextScrambleUi>();
        if (instance == null)
            instance = debugUi.gameObject.AddComponent<M12PhaseAHorrorTextScrambleUi>();
        instance.enabled = false;
        return instance;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void LateUpdate()
    {
        if (!enabled || BattleAutoSimPlugin.IsRunning)
            return;

        if (Time.unscaledTime < nextScrambleUnscaled)
            return;

        nextScrambleUnscaled = Time.unscaledTime + ScrambleIntervalSeconds;
        if (targetsDirty)
            RebuildTargetLists();
        ScrambleRegisteredText();
    }

    private void RebuildTargetLists()
    {
        targetsDirty = false;
        scrambleTmpTargets.Clear();
        scrambleLegacyTargets.Clear();

        Canvas canvas = ResolveBattleCanvas();
        if (canvas == null)
        {
            targetsDirty = true;
            return;
        }

        canvas.GetComponentsInChildren(true, scrambleTmpTargets);
        canvas.GetComponentsInChildren(true, scrambleLegacyTargets);
    }

    private Canvas ResolveBattleCanvas()
    {
        if (battleCanvas != null)
            return battleCanvas;

        BattleSimulationDebugUI debugUi = Object.FindFirstObjectByType<BattleSimulationDebugUI>();
        if (debugUi == null)
            return null;

        battleCanvas = debugUi.GetComponentInParent<Canvas>();
        if (battleCanvas == null)
            battleCanvas = debugUi.ResolveBattleUiCanvas();
        return battleCanvas;
    }

    private void ScrambleRegisteredText()
    {
        for (int i = scrambleTmpTargets.Count - 1; i >= 0; i--)
        {
            TextMeshProUGUI tmp = scrambleTmpTargets[i];
            if (tmp == null)
            {
                scrambleTmpTargets.RemoveAt(i);
                originalTmpTexts.Remove(tmp);
                continue;
            }

            if (!tmp.isActiveAndEnabled || ShouldSkip(tmp.transform))
                continue;
            if (string.IsNullOrEmpty(tmp.text))
                continue;

            if (!originalTmpTexts.ContainsKey(tmp))
                originalTmpTexts[tmp] = tmp.text;
            tmp.text = M12PhaseAHorrorTextScramble.ScrambleRichText(originalTmpTexts[tmp]);
        }

        for (int i = scrambleLegacyTargets.Count - 1; i >= 0; i--)
        {
            Text legacy = scrambleLegacyTargets[i];
            if (legacy == null)
            {
                scrambleLegacyTargets.RemoveAt(i);
                originalLegacyTexts.Remove(legacy);
                continue;
            }

            if (!legacy.isActiveAndEnabled || ShouldSkip(legacy.transform))
                continue;
            if (string.IsNullOrEmpty(legacy.text))
                continue;

            if (!originalLegacyTexts.ContainsKey(legacy))
                originalLegacyTexts[legacy] = legacy.text;
            legacy.text = M12PhaseAHorrorTextScramble.ScrambleRichText(originalLegacyTexts[legacy]);
        }
    }

    private void RestoreAllOriginalText()
    {
        foreach (KeyValuePair<TextMeshProUGUI, string> kv in originalTmpTexts)
        {
            if (kv.Key != null)
                kv.Key.text = kv.Value;
        }

        foreach (KeyValuePair<Text, string> kv in originalLegacyTexts)
        {
            if (kv.Key != null)
                kv.Key.text = kv.Value;
        }

        originalTmpTexts.Clear();
        originalLegacyTexts.Clear();
        scrambleTmpTargets.Clear();
        scrambleLegacyTargets.Clear();
        targetsDirty = true;
    }

    private static void ForceRefreshBattleUi()
    {
        BattleSimulationDebugUI debugUi = Object.FindFirstObjectByType<BattleSimulationDebugUI>();
        if (debugUi != null)
            debugUi.ForceRefreshAllBattleTextAfterHorrorScramble();
    }

    private static bool ShouldSkip(Transform t)
    {
        while (t != null)
        {
            string name = t.name;
            if (name == "EndBattlePanel" ||
                name == "TutorialBattleSettlementOverlay" ||
                name == "TutorialSettlementPanel" ||
                name == "M12SettlementOverlay" ||
                name == "BattleSettlementFreezeRoot" ||
                name == "M12HorrorWhiteFlashOverlay")
                return true;
            t = t.parent;
        }

        return false;
    }
}
