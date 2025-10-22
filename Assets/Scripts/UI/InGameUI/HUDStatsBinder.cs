using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDStatsBinder : MonoBehaviour
{
    [Header("來源（狀態層）")]
    public PlayerStats playerStats;

    [Header("UI 參考（兩個圓形 Image 疊在一起）")]
    public Image hpArc;   // 紅色半圓（左）
    public Image mpArc;   // 藍色半圓（右）
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    [Header("顯示選項")]
    public bool autoConfigureArc = true;
    public bool smoothLerp = true;
    public float lerpSpeed = 12f;

    float targetHpFill, targetMpFill;

    void OnEnable()
    {
        if (playerStats != null)
            playerStats.OnStatsChanged += RefreshTargets;

        ConfigureArcsIfNeeded();
        RefreshTargets();
        ApplyImmediate();
    }

    void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnStatsChanged -= RefreshTargets;
    }

    void Update()
    {
        if (!smoothLerp) return;

        if (hpArc)
            hpArc.fillAmount = Mathf.MoveTowards(hpArc.fillAmount, targetHpFill, Time.unscaledDeltaTime * lerpSpeed);

        if (mpArc)
            mpArc.fillAmount = Mathf.MoveTowards(mpArc.fillAmount, targetMpFill, Time.unscaledDeltaTime * lerpSpeed);
    }

    public void ApplyImmediate()
    {
        if (hpArc) hpArc.fillAmount = targetHpFill;
        if (mpArc) mpArc.fillAmount = targetMpFill;
    }

    public void RefreshTargets()
    {
        if (playerStats == null) return;

        float hpRatio = SafeRatio(playerStats.CurrentHP, playerStats.MaxHP);
        float mpRatio = SafeRatio(playerStats.CurrentMP, playerStats.MaxMP);

        // 半圈顯示 → 0..0.5
        targetHpFill = 0.5f * hpRatio;
        targetMpFill = 0.5f * mpRatio;

        if (hpText) hpText.text = $"{Mathf.CeilToInt(playerStats.CurrentHP)}/{Mathf.CeilToInt(playerStats.MaxHP)}";
        if (mpText) mpText.text = $"{Mathf.CeilToInt(playerStats.CurrentMP)}/{Mathf.CeilToInt(playerStats.MaxMP)}";
    }

    float SafeRatio(float v, float max)
    {
        if (max <= 0f) return 0f;
        return Mathf.Clamp01(v / max);
    }

    void ConfigureArcsIfNeeded()
    {
        if (!autoConfigureArc) return;

        // 兩個半圓都「由上往下」收
        if (hpArc)
        {
            hpArc.type = Image.Type.Filled;
            hpArc.fillMethod = Image.FillMethod.Radial360;
            hpArc.fillOrigin = (int)Image.Origin360.Left;   // 左半
            hpArc.fillClockwise = true;                     //  上 → 下
            hpArc.fillAmount = 0f;
        }
        if (mpArc)
        {
            mpArc.type = Image.Type.Filled;
            mpArc.fillMethod = Image.FillMethod.Radial360;
            mpArc.fillOrigin = (int)Image.Origin360.Right;  // 右半
            mpArc.fillClockwise = false;                    //  上 → 下
            mpArc.fillAmount = 0f;
        }
    }
}
