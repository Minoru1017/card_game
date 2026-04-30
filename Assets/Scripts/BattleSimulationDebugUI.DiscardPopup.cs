using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    // Multiplayer extension reserve:
    // keep player-side discard toast scaffold ready, currently disabled in 1P UX.
    [SerializeField] private bool enablePlayerDiscardToast;
    private RectTransform enemyDiscardToastRoot;
    private CanvasGroup enemyDiscardToastCg;
    private RectTransform enemyDiscardToastCardMount;
    private TextMeshProUGUI enemyDiscardToastTitle;
    private TextMeshProUGUI enemyDiscardToastMeta;
    private TextMeshProUGUI enemyDiscardToastSkill;
    private GameObject enemyDiscardToastCardObj;
    private Coroutine enemyDiscardToastRoutine;
    private RectTransform playerDiscardToastRoot;
    private CanvasGroup playerDiscardToastCg;
    private RectTransform playerDiscardToastCardMount;
    private TextMeshProUGUI playerDiscardToastTitle;
    private TextMeshProUGUI playerDiscardToastMeta;
    private TextMeshProUGUI playerDiscardToastSkill;
    private GameObject playerDiscardToastCardObj;
    private Coroutine playerDiscardToastRoutine;

    private void OnEnemyCardDiscarded(Card card)
    {
        if (card == null || uiRoot == null) return;
        if (debugUiRoot != null && debugUiRoot.activeSelf)
        {
            if (enemyDiscardToastRoutine != null)
            {
                StopCoroutine(enemyDiscardToastRoutine);
                enemyDiscardToastRoutine = null;
            }
            if (enemyDiscardToastRoot != null)
                enemyDiscardToastRoot.gameObject.SetActive(false);
            return;
        }
        EnsureEnemyDiscardToastUi();
        if (enemyDiscardToastRoot == null) return;

        if (enemyDiscardToastCardObj != null)
        {
            Destroy(enemyDiscardToastCardObj);
            enemyDiscardToastCardObj = null;
        }

        if (battleCardPrefab != null && enemyDiscardToastCardMount != null)
        {
            enemyDiscardToastCardObj = Instantiate(battleCardPrefab, enemyDiscardToastCardMount);
            RectTransform cardRt = enemyDiscardToastCardObj.GetComponent<RectTransform>();
            if (cardRt == null) cardRt = enemyDiscardToastCardObj.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = prefabCardSize * 0.96f;
            cardRt.localScale = Vector3.one;
            CardDisplay display = enemyDiscardToastCardObj.GetComponentInChildren<CardDisplay>();
            if (display != null) display.SetCard(card);
            Button b = enemyDiscardToastCardObj.GetComponent<Button>();
            if (b != null) b.interactable = false;
        }

        if (enemyDiscardToastTitle != null)
            enemyDiscardToastTitle.text = "敵方棄牌";
        if (enemyDiscardToastMeta != null)
            enemyDiscardToastMeta.text = BuildDiscardToastMetaText(card);
        if (enemyDiscardToastSkill != null)
            enemyDiscardToastSkill.text = battleManager != null ? battleManager.GetCardSkillDescription(card) : string.Empty;

        if (enemyDiscardToastRoutine != null)
            StopCoroutine(enemyDiscardToastRoutine);
        enemyDiscardToastRoutine = StartCoroutine(CoEnemyDiscardToast());
    }

    private string BuildDiscardToastMetaText(Card card)
    {
        if (card == null) return string.Empty;
        string name = card.DebugDisplayName;
        if (card is MonsterCard m)
            return name + "\nATK " + m.attack + "  HP " + m.healthPointMax;
        if (card is SpellCard sp)
            return name + "\nSpell " + sp.SpellOrdinal;
        return name;
    }

    private void OnPlayerCardDiscarded(Card card)
    {
        if (!enablePlayerDiscardToast) return;
        if (card == null || uiRoot == null) return;
        EnsurePlayerDiscardToastUi();
        if (playerDiscardToastRoot == null) return;

        if (playerDiscardToastCardObj != null)
        {
            Destroy(playerDiscardToastCardObj);
            playerDiscardToastCardObj = null;
        }

        if (battleCardPrefab != null && playerDiscardToastCardMount != null)
        {
            playerDiscardToastCardObj = Instantiate(battleCardPrefab, playerDiscardToastCardMount);
            RectTransform cardRt = playerDiscardToastCardObj.GetComponent<RectTransform>();
            if (cardRt == null) cardRt = playerDiscardToastCardObj.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = prefabCardSize * 0.96f;
            CardDisplay display = playerDiscardToastCardObj.GetComponentInChildren<CardDisplay>();
            if (display != null) display.SetCard(card);
            Button b = playerDiscardToastCardObj.GetComponent<Button>();
            if (b != null) b.interactable = false;
        }

        if (playerDiscardToastTitle != null) playerDiscardToastTitle.text = "我方棄牌";
        if (playerDiscardToastMeta != null) playerDiscardToastMeta.text = BuildDiscardToastMetaText(card);
        if (playerDiscardToastSkill != null)
            playerDiscardToastSkill.text = battleManager != null ? battleManager.GetCardSkillDescription(card) : string.Empty;

        if (playerDiscardToastRoutine != null) StopCoroutine(playerDiscardToastRoutine);
        playerDiscardToastRoutine = StartCoroutine(CoPlayerDiscardToast());
    }

    private IEnumerator CoEnemyDiscardToast()
    {
        enemyDiscardToastRoot.gameObject.SetActive(true);
        enemyDiscardToastRoot.SetAsLastSibling();
        if (enemyDiscardToastCg != null) enemyDiscardToastCg.alpha = 0f;
        Vector3 start = Vector3.one * 0.94f;
        Vector3 end = Vector3.one;
        enemyDiscardToastRoot.localScale = start;

        float fadeIn = 0.14f;
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeIn);
            if (enemyDiscardToastCg != null) enemyDiscardToastCg.alpha = p;
            enemyDiscardToastRoot.localScale = Vector3.Lerp(start, end, p);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(2.5f);

        float fadeOut = 0.18f;
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeOut);
            if (enemyDiscardToastCg != null) enemyDiscardToastCg.alpha = 1f - p;
            enemyDiscardToastRoot.localScale = Vector3.Lerp(end, start, p);
            yield return null;
        }

        if (enemyDiscardToastRoot != null)
            enemyDiscardToastRoot.gameObject.SetActive(false);
        enemyDiscardToastRoutine = null;
    }

    private void EnsureEnemyDiscardToastUi()
    {
        if (enemyDiscardToastRoot != null || uiRoot == null) return;

        GameObject root = new GameObject("EnemyDiscardToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(uiRoot, false);
        enemyDiscardToastRoot = root.GetComponent<RectTransform>();
        enemyDiscardToastRoot.anchorMin = new Vector2(1f, 0.5f);
        enemyDiscardToastRoot.anchorMax = new Vector2(1f, 0.5f);
        enemyDiscardToastRoot.pivot = new Vector2(1f, 0.5f);
        enemyDiscardToastRoot.anchoredPosition = new Vector2(-24f, 0f);
        enemyDiscardToastRoot.sizeDelta = new Vector2(700f, 460f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.93f, 0.89f, 0.82f, 0.96f);
        enemyDiscardToastCg = root.GetComponent<CanvasGroup>();

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(root.transform, false);
        RectTransform tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.offsetMin = new Vector2(16f, -64f);
        tr.offsetMax = new Vector2(-16f, -10f);
        enemyDiscardToastTitle = titleObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) enemyDiscardToastTitle.font = sharedUIFont;
        enemyDiscardToastTitle.fontSize = 40f;
        enemyDiscardToastTitle.alignment = TextAlignmentOptions.Center;
        enemyDiscardToastTitle.color = new Color(0.31f, 0.24f, 0.18f, 1f);

        GameObject cardMountObj = new GameObject("CardMount", typeof(RectTransform));
        cardMountObj.transform.SetParent(root.transform, false);
        enemyDiscardToastCardMount = cardMountObj.GetComponent<RectTransform>();
        enemyDiscardToastCardMount.anchorMin = new Vector2(0f, 0f);
        enemyDiscardToastCardMount.anchorMax = new Vector2(0f, 1f);
        enemyDiscardToastCardMount.pivot = new Vector2(0f, 0.5f);
        enemyDiscardToastCardMount.offsetMin = new Vector2(18f, 20f);
        enemyDiscardToastCardMount.offsetMax = new Vector2(270f, -20f);

        GameObject metaObj = new GameObject("Meta", typeof(RectTransform), typeof(TextMeshProUGUI));
        metaObj.transform.SetParent(root.transform, false);
        RectTransform mr = metaObj.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(0f, 1f);
        mr.anchorMax = new Vector2(1f, 1f);
        mr.pivot = new Vector2(0f, 1f);
        mr.offsetMin = new Vector2(280f, -160f);
        mr.offsetMax = new Vector2(-16f, -64f);
        enemyDiscardToastMeta = metaObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) enemyDiscardToastMeta.font = sharedUIFont;
        enemyDiscardToastMeta.fontSize = 28f;
        enemyDiscardToastMeta.alignment = TextAlignmentOptions.TopLeft;
        enemyDiscardToastMeta.color = new Color(0.22f, 0.18f, 0.14f, 1f);

        GameObject skillObj = new GameObject("Skill", typeof(RectTransform), typeof(TextMeshProUGUI));
        skillObj.transform.SetParent(root.transform, false);
        RectTransform sr = skillObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(1f, 1f);
        sr.offsetMin = new Vector2(280f, 20f);
        sr.offsetMax = new Vector2(-16f, -162f);
        enemyDiscardToastSkill = skillObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) enemyDiscardToastSkill.font = sharedUIFont;
        enemyDiscardToastSkill.fontSize = 24f;
        enemyDiscardToastSkill.alignment = TextAlignmentOptions.TopLeft;
        enemyDiscardToastSkill.enableWordWrapping = true;
        enemyDiscardToastSkill.color = new Color(0.24f, 0.2f, 0.16f, 1f);

        root.SetActive(false);
    }

    private IEnumerator CoPlayerDiscardToast()
    {
        playerDiscardToastRoot.gameObject.SetActive(true);
        playerDiscardToastRoot.SetAsLastSibling();
        if (playerDiscardToastCg != null) playerDiscardToastCg.alpha = 0f;
        Vector3 start = Vector3.one * 0.94f;
        Vector3 end = Vector3.one;
        playerDiscardToastRoot.localScale = start;

        float fadeIn = 0.14f;
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeIn);
            if (playerDiscardToastCg != null) playerDiscardToastCg.alpha = p;
            playerDiscardToastRoot.localScale = Vector3.Lerp(start, end, p);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(2.5f);

        float fadeOut = 0.18f;
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeOut);
            if (playerDiscardToastCg != null) playerDiscardToastCg.alpha = 1f - p;
            playerDiscardToastRoot.localScale = Vector3.Lerp(end, start, p);
            yield return null;
        }

        if (playerDiscardToastRoot != null) playerDiscardToastRoot.gameObject.SetActive(false);
        playerDiscardToastRoutine = null;
    }

    private void EnsurePlayerDiscardToastUi()
    {
        if (playerDiscardToastRoot != null || uiRoot == null) return;

        GameObject root = new GameObject("PlayerDiscardToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(uiRoot, false);
        playerDiscardToastRoot = root.GetComponent<RectTransform>();
        playerDiscardToastRoot.anchorMin = new Vector2(0f, 0.5f);
        playerDiscardToastRoot.anchorMax = new Vector2(0f, 0.5f);
        playerDiscardToastRoot.pivot = new Vector2(0f, 0.5f);
        playerDiscardToastRoot.anchoredPosition = new Vector2(320f, 0f);
        playerDiscardToastRoot.sizeDelta = new Vector2(700f, 460f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.93f, 0.89f, 0.82f, 0.96f);
        playerDiscardToastCg = root.GetComponent<CanvasGroup>();

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(root.transform, false);
        RectTransform tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.offsetMin = new Vector2(16f, -64f);
        tr.offsetMax = new Vector2(-16f, -10f);
        playerDiscardToastTitle = titleObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) playerDiscardToastTitle.font = sharedUIFont;
        playerDiscardToastTitle.fontSize = 40f;
        playerDiscardToastTitle.alignment = TextAlignmentOptions.Center;
        playerDiscardToastTitle.color = new Color(0.31f, 0.24f, 0.18f, 1f);

        GameObject cardMountObj = new GameObject("CardMount", typeof(RectTransform));
        cardMountObj.transform.SetParent(root.transform, false);
        playerDiscardToastCardMount = cardMountObj.GetComponent<RectTransform>();
        playerDiscardToastCardMount.anchorMin = new Vector2(0f, 0f);
        playerDiscardToastCardMount.anchorMax = new Vector2(0f, 1f);
        playerDiscardToastCardMount.pivot = new Vector2(0f, 0.5f);
        playerDiscardToastCardMount.offsetMin = new Vector2(18f, 20f);
        playerDiscardToastCardMount.offsetMax = new Vector2(270f, -20f);

        GameObject metaObj = new GameObject("Meta", typeof(RectTransform), typeof(TextMeshProUGUI));
        metaObj.transform.SetParent(root.transform, false);
        RectTransform mr = metaObj.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(0f, 1f);
        mr.anchorMax = new Vector2(1f, 1f);
        mr.pivot = new Vector2(0f, 1f);
        mr.offsetMin = new Vector2(280f, -160f);
        mr.offsetMax = new Vector2(-16f, -64f);
        playerDiscardToastMeta = metaObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) playerDiscardToastMeta.font = sharedUIFont;
        playerDiscardToastMeta.fontSize = 28f;
        playerDiscardToastMeta.alignment = TextAlignmentOptions.TopLeft;
        playerDiscardToastMeta.color = new Color(0.22f, 0.18f, 0.14f, 1f);

        GameObject skillObj = new GameObject("Skill", typeof(RectTransform), typeof(TextMeshProUGUI));
        skillObj.transform.SetParent(root.transform, false);
        RectTransform sr = skillObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(1f, 1f);
        sr.offsetMin = new Vector2(280f, 20f);
        sr.offsetMax = new Vector2(-16f, -162f);
        playerDiscardToastSkill = skillObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) playerDiscardToastSkill.font = sharedUIFont;
        playerDiscardToastSkill.fontSize = 24f;
        playerDiscardToastSkill.alignment = TextAlignmentOptions.TopLeft;
        playerDiscardToastSkill.enableWordWrapping = true;
        playerDiscardToastSkill.color = new Color(0.24f, 0.2f, 0.16f, 1f);

        root.SetActive(false);
    }
}
