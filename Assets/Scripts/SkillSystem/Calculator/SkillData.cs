using UnityEngine;

namespace RPG
{
    [CreateAssetMenu(menuName = "RPG/Skill Data", fileName = "SkillData_001")]
    public class SkillData : ScriptableObject
    {
        [Header("基本資料")]
        public string SkillName = "New Skill";
        public float BaseCooldown = 8f;
        public float BaseMpCost = 20f;
        public float CastTime = 0.3f;

        [Tooltip("技能圖示")]
        public Sprite Icon;

        [Header("命中與目標")]
        public TargetType Target = TargetType.Enemy;
        public HitType HitType = HitType.Single;
        public InteractionLayer TargetLayer = InteractionLayer.Body;

        [Header("基礎數值（公式計算使用）")]
        public float BaseDamage = 100f;
        public float BaseRange = 8f;
        public float BaseAreaRadius = 2f;

        [Tooltip("扇形角度 (度)")]
        public float BaseConeAngle = 60f; // ★ 新增參數：扇形角度

        [Header("投射物（單體可選）")]
        public bool UseProjectile = true;
        public Projectile2D ProjectilePrefab;
        public float ProjectileSpeed = 16f;
        [Tooltip("資料層的半徑：可供預覽/備援使用（Prefab 拿不到 Collider 尺寸時）")]
        public float ProjectileRadius = 0.25f;

        [Header("屬性門檻（可全關）")]
        public bool UseAttackReq = true; public float ReqAttack = 60f;
        public bool UseDefenseReq = false; public float ReqDefense = 0f;
        public bool UseAgilityReq = false; public float ReqAgility = 0f;
        public bool UseTechniqueReq = false; public float ReqTechnique = 0f;
        public bool UseHPReq = false; public float ReqHP = 0f;
        public bool UseMPReq = false; public float ReqMP = 0f;

        public bool MeetsRequirement(MainPoint mp)
        {
            if (UseAttackReq && mp.Attack < ReqAttack) return false;
            if (UseDefenseReq && mp.Defense < ReqDefense) return false;
            if (UseAgilityReq && mp.Agility < ReqAgility) return false;
            if (UseTechniqueReq && mp.Technique < ReqTechnique) return false;
            if (UseHPReq && mp.HPStat < ReqHP) return false;
            if (UseMPReq && mp.MPStat < ReqMP) return false;
            return true;
        }
    }
}
