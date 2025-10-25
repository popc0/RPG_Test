using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

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

    void Awake()
    {
        // 遊戲啟動先嘗試一次
        AutoBindPlayerStatsIfNeeded();
    }

    void OnEnable()
    {
        // 訂閱場景載入事件：切換場景後自動重新綁定
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 綁事件（若此時已有 playerStats）
        if (playerStats != null)
            playerStats.OnStatsChanged += OnStatsChanged;

        ConfigureArcsIfNeeded();
        ForceRefresh();
        ApplyImmediate();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (playerStats != null)
            playerStats.OnStatsChanged -= OnStatsChanged;
    }

    void Update()
    {
        if (!smoothLerp) return;

        if (hpArc)
            hpArc.fillAmount = Mathf.MoveTowards(hpArc.fillAmount, targetHpFill, Time.unscaledDeltaTime * lerpSpeed);

        if (mpArc)
            mpArc.fillAmount = Mathf.MoveTowards(mpArc.fillAmount, targetMpFill, Time.unscaledDeltaTime * lerpSpeed);
    }

    // 場景載入完成後（任何 LoadSceneMode），下一幀再綁一次，確保 Player 已出現在場景裡
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RebindNextFrame());
    }

    IEnumerator RebindNextFrame()
    {
        yield return null; // 等一幀讓場景物件建立完成

        var prev = playerStats;
        AutoBindPlayerStats(); // 這裡「強制」重新抓一次

        // 若對象改變，重綁事件
        if (prev != playerStats)
        {
            if (prev != null) prev.OnStatsChanged -= OnStatsChanged;
            if (playerStats != null) playerStats.OnStatsChanged += OnStatsChanged;
        }

        ForceRefresh();
        ApplyImmediate();
    }

    void OnStatsChanged(float curHP, float maxHP, float curMP, float maxMP)
    {
        UpdateTargets(curHP, maxHP, curMP, maxMP);
    }

    public void ApplyImmediate()
    {
        if (hpArc) hpArc.fillAmount = targetHpFill;
        if (mpArc) mpArc.fillAmount = targetMpFill;
    }

    public void ForceRefresh()
    {
        if (playerStats == null) return;
        UpdateTargets(playerStats.CurrentHP, playerStats.MaxHP, playerStats.CurrentMP, playerStats.MaxMP);
    }

    void UpdateTargets(float curHP, float maxHP, float curMP, float maxMP)
    {
        float hpRatio = SafeRatio(curHP, maxHP);
        float mpRatio = SafeRatio(curMP, maxMP);

        // 半圈顯示 → 0..0.5
        targetHpFill = 0.5f * hpRatio;
        targetMpFill = 0.5f * mpRatio;

        if (hpText) hpText.text = $"{Mathf.CeilToInt(curHP)}/{Mathf.CeilToInt(maxHP)}";
        if (mpText) mpText.text = $"{Mathf.CeilToInt(curMP)}/{Mathf.CeilToInt(maxMP)}";
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
            hpArc.fillOrigin = (int)Image.Origin360.Right;   // 左半
            hpArc.fillClockwise = false;                     // 上 → 下
            hpArc.fillAmount = 0f;
        }
        if (mpArc)
        {
            mpArc.type = Image.Type.Filled;
            mpArc.fillMethod = Image.FillMethod.Radial360;
            mpArc.fillOrigin = (int)Image.Origin360.Left;  // 右半
            mpArc.fillClockwise = true;                    // 上 → 下
            mpArc.fillAmount = 0f;
        }
    }

    // —— 自動綁定 —— //
    void AutoBindPlayerStatsIfNeeded()
    {
        if (playerStats == null) AutoBindPlayerStats();
    }

    void AutoBindPlayerStats()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            // 先抓本體，抓不到再往子物件找
            playerStats = playerGO.GetComponent<PlayerStats>();
            if (playerStats == null)
                playerStats = playerGO.GetComponentInChildren<PlayerStats>(true);
        }
        else
        {
            playerStats = null;
        }
    }
}
