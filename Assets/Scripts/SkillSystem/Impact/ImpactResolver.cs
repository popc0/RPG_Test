using UnityEngine;

namespace RPG
{
    /// <summary>
    /// 後續要做暴擊、狀態、穿透等可集中在這裡。
    /// 目前提供一個最簡單的直傷流程範例（未被 SkillCaster 使用也無妨）。
    /// </summary>
    public static class ImpactResolver
    {
        public static void ApplyDirectDamage(EffectApplier target, float rawDamage)
        {
            if (!target) return;
            target.ApplyIncomingRaw(rawDamage);
        }
    }
}
