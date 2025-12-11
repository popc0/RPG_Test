using System.Collections.Generic;
using UnityEngine;

namespace RPG
{
    [DisallowMultipleComponent]
    public class StatusManager : MonoBehaviour
    {
        // 為了方便全域存取 (單機遊戲常用)，也可以用 GetComponent
        public static StatusManager Instance { get; private set; }

        // ★ 修改 1: 改用 Dictionary 來計數 (Key=狀態, Value=層數)
        private Dictionary<StatusData, int> statusCounts = new Dictionary<StatusData, int>();

        // 為了 Inspector 除錯方便，我們保留一個 List 顯示目前有哪些狀態 (唯讀)
        [Header("除錯顯示 (唯讀)")]
        [SerializeField] private List<StatusData> debugActiveEffects = new List<StatusData>();

        // 快取計算結果，避免每幀重算 List
        private int _moveLockCount = 0;
        private int _skillLockCount = 0;
        private int _analogMoveCount = 0; // 類比移動計數

        // --- 對外公開的狀態查詢屬性 ---
        public bool CanMove => _moveLockCount == 0;
        public bool CanCast => _skillLockCount == 0;
        public bool IsAnalogMove => _analogMoveCount > 0;

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            Instance = this;
        }

        /// <summary>
        /// 套用狀態效果 (引用計數 + 1)
        /// </summary>
        public void Apply(StatusData statusData)
        {
            if (statusData == null) return;

            // 1. 檢查字典裡有沒有這個狀態
            if (statusCounts.ContainsKey(statusData))
            {
                // 已經有了 -> 計數 + 1
                statusCounts[statusData]++;
            }
            else
            {
                // 這是新的 -> 初始化為 1，並執行效果計算
                statusCounts[statusData] = 1;
                debugActiveEffects.Add(statusData); // 更新 Debug 清單

                // ★ 只有在「從 0 變 1」的時候才需要計算鎖定 (避免重複疊加鎖定值)
                RecalculateLocks(statusData, true);
                Debug.Log($"[StatusManager] 新增狀態: {statusData.name}");
            }
        }

        /// <summary>
        /// 移除狀態效果 (引用計數 - 1)
        /// </summary>
        public void Remove(StatusData statusData)
        {
            if (statusData == null) return;

            if (statusCounts.ContainsKey(statusData))
            {
                // 計數 - 1
                statusCounts[statusData]--;

                // 如果計數歸零，才真正移除
                if (statusCounts[statusData] <= 0)
                {
                    statusCounts.Remove(statusData);
                    debugActiveEffects.Remove(statusData); // 更新 Debug 清單

                    // ★ 只有在「真正移除」的時候才計算解鎖
                    RecalculateLocks(statusData, false);
                    Debug.Log($"[StatusManager] 移除狀態: {statusData.name}");
                }
            }
        }

        /// <summary>
        /// 強制清除所有狀態 (死亡或過場時用)
        /// </summary>
        public void ClearAll()
        {
            // 為了安全，我們反向遍歷來移除
            foreach (var status in new List<StatusData>(statusCounts.Keys))
            {
                // 強制移除邏輯：把計數歸零並執行 RecalculateLocks(false)
                RecalculateLocks(status, false);
            }
            statusCounts.Clear();
            debugActiveEffects.Clear();
            _moveLockCount = 0;
            _skillLockCount = 0;
            _analogMoveCount = 0;
        }

        private void RecalculateLocks(StatusData statusData, bool isAdding)
        {
            if (statusData.effects == null) return;

            int delta = isAdding ? 1 : -1;

            foreach (var effect in statusData.effects)
            {
                if (effect is StatusControlEffect control)
                {
                    if (control.disableMovement) _moveLockCount += delta;
                    if (control.disableSkills) _skillLockCount += delta;
                    if (control.enableAnalogMovement) _analogMoveCount += delta;
                }
            }

            // 防呆校正
            if (_moveLockCount < 0) _moveLockCount = 0;
            if (_skillLockCount < 0) _skillLockCount = 0;
            if (_analogMoveCount < 0) _analogMoveCount = 0;
        }
    }
}