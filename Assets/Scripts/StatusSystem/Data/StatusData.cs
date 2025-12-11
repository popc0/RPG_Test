using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    [CreateAssetMenu(menuName = "RPG/Status", fileName = "Status_")]
    public class StatusData : ScriptableObject
    {
        [Header("狀態資料")]
        [Tooltip("此 ID 會自動同步檔名，無需手動填寫")]
        public string statusID; // 雖然這裡看起來可寫，但 OnValidate 會強制覆蓋它
        public Sprite statusIcon;

        [Header("效果清單 (可多個效果疊加)")]
        [Tooltip("使用 [SerializeReference] 允許清單中包含多種繼承 StatusEffectBase 的類別")]
        [SerializeReference]
        public List<StatusEffectBase> effects = new List<StatusEffectBase>();

        // ★ 新增：自動同步邏輯
        // 當你在 Inspector 修改任何數值，或是選取此物件時，就會觸發
        void OnValidate()
        {
            // 檢查 ID 是否跟檔名不同
            if (statusID != this.name)
            {
                statusID = this.name;

                // 標記為已修改 (Dirty)，確保 Unity 會存檔
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
    }
}