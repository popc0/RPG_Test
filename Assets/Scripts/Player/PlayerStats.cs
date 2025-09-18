using System;
using UnityEngine;

/// <summary>
/// 玩家數值：只管理資料與事件，不碰 UI。
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public event Action OnStatsChanged;

    [Header("最大值")]
    public float MaxHP = 100f;
    public float MaxMP = 50f;

    [Header("當前值")]
    public float CurrentHP;
    public float CurrentMP;

    void Awake()
    {
        // 遊戲開始預設滿血滿魔
        CurrentHP = MaxHP;
        CurrentMP = MaxMP;
    }

    // ===== 對外操作 API =====

    public void TakeDamage(float amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP - Mathf.Abs(amount), 0f, MaxHP);
        OnStatsChanged?.Invoke();
    }

    public void Heal(float amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP + Mathf.Abs(amount), 0f, MaxHP);
        OnStatsChanged?.Invoke();
    }

    public void UseMP(float amount)
    {
        CurrentMP = Mathf.Clamp(CurrentMP - Mathf.Abs(amount), 0f, MaxMP);
        OnStatsChanged?.Invoke();
    }

    public void RecoverMP(float amount)
    {
        CurrentMP = Mathf.Clamp(CurrentMP + Mathf.Abs(amount), 0f, MaxMP);
        OnStatsChanged?.Invoke();
    }

    /// <summary>存檔或讀檔時一次寫入。</summary>
    public void SetStats(float hp, float mp)
    {
        CurrentHP = Mathf.Clamp(hp, 0f, MaxHP);
        CurrentMP = Mathf.Clamp(mp, 0f, MaxMP);
        OnStatsChanged?.Invoke();
    }

    // （可選）鍵盤測試
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) TakeDamage(10);
        if (Input.GetKeyDown(KeyCode.J)) Heal(5);
        if (Input.GetKeyDown(KeyCode.K)) UseMP(5);
        if (Input.GetKeyDown(KeyCode.L)) RecoverMP(3);
    }
}
