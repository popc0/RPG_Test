using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class HUDStatsBinder : MonoBehaviour
{
    [Header("來源（自動抓取）")]
    public PlayerStats playerStats;
    public PlayerLevel playerLevel; // [新增] 參照等級系統

    [Header("UI 參考：HP / MP")]
    public Image hpArc;   // 紅色半圓
    public Image mpArc;   // 藍色半圓
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    [Header("UI 參考：Level / EXP (新增)")]
    public TextMeshProUGUI levelText; // [新增] 顯示等級文字 (例如 "LV.5")
    public Image expBar;              // [新增] 經驗值條 (需設為 Filled)

    [Header("顯示選項")]
    public bool autoConfigureArc = true;
    public bool smoothLerp = true;
    public float lerpSpeed = 12f;

    // 用於平滑動畫的目標值
    float targetHpFill, targetMpFill;
    float targetExpFill; // [新增]

    void Awake()
    {
        AutoBindPlayerIfNeeded();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindEvents(); // 統一管理事件訂閱

        ConfigureArcsIfNeeded();
        ForceRefresh();
        ApplyImmediate();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindEvents();
    }

    void Update()
    {
        if (!smoothLerp) return;

        // HP & MP 平滑移動
        if (hpArc) hpArc.fillAmount = Mathf.MoveTowards(hpArc.fillAmount, targetHpFill, Time.unscaledDeltaTime * lerpSpeed);
        if (mpArc) mpArc.fillAmount = Mathf.MoveTowards(mpArc.fillAmount, targetMpFill, Time.unscaledDeltaTime * lerpSpeed);

        // [新增] EXP 平滑移動
        if (expBar) expBar.fillAmount = Mathf.MoveTowards(expBar.fillAmount, targetExpFill, Time.unscaledDeltaTime * lerpSpeed);
    }

    // 場景切換後的重新綁定
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RebindNextFrame());
    }

    IEnumerator RebindNextFrame()
    {
        yield return null;

        UnbindEvents(); // 先解綁舊的
        AutoBindPlayer(); // 重新抓取新的 Player
        BindEvents(); // 綁定新的

        ForceRefresh();
        ApplyImmediate();
    }

    // [修改] 統一綁定事件
    void BindEvents()
    {
        if (playerStats != null)
            playerStats.OnStatsChanged += OnStatsChanged;

        if (playerLevel != null)
        {
            playerLevel.OnExpChanged += OnExpChanged;
            playerLevel.OnLevelUp += OnLevelUp;
        }
    }

    // [修改] 統一解綁事件
    void UnbindEvents()
    {
        if (playerStats != null)
            playerStats.OnStatsChanged -= OnStatsChanged;

        if (playerLevel != null)
        {
            playerLevel.OnExpChanged -= OnExpChanged;
            playerLevel.OnLevelUp -= OnLevelUp;
        }
    }

    // HP/MP 變更回呼
    void OnStatsChanged(float curHP, float maxHP, float curMP, float maxMP)
    {
        UpdateTargets(curHP, maxHP, curMP, maxMP);
    }

    // [新增] 經驗值變更回呼
    void OnExpChanged(float currentExp, float requiredExp)
    {
        UpdateExpTarget(currentExp, requiredExp);
    }

    // [新增] 升級回呼
    void OnLevelUp(int newLevel)
    {
        if (levelText) levelText.text = $"LV.{newLevel}";
        // 升級時通常經驗值會歸零重算，這裡強制刷新一次經驗條
        if (playerLevel) UpdateExpTarget(playerLevel.CurrentExp, playerLevel.ExpToNextLevel);
    }

    public void ApplyImmediate()
    {
        if (hpArc) hpArc.fillAmount = targetHpFill;
        if (mpArc) mpArc.fillAmount = targetMpFill;
        if (expBar) expBar.fillAmount = targetExpFill; // [新增]
    }

    public void ForceRefresh()
    {
        // 刷新 HP/MP
        if (playerStats != null)
            UpdateTargets(playerStats.CurrentHP, playerStats.MaxHP, playerStats.CurrentMP, playerStats.MaxMP);

        // [新增] 刷新 Level/EXP
        if (playerLevel != null)
        {
            if (levelText) levelText.text = $"LV.{playerLevel.Level}";
            UpdateExpTarget(playerLevel.CurrentExp, playerLevel.ExpToNextLevel);
        }
    }

    void UpdateTargets(float curHP, float maxHP, float curMP, float maxMP)
    {
        float hpRatio = SafeRatio(curHP, maxHP);
        float mpRatio = SafeRatio(curMP, maxMP);

        // 如果是半圓顯示 (0..0.5)，請維持 0.5f * ratio；如果是全圓或長條，請改為 1f * ratio
        targetHpFill = 0.5f * hpRatio;
        targetMpFill = 0.5f * mpRatio;

        if (hpText) hpText.text = $"{Mathf.CeilToInt(curHP)}/{Mathf.CeilToInt(maxHP)}";
        if (mpText) mpText.text = $"{Mathf.CeilToInt(curMP)}/{Mathf.CeilToInt(maxMP)}";
    }

    // [新增] 計算經驗值比例
    void UpdateExpTarget(float current, float max)
    {
        targetExpFill = SafeRatio(current, max);
    }

    float SafeRatio(float v, float max)
    {
        if (max <= 0f) return 0f;
        return Mathf.Clamp01(v / max);
    }

    void ConfigureArcsIfNeeded()
    {
        if (!autoConfigureArc) return;
        // 原有的圓形設定邏輯保持不變...
        // 經驗條通常是橫向長條，這裡就不特別去改它的 FillMethod 了，請在 Editor 設定好
    }

    // —— 自動綁定 —— //
    void AutoBindPlayerIfNeeded()
    {
        if (playerStats == null || playerLevel == null) AutoBindPlayer();
    }

    void AutoBindPlayer()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            // 抓取 Stats
            playerStats = playerGO.GetComponent<PlayerStats>();
            if (playerStats == null) playerStats = playerGO.GetComponentInChildren<PlayerStats>(true);

            // [新增] 抓取 Level
            playerLevel = playerGO.GetComponent<PlayerLevel>();
            if (playerLevel == null) playerLevel = playerGO.GetComponentInChildren<PlayerLevel>(true);
        }
        else
        {
            playerStats = null;
            playerLevel = null;
        }
    }
}