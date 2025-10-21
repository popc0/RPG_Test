using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDStatsBinder : MonoBehaviour
{
    public PlayerStats playerStats;
    public Slider hpBar;
    public Slider mpBar;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    void OnEnable()
    {
        if (playerStats != null)
            playerStats.OnStatsChanged += UpdateUI;
        UpdateUI();
    }

    void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnStatsChanged -= UpdateUI;
    }

    void UpdateUI()
    {
        if (playerStats == null) return;

        if (hpBar)
        {
            hpBar.maxValue = playerStats.MaxHP;
            hpBar.value = playerStats.CurrentHP;
        }
        if (mpBar)
        {
            mpBar.maxValue = playerStats.MaxMP;
            mpBar.value = playerStats.CurrentMP;
        }
        if (hpText) hpText.text = $"{Mathf.CeilToInt(playerStats.CurrentHP)}/{Mathf.CeilToInt(playerStats.MaxHP)}";
        if (mpText) mpText.text = $"{Mathf.CeilToInt(playerStats.CurrentMP)}/{Mathf.CeilToInt(playerStats.MaxMP)}";
    }
}
