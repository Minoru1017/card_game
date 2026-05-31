using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Buildbeck 離場／換場景時牌組存檔與 <see cref="PlayerData"/> 同步。</summary>
public partial class DeckManager
{
    private void OnDestroy()
    {
        if (IsCanonicalDeckManagerInstance())
        {
            SceneManager.sceneLoaded -= OnSceneLoadedEnsureBuildbeckDeckUi;
            SceneManager.sceneUnloaded -= OnSceneUnloadedFlushBuildbeckSave;
        }

        UnwireDeckScrollEdgeFeel(_libraryPanelScrollRect, OnLibraryPanelScrollFeel);
        UnwireDeckScrollEdgeFeel(_deckPanelScrollRect, OnDeckPanelScrollFeel);
        if (_libraryScrollEdgePulseCo != null) StopCoroutine(_libraryScrollEdgePulseCo);
        if (_deckScrollEdgePulseCo != null) StopCoroutine(_deckScrollEdgePulseCo);
        _libraryScrollEdgePulseCo = null;
        _deckScrollEdgePulseCo = null;
    }

    private void OnEnable()
    {
        if (!IsCanonicalDeckManagerInstance()) return;
        SceneManager.sceneLoaded -= OnSceneLoadedEnsureBuildbeckDeckUi;
        SceneManager.sceneLoaded += OnSceneLoadedEnsureBuildbeckDeckUi;
        SceneManager.sceneUnloaded -= OnSceneUnloadedFlushBuildbeckSave;
        SceneManager.sceneUnloaded += OnSceneUnloadedFlushBuildbeckSave;
    }

    private void OnDisable()
    {
        if (!IsCanonicalDeckManagerInstance()) return;
        SceneManager.sceneLoaded -= OnSceneLoadedEnsureBuildbeckDeckUi;
        SceneManager.sceneUnloaded -= OnSceneUnloadedFlushBuildbeckSave;
    }

    private static void OnSceneUnloadedFlushBuildbeckSave(Scene scene)
    {
        if (!scene.IsValid() || !scene.name.Equals("Buildbeck", System.StringComparison.OrdinalIgnoreCase))
            return;
        PlayerData pd = PlayerData.ResolveCanonical();
        if (pd != null) pd.SavePlayerData();
    }

    private void OnSceneLoadedEnsureBuildbeckDeckUi(Scene scene, LoadSceneMode mode)
    {
        RequestBuildbeckUiReload();
    }
}
