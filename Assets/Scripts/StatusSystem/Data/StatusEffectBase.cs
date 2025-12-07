using System;
using UnityEngine;

namespace RPG
{
    // 所有具體狀態效果的基類
    // 必須標記 [Serializable] 才能被存入 ScriptableObject
    [Serializable]
    public abstract class StatusEffectBase
    {
        [Tooltip("此效果的唯一名稱，方便除錯")]
        public string EffectName = "New Effect";
    }
}