using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using RPG; // <-- [新增] 引用 RPG 命名空間

/// <summary>
/// 玩家數值（事件驅動 + UnityEvent 版本）
/// - 不直接碰 UI；以 C# event 與 UnityEvent 兩種方式通知外部（HUD、血條等）。
/// - 所有變動皆呼叫 NotifyChange()；可在編輯器以 UnityEvent 綁訂 UI，不必寫碼。
/// - 保留 H/J/K/L 測試鍵。
/// </summary>
[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    /// <summary>
    /// 當血量或魔力變化時觸發（C# event）。
    /// 參數：(CurrentHP, MaxHP, CurrentMP, MaxMP)
    /// </summary>
    public event Action<float, float, float, float> OnStatsChanged;

    /// <summary>
    /// 當血量或魔力變化時觸發（可在 Inspector 綁定）。
    /// 參數：(CurrentHP, MaxHP, CurrentMP, MaxMP)
    /// </summary>
    [Header("事件（可在 Inspector 綁定）")]
    public UnityEvent<float, float, float, float> OnStatsChangedUnityEvent;

    [Header("最大值")]
    public float MaxHP = 100f;
    public float MaxMP = 50f;

    [Header("當前值")]
    public float CurrentHP;
    public float CurrentMP;

    private MainPointComponent _mainPoint; // [新增] 內部變數儲存 MainPointComponent 的引用

    void Awake()
    {
        // [修改] 將尋找 MainPointComponent 的邏輯提前並存起來
        _mainPoint = GetComponentInParent<MainPointComponent>();

        // [新增] 如果有 MainPointComponent，立刻訂閱它的屬性變更事件
        if (_mainPoint != null)
        {
            // [修改] 改用 TotalHPStat / TotalMPStat 來獲取 (基礎10 + 加點) 的總值
            SetMax(_mainPoint.TotalHPStat, _mainPoint.TotalMPStat, refillCurrent: true);

            // 訂閱變更事件
            _mainPoint.OnStatChanged += OnMainPointStatChanged;
        }
        // 若尚未初始化則滿血滿魔
        if (CurrentHP <= 0f) CurrentHP = MaxHP;
        if (CurrentMP <= 0f) CurrentMP = MaxMP;
        NotifyChange();
    }
    void OnDestroy()
    {
        // [新增] 記得在銷毀時取消訂閱
        if (_mainPoint != null)
        {
            _mainPoint.OnStatChanged -= OnMainPointStatChanged;
        }
    }
    // [新增] 處理 MainPointComponent 屬性變更的方法
    void OnMainPointStatChanged()
    {
        // 當主屬性（例如 HPStat/MPStat）改變時，重新應用最大值
        if (_mainPoint != null)
        {
            // [修改] 同樣改用 TotalHPStat / TotalMPStat
            SetMax(_mainPoint.TotalHPStat, _mainPoint.TotalMPStat, refillCurrent: false);
            // 設為 false，避免屬性點分配後，玩家當前的血量被強制補滿
        }
    }

    void OnValidate()
    {
        // 在編輯器調整數值時即時校正
        MaxHP = Mathf.Max(0f, MaxHP);
        MaxMP = Mathf.Max(0f, MaxMP);
        CurrentHP = Mathf.Clamp(CurrentHP, 0f, MaxHP);
        CurrentMP = Mathf.Clamp(CurrentMP, 0f, MaxMP);
    }

    // ===== 對外操作 API =====

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        CurrentHP = Mathf.Clamp(CurrentHP - amount, 0f, MaxHP);
        NotifyChange();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0f, MaxHP);
        NotifyChange();
    }

    public void UseMP(float amount)
    {
        if (amount <= 0f) return;
        CurrentMP = Mathf.Clamp(CurrentMP - amount, 0f, MaxMP);
        NotifyChange();
    }

    public void RecoverMP(float amount)
    {
        if (amount <= 0f) return;
        CurrentMP = Mathf.Clamp(CurrentMP + amount, 0f, MaxMP);
        NotifyChange();
    }

    /// <summary>直接設定血魔（例如讀檔後）。</summary>
    public void SetStats(float hp, float mp)
    {
        CurrentHP = Mathf.Clamp(hp, 0f, MaxHP);
        CurrentMP = Mathf.Clamp(mp, 0f, MaxMP);
        NotifyChange();
    }

    /// <summary>同時設定最大值；可選是否把當前值補滿。</summary>
    public void SetMax(float maxHp, float maxMp, bool refillCurrent = true)
    {
        MaxHP = Mathf.Max(0f, maxHp);
        MaxMP = Mathf.Max(0f, maxMp);
        if (refillCurrent)
        {
            CurrentHP = MaxHP;
            CurrentMP = MaxMP;
        }
        else
        {
            CurrentHP = Mathf.Clamp(CurrentHP, 0f, MaxHP);
            CurrentMP = Mathf.Clamp(CurrentMP, 0f, MaxMP);
        }
        NotifyChange();
    }

    /// <summary>全部補滿。</summary>
    public void RefillAll()
    {
        CurrentHP = MaxHP;
        CurrentMP = MaxMP;
        NotifyChange();
    }

    /// <summary>
    /// 通知所有監聽者（HUD、其他系統）。
    /// </summary>
    void NotifyChange()
    {
        OnStatsChanged?.Invoke(CurrentHP, MaxHP, CurrentMP, MaxMP);
        OnStatsChangedUnityEvent?.Invoke(CurrentHP, MaxHP, CurrentMP, MaxMP);
    }

    // ===== 測試快捷鍵 =====
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.H)) TakeDamage(10);
        //if (Input.GetKeyDown(KeyCode.J)) Heal(5);
        //if (Input.GetKeyDown(KeyCode.K)) UseMP(5);
        //if (Input.GetKeyDown(KeyCode.L)) RecoverMP(3);
    }
}
