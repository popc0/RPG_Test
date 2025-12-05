using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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

            var data = currentSkills[skillIndex];
            if (!data) return; // 空技能槽

            // 1. 檢查屬性
            if (!data.MeetsRequirement(main.AddedPoints)) return;

            // 2. 檢查冷卻 (使用新方法)
            if (GetCooldown(skillIndex) > 0f) return;

            // 3. 計算與扣魔
            var comp = SkillCalculator.Compute(data, main.MP);
            if (playerStats.CurrentMP < comp.MpCost) return;
            playerStats.UseMP(comp.MpCost);

            // 4. 設定冷卻 (使用新方法)
            SetCooldown(skillIndex, comp.Cooldown);
            if (hudSkillStats) hudSkillStats.SetSkillCooldown(skillIndex, comp.Cooldown, comp.Cooldown);

            // 5. 開始施法
            StartCoroutine(CastRoutine(data, comp, skillIndex));
        }

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

        // ============================================================
        // 物理判定 Helper (保留原樣)
        // ============================================================
        void DoSingle2D(SkillData data, SkillComputed comp)
        {
            var origin = firePoint ? firePoint.position : Owner.position;
            var dir = GetDir();
            if (data.UseProjectile && data.ProjectilePrefab)
            {
                var spawnPos = origin + (Vector3)(dir * spawnInset);
                GameObject obj = (ObjectPool.Instance != null)
                   ? ObjectPool.Instance.Spawn(data.ProjectilePrefab.gameObject, spawnPos, Quaternion.identity)
                   : Instantiate(data.ProjectilePrefab.gameObject, spawnPos, Quaternion.identity);
                var proj = obj.GetComponent<Projectile2D>();
                if (proj) proj.Init(Owner, dir, data, comp, enemyMask, obstacleMask);
            }
            else { DoSingle2D_LegacyRay(data, comp); }
        }
        void DoSingle2D_LegacyRay(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : Owner.position;
            Vector2 dir = GetDir(); float dist = Mathf.Max(0.1f, data.BaseRange);
            RaycastHit2D hit = Physics2D.Raycast(origin3, dir, dist, enemyMask | obstacleMask);
            if (hit.collider) FlashLine(origin3, hit.point); else FlashLine(origin3, origin3 + (Vector3)(dir * dist));
        }
        void DoArea2D(SkillData data, SkillComputed comp)
        {
            Vector3 o = firePoint ? firePoint.position : Owner.position; Vector2 dir = GetDir();
            Vector2 c = (Vector2)o + dir * Mathf.Max(0.1f, data.BaseRange);
            float r = Mathf.Max(0.05f, comp.AreaRadius);
            Collider2D[] hits = Physics2D.OverlapCircleAll(c, r, enemyMask);
            foreach (var h in hits) { if (EffectApplier.TryResolveOwner(h, out var target, out var layer)) if (data.TargetLayer == layer) target.ApplyIncomingRaw(comp.Damage); }
            if (areaRing) StartCoroutine(FlashCircle(areaRing, c, r, tracerDuration));
        }
        void DoCone2D(SkillData data, SkillComputed comp)
        {
            Vector3 o = firePoint ? firePoint.position : Owner.position; Vector2 dir = GetDir();
            float dist = Mathf.Max(0.1f, data.BaseRange); float angle = comp.ConeAngle;
            Collider2D[] hits = Physics2D.OverlapCircleAll(o, dist, enemyMask);
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(Owner)) continue;
                Vector2 tDir = (h.bounds.center - o);
                if (Vector2.Angle(dir, tDir) <= angle * 0.5f)
                    if (EffectApplier.TryResolveOwner(h, out var target, out var layer)) if (data.TargetLayer == layer) target.ApplyIncomingRaw(comp.Damage);
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