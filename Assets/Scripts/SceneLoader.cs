using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public partial class SceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    public string battleSceneName = "BattleSimulation";
    public string persistentSceneName = "Persistent";
    [Header("Deck Check")]
    public PlayerData playerData;
    public CardStore cardStore;
    public Button enterBattleButton;
    public Text noDeckHintLegacyText;
    public TextMeshProUGUI noDeckHintTMPText;

    private const string NoDeckHintMessage = "尚未組建牌組";

    private void Start()
    {
        RefreshEnterBattleState();
    }

    private void OnEnable()
    {
        RefreshEnterBattleState();
    }

    // Bind this method to the "進入對戰" button OnClick.
    public void EnterBattle()
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        if (playerData != null) playerData.LoadPlayerData(); // always use latest saved deck

        RefreshEnterBattleState();
        if (!HasBuiltDeck())
        {
            ShowNoDeckHint(true);
            return;
        }

        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogError("SceneLoader: battleSceneName is empty.");
            return;
        }

        ShowBattlePreviewModal();
    }

    private void StartBattleSceneLoad()
    {
        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogError("SceneLoader: battleSceneName is empty.");
            return;
        }
        SceneManager.sceneLoaded -= OnSceneLoadedFixup;
        SceneManager.sceneLoaded += OnSceneLoadedFixup;
        SceneManager.LoadScene(battleSceneName);
    }

    // Bind this method to the "前往 Persistent" button OnClick.
    public void EnterPersistent()
    {
        if (string.IsNullOrWhiteSpace(persistentSceneName))
        {
            Debug.LogError("SceneLoader: persistentSceneName is empty.");
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(persistentSceneName))
        {
            Debug.LogError("SceneLoader: persistent scene not in Build Settings -> " + persistentSceneName);
            return;
        }

        SceneManager.LoadScene(persistentSceneName);
    }

    public void RefreshEnterBattleState() => RefreshEnterBattleState(true);

    /// <param name="reloadFromDisk">剛在本場景儲存牌組／改名後請傳 false，避免立刻從舊備份重載覆蓋記憶體。</param>
    public void RefreshEnterBattleState(bool reloadFromDisk)
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        if (reloadFromDisk && playerData != null) playerData.LoadPlayerData();

        bool hasDeck = HasBuiltDeck();
        // IMPORTANT: only control explicitly assigned battle button.
        // Avoid auto-grabbing current GameObject button, which can disable unrelated buttons.
        if (enterBattleButton != null) enterBattleButton.interactable = hasDeck;
        ShowNoDeckHint(!hasDeck);
    }

    private bool HasBuiltDeck()
    {
        if (playerData == null) return false;
        return playerData.GetSelectedDeckTotalCount() > 0;
    }

    private void ShowNoDeckHint(bool show)
    {
        if (noDeckHintLegacyText != null)
        {
            noDeckHintLegacyText.gameObject.SetActive(show);
            if (show) noDeckHintLegacyText.text = NoDeckHintMessage;
        }
        if (noDeckHintTMPText != null)
        {
            noDeckHintTMPText.gameObject.SetActive(show);
            if (show) noDeckHintTMPText.text = NoDeckHintMessage;
        }
    }

    private void OnSceneLoadedFixup(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != battleSceneName) return;
        SceneManager.sceneLoaded -= OnSceneLoadedFixup;

        // Force-enable canvases and normalize scale.
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null) continue;
            c.gameObject.SetActive(true);
            c.enabled = true;
            c.transform.localScale = Vector3.one;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.enabled = true;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.orthographic = true;
        }

        // Ensure battle manager exists to initialize UI/combat.
        BattleSimulationManager manager = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("BattleManager");
            manager = go.AddComponent<BattleSimulationManager>();
            manager.autoStartOnPlay = true;
            Debug.LogWarning("SceneLoader: Auto-created BattleManager in battle scene.");
        }
        if (manager != null)
        {
            string label = string.IsNullOrWhiteSpace(pendingDifficultyLabelZh)
                ? BattleLaunchContext.PeekDifficultyLabelZh()
                : pendingDifficultyLabelZh;
            if (!string.IsNullOrWhiteSpace(label))
                BattleLaunchContext.SetPendingDifficultyLabelZh(label);
            manager.ApplyLaunchContextDifficulty();
            manager.QueueRuntimeDifficultyConfig(
                pendingUseFixedEnemyDeck,
                pendingFixedEnemyDeckCardIds,
                pendingEnemyOverLimitAllowance,
                pendingMinEnemySpellsInDeck,
                pendingEnemyAiPlayStyle,
                label);
            manager.CaptureBattleDifficultyForRecords();
        }
    }

}
