using System.Collections.Generic;
using UnityEngine;

namespace RPG
{
    [DisallowMultipleComponent]
    public class StatusManager : MonoBehaviour
    {
        // 為了方便全域存取 (單機遊戲常用)，也可以用 GetComponent
        public static StatusManager Instance { get; private set; }

        [Header("除錯顯示")]
        [SerializeField] private List<StatusData> activeEffects = new List<StatusData>();

        // 快取計算結果，避免每幀重算 List
        private int _moveLockCount = 0;
        private int _skillLockCount = 0;

        // --- 對外公開的狀態查詢屬性 ---
        public bool CanMove => _moveLockCount == 0;
        public bool CanCast => _skillLockCount == 0;

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            Instance = this;
        }

        /// <summary>
        /// 套用狀態效果
        /// </summary>
        public void Apply(StatusData statusData) // ★ 參數類型變更
        {
            if (statusData == null) return;

            if (!activeEffects.Contains(statusData))
            {
                activeEffects.Add(statusData);
                // 必須計算 StatusData 裡面的所有效果
                RecalculateLocks(statusData, true);
                Debug.Log($"[StatusManager] 套用狀態組: {statusData.name}");

                // 待辦：如果有持續傷害 StatusDamageEffect，在這裡啟動 Coroutine
            }
        }

        /// <summary>
        /// 移除狀態效果
        /// </summary>
        public void Remove(StatusData statusData) // ★ 參數類型變更
        {
            if (statusData == null) return;

            if (activeEffects.Contains(statusData))
            {
                activeEffects.Remove(statusData);
                RecalculateLocks(statusData, false);
                Debug.Log($"[StatusManager] 移除狀態組: {statusData.name}");

                // 待辦：如果有持續傷害 StatusDamageEffect，在這裡停止 Coroutine
            }
        }

        /// <summary>
        /// 增量更新鎖定計數，遍歷 StatusData 內部的 effects
        /// </summary>
        private void RecalculateLocks(StatusData statusData, bool isAdding)
        {
            if (statusData.effects == null) return;

            int delta = isAdding ? 1 : -1;

            foreach (var effect in statusData.effects)
            {
                // 檢查是否是控制類效果
                if (effect is StatusControlEffect control)
                {
                    if (control.disableMovement) _moveLockCount += delta;
                    if (control.disableSkills) _skillLockCount += delta;

                    // 未來這裡可以處理 MovementSpeedMultiplier 的疊加或最大值/最小值邏輯
                }

                // if (effect is StatusAttributeEffect attr) { /* 處理屬性增減 */ }
                // if (effect is StatusImmunizeEffect imm) { /* 處理免疫 */ }
            }

            // 防呆：計數不應小於 0
            if (_moveLockCount < 0) _moveLockCount = 0;
            if (_skillLockCount < 0) _skillLockCount = 0;
        }
    }
}