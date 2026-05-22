using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bind designer-authored hall scene objects to existing project features.
/// </summary>
public class HallSceneFeatureBinder : MonoBehaviour
{
    private const string SceneName = "hall";
    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindHallFeatures()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribed = true;
        }
        TryBindForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindForScene(scene);
    }

    private static void TryBindForScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName) return;

        TryBindSceneButton("牌組", "Buildbeck");
        // Existing feature closest to backpack flow: return to Persistent hub.
        TryBindSceneButton("背包", "Persistent");
        TryBindSceneButton("商店", "CardStore");

        TryBindSceneButton("遊戲設定", "Settings");
        TryBindSceneButton("SettingsButton", "Settings");
        TryBindSceneButton("設定", "Settings");

        RefreshResourceDisplay();
    }

    private static void TryBindSceneButton(string objName, string targetSceneName)
    {
        GameObject go = GameObject.Find(objName);
        if (go == null) return;
        Button btn = EnsureButton(go);
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (Application.CanStreamedLevelBeLoaded(targetSceneName))
                SceneManager.LoadScene(targetSceneName);
            else
                Debug.LogError("HallSceneFeatureBinder: scene not found in Build Settings -> " + targetSceneName);
        });
    }

    private static Button EnsureButton(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    private static void RefreshResourceDisplay()
    {
        GameObject resourceArea = GameObject.Find("資源顯示區");
        if (resourceArea == null) return;

        PlayerData playerData = ResolvePlayerData();
        if (playerData != null) playerData.LoadPlayerData();
        string coinsText = playerData != null
            ? playerData.GetCoinsDisplayText()
            : (PlayerData.TryGetActiveSlotCoinsFromSave(out int coinsFromSave) ? coinsFromSave.ToString() : "0");
        int selectedDeckCount = playerData != null ? playerData.GetSelectedDeckTotalCount() : 0;

        TextMeshProUGUI[] labels = resourceArea.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TextMeshProUGUI t = labels[i];
            if (t == null) continue;
            string normalized = t.text.Replace("：", ":");
            if (normalized.Contains("金幣") || normalized.Contains("Coins"))
                t.text = "金幣: " + coinsText;
            else if (normalized.Contains("牌組張數") || normalized.Contains("Deck"))
                t.text = "牌組張數: " + selectedDeckCount;
        }
    }

    private static PlayerData ResolvePlayerData() => PlayerData.ResolveCanonical();

}
