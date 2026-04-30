using System.IO;
using UnityEngine;

public class SaveResetManager : MonoBehaviour
{
    // ???m?w?]??
    public int defaultCoins = 100;
    public int defaultCardSlots = 8;

    // ?????R?????[??s?��]?i?�[?????m?^
    public void DeleteSave()
    {
        NavigateToPlayerInfoReset();
    }

    // ?N?????m???w?]??A??i??O?_???g?J???
    public void ResetToDefaults(bool saveAfterReset = true)
    {
        NavigateToPlayerInfoReset();
    }

    void RefreshUI()
    {
        var dm = Object.FindFirstObjectByType<DeckManager>();
        if (dm == null) return;

        dm.ClearPanels();
        dm.UpdateLibrary();
    }

    private void NavigateToPlayerInfoReset()
    {
        bool opened = GlobalNavRuntime.TryOpenPlayerInfoOverlay();
        if (!opened)
            Debug.LogWarning("SaveResetManager: cannot open Global Player Info overlay.");
    }
}
