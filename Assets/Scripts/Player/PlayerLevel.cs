using System;
using UnityEngine;
using RPG;

/// <summary>
/// 玩家等級與經驗值系統
/// 掛在 Player 物件上，並連結到 MainPointComponent。
/// </summary>
public class PlayerLevel : MonoBehaviour
{
    // 事件：通知 UI 等級、經驗值和屬性點變更
    public event Action<int> OnLevelUp;
    public event Action<float, float> OnExpChanged; // (currentExp, expToNextLevel)
    public event Action OnStatPointsChanged;

    [Header("引用")]
    public MainPointComponent mainPoint;

    [Header("等級/經驗值")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private int unspentStatPoints = 0;

    [Header("升級設定")]
    [Tooltip("每升一級獲得的屬性點")]
    public int pointsPerLevel = 5;

    public int Level => level;
    public float CurrentExp => currentExp;
    public int UnspentStatPoints => unspentStatPoints;

    // 經驗值計算公式：下一級所需經驗 = BaseExp * (Level^ExpCurve)
    private const float BaseExp = 100f;
    private const float ExpCurve = 1.5f;

    /// <summary>下一級所需經驗值</summary>
    public float ExpToNextLevel => BaseExp * Mathf.Pow(Level, ExpCurve);

    void Awake()
    {
        if (mainPoint == null)
            mainPoint = GetComponentInParent<MainPointComponent>();
    }

    /// <summary>
    /// 增加經驗值。
    /// </summary>
    public void GainExp(float amount)
    {
        if (amount <= 0f) return;
        currentExp += amount;

        float requiredExp = ExpToNextLevel;

        while (currentExp >= requiredExp)
        {
            currentExp -= requiredExp;
            level++;
            unspentStatPoints += pointsPerLevel;

            Debug.Log($"玩家升級了！ Level {level}, 獲得 {pointsPerLevel} 點屬性點。");
            OnLevelUp?.Invoke(level);

            // 下一級的經驗值門檻
            requiredExp = ExpToNextLevel;
        }

        OnExpChanged?.Invoke(currentExp, requiredExp);
        OnStatPointsChanged?.Invoke();
    }

    /// <summary>
    /// 將屬性點分配到指定屬性上。
    /// </summary>
    /// <param name="statName">屬性名稱 (例如: "Attack", "HPStat")</param>
    public bool SpendStatPoint(string statName, float amount = 1f)
    {
        if (unspentStatPoints <= 0 || mainPoint == null) return false;

        if (mainPoint.TryIncrementStat(statName, amount))
        {
            unspentStatPoints--;
            OnStatPointsChanged?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 從存檔讀取後套用資料
    /// </summary>
    public void SetData(int newLevel, float exp, int points)
    {
        level = Mathf.Max(1, newLevel);
        currentExp = Mathf.Max(0f, exp);
        unspentStatPoints = Mathf.Max(0, points);

        float requiredExp = ExpToNextLevel;

        OnExpChanged?.Invoke(currentExp, requiredExp);
        OnStatPointsChanged?.Invoke();
    }
}