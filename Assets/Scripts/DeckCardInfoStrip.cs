using TMPro;
using UnityEngine;

/// <summary>
/// 牌組列表用：名稱與攻防文字區（可獨立為 prefab，由 <see cref="CardDisplay"/> 綁定）。
/// </summary>
public class DeckCardInfoStrip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI healthText;

    public TextMeshProUGUI NameText => nameText;
    public TextMeshProUGUI AttackText => attackText;
    public TextMeshProUGUI HealthText => healthText;

    public void AssignReferences(TextMeshProUGUI name, TextMeshProUGUI attack, TextMeshProUGUI health)
    {
        nameText = name;
        attackText = attack;
        healthText = health;
    }

    private void Awake()
    {
        ResolveByChildNamesIfNeeded();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveByChildNamesIfNeeded();
    }
#endif

    private void ResolveByChildNamesIfNeeded()
    {
        if (nameText == null)
        {
            Transform t = transform.Find("name");
            if (t != null) nameText = t.GetComponent<TextMeshProUGUI>();
        }

        if (attackText == null)
        {
            Transform t = transform.Find("atk");
            if (t != null) attackText = t.GetComponent<TextMeshProUGUI>();
        }

        if (healthText == null)
        {
            Transform t = transform.Find("dtf");
            if (t != null) healthText = t.GetComponent<TextMeshProUGUI>();
        }

        Transform mtDf = transform.Find("DeckCardInfoStrip_mt_df");
        if (mtDf == null) return;

        if (nameText == null)
        {
            Transform t = mtDf.Find("card name");
            if (t != null) nameText = t.GetComponent<TextMeshProUGUI>();
        }

        if (attackText == null)
        {
            Transform t = mtDf.Find("ATK");
            if (t != null) attackText = t.GetComponent<TextMeshProUGUI>();
        }

        if (healthText == null)
        {
            Transform t = mtDf.Find("HP");
            if (t != null) healthText = t.GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>將此條上的 TMP 指到 <see cref="CardDisplay"/>（不覆寫其他欄位）。</summary>
    public void BindToCardDisplay(CardDisplay display)
    {
        if (display == null) return;
        ResolveByChildNamesIfNeeded();
        display.nameText = nameText;
        display.attackText = attackText;
        display.healthText = healthText;
    }
}
