using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG;

namespace RPG
{
    // 保持 SkillGroup 定義不變，確保序列化相容
    [System.Serializable]
    public class SkillGroup
    {
        public string groupName = "技能組";
        [Header("Slot 1: 可切換普攻")]
        public SkillData switchableNormal;
        [Header("Slot 3: 可切換大招")]
        public SkillData switchableUltimate;
    }

    [DisallowMultipleComponent]
    public class SkillCaster : MonoBehaviour
    {
        [Header("核心引用")]
        public Transform owner; // 保留 Owner 給存檔或其他邏輯備用
        public UnifiedInputSource inputSource;
        public MainPointComponent main;
        public PlayerStats playerStats;
        public AimSource2D aimSource;

        [Header("執行者 (請拖曳上面的 SkillExecutor)")]
        public SkillExecutor executor;

        // ★ 新增：腳底位置參考 (如果沒設，會自動嘗試找)
        [Header("位置錨點")]
        public Transform feetPoint;

        // ============================================================
        // 1. 數據 (Data) - 這些絕對不能動，因為 SaveManager 會讀寫它們
        // ============================================================
        [Header("常駐固定技能")]
        public SkillData fixedNormalSkill;   // Slot 0
        public SkillData fixedUltimateSkill; // Slot 2

        [Header("可切換技能組")]
        public List<SkillGroup> skillGroups = new List<SkillGroup>();
        public int currentSkillGroupIndex = 0;

        // 緩存清單 (給 HUD 用)
        private List<SkillData> _cachedSkills = new List<SkillData>();
        public List<SkillData> Skills { get { RebuildSkillList(); return _cachedSkills; } }

        // ============================================================
        // 2. HUD 連結 (UI)
        // ============================================================
        [Header("HUD")]
        [SerializeField] private HUDSkillStats hudSkillStats;
        private bool _hudAutoBound = false;

        // ============================================================
        // 3. 冷卻管理 (Logic) - 內部狀態，不影響存檔
        // ============================================================
        private float[] _fixedTimers = new float[2];
        private float[] _fixedMaxs = new float[2];
        private Dictionary<int, float[]> _groupTimers = new Dictionary<int, float[]>();
        private Dictionary<int, float[]> _groupMaxs = new Dictionary<int, float[]>();

        // ============================================================
        // ★ 新增：狀態變數
        // ============================================================

        // 狀態 1: 正在詠唱 (條還在跑)
        public bool IsCasting { get; private set; }

        // 狀態 2: 正在執行攻擊或排程延遲 (CastTime 結束後，RecoveryTime 開始前)
        public bool IsActing { get; private set; }

        // 狀態 2: 正在後搖/復原 (RecoveryTime 期間)
        public bool IsRecovery { get; private set; }

        // [新增] 引用 StatusManager (Start時自動抓取)
        [Header("Status")]
        [SerializeField] private StatusManager statusManager;

        // --- 初始化與生命週期 ---
        void OnEnable() { TryAutoBindHud(); NotifyHudSkillGroupChanged(); }
        void Start()
        {
            if (playerStats && playerStats.MaxMP > 0 && playerStats.CurrentMP <= 0) playerStats.CurrentMP = playerStats.MaxMP;
            InitializeCooldowns();
            NotifyHudSkillGroupChanged();

            // 自動尋找
            if (!executor) executor = GetComponent<SkillExecutor>();
            if (!statusManager) statusManager = GetComponent<StatusManager>();
        }

        void Update()
        {
            if (hudSkillStats == null && !_hudAutoBound) TryAutoBindHud();
            UpdateCooldowns();

            // 輸入監聽
            if (inputSource != null)
            {
                if (inputSource.AttackPressedThisFrame()) TryCastSkillAtIndex(0);
                if (inputSource.Attack2PressedThisFrame()) TryCastSkillAtIndex(1);
                // if (inputSource.IsFixedUltTriggered()) TryCastSkillAtIndex(2);
                // if (inputSource.IsSwitchUltTriggered()) TryCastSkillAtIndex(3);
                if (inputSource.SwitchSkillGroupPressedThisFrame()) SwitchToNextSkillGroup();
            }
        }

