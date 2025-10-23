using UnityEngine;

namespace RPG
{
    /// <summary>技能資料（SO）</summary>
    [CreateAssetMenu(menuName = "RPG/Skill Data", fileName = "SkillData_")]
    public class SkillData : ScriptableObject
    {
        [Header("基本資料")]
        public string SkillName = "New Skill";
        public float BaseCooldown = 8f;
        public float BaseMpCost = 20f;
        public float CastTime = 0.3f;

        [Header("命中與目標")]
        public TargetType Target = TargetType.Enemy;
        public HitType HitType = HitType.Single;
        public InteractionLayer TargetLayer = InteractionLayer.Body;

        [Header("基礎數值（提供計算用）")]
        public float BaseDamage = 100f;
        public float BaseAreaRadius = 2f;

        [Header("取得條件（屬性門檻，可全部關閉）")]
        public bool UseAttackReq = false; public float ReqAttack = 0f;
        public bool UseDefenseReq = false; public float ReqDefense = 0f;
        public bool UseAgilityReq = false; public float ReqAgility = 0f;
        public bool UseTechniqueReq = false; public float ReqTechnique = 0f;
        public bool UseHPReq = false; public float ReqHPStat = 0f;
        public bool UseMPReq = false; public float ReqMPStat = 0f;

        public bool MeetsRequirement(MainPoint mp)
        {
            if (UseAttackReq && mp.Attack < ReqAttack) return false;
            if (UseDefenseReq && mp.Defense < ReqDefense) return false;
            if (UseAgilityReq && mp.Agility < ReqAgility) return false;
            if (UseTechniqueReq && mp.Technique < ReqTechnique) return false;
            if (UseHPReq && mp.HPStat < ReqHPStat) return false;
            if (UseMPReq && mp.MPStat < ReqMPStat) return false;
            return true;
        }
    }
}
