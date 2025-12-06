using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RPG
{
    // [1] 修改資料結構：每一組只包含「會隨切換變動」的那兩個技能
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
        [Header("Owner/發射者")]
        public Transform owner;
        public Transform Owner => owner ? owner : transform;

        [Header("引用")]
        public UnifiedInputSource inputSource;
        public MainPointComponent main;
        public PlayerStats playerStats;
        public AimSource2D aimSource;

        // ============================================================
        // [修改] 技能配置結構 (4 Slot 混合架構)
        // ============================================================
        [Header("常駐固定技能")]
        [Tooltip("Slot 0: 不會隨切換改變的普攻")]
        public SkillData fixedNormalSkill;
        [Tooltip("Slot 2: 不會隨切換改變的大招")]
        public SkillData fixedUltimateSkill;

        [Header("可切換技能組")]
        public List<SkillGroup> skillGroups = new List<SkillGroup>();
        public int currentSkillGroupIndex = 0;

        // 內部緩存，避免每次存取 Skills 屬性都產生垃圾
        private List<SkillData> _cachedSkills = new List<SkillData>();

        // [核心修改] 動態組出 4 個技能回傳給外部 (HUD, AimPreview)
        // 順序：0:固定普攻, 1:切換普攻, 2:固定大招, 3:切換大招
        public List<SkillData> Skills
        {
            get
            {
                RebuildSkillList();
                return _cachedSkills;
            }
        }

        // 輔助：重建清單
        void RebuildSkillList()
        {
            _cachedSkills.Clear();

            // Slot 0
            _cachedSkills.Add(fixedNormalSkill);

            // 取得當前組
            SkillGroup currentGroup = null;
            if (skillGroups != null && skillGroups.Count > 0)
            {
                int idx = Mathf.Clamp(currentSkillGroupIndex, 0, skillGroups.Count - 1);
                currentGroup = skillGroups[idx];
            }

            // Slot 1
            _cachedSkills.Add(currentGroup != null ? currentGroup.switchableNormal : null);
            // Slot 2
            _cachedSkills.Add(fixedUltimateSkill);
            // Slot 3
            _cachedSkills.Add(currentGroup != null ? currentGroup.switchableUltimate : null);
        }

        [Header("命中設定 (2D)")]
        public Transform firePoint;
        [SerializeField] private LayerMask enemyMask = 0;
        [SerializeField] private LayerMask obstacleMask = 0;
        [SerializeField] private float spawnInset = 0.05f;

        [Header("可視化")]
        public bool drawTracer = true;
        public float tracerDuration = 0.08f;
        public LineRenderer tracer;
        public LineRenderer areaRing;
        public int areaSegments = 48;

        [Header("HUD")]
        [SerializeField] private HUDSkillStats hudSkillStats;
        private bool _hudAutoBound = false;

        // ============================================================
        // 冷卻系統 (獨立追蹤)
        // ============================================================
        // 固定技能冷卻：[0]=Slot0, [1]=Slot2
        private float[] _fixedTimers = new float[2];
        private float[] _fixedMaxs = new float[2];

        // 切換技能冷卻：Key=GroupIndex, Value=[0]=Slot1, [1]=Slot3
        // 這樣即使切換到別組，原本那組的 CD 也會繼續跑
        private Dictionary<int, float[]> _groupTimers = new Dictionary<int, float[]>();
        private Dictionary<int, float[]> _groupMaxs = new Dictionary<int, float[]>();

        private bool isCasting;

        void OnValidate() { EnsureOwnerAndFirePoint(); }

        void OnEnable()
        {
            EnsureOwnerAndFirePoint();
            TryAutoBindHud();
            NotifyHudSkillGroupChanged();
        }

        void Start()
        {
            if (playerStats && playerStats.MaxMP > 0 && playerStats.CurrentMP <= 0)
                playerStats.CurrentMP = playerStats.MaxMP;

            InitializeCooldowns(); // 初始化冷卻結構
            NotifyHudSkillGroupChanged();
        }

        void EnsureOwnerAndFirePoint()
        {
            if (!owner) owner = transform;
            if (!firePoint) firePoint = owner;
        }

        // 初始化所有組的冷卻記憶體
        void InitializeCooldowns()
        {
            _fixedTimers = new float[2];
            _fixedMaxs = new float[2];
            _groupTimers.Clear();
            _groupMaxs.Clear();

            for (int i = 0; i < skillGroups.Count; i++)
            {
                _groupTimers[i] = new float[2]; // [0] for Slot 1, [1] for Slot 3
                _groupMaxs[i] = new float[2];
            }
        }

        void Update()
        {
            if (hudSkillStats == null && !_hudAutoBound) TryAutoBindHud();

            UpdateCooldowns(); // 更新計時器

            if (inputSource != null)
            {
                // [修改] 輸入對應邏輯

                // 按鈕 Attack -> 施放 Slot 0 (固定普攻)
                if (inputSource.AttackPressedThisFrame()) TryCastSkillAtIndex(0);

                // 按鈕 Attack2 -> 施放 Slot 1 (切換普攻)
                if (inputSource.Attack2PressedThisFrame()) TryCastSkillAtIndex(1);

                // 體感/點擊/按鍵 -> 施放 Slot 2 (固定大招) 或 Slot 3 (切換大招)
                // 這裡保留介面讓您可以從 UnifiedInputSource 呼叫
                // 例如： if (inputSource.IsFixedUltTriggered()) TryCastSkillAtIndex(2);
                // 例如： if (inputSource.IsSwitchUltTriggered()) TryCastSkillAtIndex(3);

                // 切換技能組
                if (inputSource.SwitchSkillGroupPressedThisFrame()) SwitchToNextSkillGroup();
            }
        }

        public void SwitchToNextSkillGroup()
        {
            if (skillGroups == null || skillGroups.Count <= 1) return;
            currentSkillGroupIndex = (currentSkillGroupIndex + 1) % skillGroups.Count;
            NotifyHudSkillGroupChanged();
        }

        public void SetSkillGroupIndex(int index)
        {
            if (skillGroups == null || skillGroups.Count == 0) return;
            currentSkillGroupIndex = Mathf.Clamp(index, 0, skillGroups.Count - 1);
            NotifyHudSkillGroupChanged();
        }

        void NotifyHudSkillGroupChanged()
        {
            if (hudSkillStats == null) return;
            hudSkillStats.SetSkillSetIndex(currentSkillGroupIndex);

            // 傳遞完整的 4 個技能給 HUD
            hudSkillStats.SetSkillSetData(currentSkillGroupIndex, Skills);

            // 立即刷新 HUD 上的冷卻顯示 (因為 Slot 1/3 的內容和冷卻可能變了)
            RefreshHudCooldowns();
        }

        void RefreshHudCooldowns()
        {
            if (hudSkillStats == null) return;
            for (int i = 0; i < 4; i++)
            {
                float current = GetCooldown(i);
                float max = GetMaxCooldown(i);
                hudSkillStats.SetSkillCooldown(i, current, max);
            }
        }

        //===================================
        //  新增以及刪除
        //===================================
        // [新增] 建立一個全新的空白技能組
        public void AddNewSkillGroup()
        {
            if (skillGroups == null) skillGroups = new List<SkillGroup>();
            var newGroup = new SkillGroup();

            // 1. 計算插入位置：插在當前索引的「下一格」
            // 如果清單是空的，就插在 0
            int insertIndex = (skillGroups.Count == 0) ? 0 : currentSkillGroupIndex + 1;

            // 2. 執行插入 (Insert)
            if (insertIndex < skillGroups.Count)
            {
                skillGroups.Insert(insertIndex, newGroup);
            }
            else
            {
                // 如果當前已經是最後一個，就直接加在最後
                skillGroups.Add(newGroup);
            }

            // 3. 重新命名所有組別 (1, 2, 3...) 確保順序名稱正確
            RefreshGroupNames();

            // 4. 自動切換到這個新建立的組別 (就在下一頁)
            currentSkillGroupIndex = insertIndex;

            // 初始化這一組的冷卻計時器 (重要！否則報錯)
            InitializeCooldowns();

            // 通知 HUD 更新
            NotifyHudSkillGroupChanged();
        }

        // [新增] 刪除當前選中的技能組
        public void RemoveCurrentSkillGroup()
        {
            if (skillGroups == null || skillGroups.Count <= 1)
            {
                Debug.LogWarning("至少要保留一個技能組，無法刪除！");
                return;
            }

            // 移除當前索引的組
            skillGroups.RemoveAt(currentSkillGroupIndex);

            // [新增] 每次刪除後，重新命名所有組別 (自動往前補)
            RefreshGroupNames();

            // 修正索引：如果刪的是最後一個 (index 2)，刪完後剩 2 個 (max index 1)，需倒退
            if (currentSkillGroupIndex >= skillGroups.Count)
            {
                currentSkillGroupIndex = skillGroups.Count - 1;
            }

            // 重建冷卻計時器結構
            InitializeCooldowns();

            // 通知 HUD 更新
            NotifyHudSkillGroupChanged();
        }

        // [新增] 輔助方法：將所有群組依序重新命名
        private void RefreshGroupNames()
        {
            if (skillGroups == null) return;
            for (int i = 0; i < skillGroups.Count; i++)
            {
                // 自動設為 "技能組 1", "技能組 2", ...
                skillGroups[i].groupName = $"技能組 {i + 1}";
            }
        }

        // ============================================================
        // 冷卻管理 (區分固定/切換)
        // ============================================================
        // 取得特定 Slot 的剩餘冷卻
        float GetCooldown(int slotIndex)
        {
            if (slotIndex == 0) return _fixedTimers[0]; // Slot 0
            if (slotIndex == 2) return _fixedTimers[1]; // Slot 2

            // 切換技能 (1, 3)
            int localIdx = (slotIndex == 1) ? 0 : 1; // 映射: Slot 1->0, Slot 3->1
            if (_groupTimers.ContainsKey(currentSkillGroupIndex))
                return _groupTimers[currentSkillGroupIndex][localIdx];
            return 0f;
        }

        float GetMaxCooldown(int slotIndex)
        {
            if (slotIndex == 0) return _fixedMaxs[0];
            if (slotIndex == 2) return _fixedMaxs[1];

            int localIdx = (slotIndex == 1) ? 0 : 1;
            if (_groupMaxs.ContainsKey(currentSkillGroupIndex))
                return _groupMaxs[currentSkillGroupIndex][localIdx];
            return 0f;
        }

        // 設定特定 Slot 的冷卻
        void SetCooldown(int slotIndex, float val)
        {
            if (slotIndex == 0) { _fixedTimers[0] = val; _fixedMaxs[0] = val; return; }
            if (slotIndex == 2) { _fixedTimers[1] = val; _fixedMaxs[1] = val; return; }

            int localIdx = (slotIndex == 1) ? 0 : 1;
            if (!_groupTimers.ContainsKey(currentSkillGroupIndex))
                InitializeCooldowns(); // 防呆

            _groupTimers[currentSkillGroupIndex][localIdx] = val;
            _groupMaxs[currentSkillGroupIndex][localIdx] = val;
        }

        // 每幀更新所有計時器
        void UpdateCooldowns()
        {
            float dt = Time.deltaTime;

            // 1. 更新固定技能
            for (int i = 0; i < 2; i++)
            {
                if (_fixedTimers[i] > 0f)
                {
                    _fixedTimers[i] = Mathf.Max(0f, _fixedTimers[i] - dt);
                    // Slot 0 或 2 更新 UI
                    int slot = (i == 0) ? 0 : 2;
                    if (hudSkillStats) hudSkillStats.SetSkillCooldown(slot, _fixedTimers[i], _fixedMaxs[i]);
                }
            }

            // 2. 更新所有組的切換技能 (後台也要跑 CD)
            foreach (var kvp in _groupTimers)
            {
                int gIdx = kvp.Key;
                float[] timers = kvp.Value;
                float[] maxs = _groupMaxs[gIdx];

                for (int k = 0; k < 2; k++) // k=0 -> Slot1, k=1 -> Slot3
                {
                    if (timers[k] > 0f)
                    {
                        timers[k] = Mathf.Max(0f, timers[k] - dt);
                        // 只有「當前組」才更新 UI
                        if (gIdx == currentSkillGroupIndex && hudSkillStats)
                        {
                            int slot = (k == 0) ? 1 : 3;
                            hudSkillStats.SetSkillCooldown(slot, timers[k], maxs[k]);
                        }
                    }
                }
            }
        }

        void TryAutoBindHud()
        {
            if (hudSkillStats != null) { _hudAutoBound = true; return; }
            hudSkillStats = FindObjectOfType<HUDSkillStats>();
            if (hudSkillStats != null) { _hudAutoBound = true; NotifyHudSkillGroupChanged(); }
        }

        // ============================================================
        // 施法邏輯 (整合)
        // ============================================================
        public void TryCastSkillAtIndex(int skillIndex)
        {
            if (!enabled || isCasting || !main || !playerStats) return;

            // 從目前的 4 個技能中獲取資料
            var currentSkills = Skills;
            if (skillIndex < 0 || skillIndex >= currentSkills.Count) return;

            var rootData = currentSkills[skillIndex];
            if (!rootData) return;

            // 1. 檢查與消耗 (使用新的 CheckCastReq)
            if (!rootData.CheckCastReq(main.AddedPoints)) return;
            if (GetCooldown(skillIndex) > 0f) return;

            // 2. 計算消耗 (只看 Root)
            var rootComp = SkillCalculator.Compute(rootData, main.MP);
            if (playerStats.CurrentMP < rootComp.MpCost) return;

            playerStats.UseMP(rootComp.MpCost);
            SetCooldown(skillIndex, rootComp.Cooldown);
            if (hudSkillStats) hudSkillStats.SetSkillCooldown(skillIndex, rootComp.Cooldown, rootComp.Cooldown);

            // 3. 啟動排程
            StartCoroutine(SequenceRoutine(rootData, rootComp));
        }

        IEnumerator SequenceRoutine(SkillData rootData, SkillComputed rootComp)
        {
            // [Snapshot] 鎖定當下位置與方向
            // 所有子技能都將使用這個起始點，不會隨玩家移動而改變
            Vector3 startOrigin = firePoint ? firePoint.position : transform.position;
            Vector2 startDir = GetDir();

            // ==========================
            // Phase 1: 詠唱 (Cast Time)
            // ==========================
            isCasting = true; // 鎖住玩家行動

            float timer = rootComp.CastTime;
            while (timer > 0f)
            {
                if (enabled && Time.timeScale > 0f) timer -= Time.deltaTime;
                yield return null;
            }

            isCasting = false; // 解鎖玩家行動

            // ==========================
            // Phase 2: 執行第一招 (Root)
            // ==========================
            ExecuteAction(rootData, rootComp, startOrigin, startDir);

            // ==========================
            // Phase 3: 執行排程 (Sequence)
            // ==========================
            if (rootData.sequence != null && rootData.sequence.Count > 0)
            {
                foreach (var step in rootData.sequence)
                {
                    if (step.skill == null) continue;

                    // 等待延遲
                    if (step.delay > 0f)
                        yield return new WaitForSeconds(step.delay);

                    // 重新計算子技能數值 (使用當下的 main.MP)
                    var subComp = SkillCalculator.Compute(step.skill, main.MP);

                    // 執行子技能 (使用 Snapshot 的位置與方向)
                    ExecuteAction(step.skill, subComp, startOrigin, startDir);
                }
            }
        }

        // 統一執行入口：根據 HitType 分派
        void ExecuteAction(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            if (data.HitType == HitType.Area)
                DoArea2D(data, comp, origin, dir);
            else if (data.HitType == HitType.Cone)
                DoCone2D(data, comp, origin, dir);
            else
                DoSingle2D(data, comp, origin, dir);
        }
        /*
        IEnumerator CastRoutine(SkillData data, SkillComputed comp, int skillIndex)
        {
            isCasting = true;
            // 前搖
            float timer = comp.CastTime;
            while (timer > 0f)
            {
                if (enabled && Time.timeScale > 0f) timer -= Time.deltaTime;
                yield return null;
            }

            // 執行判定
            if (data.HitType == HitType.Area) DoArea2D(data, comp);
            else if (data.HitType == HitType.Cone) DoCone2D(data, comp);
            else DoSingle2D(data, comp);

            isCasting = false;
        }
        */

        // ============================================================
        // 物理判定 (稍微修改以接受傳入的 origin 和 dir)
        // ============================================================

        void DoSingle2D(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            if (data.UseProjectile && data.ProjectilePrefab)
            {
                var spawnPos = origin + (Vector3)(dir * spawnInset);

                GameObject obj;
                if (ObjectPool.Instance != null)
                    obj = ObjectPool.Instance.Spawn(data.ProjectilePrefab.gameObject, spawnPos, Quaternion.identity);
                else
                    obj = Instantiate(data.ProjectilePrefab.gameObject, spawnPos, Quaternion.identity);

                var proj = obj.GetComponent<Projectile2D>();
                if (proj) proj.Init(Owner, dir, data, comp, enemyMask, obstacleMask);
            }
            else
            {
                DoSingle2D_LegacyRay(data, comp, origin, dir);
            }
        }

        void DoSingle2D_LegacyRay(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            float dist = Mathf.Max(0.1f, data.BaseRange);
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, enemyMask | obstacleMask);

            Vector3 endPos = origin + (Vector3)(dir * dist);

            if (hit.collider != null)
            {
                endPos = hit.point;
                if (EffectApplier.TryResolveOwner(hit.collider, out var target, out var layer))
                {
                    if (data.TargetLayer == layer)
                        target.ApplyIncomingRaw(comp.Damage);
                }
            }
            FlashLine(origin, endPos);
        }

        void DoArea2D(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            // 計算中心點：從 origin 往 dir 延伸 BaseRange 距離
            Vector2 center = (Vector2)origin + dir * Mathf.Max(0.1f, data.BaseRange);
            float r = Mathf.Max(0.05f, comp.AreaRadius);

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, r, enemyMask);
            foreach (var h in hits)
            {
                if (EffectApplier.TryResolveOwner(h, out var target, out var layer))
                    if (data.TargetLayer == layer) target.ApplyIncomingRaw(comp.Damage);
            }
            if (areaRing) StartCoroutine(FlashCircle(areaRing, center, r, tracerDuration));
        }

        void DoCone2D(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            float dist = Mathf.Max(0.1f, data.BaseRange);
            float angle = comp.ConeAngle;

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, dist, enemyMask);
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(Owner)) continue;
                Vector2 tDir = (h.bounds.center - origin);
                if (Vector2.Angle(dir, tDir) <= angle * 0.5f)
                {
                    if (EffectApplier.TryResolveOwner(h, out var target, out var layer))
                        if (data.TargetLayer == layer) target.ApplyIncomingRaw(comp.Damage);
                }
            }
        }
        Vector2 GetDir() { return (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f) ? aimSource.AimDir : Vector2.right; }
        void FlashLine(Vector3 a, Vector3 b) { if (!drawTracer || !tracer) return; tracer.positionCount = 2; tracer.SetPosition(0, a); tracer.SetPosition(1, b); StopCoroutine("LineRoutine"); StartCoroutine("LineRoutine"); }
        IEnumerator LineRoutine() { tracer.enabled = true; yield return new WaitForSeconds(tracerDuration); tracer.enabled = false; }
        IEnumerator FlashCircle(LineRenderer lr, Vector3 center, float radius, float dur)
        {
            if (!lr) yield break; int segs = Mathf.Max(6, areaSegments); lr.positionCount = segs + 1;
            float step = 2f * Mathf.PI / segs;
            for (int i = 0; i <= segs; i++) lr.SetPosition(i, center + new Vector3(Mathf.Cos(i * step) * radius, Mathf.Sin(i * step) * radius, 0));
            lr.enabled = true; yield return new WaitForSeconds(dur); lr.enabled = false;
        }
    }
}