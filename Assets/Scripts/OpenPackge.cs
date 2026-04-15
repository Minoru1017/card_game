using System.Collections.Generic;
using UnityEngine;
using System.Collections;

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
        for (int i = 0; i < rewards.Count; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, cardPool);
            if (!newCard.TryGetComponent<CardDisplay>(out var display))
                throw new System.InvalidOperationException("cardPrefab missing CardDisplay component.");

            display.SetCard(rewards[i]);
            cards.Add(newCard);
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
