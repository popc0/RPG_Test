using UnityEngine;
using System;

namespace RPG
{
    // 傷害效果：持續傷害 (DoT)
    [Serializable]
    public class StatusDamageEffect : StatusEffectBase
    {
        [Header("持續傷害設定")]
        public float damagePerTick = 5f;
        [Tooltip("每幾秒造成一次傷害")]
        public float tickInterval = 1f;
        [Tooltip("總持續時間 (0 表示永久直到被移除)")]
        public float duration = 5f;
    }
}