        // ============================================================
        // 4. 施法主流程 (Logic)
        // ============================================================

        public void TryCastSkillAtIndex(int skillIndex)
        {
            // ★ 修改：加入 statusManager.CanCast 檢查
            // 如果狀態管理器說不能放招，就直接 return
            if (statusManager != null && !statusManager.CanCast)
            {
                // 這裡可以加個 UI 提示 "沉默中"
                return;
            }
            if (!enabled || !main || !playerStats) return;

            var currentSkills = Skills;
            if (skillIndex < 0 || skillIndex >= currentSkills.Count) return;

            var rootData = currentSkills[skillIndex];
            if (!rootData) return;

            // 檢查條件
            if (!rootData.CheckCastReq(main.AddedPoints)) return;
            if (GetCooldown(skillIndex) > 0f) return;

            // 計算消耗
            var rootComp = SkillCalculator.Compute(rootData, main.MP);
            if (playerStats.CurrentMP < rootComp.MpCost) return;

            // 執行扣除
            playerStats.UseMP(rootComp.MpCost);
            SetCooldown(skillIndex, rootComp.Cooldown);
            if (hudSkillStats) hudSkillStats.SetSkillCooldown(skillIndex, rootComp.Cooldown, rootComp.Cooldown);

            // 開始流程
            StartCoroutine(SequenceRoutine(rootData, rootComp));
        }

        IEnumerator SequenceRoutine(SkillData rootData, SkillComputed rootComp)
        {
            // ----------------------------------------------------
            // Phase 1: 詠唱階段 (Casting)
            // ----------------------------------------------------
            IsCasting = true;

            // [應用狀態] 如果有啟用且清單不為空
            if (rootData.UseCastingStatus)
                ApplyStatusEffects(rootData.CastingStatusEffects); // ★ 改用 Helper 方法

            float timer = rootComp.CastTime;
            while (timer > 0f)
            {
                if (enabled && Time.timeScale > 0f) timer -= Time.deltaTime;
                yield return null;
            }

            IsCasting = false;

            // [解除狀態]
            if (rootData.UseCastingStatus)
                RemoveStatusEffects(rootData.CastingStatusEffects); // ★ 改用 Helper 方法

            // ----------------------------------------------------
            // Phase 2: 執行/動作階段 (Acting)
            // ----------------------------------------------------
            IsActing = true;

            // [應用狀態]
            if (rootData.UseActingStatus)
                ApplyStatusEffects(rootData.ActingStatusEffects);

            // [Snapshot] 鎖定位置與方向
            // [Snapshot] 鎖定位置與方向
            // ★ 修改：傳入 Prefab (如果有) 以決定初始錨點
            Vector3 finalOrigin = GetCurrentOrigin(rootData.ProjectilePrefab);
            Vector2 finalDir = GetCurrentAimDir();

            // 定義發射函式
            void ExecuteShot(SkillData data, SkillComputed comp)
            {
                // A. 判斷是否要更新「施放點」
                if (rootData.TrackFirePoint)
                {
                    // ★ 修改：即時更新時，也要根據該技能 Prefab 的設定來抓點
                    finalOrigin = GetCurrentOrigin(data.ProjectilePrefab);
                }

                // B. 判斷是否要更新「瞄準方向」
                if (rootData.TrackAimDirection)
                {
                    finalDir = GetCurrentAimDir();
                }

                if (executor) executor.ExecuteSkill(data, comp, finalOrigin, finalDir);
            }

            // 2. 執行第一招
            ExecuteShot(rootData, rootComp);

            // 3. 執行排程
            if (rootData.sequence != null && rootData.sequence.Count > 0)
            {
                foreach (var step in rootData.sequence)
                {
                    if (step.skill == null) continue;
                    if (step.delay > 0f) yield return new WaitForSeconds(step.delay);

                    var subComp = SkillCalculator.Compute(step.skill, main.MP);

                    // 排程中的每一發都會重新檢查追蹤設定
                    ExecuteShot(step.skill, subComp);
                }
            }
            IsActing = false;

            // [解除狀態]
            if (rootData.UseActingStatus)
                RemoveStatusEffects(rootData.ActingStatusEffects);

            // ----------------------------------------------------
            // Phase 3: 後搖/復原階段 (Recovery)
            // ----------------------------------------------------
            float recoveryTime = rootData.RecoveryTime;

            if (recoveryTime > 0f)
            {
                IsRecovery = true;

                // [應用狀態]
                if (rootData.UseRecoveryStatus)
                    ApplyStatusEffects(rootData.RecoveryStatusEffects);

                yield return new WaitForSeconds(recoveryTime);

                IsRecovery = false;

                // [解除狀態]
                if (rootData.UseRecoveryStatus)
                    RemoveStatusEffects(rootData.RecoveryStatusEffects);
            }

            // 技能流程完全結束
        }

