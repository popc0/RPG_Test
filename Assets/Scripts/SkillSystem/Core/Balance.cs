using UnityEngine;

namespace RPG
{
    /// <summary>全域平衡參數與公式</summary>
    public static class Balance
    {
        // ===== 常數（可在此調） =====
        public const float ATTACK_TO_DAMAGE_FACTOR = 1.0f;    // 攻擊→傷害線性係數
        public const float BASE_DAMAGE_FALLOFF = 0.0f;     // 額外微調

        public const float DEFENSE_MAX_REDUCE = 0.7f;          // 防禦最多減 70%
        public const float DEFENSE_K = 0.0025f;        // 線性比例（例：DEF=100 → 減 25%）

        public const float COOLDOWN_MIN_RATIO = 0.5f;          // 冷卻最少到 50%
        public const float COOLDOWN_EXP_A = 0.0035f;       // 指數衰減係數（敏捷）

        public const float MPCOST_MIN_RATIO = 0.5f;          // MP 消耗最少到 50%
        public const float MPCOST_EXP_A = 0.0030f;       // 指數衰減係數（技巧）

        public const float AREA_RADIUS_MIN = 0.2f;          // AoE 最小半徑下限

        // [新增] 移動速度參數
        public const float SPEED_AGI_FACTOR = 0.005f; // 每點敏捷增加 0.5% 速度
        public const float SPEED_MAX_MULTIPLIER = 2.0f; // 最多加速到 200% (兩倍速)

        // [新增] 碰撞箱縮放參數
        public const float HITBOX_AGI_REDUCTION = 0.002f; // 每點敏捷減少 0.2% 體積
        public const float HITBOX_MIN_SCALE = 0.7f;       // 最小縮到 70%

        // ===== 公式 =====

        /// <summary>攻擊加成（線性）</summary>
        public static float AttackBonus(float baseDamage, float attack)
        {
            return baseDamage + attack * ATTACK_TO_DAMAGE_FACTOR - BASE_DAMAGE_FALLOFF;
        }

        /// <summary>防禦減傷比例（0~1）線性+封頂</summary>
        public static float DefenseReduction(float defense)
        {
            float r = defense * DEFENSE_K;
            return Mathf.Clamp(r, 0f, DEFENSE_MAX_REDUCE);
        }

        /// <summary>冷卻縮減（指數衰減，越高敏捷越接近 MinRatio）</summary>
        public static float CooldownScale(float baseSeconds, float agility)
        {
            float ratio = Mathf.Lerp(1f, COOLDOWN_MIN_RATIO, 1f - Mathf.Exp(-COOLDOWN_EXP_A * Mathf.Max(0f, agility)));
            return baseSeconds * ratio;
        }

        /// <summary>MP 消耗縮減（指數衰減，以技巧為主）</summary>
        public static float MpCostScale(float baseMp, float technique)
        {
            float ratio = Mathf.Lerp(1f, MPCOST_MIN_RATIO, 1f - Mathf.Exp(-MPCOST_EXP_A * Mathf.Max(0f, technique)));
            return baseMp * ratio;
        }

        /// <summary>範圍半徑與敏捷的微幅縮放（可選）</summary>
        public static float AreaRadiusScale(float baseRadius, float agility)
        {
            // 讓敏捷高 → 圈稍小（更精準），最低不小於 20% 的差值
            float t = Mathf.Clamp01(agility / 300f); // 300 時效果趨近穩定
            float scale = Mathf.Lerp(1f, 0.8f, t);   // 最多縮到 80%
            return Mathf.Max(AREA_RADIUS_MIN, baseRadius * scale);
        }

        /// <summary>
        /// 計算移動速度 (基礎速度 * (1 + 敏捷加成))
        /// </summary>
        public static float MoveSpeed(float baseSpeed, float agility)
        {
            // 計算加成倍率：敏捷 * 0.5%
            float bonus = agility * SPEED_AGI_FACTOR;

            // 限制最大倍率 (例如原本 1.0，加成後變成 1.5，但不能超過 2.0)
            float totalMultiplier = Mathf.Clamp(1f + bonus, 1f, SPEED_MAX_MULTIPLIER);

            return baseSpeed * totalMultiplier;
        }
        // [新增] 玩家受擊判定縮放公式
        // 敏捷 0  => 回傳 1.0 (原尺寸)
        // 敏捷 100 => 回傳 0.8 (縮小 20%)
        public static float PlayerHitboxScale(float agility)
        {
            float reduction = agility * HITBOX_AGI_REDUCTION;
            float scale = 1f - reduction;
            return Mathf.Clamp(scale, HITBOX_MIN_SCALE, 1.0f);
        }
    }
}
