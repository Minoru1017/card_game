using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class GlobalNavRuntime : MonoBehaviour
{
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshNavFontAndApplyToAllTexts();
        ApplySceneState(scene.name);
    }

    private void RefreshNavFontAndApplyToAllTexts()
    {
        navLabelFont = null;
        EnsureNavLabelFont();
        if (navLabelFont == null) return;
        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null) continue;
            labels[i].font = navLabelFont;
        }
    }

    private void ApplySceneState(string sceneName)
    {
        bool hidden = string.Equals(sceneName, "login", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sceneName, "Buildbeck", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sceneName, "Builddeck", StringComparison.OrdinalIgnoreCase);
        if (config.hideInSceneNameContains != null)
        {
            string lower = (sceneName ?? string.Empty).ToLowerInvariant();
            for (int i = 0; i < config.hideInSceneNameContains.Count; i++)
            {
                string key = config.hideInSceneNameContains[i];
                if (string.IsNullOrEmpty(key)) continue;
                if (lower.Contains(key.ToLowerInvariant()))
                {
                    hidden = true;
                    break;
                }
            }
        }

        if (string.Equals(sceneName, DeckPackSceneController.SceneName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sceneName, FreeBattleBattleCopy.SceneName, StringComparison.OrdinalIgnoreCase))
            hidden = false;

        if (view != null && view.triggerButtonObject != null) view.triggerButtonObject.SetActive(!hidden);
        SetTabPanelOpen(false);
        if (playerInfoOverlayRoot != null) playerInfoOverlayRoot.SetActive(false);
        CloseValuablesVaultOverlay();
    }

    private static void TryLoadHomeScene()
    {
        string home = string.IsNullOrWhiteSpace(config != null ? config.homeSceneName : null)
            ? "hall"
            : config.homeSceneName;
        string resolved = ResolveSceneFromBuildSettings(home);
        if (string.IsNullOrEmpty(resolved))
        {
            Debug.LogError("GlobalNavRuntime: home scene not found in Build Settings -> " + home);
            return;
        }
        SceneManager.LoadScene(resolved);
    }

    private static void TryLoadLoginScene()
    {
        Time.timeScale = 1f;
        string resolved = ResolveSceneFromBuildSettings("login");
        if (string.IsNullOrEmpty(resolved))
        {
            Debug.LogError("GlobalNavRuntime: login scene not found in Build Settings -> login");
            return;
        }
        SceneManager.LoadScene(resolved);
    }

    private static void TryLoadBackpackScene()
    {
        DeckPackViewSession.Clear();
        string preferred = string.IsNullOrWhiteSpace(config != null ? config.backpackSceneName : null)
            ? "Persistent"
            : config.backpackSceneName;
        string resolved = ResolveSceneFromBuildSettings(preferred);
        if (string.IsNullOrEmpty(resolved))
        {
            Debug.LogError("GlobalNavRuntime: backpack scene not found in Build Settings -> " + preferred);
            return;
        }
        SceneManager.LoadScene(resolved);
    }

    private static void TryLoadSettingsScene()
    {
        string preferred = string.IsNullOrWhiteSpace(config != null ? config.settingsSceneName : null)
            ? "Settings"
            : config.settingsSceneName;
        string resolved = ResolveSceneFromBuildSettings(preferred);
        if (string.IsNullOrEmpty(resolved))
        {
            Debug.LogError("GlobalNavRuntime: settings scene not found in Build Settings -> " + preferred);
            return;
        }
        SceneManager.LoadScene(resolved);
    }

    private static string ResolveSceneFromBuildSettings(string preferredName)
    {
        if (string.IsNullOrEmpty(preferredName)) return null;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;
            string sceneName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(sceneName, preferredName, StringComparison.OrdinalIgnoreCase))
                return sceneName;
        }
        return null;
    }
}
