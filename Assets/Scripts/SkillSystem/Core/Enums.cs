using System;

namespace RPG
{
    public enum TargetType
    {
        Enemy,
        Ally,
        Self
    }

    public enum HitType
    {
        Single,   // 直線/單體
        Area      // AoE（不理牆）
    }

    // 角色互動層：技能可指定命中 Body 或 Feet
    public enum InteractionLayer
    {
        Body,
        Feet
    }
}
