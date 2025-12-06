using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    // 排程步驟：時間 + 技能
    [System.Serializable]
    public struct SkillSequenceStep
    {
        [Tooltip("相對於上一個動作開始後的延遲時間")]
        public float delay;
        public SkillData skill;
    }

    [CreateAssetMenu(menuName = "RPG/Skill Data", fileName = "NewSkill")]
    public class SkillData : ScriptableObject
    {
        // ============================================================
        // 1. 識別 (Identity)
        // ============================================================
        // [HideInInspector] // 這些交給 Editor 顯示
        public SkillType type = SkillType.Normal;
        public int rank = 1;
        public int familySerial = 1;
        public string skillID; // 自動生成：T RR SSSSSS

        public SkillData nextEvolution; // 進化

        // ============================================================
        // 2. 學習與裝備 (Root Only)
        // 這些限制只在「學習技能」或「裝備技能」時檢查
        // ============================================================
        public string SkillName = "New Skill";
        public Sprite Icon;

        public float BaseCooldown = 5f;
        public float BaseMpCost = 10f;

        // --- 屬性門檻 (下限 Min, 上限 Max) ---
        // 1. 攻擊力 (Attack)
        public bool UseAttackReq = false;
        public float ReqAttackMin = 0f;
        public bool UseAttackCap = false;
        public float ReqAttackMax = 999f;

        // 2. 防禦力 (Defense)
        public bool UseDefenseReq = false;
        public float ReqDefenseMin = 0f;
        public bool UseDefenseCap = false;
        public float ReqDefenseMax = 999f;

        // 3. 敏捷 (Agility)
        public bool UseAgilityReq = false;
        public float ReqAgilityMin = 0f;
        public bool UseAgilityCap = false;
        public float ReqAgilityMax = 999f;

        // 4. 技巧 (Technique)
        public bool UseTechniqueReq = false;
        public float ReqTechniqueMin = 0f;
        public bool UseTechniqueCap = false;
        public float ReqTechniqueMax = 999f;

        // 5. HP (Health Points)
        public bool UseHPReq = false;
        public float ReqHPMin = 0f;
        public bool UseHPCap = false;
        public float ReqHPMax = 9999f;

        // 6. MP (Mana Points)
        public bool UseMPReq = false;
        public float ReqMPMin = 0f;
        public bool UseMPCap = false;
        public float ReqMPMax = 9999f;

        // (其他的 Defense, Agility... 可依此類推，這裡先寫 Attack 當範例)

        // ============================================================
        // 3. 執行參數 (Action)
        // ============================================================
        public float CastTime = 0.3f; // 只有第一招會用到，後續招式通常視為 0 或忽略

        public TargetType Target = TargetType.Enemy;
        public HitType HitType = HitType.Single;
        public InteractionLayer TargetLayer = InteractionLayer.Body;

        public float BaseDamage = 100f;
        public float BaseRange = 8f;       // 射程 / 距離
        public float BaseAreaRadius = 2f;  // Area 用
        public float BaseConeAngle = 60f;  // Cone 用

        // ============================================================
        // 4. 投射物 (Projectile)
        // ============================================================
        public bool UseProjectile = false;
        public Projectile2D ProjectilePrefab;
        public float ProjectileSpeed = 15f;
        public float ProjectileRadius = 0.25f;

        // ============================================================
        // 5. 排程 (Sequence)
        // ============================================================
        public List<SkillSequenceStep> sequence = new List<SkillSequenceStep>();

        // ============================================================
        // 邏輯
        // ============================================================
        void OnValidate()
        {
            string typeCode = "N";
            if (type == SkillType.Ultimate) typeCode = "U";
            if (type == SkillType.Passive) typeCode = "P";
            skillID = $"{typeCode}{rank:D2}{familySerial:D6}";
        }

        // 檢查學習條件 (包含上限)
        public bool CheckLearningReq(MainPoint mp)
        {
            // 1. 檢查下限 (必須高於 Min)
            if (UseAttackReq && mp.Attack < ReqAttackMin) return false;

            // 2. 檢查上限 (必須低於 Max) - 僅在學習當下檢查
            if (UseAttackCap && mp.Attack >= ReqAttackMax) return false;
            // 防禦
            if (UseDefenseReq && mp.Defense < ReqDefenseMin) return false;
            if (UseDefenseCap && mp.Defense >= ReqDefenseMax) return false;

            // 敏捷
            if (UseAgilityReq && mp.Agility < ReqAgilityMin) return false;
            if (UseAgilityCap && mp.Agility >= ReqAgilityMax) return false;

            // 技巧
            if (UseTechniqueReq && mp.Technique < ReqTechniqueMin) return false;
            if (UseTechniqueCap && mp.Technique >= ReqTechniqueMax) return false;

            // HP (通常指最大血量需求)
            if (UseHPReq && mp.HPStat < ReqHPMin) return false;
            if (UseHPCap && mp.HPStat >= ReqHPMax) return false;

            // MP (通常指最大魔力需求)
            if (UseMPReq && mp.MPStat < ReqMPMin) return false;
            if (UseMPCap && mp.MPStat >= ReqMPMax) return false;
            return true;
        }

        // 檢查施放條件 (通常只看下限，不看上限)
        public bool CheckCastReq(MainPoint mp)
        {
            if (UseAttackReq && mp.Attack < ReqAttackMin) return false;
            if (UseDefenseReq && mp.Defense < ReqDefenseMin) return false;
            if (UseAgilityReq && mp.Agility < ReqAgilityMin) return false;
            if (UseTechniqueReq && mp.Technique < ReqTechniqueMin) return false;
            if (UseHPReq && mp.HPStat < ReqHPMin) return false;
            if (UseMPReq && mp.MPStat < ReqMPMin) return false;
            return true;
        }
    }
}