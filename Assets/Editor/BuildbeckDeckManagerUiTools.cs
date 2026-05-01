#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helpers when Buildbeck UI prefabs / hierarchy change and <see cref="DeckManager"/> still holds old references.
/// </summary>
public static class BuildbeckDeckManagerUiTools
{
    [MenuItem("Tools/Buildbeck/Clear DeckManager UI References (rebind on Play)")]
    private static void ClearDeckManagerUiRefs()
    {
        int n = 0;
        foreach (DeckManager dm in Object.FindObjectsByType<DeckManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(dm, "Clear DeckManager UI refs");
            dm.libraryPanel = null;
            dm.deckPanel = null;
            dm.deckSlotButton1 = null;
            dm.deckSlotButton2 = null;
            dm.deckSlotButton3 = null;
            dm.deckSlotButton4 = null;
            dm.deckSlotButton5 = null;
            EditorUtility.SetDirty(dm);
            n++;
        }

        if (n == 0)
            Debug.LogWarning("Buildbeck: No DeckManager in loaded scenes.");
        else
            Debug.Log($"Buildbeck: Cleared UI references on {n} DeckManager instance(s). Save the scene, then enter Play — layout auto-binder will reassign by name.");
    }
}
#endif
