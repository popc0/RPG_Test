using UnityEngine;

namespace RPG
{
    /// <summary>
    /// 可掛在主體或子物件（Body / Feet）。
    /// 命中時會往上找 PlayerStats 與 MainPointComponent。
    /// </summary>
    [DisallowMultipleComponent]
    public class EffectApplier : MonoBehaviour
    {
        [Header("主體組件（自動往上找）")]
        public PlayerStats stats;
        public MainPointComponent main;

        [Header("這個物件屬於哪一層互動層（Body / Feet）")]
        public InteractionLayer layer = InteractionLayer.Body;

        void Awake()
        {
            if (!stats) stats = GetComponentInParent<PlayerStats>();
            if (!main) main = GetComponentInParent<MainPointComponent>();
        }

        /// <summary>從 Collider 推回受擊主體與互動層。</summary>
        public static bool TryResolveOwner(Collider2D col, out EffectApplier owner, out InteractionLayer layer)
        {
            owner = null;
            layer = InteractionLayer.Body;
            if (!col) return false;

            owner = col.GetComponentInParent<EffectApplier>();
            if (owner)
            {
                layer = owner.layer;
                return true;
            }
            return false;
        }

        public void ApplyIncomingRaw(float outgoingDamage)
        {
            if (!stats || !main)
            {
                Debug.LogWarning($"{name}: 缺少 PlayerStats 或 MainPointComponent。");
                return;
            }
            float finalDamage = main.AfterDefense(outgoingDamage);
            stats.TakeDamage(finalDamage);
            Debug.Log($"[{name}] 受到 {finalDamage:F1} 傷害 (raw {outgoingDamage:F1}, DEF {main.Defense:F1})");
        }
    }
}
