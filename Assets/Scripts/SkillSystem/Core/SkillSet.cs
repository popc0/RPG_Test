using UnityEngine;
using System;

namespace RPG
{
    [Serializable]
    public class SkillSet
    {
        [Header("技能組設定")]
        public string setName = "Skill Set";

        [Tooltip("對應手把/鍵盤的 Skill Button 1")]
        public SkillData skill0;

        [Tooltip("對應手把/鍵盤的 Skill Button 2")]
        public SkillData skill1;

        // [Header("未來擴充")]
        // public SkillData passiveSkill; // 您提到的常駐技能欄位，先預留
    }
}