        // ============================================================
        // 防重複裝備邏輯 (Logic) - 這是您剛加的，必須保留
        // ============================================================
        public string TryEquipSkill(int slotIndex, SkillData newSkill, int targetGroupIndex)
        {
            if (newSkill == null) { ApplyEquip(slotIndex, null, targetGroupIndex); return null; }
            if (skillGroups == null || targetGroupIndex < 0 || targetGroupIndex >= skillGroups.Count) return "錯誤索引";

            // 檢查重複
            if (slotIndex == 1) // Slot 1 副普攻
            {
                if (fixedNormalSkill == newSkill) return "與主技能 (Slot 0) 重複！";
                ApplyEquip(slotIndex, newSkill, targetGroupIndex);
            }
            else if (slotIndex == 3) // Slot 3 副大招
            {
                if (fixedUltimateSkill == newSkill) return "與主技能 (Slot 2) 重複！";
                ApplyEquip(slotIndex, newSkill, targetGroupIndex);
            }
            else if (slotIndex == 0) // Slot 0 主普攻
            {
                foreach (var group in skillGroups)
                    if (group.switchableNormal == newSkill) group.switchableNormal = null;
                ApplyEquip(slotIndex, newSkill, targetGroupIndex);
            }
            else if (slotIndex == 2) // Slot 2 主大招
            {
                foreach (var group in skillGroups)
                    if (group.switchableUltimate == newSkill) group.switchableUltimate = null;
                ApplyEquip(slotIndex, newSkill, targetGroupIndex);
            }
            return null;
        }

        private void ApplyEquip(int slotIndex, SkillData skill, int groupIdx)
        {
            var group = skillGroups[groupIdx];
            switch (slotIndex)
            {
                case 0: fixedNormalSkill = skill; break;
                case 1: group.switchableNormal = skill; break;
                case 2: fixedUltimateSkill = skill; break;
                case 3: group.switchableUltimate = skill; break;
            }
        }

        // ============================================================
        // 下面全是輔助函式 (Helper) 
        // ============================================================

        // ============================================================
        //  Helper 方法：處理 List 迴圈
        // (您需要另外實作 StatusManager 來接收這些 ScriptableObject)
        // ============================================================
        void ApplyStatusEffects(List<StatusData> effects) // ★ 類型變更
        {
            if (effects == null || statusManager == null) return;
            foreach (var effect in effects)
            {
                statusManager.Apply(effect); // 傳入 StatusData
            }
        }

        void RemoveStatusEffects(List<StatusData> effects) // ★ 類型變更
        {
            if (effects == null || statusManager == null) return;
            foreach (var effect in effects)
            {
                statusManager.Remove(effect); // 傳入 StatusData
            }
        }
        void RebuildSkillList()
        {
            _cachedSkills.Clear();
            _cachedSkills.Add(fixedNormalSkill);
            var g = (skillGroups.Count > 0) ? skillGroups[Mathf.Clamp(currentSkillGroupIndex, 0, skillGroups.Count - 1)] : null;
            _cachedSkills.Add(g?.switchableNormal);
            _cachedSkills.Add(fixedUltimateSkill);
            _cachedSkills.Add(g?.switchableUltimate);
        }

