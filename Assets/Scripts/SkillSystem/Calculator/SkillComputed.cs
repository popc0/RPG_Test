namespace RPG
{
    /// <summary>SkillCalculator 的輸出（每次施放前計算）</summary>
    public struct SkillComputed
    {
        public string SkillName;
        public float Damage;
        public float Cooldown;
        public float MpCost;
        public float CastTime;
        public float AreaRadius;
        public float ConeAngle; // ★ 新增欄位
    }
}
