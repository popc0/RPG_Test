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

        [Header("特殊能力")]
        [Tooltip("是否啟用類比移動？\nFalse(預設): 無論推多輕，都視為全速移動(保留透視)。\nTrue: 允許輕推慢走 (細膩操作)。")]
        public bool enableAnalogMovement = false; // ★ 新增：解鎖類比移動
    }
}