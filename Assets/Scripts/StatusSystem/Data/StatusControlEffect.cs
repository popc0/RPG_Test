using UnityEngine;
using System;

namespace RPG
{
    // 控制效果：決定移動和施法權限
    [Serializable]
    public class StatusControlEffect : StatusEffectBase
    {
        [Header("限制設定")]
        [Tooltip("是否禁止移動")]
        public bool disableMovement = false;

        [Tooltip("是否禁止施放技能")]
        public bool disableSkills = false;
    }
}