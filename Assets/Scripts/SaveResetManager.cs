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
        string path = Path.Combine(Application.persistentDataPath, "playerdata.csv");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Deleted save: " + path);
        }
        else
        {
            Debug.Log("No save file to delete at: " + path);
        }
        // ?P?B?M??O???�\?????
        ResetToDefaults(false);
        // ?i??G???s?x?s??????
        // var pd = FindObjectOfType<PlayerData>();
        // if (pd != null) pd.SavePlayerData();
        ResetToDefaults(true); // ????R????????@?????b?s??
        RefreshUI();
    }

    // ?N?????m???w?]??A??i??O?_???g?J???
    public void ResetToDefaults(bool saveAfterReset = true)
    {
        Debug.Log("Saved after reset to: " + Path.Combine(Application.persistentDataPath, "playerdata.csv"));
        // ??? FindFirstObjectByType?]?? FindAnyObjectByType?^
        var pd = Object.FindFirstObjectByType<PlayerData>();
        // var pd = Object.FindAnyObjectByType<PlayerData>();

        if (pd != null)
        {
            pd.playerCoins = defaultCoins;
            pd.totalCoins = pd.playerCoins;
            pd.ClearAllCollectionAndDecks();

            if (saveAfterReset)
                pd.SavePlayerData();

            Debug.Log("Reset to defaults: coins=" + pd.playerCoins);
        }
        else
        {
            Debug.LogError("Cannot reset: PlayerData not found in scene.");
        }

        RefreshUI();
    }

    void RefreshUI()
    {
        var dm = Object.FindFirstObjectByType<DeckManager>();
        if (dm == null) return;

        dm.ClearPanels();
        dm.UpdateLibrary();
    }
}
