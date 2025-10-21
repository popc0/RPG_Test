using UnityEngine;

namespace RPG
{
    /// <summary>本次施放所需的核心數字（由公式算出，供後續流程使用）</summary>
    public struct SkillComputed
    {
        // 來源（可選，用於除錯）
        public string SkillName;

        // 計算結果
        public float Damage;       // 攻擊端主傷（尚未扣目標防禦）
        public float Cooldown;     // 套用 技術 指數後
        public float MpCost;       // 套用 技術 指數後
        public float AreaRadius;   // 套用 敏捷 指數後
        public float CastTime;     // 原樣帶出（本版不受屬性影響）

        // 便捷旗標
        public bool IsArea;       // 供命中體決策
        public bool IsSingle;

        // 附帶基礎值（可用於 UI 顯示或除錯）
        public float BaseDamage;
        public float BaseCooldown;
        public float BaseMpCost;
        public float BaseAreaRadius;

        public override string ToString()
        {
            return $"[{SkillName}] Dmg={Damage:F1}, CD={Cooldown:F2}s, MP={MpCost:F1}, AreaR={AreaRadius:F2}";
        }
    }
}
