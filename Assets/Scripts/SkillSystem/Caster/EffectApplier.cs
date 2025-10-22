using UnityEngine;

namespace RPG
{
    public class EffectApplier : MonoBehaviour
    {
        [Header("受擊者組件（可留空，會自動往父物件找）")]
        public MainPointComponent main;
        public PlayerStats stats;

        void Awake()
        {
            if (!main) main = GetComponentInParent<MainPointComponent>();
            if (!stats) stats = GetComponentInParent<PlayerStats>();
        }

        public void ApplyIncomingRaw(float outgoingDamage)
        {
            if (!stats || !main)
            {
                Debug.LogWarning($"{name} 缺少 PlayerStats 或 MainPointComponent，無法扣血");
                return;
            }

            float finalDamage = main.AfterDefense(outgoingDamage);
            stats.TakeDamage(finalDamage);
            Debug.Log($"{name} 受到 {finalDamage:F1} 傷害（raw={outgoingDamage:F1}，DEF={main.Defense:F1}）");
        }

        public void ApplyFinalDamage(float finalDamage)
        {
            if (!stats)
            {
                Debug.LogWarning($"{name} 缺少 PlayerStats，無法扣血");
                return;
            }
            stats.TakeDamage(finalDamage);
            Debug.Log($"{name} 扣血 {finalDamage:F1}");
        }
    }
}
