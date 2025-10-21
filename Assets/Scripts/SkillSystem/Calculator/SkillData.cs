using UnityEngine;

namespace RPG
{
    [CreateAssetMenu(menuName = "RPG/Skill Data", fileName = "SkillData_New")]
    public class SkillData : ScriptableObject
    {
        [Header("基本資料")]
        public string SkillName = "New Skill";
        [Tooltip("秒")]
        public float BaseCooldown = 8f;
        [Tooltip("MP")]
        public float BaseMpCost = 20f;
        [Tooltip("秒（0 = 瞬發）")]
        public float CastTime = 0.3f;

        [Header("命中與目標")]
        public TargetSide Target = TargetSide.Enemy;
        public HitType HitType = HitType.Single;             // 預設單體
        public InteractionLayer Layer = InteractionLayer.Body;

        [Header("基礎數值（供計算層使用）")]
        public float BaseDamage = 100f;                      // 主傷基礎
        public float BaseAreaRadius = 2.0f;                  // AoE 半徑（HitType=Area 時才用）

        [Header("取得條件（屬性門檻，可全部關閉）")]
        public bool UseAttackReq; public float ReqAttack;
        public bool UseDefenseReq; public float ReqDefense;
        public bool UseAgilityReq; public float ReqAgility;
        public bool UseTechniqueReq; public float ReqTechnique;
        public bool UseHPReq; public float ReqHPStat;
        public bool UseMPReq; public float ReqMPStat;

        /// <summary>
        /// 是否通過學習/裝備條件（不做字串解析，策劃用勾選＋數值）
        /// </summary>
        public bool CanAcquire(MainPoint mp)
        {
            if (UseAttackReq && mp.Attack < ReqAttack) return false;
            if (UseDefenseReq && mp.Defense < ReqDefense) return false;
            if (UseAgilityReq && mp.Agility < ReqAgility) return false;
            if (UseTechniqueReq && mp.Technique < ReqTechnique) return false;
            if (UseHPReq && mp.HPStat < ReqHPStat) return false;
            if (UseMPReq && mp.MPStat < ReqMPStat) return false;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 簡單資料校驗，避免打架
            if (HitType != HitType.Area) BaseAreaRadius = Mathf.Max(0f, BaseAreaRadius); // 保留值但不報錯
            if (HitType == HitType.Area && BaseAreaRadius <= 0f)
                BaseAreaRadius = 1.0f; // 給最小半徑，避免為 0
        }
#endif
    }
}
