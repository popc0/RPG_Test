using UnityEngine;

namespace RPG
{
    /// <summary>
    /// 讀取 SkillData + 施放者 MainPoint，依「定案公式」計算本次施放的核心數字。
    /// 不處理命中、扣血、詞條與特效；只回傳數字。
    /// </summary>
    public static class SkillCalculator
    {
        /// <summary>計算本次施放的核心數字</summary>
        public static SkillComputed Compute(SkillData data, MainPoint caster)
        {
            // 1) 主傷：攻擊線性
            float outgoingDamage = caster.CalcOutgoingDamage(data.BaseDamage);

            // 2) 冷卻/耗魔：技術指數（趨近 50%）
            float cdMul = caster.CooldownMul();
            float mpMul = caster.MpCostMul();
            float cd = Mathf.Max(0.01f, data.BaseCooldown * cdMul);
            float mp = Mathf.Max(0f, data.BaseMpCost * mpMul);

            // 3) 範圍：敏捷指數（趨近 10%）
            float areaR = data.BaseAreaRadius * caster.AreaScale();

            // 4) 命中類型旗標（供後續 ImpactResolver 判斷）
            bool isArea = (data.HitType == HitType.Area);
            bool isSingle = (data.HitType == HitType.Single);

            return new SkillComputed
            {
                SkillName = data.SkillName,
                Damage = outgoingDamage,
                Cooldown = cd,
                MpCost = mp,
                AreaRadius = areaR,
                CastTime = data.CastTime,
                IsArea = isArea,
                IsSingle = isSingle,
                BaseDamage = data.BaseDamage,
                BaseCooldown = data.BaseCooldown,
                BaseMpCost = data.BaseMpCost,
                BaseAreaRadius = data.BaseAreaRadius
            };
        }

        /// <summary>
        /// 計算「對某目標」的最終傷害：把 Compute.Damage 丟給受擊者的防禦線性扣減。
        /// 仍不處理抗性/減傷詞條，僅套 MainPoint 防禦線性與最小傷害。
        /// </summary>
        public static float ResolveFinalDamageForTarget(SkillComputed comp, MainPoint target)
        {
            return target.CalcIncomingAfterDefense(comp.Damage);
        }

        /// <summary>
        /// 簡易檢查：施放者是否通過技能習得條件（與 Step2 的 CanAcquire 保持一致）
        /// </summary>
        public static bool PassAcquireCheck(SkillData data, MainPoint caster)
        {
            return data == null || caster == null || data.CanAcquire(caster);
        }
    }
}
