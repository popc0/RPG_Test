using UnityEngine;

namespace RPG
{
    /// <summary>
    /// 受擊端：用 PlayerStats 扣 HP；可選擇「外部已算好最終傷害」或「這裡再依防禦線性扣」。
    /// HUD 應訂閱 PlayerStats 的事件（如 OnStatsChanged）。
    /// </summary>
    public class EffectApplier : MonoBehaviour
    {
        [Header("受擊者屬性與狀態")]
        public MainPoint mainPoint;   // 防禦線性運算來源
        public PlayerStats stats;     // 狀態（CurrentHP/MaxHP/...）

        /// <summary>
        /// 入口 A：輸入「原始攻擊端傷害」，在受擊端依防禦線性扣減後再扣血。
        /// </summary>
        public void ApplyIncomingRaw(float outgoingDamage, MainPoint attackerMp = null)
        {
            if (stats == null || mainPoint == null)
            {
                Debug.LogWarning($"{name} 缺少 PlayerStats 或 MainPoint，無法計算/扣血");
                return;
            }

            // 攻防線性：final = max(min, outgoing - DEF×B)
            float finalDamage = mainPoint.CalcIncomingAfterDefense(outgoingDamage);
            stats.TakeDamage(finalDamage);
            Debug.Log($"{name} 受到 {finalDamage:F1} 傷害（raw={outgoingDamage:F1}，DEF={mainPoint.Defense:F1}）");
        }

        /// <summary>
        /// 入口 B：若外部已計算「最終傷害」，可直接扣。
        /// </summary>
        public void ApplyFinalDamage(float finalDamage)
        {
            if (stats == null)
            {
                Debug.LogWarning($"{name} 缺少 PlayerStats，無法扣血");
                return;
            }
            stats.TakeDamage(finalDamage);
            Debug.Log($"{name} 直接扣血 {finalDamage:F1}");
        }
    }
}
