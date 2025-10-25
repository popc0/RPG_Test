using UnityEngine;
using TMPro;

public class DamageHUDFeed : MonoBehaviour
{
    [Header("HUD Εγ₯ά")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    void OnEnable()
    {
        RPG.EffectApplier.OnAnyDamaged += HandleAnyDamaged;
    }

    void OnDisable()
    {
        RPG.EffectApplier.OnAnyDamaged -= HandleAnyDamaged;
    }

    void HandleAnyDamaged(string displayName, float curHP, float maxHP, float curMP, float maxMP)
    {
        if (nameText) nameText.text = displayName;
        if (hpText) hpText.text = $"{Mathf.CeilToInt(curHP)}/{Mathf.CeilToInt(maxHP)}";
        if (mpText) mpText.text = $"{Mathf.CeilToInt(curMP)}/{Mathf.CeilToInt(maxMP)}";
    }
}
