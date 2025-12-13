using System;

namespace RPG
{
    public enum SkillType
    {
        Normal,   // N
        Ultimate, // U
        Passive   // P
    }
    public enum TargetType
    {
        Enemy,
        Ally,
        Self
    }

    public enum HitType
    {
        Single,   // 直線/單體
        Area,      // AoE（不理牆）
        Cone      // 扇形
    }

    // ★ 新增：扇形揮舞設定
    public enum SwingDir 
    {
        RightToLeft, LeftToRight  // 逆時針 vs 順時針
    } // 逆時針 vs 順時針

    // 角色互動層：技能可指定命中 Body 或 Feet
    public enum InteractionLayer
    {
        Body,
        Feet
    }
    public enum FacingAxis
    {
        Right,
        Up
    }
    // ★ 新增：生成錨點類型
    public enum SpawnAnchorType
    {
        Body, // 身體/槍口/手部 (FirePoint)
        Feet  // 腳底/地面 (FeetPoint)
    }
}
