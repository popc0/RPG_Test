using UnityEngine;
using System;

namespace RPG
{
    /// <summary>
    /// 受擊端：可掛在主體或子物件（Body/Feet）。
    /// 命中時會往上找 PlayerStats / MainPointComponent；並保留此受擊器的 InteractionLayer 標記。
    /// </summary>
    [DisallowMultipleComponent]
    public class EffectApplier : MonoBehaviour
    {
        [Header("主體組件（自動往上找）")]
        public PlayerStats stats;                 // 你的現有 HP/MP 腳本
        public MainPointComponent main;           // 主屬性容器

        [Header("受擊區層")]
        public InteractionLayer layer = InteractionLayer.Body;

        [Header("顯示名稱（空白則用物件名）")]
        public string displayName;

        /// <summary>
        /// 任一單位受到傷害時廣播：(name, curHP, maxHP, curMP, maxMP)
        /// 供 HUD 顯示「誰受傷、剩餘血魔」。
        /// </summary>
        public static event Action<string, float, float, float, float> OnAnyDamaged;

        void Awake()
        {
            if (!stats) stats = GetComponentInParent<PlayerStats>();
            if (!main) main = GetComponentInParent<MainPointComponent>();
            if (string.IsNullOrWhiteSpace(displayName)) displayName = gameObject.name;
        }

        /// <summary>
        /// 從被命中的 Collider2D 推回受擊主體與互動層（Body/Feet）。
        /// 需確保該 Collider2D 所在物件或其父物件，有掛 EffectApplier。
        /// </summary>
        public static bool TryResolveOwner(Collider2D col, out EffectApplier owner, out InteractionLayer hitLayer)
        {
            owner = null;
            hitLayer = InteractionLayer.Body;
            if (!col) return false;

            owner = col.GetComponentInParent<EffectApplier>();
            if (!owner) return false;

            hitLayer = owner.layer;
            return true;
        }

        /// <summary>傳入攻擊端的原始傷害，這裡會依防禦計算後扣血。</summary>
        public void ApplyIncomingRaw(float outgoingDamage)
        {
            if (!stats || !main)
            {
                Debug.LogWarning($"{name}: 缺少 PlayerStats 或 MainPointComponent，無法受擊。");
                return;
            }
            float finalDamage = main.AfterDefense(outgoingDamage);
            stats.TakeDamage(finalDamage); // ★ 實際扣血

            // 廣播給 HUD（誰受傷＆剩餘血魔）
            OnAnyDamaged?.Invoke(displayName, stats.CurrentHP, stats.MaxHP, stats.CurrentMP, stats.MaxMP);

            Debug.Log($"[{displayName}] 受到 {finalDamage:F1} 傷害（raw={outgoingDamage:F1}, DEF={main.Defense:F1}）");
        }

        /// <summary>若外部已算好最終傷害，可直接扣。</summary>
        public void ApplyFinalDamage(float finalDamage)
        {
            if (!stats)
            {
                Debug.LogWarning($"{name}: 缺少 PlayerStats，無法直接扣血。");
                return;
            }
            stats.TakeDamage(finalDamage); // ★ 實際扣血
            OnAnyDamaged?.Invoke(displayName, stats.CurrentHP, stats.MaxHP, stats.CurrentMP, stats.MaxMP);
            Debug.Log($"[{displayName}] 扣血 {finalDamage:F1}");
        }
    }
}
