using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpenPackge : MonoBehaviour
{
    [Header("Pack Config")]
    [SerializeField] private int packCost = 2;
    [SerializeField] private int cardsPerPack = 5;
    [SerializeField] private float openTimeoutSeconds = 15f;

    public GameObject cardPrefab;
    public Transform cardPool;

    public CardStore cardStore;
    public PlayerData playerData;

    [Header("Video")]
    public PackVideoController packVideo;

    [Header("Pack Reveal Layout")]
    [Tooltip("Card.prefab 根節點 scale=2 時的文字視覺；開包改 scale=1 貼 Grid 後，需補回文字倍率。")]
    [SerializeField] private float packRevealTextScaleMul = 2f;
    [SerializeField] private float packRevealInspectHoldSeconds = 0.4f;

    private ICardInspectPanelHost inspectHost;
    private readonly List<GameObject> cards = new List<GameObject>();
    private bool pendingOpen;
    private bool openingFinalized;
    private int reservedCoins;
    private Coroutine openTimeoutRoutine;

    void OnEnable()
    {
        if (packVideo != null) packVideo.Finished += OnVideoFinished;
    }

    void OnDisable()
    {
        if (packVideo != null) packVideo.Finished -= OnVideoFinished;
        if (pendingOpen) FailAndRollback("Open flow interrupted (disabled)");
    }

    void Start()
    {
        if (cardStore == null) cardStore = Object.FindFirstObjectByType<CardStore>();
        if (playerData == null) playerData = Object.FindFirstObjectByType<PlayerData>();
        EnsureInspectHost();
    }

    void Update()
    {
        if (inspectHost == null) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            inspectHost.HideCardInspect();
    }

    public void OnClickOpen()
    {
        if (pendingOpen) return;
        if (!ValidateRefs()) return;
        if (packCost <= 0 || cardsPerPack <= 0)
        {
            Debug.LogError("OpenPackge config invalid: packCost/cardsPerPack must be > 0");
            return;
        }

        if (playerData.playerCoins < packCost) return;

        pendingOpen = true;
        openingFinalized = false;
        reservedCoins = packCost;

        playerData.playerCoins -= packCost;
        playerData.RefreshCoins();

        ClearPool();
        StartTimeoutGuard();

        try
        {
            packVideo.PlayOnce();
        }
        catch (System.Exception ex)
        {
            FailAndRollback("Video play failed", ex);
        }
    }

    private void OnVideoFinished()
    {
        if (!pendingOpen || openingFinalized) return;
        openingFinalized = true;
        StopTimeoutGuard();

        try
        {
            List<Card> rewards = GeneratePackRewards();
            SaveCardData(rewards);
            playerData.SavePlayerData();
            SpawnRewardCards(rewards);

            pendingOpen = false;
            reservedCoins = 0;
        }
        catch (System.Exception ex)
        {
            FailAndRollback("Open flow commit failed", ex);
        }
    }

    public void ClearPool()
    {
        foreach (var card in cards) Destroy(card);
        cards.Clear();
    }

    private void SaveCardData(List<Card> rewards)
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            playerData.AddCollection(rewards[i].id, 1);
        }
    }

    private List<Card> GeneratePackRewards()
    {
        var rewards = new List<Card>(cardsPerPack);
        for (int i = 0; i < cardsPerPack; i++)
        {
            Card c = cardStore.RandomCard();
            if (c == null) throw new System.InvalidOperationException("CardStore.RandomCard() returned null.");
            rewards.Add(c);
        }

        return rewards;
    }

    private void SpawnRewardCards(List<Card> rewards)
    {
        Vector2 cellSize = ResolvePackRevealCardSize();
        for (int i = 0; i < rewards.Count; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, cardPool);
            if (!newCard.TryGetComponent<CardDisplay>(out var display))
                throw new System.InvalidOperationException("cardPrefab missing CardDisplay component.");

            ApplyPackRevealCardLayout(newCard, cellSize);
            display.SetCard(rewards[i]);
            ApplyPackRevealHideCombatStats(display);
            ApplyPackRevealCardTextScale(newCard, packRevealTextScaleMul);
            AttachPackRevealInspectLongPress(newCard, display, rewards[i]);
            cards.Add(newCard);
        }
    }

    private void EnsureInspectHost()
    {
        if (inspectHost != null) return;

        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        if (deckManager != null)
        {
            inspectHost = deckManager;
            return;
        }

        CardInspectPanelHost localHost = GetComponent<CardInspectPanelHost>();
        if (localHost == null)
            localHost = gameObject.AddComponent<CardInspectPanelHost>();
        localHost.Assign(playerData, cardStore);
        inspectHost = localHost;
    }

    private void AttachPackRevealInspectLongPress(GameObject cardObj, CardDisplay display, Card card)
    {
        EnsureInspectHost();
        if (inspectHost == null || card == null || display == null) return;

        CardInspectLongPress lp = cardObj.GetComponent<CardInspectLongPress>();
        if (lp == null) lp = cardObj.AddComponent<CardInspectLongPress>();
        lp.Setup(card, display, inspectHost, packRevealInspectHoldSeconds);
    }

    private static void ApplyPackRevealHideCombatStats(CardDisplay display)
    {
        if (display == null) return;
        if (display.attackText != null) display.attackText.gameObject.SetActive(false);
        if (display.healthText != null) display.healthText.gameObject.SetActive(false);
    }

    private Vector2 ResolvePackRevealCardSize()
    {
        if (cardPool != null && cardPool.TryGetComponent<GridLayoutGroup>(out GridLayoutGroup grid))
            return grid.cellSize;
        return new Vector2(CardArtLayoutSpec.PrefabRootWidthPx, CardArtLayoutSpec.PrefabRootHeightPx);
    }

    private static void ApplyPackRevealCardLayout(GameObject cardObj, Vector2 cellSize)
    {
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        if (rt == null) return;

        // Card.prefab 根節點預設 scale=2（對戰用）；開包 Grid 只設 sizeDelta 不會壓縮 scale，會顯得過大。
        rt.localScale = Vector3.one;
        rt.sizeDelta = cellSize;

        LayoutElement le = cardObj.GetComponent<LayoutElement>();
        if (le == null) le = cardObj.AddComponent<LayoutElement>();
        le.preferredWidth = cellSize.x;
        le.preferredHeight = cellSize.y;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;
    }

    private static void ApplyPackRevealCardTextScale(GameObject cardObj, float multiplier)
    {
        if (cardObj == null || multiplier <= 0.001f || Mathf.Approximately(multiplier, 1f))
            return;

        TextMeshProUGUI[] texts = cardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI tmp = texts[i];
            if (tmp == null) continue;
            RectTransform tr = tmp.rectTransform;
            Vector3 s = tr.localScale;
            tr.localScale = new Vector3(s.x * multiplier, s.y * multiplier, s.z);
        }
    }

    private bool ValidateRefs()
    {
        if (playerData == null || cardStore == null || packVideo == null || cardPrefab == null || cardPool == null)
        {
            Debug.LogError("OpenPackge missing refs: playerData/cardStore/packVideo/cardPrefab/cardPool");
            return false;
        }

        return true;
    }

    private void StartTimeoutGuard()
    {
        StopTimeoutGuard();
        if (openTimeoutSeconds > 0f)
            openTimeoutRoutine = StartCoroutine(OpenTimeoutRoutine());
    }

    private void StopTimeoutGuard()
    {
        if (openTimeoutRoutine == null) return;
        StopCoroutine(openTimeoutRoutine);
        openTimeoutRoutine = null;
    }

    private IEnumerator OpenTimeoutRoutine()
    {
        yield return new WaitForSeconds(openTimeoutSeconds);
        if (pendingOpen && !openingFinalized)
            FailAndRollback($"Open timeout after {openTimeoutSeconds:0.##} sec");
    }

    private void FailAndRollback(string reason, System.Exception ex = null)
    {
        if (!pendingOpen) return;

        Debug.LogError(ex == null ? $"OpenPackge failed: {reason}" : $"OpenPackge failed: {reason}\n{ex}");

        StopTimeoutGuard();
        pendingOpen = false;
        openingFinalized = true;

        if (reservedCoins > 0 && playerData != null)
        {
            playerData.playerCoins += reservedCoins;
            playerData.RefreshCoins();
            try
            {
                playerData.SavePlayerData();
            }
            catch (System.Exception saveEx)
            {
                Debug.LogError($"OpenPackge rollback save failed: {saveEx}");
            }
        }

        reservedCoins = 0;
    }
}
