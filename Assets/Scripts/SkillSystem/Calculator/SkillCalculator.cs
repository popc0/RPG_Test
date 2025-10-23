using UnityEngine;

namespace RPG
{
    /// <summary>把所有公式集中處理，回傳 SkillComputed</summary>
    public static class SkillCalculator
    {
        public static SkillComputed Compute(SkillData data, MainPoint mp)
        {
            SkillComputed c = new SkillComputed
            {
                SkillName = data.SkillName,
                CastTime = Mathf.Max(0f, data.CastTime)
            };

            // 傷害
            float dmg = Balance.AttackBonus(data.BaseDamage, mp.Attack);
            c.Damage = Mathf.Max(0f, dmg);

            // 冷卻（受敏捷影響）
            c.Cooldown = Balance.CooldownScale(data.BaseCooldown, mp.Agility);

            // MP 消耗（受技巧影響）
            c.MpCost = Mathf.Max(0f, Balance.MpCostScale(data.BaseMpCost, mp.Technique));

            // AoE 半徑（可受敏捷微幅縮放）
            c.AreaRadius = Balance.AreaRadiusScale(data.BaseAreaRadius, mp.Agility);

            return c;
        }
    }
}
