using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    private int ComputeFieldSignature()
    {
        if (battleManager == null) return 0;
        Card p = battleManager.GetPlayerFieldCard();
        Card e = battleManager.GetEnemyFieldCard();
        Card ps = battleManager.GetPlayerFieldSpellCard();
        Card es = battleManager.GetEnemyFieldSpellCard();
        return System.HashCode.Combine(
            FieldMonsterFingerprint(p),
            FieldMonsterFingerprint(e),
            FieldSpellFingerprint(ps),
            FieldEnemySpellFingerprint(es));
    }

    private static int FieldMonsterFingerprint(Card c)
    {
        if (c == null) return 0;
        if (c is MonsterCard m)
            return System.HashCode.Combine(m.id, m.healthPoint, m.healthPointMax, m.attack);
        return System.HashCode.Combine(c.id, -1, -1, -1);
    }

    private int FieldSpellFingerprint(Card ps)
    {
        if (ps == null) return 0;
        int rounds = battleManager != null && battleManager.PlayerLinGazeActive()
            ? battleManager.GetPlayerLinGazeRoundsRemaining()
            : 0;
        int ord = ps is SpellCard sp ? sp.SpellOrdinal : 0;
        return System.HashCode.Combine(ps.id, ord, rounds);
    }

    private int FieldEnemySpellFingerprint(Card es)
    {
        if (es == null) return 0;
        int rounds = battleManager != null && battleManager.EnemyLinGazeActive()
            ? battleManager.GetEnemyLinGazeRoundsRemaining()
            : 0;
        int ord = es is SpellCard sp ? sp.SpellOrdinal : 0;
        return System.HashCode.Combine(es.id, ord, rounds);
    }
    /// <summary>僅更新既有場上卡的 sizeDelta／localScale（Inspector 調場上大小滑塊時）。</summary>
    private void RefreshFieldCardLayoutsOnly()
    {
        ApplyLayoutToExistingFieldCard(playerFieldCardObj, false, false);
        ApplyLayoutToExistingFieldCard(playerSpellFieldCardObj, false, true);
        if (!deferEnemyFieldRefresh)
        {
            ApplyLayoutToExistingFieldCard(enemyFieldCardObj, true, false);
        }
        ApplyLayoutToExistingFieldCard(enemySpellFieldCardObj, true, true);
    }

    private void RefreshFieldCardVisualTuningOnly()
    {
        ApplyVisualTuningToFieldHolder(playerFieldCardObj, false, false);
        ApplyVisualTuningToFieldHolder(playerSpellFieldCardObj, false, true);
        if (!deferEnemyFieldRefresh)
        {
            ApplyVisualTuningToFieldHolder(enemyFieldCardObj, true, false);
        }
        ApplyVisualTuningToFieldHolder(enemySpellFieldCardObj, true, true);
    }

    private void ApplyLayoutToExistingFieldCard(GameObject holder, bool enemy, bool isSpell)
    {
        if (holder == null) return;
        RectTransform rect = holder.GetComponent<RectTransform>();
        if (rect != null) ApplyBattleFieldCardRectLayout(rect, enemy, isSpell);
    }

    private void ApplyVisualTuningToFieldHolder(GameObject holder, bool enemy, bool isSpell)
    {
        if (holder == null) return;
        CardDisplay display = holder.GetComponentInChildren<CardDisplay>();
        if (display == null) return;
        ApplyPrefabVisualTuning(display, true, isSpell);
        display.RefreshCardArtRarityOverlayExternal();
        if (display.card != null) ApplyFieldDamageHealthColor(display, display.card);
    }

    private void RefreshFieldCards()
    {
        Card playerCard = battleManager.GetPlayerFieldCard();
        Card enemyCard = battleManager.GetEnemyFieldCard();
        Card playerSpell = battleManager.GetPlayerFieldSpellCard();
        Card enemySpell = battleManager.GetEnemyFieldSpellCard();

        bool playerExists = playerCard != null;
        bool enemyExists = enemyCard != null;
        bool playerSpellExists = playerSpell != null;
        bool enemySpellExists = enemySpell != null;

        if (!holdPlayerFieldCardUntilFireballHit)
        {
            RebuildSingleFieldCard(playerFieldArea, ref playerFieldCardObj, playerCard, false, lastPlayerFieldExists, playerExists);
            lastPlayerFieldExists = playerExists;
        }

        if (playerSpellFieldArea != null)
        {
            RebuildSingleFieldCard(playerSpellFieldArea, ref playerSpellFieldCardObj, playerSpell, false, lastPlayerSpellFieldExists, playerSpellExists);
            lastPlayerSpellFieldExists = playerSpellExists;
        }
        if (!deferEnemyFieldRefresh && !holdEnemyFieldCardUntilFireballHit)
        {
            RebuildSingleFieldCard(enemyFieldArea, ref enemyFieldCardObj, enemyCard, true, lastEnemyFieldExists, enemyExists);
            lastEnemyFieldExists = enemyExists;
        }
        if (enemySpellFieldArea != null)
        {
            RebuildSingleFieldCard(enemySpellFieldArea, ref enemySpellFieldCardObj, enemySpell, true, lastEnemySpellFieldExists, enemySpellExists);
            lastEnemySpellFieldExists = enemySpellExists;
        }
    }

    private void RebuildSingleFieldCard(RectTransform area, ref GameObject holder, Card card, bool enemy, bool existedBefore, bool existsNow)
    {
        if (area == null) return;
        GameObject fieldPrefab = (battleManager != null && battleManager.fieldMonsterPrefab != null)
            ? battleManager.fieldMonsterPrefab
            : battleCardPrefab;
        if (fieldPrefab == null) return;

        if (!existsNow)
        {
            if (holder != null)
            {
                StartCoroutine(AnimateDeathAndDestroy(holder));
                holder = null;
            }
            return;
        }

        if (holder != null) Destroy(holder);

        GameObject cardObj = Instantiate(fieldPrefab, area);
        cardObj.name = enemy ? "EnemyFieldCard" : "PlayerFieldCard";
        cardObj.transform.SetAsLastSibling();
        RectTransform rect = cardObj.GetComponent<RectTransform>();
        if (rect == null) rect = cardObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        bool isSpell = card is SpellCard;
        ApplyBattleFieldCardRectLayout(rect, enemy, isSpell);

        CardDisplay display = cardObj.GetComponentInChildren<CardDisplay>();
        if (display != null)
        {
            display.SetCard(card);
            if (isSpell && display.effectText != null)
            {
                display.effectText.gameObject.SetActive(false);
            }
            ApplyPrefabVisualTuning(display, true, isSpell);
            display.RefreshCardArtRarityOverlayExternal();
            ApplyFieldDamageHealthColor(display, card);
        }

        Button b = cardObj.GetComponent<Button>();
        if (b != null) b.interactable = false;
        holder = cardObj;

        if (card is SpellCard spField && spField.SpellOrdinal == 2)
        {
            AttachLinGazeShieldFieldVisual(cardObj);
        }
        else if (card is MonsterCard)
        {
            AttachFieldSelectHalo(cardObj);
            ApplyFieldRestrictionBadge(cardObj, enemy
                ? battleManager.GetEnemyFieldMonsterStatusBadge()
                : battleManager.GetPlayerFieldMonsterStatusBadge());
        }

        if (!existedBefore)
        {
            StartCoroutine(AnimateSpawn(cardObj, enemy, isSpell));
        }
    }

    /// <summary>林可的凝視置於我方咒術區時：防護罩式脈衝，表示敵方攻擊無效。</summary>
    private void AttachLinGazeShieldFieldVisual(GameObject fieldCardRoot)
    {
        if (fieldCardRoot == null) return;

        Transform existing = fieldCardRoot.transform.Find("LinGazeShieldRoot");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject shieldRoot = new GameObject("LinGazeShieldRoot", typeof(RectTransform));
        shieldRoot.transform.SetParent(fieldCardRoot.transform, false);
        RectTransform rootRt = shieldRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        rootRt.localScale = Vector3.one;
        shieldRoot.transform.SetAsLastSibling();

        GameObject goHalo = new GameObject("ShieldHalo", typeof(RectTransform), typeof(Image));
        goHalo.transform.SetParent(shieldRoot.transform, false);
        RectTransform rh = goHalo.GetComponent<RectTransform>();
        rh.anchorMin = new Vector2(0.5f, 0.5f);
        rh.anchorMax = new Vector2(0.5f, 0.5f);
        rh.pivot = new Vector2(0.5f, 0.5f);
        rh.sizeDelta = new Vector2(200f, 200f);
        rh.anchoredPosition = Vector2.zero;
        Image imgHalo = goHalo.GetComponent<Image>();
        imgHalo.sprite = GetUnitWhiteSprite();
        imgHalo.type = Image.Type.Simple;
        imgHalo.raycastTarget = false;
        imgHalo.color = BattleFxColors.FieldHaloCore;
        CanvasGroup cgHalo = goHalo.AddComponent<CanvasGroup>();

        GameObject goInner = new GameObject("ShieldRingInner", typeof(RectTransform), typeof(Image));
        goInner.transform.SetParent(shieldRoot.transform, false);
        RectTransform ri = goInner.GetComponent<RectTransform>();
        ri.anchorMin = Vector2.zero;
        ri.anchorMax = Vector2.one;
        ri.offsetMin = new Vector2(-12f, -12f);
        ri.offsetMax = new Vector2(12f, 12f);
        ri.localScale = Vector3.one;
        Image imgInner = goInner.GetComponent<Image>();
        imgInner.sprite = GetUnitWhiteSprite();
        imgInner.type = Image.Type.Simple;
        imgInner.raycastTarget = false;
        imgInner.color = BattleFxColors.FieldHaloInner;
        CanvasGroup cgInner = goInner.AddComponent<CanvasGroup>();

        GameObject goOuter = new GameObject("ShieldRingOuter", typeof(RectTransform), typeof(Image));
        goOuter.transform.SetParent(shieldRoot.transform, false);
        RectTransform ro = goOuter.GetComponent<RectTransform>();
        ro.anchorMin = Vector2.zero;
        ro.anchorMax = Vector2.one;
        ro.offsetMin = new Vector2(-26f, -26f);
        ro.offsetMax = new Vector2(26f, 26f);
        ro.localScale = Vector3.one;
        Image imgOuter = goOuter.GetComponent<Image>();
        imgOuter.sprite = GetUnitWhiteSprite();
        imgOuter.type = Image.Type.Simple;
        imgOuter.raycastTarget = false;
        imgOuter.color = BattleFxColors.FieldHaloOuter;
        CanvasGroup cgOuter = goOuter.AddComponent<CanvasGroup>();

        LinGazeShieldFieldVisual driver = shieldRoot.AddComponent<LinGazeShieldFieldVisual>();
        driver.Initialize(cgOuter, cgInner, cgHalo, rootRt, goOuter.transform);
    }

    private sealed class LinGazeShieldFieldVisual : MonoBehaviour
    {
        private CanvasGroup outerCg;
        private CanvasGroup innerCg;
        private CanvasGroup haloCg;
        private RectTransform pulseRt;
        private Transform rotateTf;
        private float enabledUnscaledTime;

        public void Initialize(
            CanvasGroup outer,
            CanvasGroup inner,
            CanvasGroup halo,
            RectTransform pulse,
            Transform rotateOuter)
        {
            outerCg = outer;
            innerCg = inner;
            haloCg = halo;
            pulseRt = pulse;
            rotateTf = rotateOuter;
            enabledUnscaledTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (pulseRt == null) return;
            float t = Time.unscaledTime;
            float fadeIn = Mathf.Clamp01((t - enabledUnscaledTime) / 0.42f);

            if (rotateTf != null)
                rotateTf.Rotate(0f, 0f, 16f * Time.unscaledDeltaTime);

            float phase = Mathf.Sin(t * 2.35f) * 0.5f + 0.5f;
            if (outerCg != null)
                outerCg.alpha = Mathf.Lerp(0.16f, 0.44f, phase) * fadeIn;
            if (innerCg != null)
                innerCg.alpha = Mathf.Lerp(0.1f, 0.36f, 1f - phase) * fadeIn;
            if (haloCg != null)
                haloCg.alpha = (0.05f + 0.06f * Mathf.Sin(t * 3.8f)) * fadeIn;

            float s = 1f + 0.032f * Mathf.Sin(t * 3.45f);
            pulseRt.localScale = new Vector3(s, s, 1f);
        }
    }

    private void ApplyFieldDamageHealthColor(CardDisplay display, Card card)
    {
        if (display == null || display.healthText == null) return;

        MonsterCard monster = card as MonsterCard;
        if (monster == null)
        {
            display.healthText.color = Color.white;
            return;
        }

        if (monster.healthPoint > monster.healthPointMax)
        {
            display.ShowCard();
            return;
        }

        display.healthText.richText = false;
        if (display.healthText.text != monster.healthPoint.ToString())
        {
            display.healthText.text = monster.healthPoint.ToString();
        }

        bool isDamaged = monster.healthPoint < monster.healthPointMax;
        display.healthText.color = isDamaged
            ? BattleFxColors.FieldHpHurt
            : Color.white;
    }

    /// <summary>場上怪獸常駐選取光暈（§3.4 FIELD_HALO_*）。</summary>
    private void AttachFieldSelectHalo(GameObject fieldCardRoot)
    {
        if (fieldCardRoot == null) return;

        Transform existing = fieldCardRoot.transform.Find("FieldSelectHaloRoot");
        if (existing != null) return;

        GameObject haloRoot = new GameObject("FieldSelectHaloRoot", typeof(RectTransform));
        haloRoot.transform.SetParent(fieldCardRoot.transform, false);
        RectTransform rootRt = haloRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        rootRt.localScale = Vector3.one;
        haloRoot.transform.SetAsFirstSibling();

        CreateFieldHaloImage(haloRoot.transform, "FieldHaloOuter", Vector2.zero, Vector2.one,
            new Vector2(-22f, -22f), new Vector2(22f, 22f), BattleFxColors.FieldHaloOuter);
        CreateFieldHaloImage(haloRoot.transform, "FieldHaloInner", Vector2.zero, Vector2.one,
            new Vector2(-12f, -12f), new Vector2(12f, 12f), BattleFxColors.FieldHaloInner);
        CreateFieldHaloImage(haloRoot.transform, "FieldHaloCore", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, BattleFxColors.FieldHaloCore, new Vector2(0.5f, 0.5f), new Vector2(168f, 168f));
        haloRoot.SetActive(false);
    }

    private void CreateFieldHaloImage(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color,
        Vector2? pivot = null,
        Vector2? sizeDelta = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        if (sizeDelta.HasValue)
        {
            rt.sizeDelta = sizeDelta.Value;
            rt.anchoredPosition = Vector2.zero;
        }
        else
        {
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        Image img = go.GetComponent<Image>();
        img.sprite = GetUnitWhiteSprite();
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        img.color = color;
    }

    private void UpdateFieldSelectHaloVisibility()
    {
        if (battleManager == null) return;
        bool playerTurn = battleManager.IsPlayerTurn();
        SetFieldSelectHaloVisible(playerFieldCardObj, playerTurn && battleManager.PlayerHasFieldMonster());
        SetFieldSelectHaloVisible(enemyFieldCardObj,
            playerTurn && battleManager.PlayerHasFieldMonster() && battleManager.EnemyHasFieldMonster());
    }

    private void UpdateFieldRestrictionBadges()
    {
        if (battleManager == null) return;
        ApplyFieldRestrictionBadge(playerFieldCardObj, battleManager.GetPlayerFieldMonsterStatusBadge());
        ApplyFieldRestrictionBadge(enemyFieldCardObj, battleManager.GetEnemyFieldMonsterStatusBadge());
    }

    private void ApplyFieldRestrictionBadge(GameObject fieldRoot, BattleSimulationManager.FieldMonsterStatusBadge badge)
    {
        if (fieldRoot == null) return;

        Transform existing = fieldRoot.transform.Find(FieldCardStatusIndex.UiFieldRestrictionBadge);
        if (!badge.HasValue)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        bool isCounterBlock = badge.Primary == FieldCardStatusIndex.BadgeCannotCounter;
        Color border = isCounterBlock
            ? BattleFxColors.RestrictionBadgeBorderCounter
            : BattleFxColors.RestrictionBadgeBorderAttack;

        GameObject badgeRoot;
        TextMeshProUGUI primaryTmp;
        TextMeshProUGUI secondaryTmp;
        RectTransform badgeRt;
        if (existing == null)
        {
            badgeRoot = new GameObject(FieldCardStatusIndex.UiFieldRestrictionBadge, typeof(RectTransform), typeof(CanvasGroup));
            badgeRoot.transform.SetParent(fieldRoot.transform, false);
            badgeRoot.transform.SetAsLastSibling();
            badgeRt = badgeRoot.GetComponent<RectTransform>();
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = Vector2.zero;
            badgeRt.sizeDelta = new Vector2(152f, 72f);

            GameObject shadowObj = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            shadowObj.transform.SetParent(badgeRoot.transform, false);
            RectTransform shadowRt = shadowObj.GetComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(5f, -5f);
            shadowRt.offsetMax = new Vector2(9f, -1f);
            Image shadowImg = shadowObj.GetComponent<Image>();
            shadowImg.sprite = GetUnitWhiteSprite();
            shadowImg.color = BattleFxColors.RestrictionBadgeShadow;
            shadowImg.raycastTarget = false;

            GameObject glowObj = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(badgeRoot.transform, false);
            RectTransform glowRt = glowObj.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.offsetMin = new Vector2(-10f, -8f);
            glowRt.offsetMax = new Vector2(10f, 8f);
            Image glowImg = glowObj.GetComponent<Image>();
            glowImg.sprite = GetUnitWhiteSprite();
            glowImg.color = BattleFxColors.RestrictionBadgeGlow;
            glowImg.raycastTarget = false;

            GameObject bgObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(badgeRoot.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = GetUnitWhiteSprite();
            bgImg.color = BattleFxColors.RestrictionBadgeBg;
            bgImg.raycastTarget = false;

            GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(badgeRoot.transform, false);
            borderObj.name = "Border";
            borderObj.transform.SetSiblingIndex(3);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-3f, -3f);
            borderRt.offsetMax = new Vector2(3f, 3f);
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.sprite = GetUnitWhiteSprite();
            borderImg.raycastTarget = false;

            GameObject primaryObj = new GameObject("Primary", typeof(RectTransform), typeof(TextMeshProUGUI));
            primaryObj.transform.SetParent(badgeRoot.transform, false);
            RectTransform primaryRt = primaryObj.GetComponent<RectTransform>();
            primaryRt.anchorMin = new Vector2(0f, 0.48f);
            primaryRt.anchorMax = new Vector2(1f, 1f);
            primaryRt.offsetMin = new Vector2(6f, 0f);
            primaryRt.offsetMax = new Vector2(-6f, -4f);
            primaryTmp = primaryObj.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset font = ResolveUIFont();
            if (font != null) primaryTmp.font = font;
            primaryTmp.fontSize = 22f;
            primaryTmp.fontStyle = FontStyles.Bold;
            primaryTmp.alignment = TextAlignmentOptions.Center;
            primaryTmp.color = BattleFxColors.RestrictionBadgePrimary;
            primaryTmp.outlineWidth = 0.2f;
            primaryTmp.outlineColor = new Color32(0, 0, 0, 210);
            primaryTmp.raycastTarget = false;

            GameObject secondaryObj = new GameObject("Secondary", typeof(RectTransform), typeof(TextMeshProUGUI));
            secondaryObj.transform.SetParent(badgeRoot.transform, false);
            RectTransform secondaryRt = secondaryObj.GetComponent<RectTransform>();
            secondaryRt.anchorMin = new Vector2(0f, 0f);
            secondaryRt.anchorMax = new Vector2(1f, 0.5f);
            secondaryRt.offsetMin = new Vector2(6f, 4f);
            secondaryRt.offsetMax = new Vector2(-6f, 0f);
            secondaryTmp = secondaryObj.GetComponent<TextMeshProUGUI>();
            if (font != null) secondaryTmp.font = font;
            secondaryTmp.fontSize = 19f;
            secondaryTmp.fontStyle = FontStyles.Bold;
            secondaryTmp.alignment = TextAlignmentOptions.Center;
            secondaryTmp.color = BattleFxColors.RestrictionBadgeSecondary;
            secondaryTmp.outlineWidth = 0.16f;
            secondaryTmp.outlineColor = new Color32(0, 0, 0, 180);
            secondaryTmp.raycastTarget = false;

            FieldRestrictionBadgePulse pulse = badgeRoot.AddComponent<FieldRestrictionBadgePulse>();
            pulse.Bind(badgeRt);
        }
        else
        {
            badgeRoot = existing.gameObject;
            badgeRt = badgeRoot.GetComponent<RectTransform>();
            primaryTmp = badgeRoot.transform.Find("Primary")?.GetComponent<TextMeshProUGUI>();
            secondaryTmp = badgeRoot.transform.Find("Secondary")?.GetComponent<TextMeshProUGUI>();
            Transform borderT = badgeRoot.transform.Find("Border");
            if (borderT != null)
            {
                Image borderImg = borderT.GetComponent<Image>();
                if (borderImg != null) borderImg.color = border;
            }
        }

        if (badgeRoot == null || primaryTmp == null || secondaryTmp == null) return;

        badgeRoot.SetActive(true);
        badgeRoot.transform.SetAsLastSibling();
        primaryTmp.text = badge.Primary;
        secondaryTmp.text = string.IsNullOrEmpty(badge.Secondary) ? string.Empty : badge.Secondary;
        secondaryTmp.gameObject.SetActive(!string.IsNullOrEmpty(badge.Secondary));

        Transform borderTransform = badgeRoot.transform.Find("Border");
        if (borderTransform != null)
        {
            Image borderImg = borderTransform.GetComponent<Image>();
            if (borderImg != null) borderImg.color = border;
        }
    }

    private sealed class FieldRestrictionBadgePulse : MonoBehaviour
    {
        private RectTransform targetRt;

        public void Bind(RectTransform rt)
        {
            targetRt = rt;
        }

        private void Update()
        {
            if (targetRt == null) return;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4.2f) * 0.035f;
            targetRt.localScale = Vector3.one * pulse;
        }
    }

    private static void SetFieldSelectHaloVisible(GameObject fieldRoot, bool visible)
    {
        if (fieldRoot == null) return;
        Transform halo = fieldRoot.transform.Find("FieldSelectHaloRoot");
        if (halo != null)
            halo.gameObject.SetActive(visible);
    }

    private IEnumerator AnimateSpawn(GameObject obj, bool enemy, bool isSpell)
    {
        if (obj == null) yield break;
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) yield break;

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();

        Vector3 endScale = Vector3.one * GetBattleFieldUniformScale(enemy, isSpell);
        Vector3 startScale = endScale * 0.72f;
        Vector2 endPos = Vector2.zero;
        Vector2 startPos = endPos;
        if (enemy)
        {
            startPos = new Vector2(0f, 80f);
            if (enemyHandArea != null)
            {
                RectTransform parentRt = rt.parent as RectTransform;
                Vector2 projected;
                if (parentRt != null &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentRt,
                        RectTransformUtility.WorldToScreenPoint(null, enemyHandArea.position),
                        null,
                        out projected))
                {
                    startPos = projected;
                }
            }
        }
        rt.anchoredPosition = startPos;
        rt.localScale = startScale;
        cg.alpha = 0f;

        float t = 0f;
        const float dur = 0.2f;
        while (t < dur && obj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            rt.localScale = Vector3.Lerp(startScale, endScale, p);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            cg.alpha = p;
            yield return null;
        }

        if (obj != null)
        {
            rt.anchoredPosition = endPos;
            rt.localScale = endScale;
            cg.alpha = 1f;
        }
    }

    private IEnumerator AnimateDeathAndDestroy(GameObject obj)
    {
        if (obj == null) yield break;
        RectTransform rt = obj.GetComponent<RectTransform>();
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();

        Vector3 startScale = rt != null ? rt.localScale : Vector3.one;
        Vector3 endScale = startScale * 0.72f;
        float startAlpha = cg.alpha <= 0f ? 1f : cg.alpha;

        float t = 0f;
        const float dur = 0.16f;
        while (t < dur && obj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            if (rt != null) rt.localScale = Vector3.Lerp(startScale, endScale, p);
            cg.alpha = Mathf.Lerp(startAlpha, 0f, p);
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }
    private void OnBattleLayoutVisualRefreshRequested()
    {
        lastFieldSignature = int.MinValue;
        nextRefreshTime = 0f;
        if (handArea == null || battleManager == null) return;
        if (battleManager.PeekDeferEnemyFieldUiClearAfterPlayerFireballKill())
        {
            holdEnemyFieldCardUntilFireballHit = true;
            battleManager.ClearDeferEnemyFieldUiClearAfterPlayerFireballKill();
        }
        if (battleManager.PeekDeferPlayerFieldUiClearAfterEnemyFireballKill())
        {
            holdPlayerFieldCardUntilFireballHit = true;
            battleManager.ClearDeferPlayerFieldUiClearAfterEnemyFireballKill();
        }
        if (deferFieldRefreshDuringAttack)
        {
            pendingFieldRefreshAfterAttack = true;
            return;
        }
        RefreshFieldCards();
        lastFieldSignature = ComputeFieldSignature();
    }

}
