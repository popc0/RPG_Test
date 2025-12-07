using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    [CreateAssetMenu(menuName = "RPG/Status/Status Data (Unified)", fileName = "NewStatus_")]
    public class StatusData : ScriptableObject
    {
        [Header("狀態資料")]
        public string statusID;
        public Sprite statusIcon;

        [Header("效果清單 (可多個效果疊加)")]
        [Tooltip("使用 [SerializeReference] 允許清單中包含多種繼承 StatusEffectBase 的類別")]
        [SerializeReference]
        // ★ 核心變更：使用 [SerializeReference] 讓清單可容納多態物件
        public List<StatusEffectBase> effects = new List<StatusEffectBase>();
    }
}