using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    /// <summary>選填：顯示 <see cref="Card.rarity"/>（例如 N / SR）；Prefab 未綁定時略過。</summary>
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI effectText;

    public Image backgroundImage;
    public Card card;

    [Header("CardArt 稀有度框（僅 CardArt 脈絡顯示；組牌 DeckThumb 不顯示）")]
    [Tooltip("選填。未指定時嘗試 Resources.Load(\"UI/Rarity/稀有度N\")（需將圖放於 Assets/Resources/UI/Rarity/）。")]
    [SerializeField] private Sprite cardArtRarityFrameN;
    [Tooltip("選填。未指定時嘗試 Resources.Load(\"UI/Rarity/稀有度R\")（檔於 Assets/Resources/UI/Rarity/）；其次 UI/Rarity/R。")]
    [SerializeField] private Sprite cardArtRarityFrameR;
    [Tooltip("選填。未指定時嘗試 Resources.Load(\"UI/Rarity/稀有度SR\")（檔於 Assets/Resources/UI/Rarity/）。")]
    [SerializeField] private Sprite cardArtRarityFrameSr;
    [Tooltip("選填。未指定時嘗試 Resources.Load(\"UI/Rarity/稀有度SSR\")（檔於 Assets/Resources/UI/Rarity/）。")]
    [SerializeField] private Sprite cardArtRarityFrameSsr;
    [Tooltip("選填。未指定時嘗試 Resources.Load(\"UI/Rarity/稀有度UR\")；其次 UI/Rarity/UR。")]
    [SerializeField] private Sprite cardArtRarityFrameUr;

    /// <summary>執行期共用 N 框 Sprite（由任一 CardDisplay 指定或 Resources 載入）。</summary>
    private static Sprite s_sharedCardArtRarityFrameN;
    /// <summary>執行期共用 R 框 Sprite（由任一 CardDisplay 指定或 Resources 載入）。</summary>
    private static Sprite s_sharedCardArtRarityFrameR;
    /// <summary>執行期共用 SR 框 Sprite（由任一 CardDisplay 指定或 Resources 載入）。</summary>
    private static Sprite s_sharedCardArtRarityFrameSr;
    /// <summary>執行期共用 SSR 框 Sprite（由任一 CardDisplay 指定或 Resources 載入）。</summary>
    private static Sprite s_sharedCardArtRarityFrameSsr;
    /// <summary>執行期共用 UR 框 Sprite（由任一 CardDisplay 指定或 Resources 載入）。</summary>
    private static Sprite s_sharedCardArtRarityFrameUr;

    void Awake()
    {
        if (cardArtRarityFrameN != null && s_sharedCardArtRarityFrameN == null)
            s_sharedCardArtRarityFrameN = cardArtRarityFrameN;
        if (cardArtRarityFrameR != null && s_sharedCardArtRarityFrameR == null)
            s_sharedCardArtRarityFrameR = cardArtRarityFrameR;
        if (cardArtRarityFrameSr != null && s_sharedCardArtRarityFrameSr == null)
            s_sharedCardArtRarityFrameSr = cardArtRarityFrameSr;
        if (cardArtRarityFrameSsr != null && s_sharedCardArtRarityFrameSsr == null)
            s_sharedCardArtRarityFrameSsr = cardArtRarityFrameSsr;
        if (cardArtRarityFrameUr != null && s_sharedCardArtRarityFrameUr == null)
            s_sharedCardArtRarityFrameUr = cardArtRarityFrameUr;
    }

    void Start()
    {
        
    }

    public void SetCard(Card c)
    {
        card = c;
        if (card == null)
        {
            Debug.LogError("CardDisplay.SetCard: card is null");
            return;
        }
        ShowCard();
    }


    public void ShowCard()
    {
        if (card == null) return;
        if (nameText != null) nameText.text = card.cardName;
        if (rarityText != null)
        {
            rarityText.gameObject.SetActive(true);
            rarityText.text = card.rarity.ToString();
        }

        Sprite portrait = ResolvePortraitSpriteForCurrentContext();
        if (portrait != null)
            ApplyArtworkSprite(portrait);

        // ?????????}?]??????~???^
        if (attackText != null) attackText.gameObject.SetActive(true);
        if (healthText != null) healthText.gameObject.SetActive(true);
        if (effectText != null) effectText.gameObject.SetActive(true);

        if (card is MonsterCard monster)
        {
            if (attackText != null) attackText.text = monster.attack.ToString();
            if (healthText != null)
            {
                if (monster.healthPoint > monster.healthPointMax)
                {
                    int ov = monster.healthPoint - monster.healthPointMax;
                    healthText.overflowMode = TMPro.TextOverflowModes.Overflow;
                    healthText.richText = true;
                    healthText.text =
                        "<color=#FFFFFF>" + monster.healthPointMax + "</color> <color=#66FF99>+" + ov + "</color>";
                    healthText.color = Color.white;
                }
                else
                {
                    healthText.richText = false;
                    healthText.text = monster.healthPoint.ToString();
                }
            }
            if (effectText != null) effectText.gameObject.SetActive(false);
        }
        else if (card is SpellCard spell)
        {
            if (effectText != null) effectText.text = spell.effect;
            if (attackText != null) attackText.gameObject.SetActive(false);
            if (healthText != null) healthText.gameObject.SetActive(false);
        }

        RefreshCardArtRarityOverlay();
    }

    private static ClickCard ResolveClickCardInContext(MonoBehaviour host)
    {
        if (host == null) return null;
        ClickCard onSelf = host.GetComponent<ClickCard>();
        if (onSelf != null) return onSelf;
        return host.GetComponentInParent<ClickCard>();
    }

    private Sprite ResolvePortraitSpriteForCurrentContext()
    {
        if (card == null) return null;
        ClickCard clickCard = ResolveClickCardInContext(this);
        bool isDeckCard = clickCard != null && clickCard.state == CardState.Deck;
        if (isDeckCard)
            return card.ResolveDeckThumbSprite();

        bool isLibraryCard = clickCard != null && clickCard.state == CardState.Library;
        // Buildbeck「Library Grid Viewport」館藏格：與牌組區一致使用 DeckThumb；其餘場景館藏仍用本體立繪。
        if (isLibraryCard && IsBuildbeckLibraryGridContext())
        {
            Sprite thumb = card.ResolveDeckThumbSprite();
            if (thumb != null) return thumb;
        }

        return card.ResolveCardArtSprite();
    }

    /// <summary>
    /// 本體立繪 CardArt 上才疊稀有度框：排除組牌 DeckThumb、Buildbeck 館藏格 DeckThumb。
    /// 對戰場景下手牌（<c>HandArea</c>）與場上牌（<c>PlayerFieldArea</c>／<c>EnemyFieldArea</c>等）一律允許，
    /// 含 Prefab 誤掛 <see cref="CardState.Deck"/> 導致肖像為縮圖時仍可顯示 N／R／SR／SSR／UR 框。
    /// </summary>
    private bool ShouldShowCardArtRarityOverlay()
    {
        if (card == null) return false;

        if (IsBattleSimulationPortraitContext() &&
            IsBattleSimulationHandOrFieldAreaForRarityOverlay(transform))
        {
            return ResolvePortraitSpriteForCurrentContext() != null || card.ResolveCardArtSprite() != null;
        }

        ClickCard clickCard = ResolveClickCardInContext(this);
        if (clickCard != null && clickCard.state == CardState.Deck)
            return false;

        bool isLibraryCard = clickCard != null && clickCard.state == CardState.Library;
        if (isLibraryCard && IsBuildbeckLibraryGridContext())
            return false;

        Sprite portrait = ResolvePortraitSpriteForCurrentContext();
        Sprite fullArt = card.ResolveCardArtSprite();
        if (portrait == null || fullArt == null) return false;
        return ReferenceEquals(portrait, fullArt);
    }

    /// <summary>對戰 UI 在 <see cref="BattleSimulationDebugUI.ApplyPrefabVisualTuning"/> 之後呼叫，讓稀有度框對齊 Role（繼承縮放）並疊在立繪之上。</summary>
    public void RefreshCardArtRarityOverlayExternal()
    {
        RefreshCardArtRarityOverlay();
    }

    private static bool IsTransformUnderNamedAncestor(Transform t, string ancestorName)
    {
        if (t == null || string.IsNullOrEmpty(ancestorName)) return false;
        for (Transform p = t; p != null; p = p.parent)
        {
            if (p.name == ancestorName)
                return true;
        }
        return false;
    }

    /// <summary>對戰 UI：手牌區或怪物／咒術場地區（與手牌相同稀有度框規則）。</summary>
    private static bool IsBattleSimulationHandOrFieldAreaForRarityOverlay(Transform cardDisplayTransform)
    {
        if (cardDisplayTransform == null) return false;
        return IsTransformUnderNamedAncestor(cardDisplayTransform, "HandArea") ||
               IsTransformUnderNamedAncestor(cardDisplayTransform, "PlayerFieldArea") ||
               IsTransformUnderNamedAncestor(cardDisplayTransform, "EnemyFieldArea") ||
               IsTransformUnderNamedAncestor(cardDisplayTransform, "PlayerSpellFieldArea") ||
               IsTransformUnderNamedAncestor(cardDisplayTransform, "EnemySpellFieldArea");
    }

    /// <summary>
    /// 多 Slice 的稀有度圖在部分 Unity 版本下 <c>Resources.Load&lt;Sprite&gt;</c> 會為 null，改以 <c>LoadAll</c> 取第一個 Sprite。
    /// </summary>
    private static Sprite LoadRaritySpriteFromResources(ref Sprite cacheField, string resourcesPath)
    {
        if (cacheField != null)
            return cacheField;
        cacheField = Resources.Load<Sprite>(resourcesPath);
        if (cacheField == null)
        {
            Sprite[] slices = Resources.LoadAll<Sprite>(resourcesPath);
            if (slices != null)
            {
                for (int i = 0; i < slices.Length; i++)
                {
                    if (slices[i] != null)
                    {
                        cacheField = slices[i];
                        break;
                    }
                }
            }
        }
        return cacheField;
    }

    private Sprite ResolveCardArtRarityNFrameSprite()
    {
        if (cardArtRarityFrameN != null)
            return cardArtRarityFrameN;
        LoadRaritySpriteFromResources(ref s_sharedCardArtRarityFrameN, "UI/Rarity/稀有度N");
        return s_sharedCardArtRarityFrameN;
    }

    private Sprite ResolveCardArtRarityRFrameSprite()
    {
        if (cardArtRarityFrameR != null)
            return cardArtRarityFrameR;
        return ResolveSharedCardArtRarityRFrameSprite();
    }

    /// <summary>與 N 框相同規則：主檔 <c>Resources/UI/Rarity/稀有度R</c>；無則嘗試 <c>UI/Rarity/R</c>。</summary>
    private static Sprite ResolveSharedCardArtRarityRFrameSprite()
    {
        if (s_sharedCardArtRarityFrameR != null)
            return s_sharedCardArtRarityFrameR;
        LoadRaritySpriteFromResources(ref s_sharedCardArtRarityFrameR, "UI/Rarity/稀有度R");
        if (s_sharedCardArtRarityFrameR == null)
            LoadRaritySpriteFromResources(ref s_sharedCardArtRarityFrameR, "UI/Rarity/R");
        return s_sharedCardArtRarityFrameR;
    }

    private Sprite ResolveCardArtRaritySrFrameSprite()
    {
        if (cardArtRarityFrameSr != null)
            return cardArtRarityFrameSr;
        return ResolveSharedCardArtRaritySrFrameSprite();
    }

    /// <summary>主檔 <c>Resources/UI/Rarity/稀有度SR</c>。</summary>
    private static Sprite ResolveSharedCardArtRaritySrFrameSprite()
    {
        if (s_sharedCardArtRarityFrameSr != null)
            return s_sharedCardArtRarityFrameSr;
        LoadRaritySpriteFromResources(ref s_sharedCardArtRarityFrameSr, "UI/Rarity/稀有度SR");
        return s_sharedCardArtRarityFrameSr;
    }

    private Sprite ResolveCardArtRaritySsrFrameSprite()
    {
        if (cardArtRarityFrameSsr != null)
            return cardArtRarityFrameSsr;
        return ResolveSharedCardArtRaritySsrFrameSprite();
    }

    /// <summary>主檔 <c>Resources/UI/Rarity/稀有度SSR</c>。</summary>
    private static Sprite ResolveSharedCardArtRaritySsrFrameSprite()
    {
        if (s_sharedCardArtRarityFrameSsr != null)
            return s_sharedCardArtRarityFrameSsr;
        LoadRaritySpriteFromResources(ref s_sharedCardArtRarityFrameSsr, "UI/Rarity/稀有度SSR");
        if (s_sharedCardArtRarityFrameSsr == null)
            LoadRaritySpriteFromResources(ref s_sharedCardArtRarityFrameSsr, "UI/Rarity/SSR");
        return s_sharedCardArtRarityFrameSsr;
    }

    private Sprite ResolveCardArtRarityUrFrameSprite()
    {
        if (cardArtRarityFrameUr != null)
            return cardArtRarityFrameUr;
        return ResolveSharedCardArtRarityUrFrameSprite();
    }

    /// <summary>主檔 <c>Resources/UI/Rarity/稀有度UR</c>；備援 <c>UI/Rarity/UR</c>。</summary>
    private static Sprite ResolveSharedCardArtRarityUrFrameSprite()
    {
        if (s_sharedCardArtRarityFrameUr != null)
            return s_sharedCardArtRarityFrameUr;
        LoadRaritySpriteFromResources(ref s_sharedCardArtRarityFrameUr, "UI/Rarity/稀有度UR");
        if (s_sharedCardArtRarityFrameUr == null)
            LoadRaritySpriteFromResources(ref s_sharedCardArtRarityFrameUr, "UI/Rarity/UR");
        return s_sharedCardArtRarityFrameUr;
    }

    private static Sprite ResolveCardArtRarityOverlaySpriteForCard(Card card, CardDisplay hostCd)
    {
        if (card == null) return null;
        switch (card.rarity)
        {
            case CardRarity.N:
                if (hostCd != null)
                    return hostCd.ResolveCardArtRarityNFrameSprite();
                LoadRaritySpriteFromResources(ref s_sharedCardArtRarityFrameN, "UI/Rarity/稀有度N");
                return s_sharedCardArtRarityFrameN;
            case CardRarity.R:
                if (hostCd != null)
                    return hostCd.ResolveCardArtRarityRFrameSprite();
                return ResolveSharedCardArtRarityRFrameSprite();
            case CardRarity.SR:
                if (hostCd != null)
                    return hostCd.ResolveCardArtRaritySrFrameSprite();
                return ResolveSharedCardArtRaritySrFrameSprite();
            case CardRarity.SSR:
                if (hostCd != null)
                    return hostCd.ResolveCardArtRaritySsrFrameSprite();
                return ResolveSharedCardArtRaritySsrFrameSprite();
            case CardRarity.UR:
                if (hostCd != null)
                    return hostCd.ResolveCardArtRarityUrFrameSprite();
                return ResolveSharedCardArtRarityUrFrameSprite();
            default:
                return null;
        }
    }

    /// <summary>背包 Inspect 等大圖僅有 Image 時，強制依 CardArt 規則疊加 N／R／SR／SSR／UR 框。</summary>
    public static void SyncCardArtRarityOverlay(Image portraitImage, Card card)
    {
        ApplyCardArtRarityOverlayInternal(portraitImage, card, treatAsCardArtContext: true);
    }

    private void RefreshCardArtRarityOverlay()
    {
        Image artTarget = backgroundImage != null ? backgroundImage : ResolveArtworkTargetImage();
        ApplyCardArtRarityOverlayInternal(artTarget, card, ShouldShowCardArtRarityOverlay());
    }

    private static void ApplyCardArtRarityOverlayInternal(Image portraitImage, Card card, bool treatAsCardArtContext)
    {
        const string overlayName = "CardArtRarityOverlay";
        if (portraitImage == null)
            return;

        Transform portraitTf = portraitImage.transform;
        Transform overlayParent = portraitTf;

        void HideOverlay()
        {
            Transform nested = portraitTf.Find(overlayName);
            if (nested != null)
                nested.gameObject.SetActive(false);

            Transform root = portraitTf.parent;
            if (root != null)
            {
                Transform legacy = root.Find(overlayName);
                if (legacy != null && legacy.parent == root)
                    legacy.gameObject.SetActive(false);
            }
        }

        if (!treatAsCardArtContext || card == null)
        {
            HideOverlay();
            return;
        }

        CardDisplay hostCd = portraitImage.GetComponentInParent<CardDisplay>();
        Sprite frame = ResolveCardArtRarityOverlaySpriteForCard(card, hostCd);

        if (frame == null)
        {
            HideOverlay();
            return;
        }

        Transform rootTf = portraitTf.parent;
        if (rootTf != null)
        {
            Transform legacyOnCardRoot = rootTf.Find(overlayName);
            if (legacyOnCardRoot != null && legacyOnCardRoot.parent == rootTf)
                Object.Destroy(legacyOnCardRoot.gameObject);
        }

        Transform existingTf = overlayParent != null ? overlayParent.Find(overlayName) : null;
        Image overlayImg;
        if (existingTf == null)
        {
            GameObject go = new GameObject(overlayName, typeof(RectTransform), typeof(Image));
            overlayImg = go.GetComponent<Image>();
            RectTransform ort = go.GetComponent<RectTransform>();
            ort.SetParent(overlayParent, false);
            overlayImg.raycastTarget = false;
            overlayImg.preserveAspect = true;
        }
        else
        {
            overlayImg = existingTf.GetComponent<Image>();
            if (overlayImg == null)
                overlayImg = existingTf.gameObject.AddComponent<Image>();
        }

        overlayImg.sprite = frame;
        overlayImg.color = Color.white;
        overlayImg.gameObject.SetActive(true);

        RectTransform ortRt = overlayImg.rectTransform;
        ortRt.anchorMin = Vector2.zero;
        ortRt.anchorMax = Vector2.one;
        ortRt.offsetMin = Vector2.zero;
        ortRt.offsetMax = Vector2.zero;
        ortRt.localScale = Vector3.one;
        overlayImg.transform.SetAsLastSibling();
    }

    private static bool IsBuildbeckLibraryGridContext()
    {
        Scene s = SceneManager.GetActiveScene();
        return s.IsValid() && s.name.Equals("Buildbeck", System.StringComparison.OrdinalIgnoreCase);
    }

    private Image ResolveArtworkTargetImage()
    {
        if (backgroundImage != null) return backgroundImage;

        // Prefer common art node names used by current card prefabs.
        string[] preferredNames = { "Role", "BGImage", "Artwork", "Art", "Card Art" };
        for (int i = 0; i < preferredNames.Length; i++)
        {
            Transform t = FindDeepChildByName(transform, preferredNames[i]);
            if (t == null) continue;
            Image namedImg = t.GetComponent<Image>();
            if (namedImg != null)
            {
                backgroundImage = namedImg;
                return backgroundImage;
            }
        }

        // Fallback: first child Image in this card object hierarchy.
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) continue;
            if (images[i].transform == transform) continue;
            backgroundImage = images[i];
            return backgroundImage;
        }

        return null;
    }

    private void ApplyArtworkSprite(Sprite sprite)
    {
        if (sprite == null) return;
        ClickCard clickCard = ResolveClickCardInContext(this);
        bool isLibraryCard = clickCard != null && clickCard.state == CardState.Library;
        bool isDeckCard = clickCard != null && clickCard.state == CardState.Deck;

        // 組建牌組區：僅 DeckThumb，套在 DeckCardInfoStrip mt 的 Art 上。
        if (isDeckCard)
        {
            Image dfArt = FindDeckStripShellArtImage(DeckStripMtDfName);
            Image oiArt = FindDeckStripShellArtImage(DeckStripMtOiName);

            if (dfArt != null || oiArt != null)
            {
                ApplySpriteToDeckStripArtTarget(dfArt, sprite);
                ApplySpriteToDeckStripArtTarget(oiArt, sprite);
                backgroundImage = dfArt != null ? dfArt : oiArt;
                return;
            }

            Image stripArt = FindNamedImage("Art");
            if (stripArt != null && IsUnderDeckCardInfoStrip(stripArt.transform))
            {
                ApplySpriteToDeckStripArtTarget(stripArt, sprite);
                backgroundImage = stripArt;
            }

            return;
        }

        // 館藏：Buildbeck Grid 上為 DeckThumb（與 ResolvePortrait 一致）；DeckGen 橢圓槽位 + 可選 Card Art 層。
        if (isLibraryCard)
        {
            Image libraryCardArt = FindNamedImage("Card Art");
            if (libraryCardArt != null)
            {
                ApplyCardPresetArtSprite(libraryCardArt, sprite);
                backgroundImage = libraryCardArt;
            }

            Image dfArt = FindDeckGenLibraryShellArtImage("DeckGen_Library_df");
            Image oiArt = FindDeckGenLibraryShellArtImage("DeckGen_Library_oi");
            if (oiArt == null) oiArt = FindDeckGenLibraryShellArtImage("DeckGen_Library_ol");

            if (dfArt != null || oiArt != null)
            {
                ApplySpriteToLibraryArtTarget(dfArt, sprite);
                ApplySpriteToLibraryArtTarget(oiArt, sprite);
                if (backgroundImage == null)
                    backgroundImage = dfArt != null ? dfArt : oiArt;
                return;
            }

            if (backgroundImage != null) return;

            Image libraryTarget = FindNamedImage("BGImage");
            if (libraryTarget == null)
            {
                Transform libDf = FindDeepChildByName(transform, "DeckGen_Library_df");
                if (libDf != null) libraryTarget = libDf.GetComponent<Image>();
            }

            if (libraryTarget != null)
            {
                ApplyCardPresetArtSprite(libraryTarget, sprite);
                backgroundImage = libraryTarget;
                return;
            }
        }

        // Try commonly used art layers first.
        Image nbBg = FindNamedImage("NB_BG");
        Image role = FindNamedImage("Role");
        Image bgImage = FindNamedImage("BGImage");
        Image art = FindNamedImage("Art");
        Image artwork = FindNamedImage("Artwork");
        Image cardArt = FindNamedImage("Card Art");

        // Prefer an already-visible layer to avoid layout side effects.
        Image target = PickFirstVisible(nbBg, role, bgImage, art, artwork, cardArt);
        if (target == null) target = PickFirstNonNull(nbBg, role, bgImage, art, artwork, cardArt, ResolveArtworkTargetImage());
        if (target == null) return;

        if (IsBattleSimulationPortraitContext())
        {
            ApplyBattlePortraitSprite(target, sprite);
            backgroundImage = target;
            return;
        }

        ApplyCardPresetArtSprite(target, sprite);
        backgroundImage = target;
    }

    private static bool IsBattleSimulationPortraitContext()
    {
        return SceneManager.GetActiveScene().name == "BattleSimulation";
    }

    /// <summary>對戰手牌／場上：只換圖、等比縮放，不改卡框比例。</summary>
    private static void ApplyBattlePortraitSprite(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        target.sprite = sprite;
        target.color = Color.white;
        target.preserveAspect = true;
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
    }

    /// <summary>本體立繪：背包／詳情等用 337×491。</summary>
    private void ApplyCardPresetArtSprite(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;

        RectTransform rt = target.rectTransform;
        if (rt != null && IsCardPresetArtLayer(target.transform))
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = CardArtLayoutSpec.PresetArtSize;
        }

        target.sprite = sprite;
        target.color = Color.white;
        target.preserveAspect = false;
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
    }

    private static bool IsCardPresetArtLayer(Transform t)
    {
        if (t == null) return false;
        string n = t.name;
        return n == "Role" || n == "Card Art" || n == "Artwork";
    }

    private static Image PickFirstVisible(params Image[] images)
    {
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null) continue;
            if (!img.gameObject.activeSelf) continue;
            if (img.color.a <= 0.001f) continue;
            return img;
        }
        return null;
    }

    private static Image PickFirstNonNull(params Image[] images)
    {
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null) return images[i];
        }
        return null;
    }

    private Image FindNamedImage(string name)
    {
        Transform t = FindDeepChildByName(transform, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private const string LibraryDeckGenArtChildName = "Art";
    private const string DeckStripMtDfName = "DeckCardInfoStrip_mt_df";
    private const string DeckStripMtOiName = "DeckCardInfoStrip_mt_oi";

    /// <summary>在 <c>DeckCardInfoStrip_mt_df</c>／<c>mt_oi</c> 子階層的 <c>Art</c> 上解析縮圖用 <see cref="Image"/>。</summary>
    private Image FindDeckStripShellArtImage(string shellExactName)
    {
        Transform shell = FindDeepChildByName(transform, shellExactName);
        if (shell == null) return null;
        Transform art = shell.Find(LibraryDeckGenArtChildName);
        if (art == null) art = FindDeepChildByName(shell, LibraryDeckGenArtChildName);
        if (art == null) return null;
        Image onArt = art.GetComponent<Image>();
        if (onArt != null) return onArt;
        return art.GetComponentInChildren<Image>(true);
    }

    private static bool IsUnderDeckCardInfoStrip(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            if (p.name == "DeckCardInfoStrip") return true;
        }

        return false;
    }

    private static void ApplySpriteToDeckStripArtTarget(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        for (Transform t = target.transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            string n = t.name;
            if (n == DeckStripMtDfName || n == DeckStripMtOiName || n == "DeckCardInfoStrip")
                break;
        }

        target.sprite = sprite;
        target.color = Color.white;
        target.preserveAspect = false;
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
    }

    /// <summary>在 <c>DeckGen_Library_df</c>／<c>DeckGen_Library_oi</c> 子階層的 <c>Art</c> 上解析立繪用 <see cref="Image"/>（Art 本體或子物件皆可）。</summary>
    private Image FindDeckGenLibraryShellArtImage(string shellExactName)
    {
        Transform shell = FindDeepChildByName(transform, shellExactName);
        if (shell == null) return null;
        Transform art = shell.Find(LibraryDeckGenArtChildName);
        if (art == null) art = FindDeepChildByName(shell, LibraryDeckGenArtChildName);
        if (art == null) return null;
        Image onArt = art.GetComponent<Image>();
        if (onArt != null) return onArt;
        return art.GetComponentInChildren<Image>(true);
    }

    private static void ApplySpriteToLibraryArtTarget(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        for (Transform t = target.transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            string n = t.name;
            if (n == "DeckGen_Library_df" || n == "DeckGen_Library_oi" || n == "DeckGen_Library_ol")
                break;
        }

        target.sprite = sprite;
        target.color = Color.white;
        target.preserveAspect = false;
    }

    private static Transform FindDeepChildByName(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrEmpty(exactName)) return null;
        if (root.name == exactName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChildByName(root.GetChild(i), exactName);
            if (found != null) return found;
        }
        return null;
    }
}
