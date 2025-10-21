using UnityEngine;
using System;

namespace RPG
{
    /// <summary>角色主屬性（可掛在角色Root或作為資料結構持有）</summary>
    [Serializable]
    public class MainPoint
    {
        [Header("主屬性")]
        public float Attack;    // 攻擊
        public float Defense;   // 防禦
        public float Agility;   // 敏捷
        public float Technique; // 技術
        public float HPStat;    // HP 點數
        public float MPStat;    // MP 點數

        // ── 攻擊與防禦 ───────────────────────
        public float CalcOutgoingDamage(float baseDamage)
            => baseDamage + Attack * Balance.ATK_A;

        public float CalcIncomingAfterDefense(float incoming)
            => Mathf.Max(Balance.MIN_DAMAGE, incoming - Defense * Balance.DEF_B);

        // ── 敏捷與技術 ───────────────────────
        public float AreaScale()
            => 0.1f + 0.9f * Mathf.Exp(-Balance.K_AGI * Agility);

        public float MoveSpeed(float baseSpeed)
            => baseSpeed + Agility * Balance.AGI_D;

        public float CooldownMul()
            => 0.5f + 0.5f * Mathf.Exp(-Balance.K_TECH * Technique);

        public float MpCostMul()
            => 0.5f + 0.5f * Mathf.Exp(-Balance.K_TECH * Technique);

        // ── HP / MP ──────────────────────────
        public float MaxHP(float baseHP) => baseHP + HPStat * Balance.HP_G;
        public float MaxMP(float baseMP) => baseMP + MPStat * Balance.MP_H;
    }
}
