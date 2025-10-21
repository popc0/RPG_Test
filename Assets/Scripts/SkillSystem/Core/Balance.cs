using UnityEngine;

namespace RPG
{
    /// <summary>平衡常數（集中管理，改這裡全系統同步）</summary>
    public static class Balance
    {
        // 線性係數
        public const float ATK_A = 1.0f;   // 攻擊線性倍率
        public const float DEF_B = 0.8f;   // 防禦線性倍率
        public const float AGI_D = 0.01f;  // 敏捷→移速倍率
        public const float HP_G = 10f;    // HP成長
        public const float MP_H = 5f;     // MP成長

        // 指數曲線（已依需求計算）
        public const float K_TECH = 0.0077f; // 技術：冷卻/耗魔 → 趨近50%
        public const float K_AGI = 0.0073f; // 敏捷：範圍 → 趨近10%

        // 安全底線
        public const float MIN_DAMAGE = 1f;  // 最小傷害
    }
}