        void InitializeCooldowns()
        { /* ... 同原版 ... */
            _fixedTimers = new float[2]; _fixedMaxs = new float[2]; _groupTimers.Clear(); _groupMaxs.Clear();
            for (int i = 0; i < skillGroups.Count; i++) { _groupTimers[i] = new float[2]; _groupMaxs[i] = new float[2]; }
        }

        // ... (GetCooldown, SetCooldown, UpdateCooldowns, SwitchToNextSkillGroup, TryAutoBindHud 等等完全保持原樣) ...
        // 為了節省篇幅，這裡省略這些未改動的程式碼，請直接複製原有的內容即可。
        // 重點是移除了所有 DoArea2D, DoSingle2D, LineRenderer 變數。

        // 這裡補上必要的 Helper 以確保編譯通過
        float GetCooldown(int slotIndex)
        {
            if (slotIndex == 0) return _fixedTimers[0]; if (slotIndex == 2) return _fixedTimers[1];
            int localIdx = (slotIndex == 1) ? 0 : 1;
            if (_groupTimers.ContainsKey(currentSkillGroupIndex)) return _groupTimers[currentSkillGroupIndex][localIdx];
            return 0f;
        }
        void SetCooldown(int slotIndex, float val)
        {
            if (slotIndex == 0) { _fixedTimers[0] = val; _fixedMaxs[0] = val; return; }
            if (slotIndex == 2) { _fixedTimers[1] = val; _fixedMaxs[1] = val; return; }
            int localIdx = (slotIndex == 1) ? 0 : 1;
            if (!_groupTimers.ContainsKey(currentSkillGroupIndex)) InitializeCooldowns();
            _groupTimers[currentSkillGroupIndex][localIdx] = val; _groupMaxs[currentSkillGroupIndex][localIdx] = val;
        }
        void UpdateCooldowns()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < 2; i++) if (_fixedTimers[i] > 0f) { _fixedTimers[i] = Mathf.Max(0f, _fixedTimers[i] - dt); if (hudSkillStats) hudSkillStats.SetSkillCooldown((i == 0) ? 0 : 2, _fixedTimers[i], _fixedMaxs[i]); }
            foreach (var kvp in _groupTimers)
            {
                for (int k = 0; k < 2; k++) if (kvp.Value[k] > 0f) { kvp.Value[k] = Mathf.Max(0f, kvp.Value[k] - dt); if (kvp.Key == currentSkillGroupIndex && hudSkillStats) hudSkillStats.SetSkillCooldown((k == 0) ? 1 : 3, kvp.Value[k], _groupMaxs[kvp.Key][k]); }
            }
        }
        public void SwitchToNextSkillGroup() { if (skillGroups.Count <= 1) return; currentSkillGroupIndex = (currentSkillGroupIndex + 1) % skillGroups.Count; NotifyHudSkillGroupChanged(); }
        public void SetSkillGroupIndex(int index) { if (skillGroups.Count == 0) return; currentSkillGroupIndex = Mathf.Clamp(index, 0, skillGroups.Count - 1); NotifyHudSkillGroupChanged(); }
        void NotifyHudSkillGroupChanged() { if (hudSkillStats) { hudSkillStats.SetSkillSetIndex(currentSkillGroupIndex); hudSkillStats.SetSkillSetData(currentSkillGroupIndex, Skills); RefreshHudCooldowns(); } }
        void RefreshHudCooldowns() { if (!hudSkillStats) return; for (int i = 0; i < 4; i++) hudSkillStats.SetSkillCooldown(i, GetCooldown(i), GetMaxCooldown(i)); }
        float GetMaxCooldown(int slotIndex) { if (slotIndex == 0) return _fixedMaxs[0]; if (slotIndex == 2) return _fixedMaxs[1]; int localIdx = (slotIndex == 1) ? 0 : 1; if (_groupMaxs.ContainsKey(currentSkillGroupIndex)) return _groupMaxs[currentSkillGroupIndex][localIdx]; return 0f; }

        // 新增/刪除群組功能也保持不變
        public void AddNewSkillGroup()
        {
            if (skillGroups == null) skillGroups = new List<SkillGroup>();
            var newGroup = new SkillGroup();
            int insertIndex = (skillGroups.Count == 0) ? 0 : currentSkillGroupIndex + 1;
            if (insertIndex < skillGroups.Count) skillGroups.Insert(insertIndex, newGroup); else skillGroups.Add(newGroup);
            RefreshGroupNames(); currentSkillGroupIndex = insertIndex; InitializeCooldowns(); NotifyHudSkillGroupChanged();
        }
        public void RemoveCurrentSkillGroup()
        {
            if (skillGroups == null || skillGroups.Count <= 1) { Debug.LogWarning("至少保留一組"); return; }
            skillGroups.RemoveAt(currentSkillGroupIndex); RefreshGroupNames();
            if (currentSkillGroupIndex >= skillGroups.Count) currentSkillGroupIndex = skillGroups.Count - 1;
            InitializeCooldowns(); NotifyHudSkillGroupChanged();
        }
        private void RefreshGroupNames() { if (skillGroups == null) return; for (int i = 0; i < skillGroups.Count; i++) skillGroups[i].groupName = $"技能組 {i + 1}"; }
        void TryAutoBindHud() { if (hudSkillStats) { _hudAutoBound = true; return; } hudSkillStats = FindObjectOfType<HUDSkillStats>(); if (hudSkillStats) { _hudAutoBound = true; NotifyHudSkillGroupChanged(); } }
        void EnsureOwnerAndFirePoint() { if (!owner) owner = transform; }
        void OnValidate() 
        {
            EnsureOwnerAndFirePoint();
            // ★ 自動找 Feet
            if (feetPoint == null)
            {
                // 嘗試找名為 "Feet", "Foot", "Ground" 的子物件，或者如果有掛 Collider2D 且不是 Trigger 的通常是腳
                // 這裡簡單實作：找不到就用 owner (root)
                feetPoint = transform;
            }
        }

        // ★ 輔助方法 (如果還沒加的話記得加上)
        // ============================================================
        // ★ 新增/修改：取得發射點的邏輯
        // ============================================================

        /// <summary>
        /// 根據投射物 Prefab 的設定 (Body vs Feet) 決定生成點
        /// </summary>
        public Vector3 GetCurrentOrigin(ProjectileBase prefab)
        {
            // 預設用 Body (FirePoint)
            SpawnAnchorType anchor = SpawnAnchorType.Body;

            // 如果有 Prefab，就問它的設定
            if (prefab != null)
            {
                anchor = prefab.AnchorType;
            }

            return GetAnchorPosition(anchor);
        }

        /// <summary>
        /// 取得指定類型的座標 (公開給 AimPreview2D 用)
        /// </summary>
        public Vector3 GetAnchorPosition(SpawnAnchorType type)
        {
            if (type == SpawnAnchorType.Feet && feetPoint != null)
            {
                return feetPoint.position;
            }
            else
            {
                // Body: 優先用 executor 的 firePoint，沒有就用 transform
                if (executor && executor.firePoint) return executor.firePoint.position;
                return transform.position;
            }
        }

        // 舊方法 (為了相容性可保留，或改為呼叫上面)
        Vector3 GetCurrentFirePoint() => GetAnchorPosition(SpawnAnchorType.Body);

        Vector2 GetCurrentAimDir()
        {
            // 這裡會去抓滑鼠/搖桿的最新輸入
            return (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f) ? aimSource.AimDir : Vector2.right;
        }
    }
}