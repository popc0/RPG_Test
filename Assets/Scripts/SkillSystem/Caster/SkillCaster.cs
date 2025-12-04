using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; // 使用 Unity 新版輸入系統

namespace RPG
{
    // [1] 資料結構：用於在 Inspector 中定義「一組」技能。
    // [System.Serializable] 確保這個自定義類別的 public 欄位可以在 Unity 編輯器中顯示和編輯。
    [System.Serializable]
    public class SkillGroup
    {
        public string groupName = "技能組"; // 技能組的名稱（例如：近戰組、遠程組）
        public List<SkillData> skills = new List<SkillData>(); // 該技能組包含的 SkillData 實例清單
    }

    // [2] 核心控制器：掛在玩家物件上，負責執行所有技能邏輯。
    [DisallowMultipleComponent]
    public class SkillCaster : MonoBehaviour
    {
        [Header("Owner/發射者")]
        public Transform owner;
        public Transform Owner => owner ? owner : transform; // 獲取施法者物件的 Transform

        [Header("引用 (數據和控制來源)")]
        //  新增：引用 IInputSource 介面
        public UnifiedInputSource inputSource;
        public MainPointComponent main; // 引用玩家的主要屬性（Attack, Agility等），用於 SkillCalculator
        public PlayerStats playerStats; // 引用玩家狀態（HP, MP），用於檢查消耗和執行扣魔
        public AimSource2D aimSource; // 引用瞄準方向（例如滑鼠位置或搖桿推的方向）

        // ============================================================
        // 技能數據存取 (Data Reading)
        // ============================================================
        [Header("技能組清單")]
        public List<SkillGroup> skillGroups = new List<SkillGroup>(); // 儲存所有可切換的 SkillGroup

        public int currentSkillGroupIndex = 0; // 當前正在使用的技能組索引 (Key for skillGroups)
        //  【新增欄位】：定義兩個按鈕分別對應的技能槽索引
        [Header("施法槽位配置")]
        [Tooltip("按鈕 1 對應當前技能組清單的索引")]
        public int skillSlotIndex1 = 0; // 對應技能組的第一個技能
        [Tooltip("按鈕 2 對應當前技能組清單的索引")]
        public int skillSlotIndex2 = 1; // 對應技能組的第二個技能
        // 屬性：這是程式碼**解讀資料的第一步**。根據當前索引，動態返回對應的 SkillData 清單。
        public List<SkillData> Skills
        {
            get
            {
                if (skillGroups == null || skillGroups.Count == 0) return null;
                // 確保索引在安全範圍內
                int idx = Mathf.Clamp(currentSkillGroupIndex, 0, skillGroups.Count - 1);
                // 返回選定技能組內的 SkillData 清單
                return skillGroups[idx].skills;
            }
        }

        [Header("命中設定 (2D)")]
        public Transform firePoint;
        [SerializeField] private LayerMask enemyMask = 0;
        [SerializeField] private LayerMask obstacleMask = 0;
        [SerializeField] private float spawnInset = 0.05f;

        [Header("可視化（引用既有 LR）")]
        public bool drawTracer = true;
        public float tracerDuration = 0.08f;
        public LineRenderer tracer; // 用於射線/彈道的視覺線條
        public LineRenderer areaRing; // 用於範圍技能的圓圈預覽
        public int areaSegments = 48;

        // ============================================================
        // HUD 引用與冷卻數據 (Data State)
        // ============================================================
        [Header("HUD 冷卻顯示")]
        [SerializeField] private HUDSkillStats hudSkillStats; // UI 顯示組件的引用
        private bool _hudAutoBound = false; // 自動綁定旗標

        // 冷卻字典：使用組索引作為 Key 來分離不同技能組的冷卻狀態
        private Dictionary<int, float[]> cooldownTimersPerGroup = new Dictionary<int, float[]>(); // 儲存各組技能的「剩餘冷卻秒數」
        private Dictionary<int, float[]> cooldownMaxPerGroup = new Dictionary<int, float[]>();   // 儲存各組技能的「總冷卻秒數」（用於 UI 比例）

        private bool isCasting; // 施法狀態旗標

        // ============================================================
        // Unity 生命週期與 Update 流程 (Execution Flow)
        // ============================================================
        void OnValidate() { EnsureOwnerAndFirePoint(); }

        void OnEnable()
        {
            // 啟用時：確保當前技能組的冷卻陣列已準備好
            EnsureOwnerAndFirePoint();

            TryAutoBindHud();
            NotifyHudSkillGroupChanged(); // 通知 UI 刷新當前技能組的圖示
        }

        void OnDisable()
        {
        }

        void EnsureOwnerAndFirePoint()
        {
            if (!owner) owner = transform;
            if (!firePoint) firePoint = owner;
        }
        void InitializeAllCooldowns()
        {
            // 遍歷所有在 Inspector 中設定的 SkillGroup
            for (int groupIndex = 0; groupIndex < skillGroups.Count; groupIndex++)
            {
                var group = skillGroups[groupIndex];
                int n = Mathf.Max(1, group.skills?.Count ?? 0);

                // 確保 Timer 陣列存在並重新設定大小
                // 陣列預設值為 0，確保冷卻是空的。
                cooldownTimersPerGroup[groupIndex] = new float[n];
                cooldownMaxPerGroup[groupIndex] = new float[n];
            }
        }

        void Start()
        {
            // 初始化魔力
            if (playerStats && playerStats.MaxMP > 0 && playerStats.CurrentMP <= 0)
                playerStats.CurrentMP = playerStats.MaxMP;

            //  呼叫新的方法：在遊戲開始時，預先初始化所有技能組的冷卻陣列
            InitializeAllCooldowns();

            NotifyHudSkillGroupChanged();
        }

        void Update()
        {
            // 檢查 HUD 綁定
            if (hudSkillStats == null && !_hudAutoBound)
                TryAutoBindHud();

            // [流程 1] 處理所有冷卻時間並同步到 UI
            UpdateCooldowns();

            //  [流程 2] 偵測施法輸入：使用 Attack/Attack2 方法
            if (inputSource != null && inputSource.AttackPressedThisFrame())
            {
                // 嘗試施放第一個槽位的技能
                TryCastSkillAtIndex(skillSlotIndex1);
            }

            if (inputSource != null && inputSource.Attack2PressedThisFrame())
            {
                // 嘗試施放第二個槽位的技能
                TryCastSkillAtIndex(skillSlotIndex2);
            }

            //  [流程 3] 偵測切換技能組輸入：使用 SwitchSkillGroupPressedThisFrame()
            if (inputSource != null && inputSource.SwitchSkillGroupPressedThisFrame())
            {
                SwitchToNextSkillGroup(); // 執行切換組邏輯
            }
        }

        // ============================================================
        // 技能組切換邏輯
        // ============================================================
        // 切換到下一個技能組
        public void SwitchToNextSkillGroup()
        {
            if (skillGroups == null || skillGroups.Count <= 1) return;

            // 循環計算下一個索引 (0 -> 1 -> 2 -> 0)
            currentSkillGroupIndex = (currentSkillGroupIndex + 1) % skillGroups.Count;

            NotifyHudSkillGroupChanged(); // 通知 HUD 顯示新的技能組
        }

        // 手動設定技能組索引
        public void SetSkillGroupIndex(int index)
        {
            if (skillGroups == null || skillGroups.Count == 0) return;
            currentSkillGroupIndex = Mathf.Clamp(index, 0, skillGroups.Count - 1);
            NotifyHudSkillGroupChanged();
        }

        // 資料輸出：通知 HUD 更新技能組名稱/圖示
        void NotifyHudSkillGroupChanged()
        {
            if (hudSkillStats == null) return;

            // 1. 通知 HUD 切換到哪個索引 (原邏輯，用於更新 Label 名稱)
            hudSkillStats.SetSkillSetIndex(currentSkillGroupIndex);

            // 2. 獲取當前技能組的 SkillData 清單 (從 Skills 屬性中獲取)
            var skillsList = Skills;

            if (skillsList != null)
            {
                //  新增：呼叫 HUDSkillStats 的新方法，傳遞數據清單
                hudSkillStats.SetSkillSetData(currentSkillGroupIndex, skillsList);
            }
        }

        // ============================================================
        // 冷卻系統 (Data State Management)
        // ============================================================
        // 冷卻陣列初始化和大小檢查 (確保陣列大小總是匹配當前 SkillGroup 的技能數量)
        void EnsureCooldownArrayForCurrentGroup()
        {
            var skills = Skills; // 讀取當前組的 SkillData 清單
            int n = Mathf.Max(1, skills?.Count ?? 0); // 獲取技能數量

            // 檢查並創建/重設 Timer 陣列
            if (!cooldownTimersPerGroup.ContainsKey(currentSkillGroupIndex) ||
                cooldownTimersPerGroup[currentSkillGroupIndex].Length != n)
                cooldownTimersPerGroup[currentSkillGroupIndex] = new float[n];

            // 檢查並創建/重設 Max 陣列
            if (!cooldownMaxPerGroup.ContainsKey(currentSkillGroupIndex) ||
                cooldownMaxPerGroup[currentSkillGroupIndex].Length != n)
                cooldownMaxPerGroup[currentSkillGroupIndex] = new float[n];
        }

        // 每幀更新冷卻時間並將資料同步到 UI
        void UpdateCooldowns()
        {
            // 儲存當前技能組索引，避免在迴圈中重複讀取
            int currentGroup = currentSkillGroupIndex;

            //  遍歷所有技能組的冷卻計時器 (字典中的 Key-Value Pair)
            foreach (var entry in cooldownTimersPerGroup)
            {
                int groupIndex = entry.Key; // 正在處理的技能組索引
                var timers = entry.Value;   // 該組的剩餘冷卻時間陣列

                // 嘗試安全地獲取 Max 陣列 (Max 陣列用於 HUD 計算比例，且與 timers 大小相同)
                if (!cooldownMaxPerGroup.TryGetValue(groupIndex, out var maxArr))
                {
                    // 如果 Max 陣列不存在，則跳過該組的處理
                    continue;
                }

                bool isCurrentGroup = (groupIndex == currentGroup);

                for (int i = 0; i < timers.Length; i++)
                {
                    // 核心邏輯：所有組的冷卻時間都必須減少 (時間減少不會因為組別不同而停止)
                    if (timers[i] > 0f)
                        timers[i] = Mathf.Max(0f, timers[i] - Time.deltaTime);

                    // [資料輸出] 只有當前激活的技能組需要通知 HUD 更新
                    if (isCurrentGroup && hudSkillStats != null && maxArr[i] > 0f)
                    {
                        // 將冷卻狀態推送給 HUD UI
                        // i: 技能槽索引 (0, 1, 2...)
                        // timers[i]: 當前剩餘時間
                        // maxArr[i]: 總冷卻時間
                        hudSkillStats.SetSkillCooldown(i, timers[i], maxArr[i]);
                    }
                }
            }
        }

        // ============================================================
        // HUD 引用
        // ============================================================
        void TryAutoBindHud()
        {
            if (hudSkillStats != null)
            {
                _hudAutoBound = true;
                return;
            }

            // 嘗試使用 FindObjectOfType 尋找 UI 實例
            hudSkillStats = FindObjectOfType<HUDSkillStats>();
            if (hudSkillStats != null)
            {
                _hudAutoBound = true;
                NotifyHudSkillGroupChanged(); // 找到後立即更新 UI 顯示
            }
        }

        // ============================================================
        // 施放技能核心邏輯 (Data Application Flow)
        // ============================================================
        // [新方法] 施放指定索引的技能
        public void TryCastSkillAtIndex(int skillIndex) // <--- 注意這裡的參數
        {
            //  新增：檢查 SkillCaster 腳本是否正在啟用
            // 因為 PlayerPauseAgent 應該會將此腳本的 enabled 設為 false
            if (!enabled)
            {
                Debug.Log($"[SkillCaster] Denied cast attempt while paused.");
                return; // 在暫停時，直接返回，不執行任何後續邏輯
            }
            if (isCasting) return;
            if (!main || !playerStats) return;

            var skills = Skills;
            if (skills == null || skills.Count == 0) return;

            // 檢查傳入的索引是否有效
            if (skillIndex < 0 || skillIndex >= skills.Count) return;

            // [資料讀取] 獲取指定索引的 SkillData 藍圖
            var data = skills[skillIndex]; // <--- 使用 skillIndex
            if (!data) return;

            // ---------------------------------------------------------
            // [新增] 檢查屬性門檻 (只看加點值，不含基礎值)
            // ---------------------------------------------------------
            // 使用 main.AddedPoints (我們上一輪加的屬性)
            if (!data.MeetsRequirement(main.AddedPoints))
            {
                Debug.Log($"[SkillCaster] 屬性不足，無法施放 {data.SkillName}");
                return;
            }
            // ---------------------------------------------------------

            EnsureCooldownArrayForCurrentGroup();
            var timers = cooldownTimersPerGroup[currentSkillGroupIndex];

            // [資料檢查] 檢查冷卻
            if (timers[skillIndex] > 0f) return; // <--- 使用 skillIndex

            // [資料計算] 核心步驟：計算最終執行數值
            var comp = SkillCalculator.Compute(data, main.MP);

            // [資料檢查] 檢查魔力消耗
            if (playerStats.CurrentMP < comp.MpCost) return;
            playerStats.UseMP(comp.MpCost); // 執行扣魔

            var maxArr = cooldownMaxPerGroup[currentSkillGroupIndex];

            // [資料應用] 設定新的冷卻時間
            timers[skillIndex] = comp.Cooldown;     // <--- 使用 skillIndex
            maxArr[skillIndex] = comp.Cooldown;     // <--- 使用 skillIndex

            // [資料輸出] 通知 HUD 該技能槽的冷卻已啟動
            if (hudSkillStats != null)
                hudSkillStats.SetSkillCooldown(skillIndex, timers[skillIndex], maxArr[skillIndex]); // <--- 使用 skillIndex

            // 執行施法協程
            // 注意：協程需要知道是哪個槽位在施法，所以傳入 skillIndex
            StartCoroutine(CastRoutine(data, comp, skillIndex));
        }

        // 施法流程協程
        // 施法流程協程 (修改點 5: 增加 skillIndex 參數)
        IEnumerator CastRoutine(SkillData data, SkillComputed comp, int skillIndex)
        {
            isCasting = true;
            //  關鍵修改點 1：先等待施法前搖時間
            float castTimer = comp.CastTime;

            while (castTimer > 0f)
            {
                //  關鍵修改點 2：在等待期間，檢查是否處於暫停狀態
                // 只有當腳本啟用且 timeScale > 0 時，才減少時間。
                if (enabled && Time.timeScale > 0f)
                {
                    castTimer -= Time.deltaTime;
                }

                // 繼續下一幀
                yield return null;
            }
            // 施法狀態旗標：通知 UI 開始顯示詠唱狀態 
            // hudSkillStats.SetSkillCasting(skillIndex, true); // <--- 使用 skillIndex



            // [資料應用] 施法前搖延遲
            if (comp.CastTime > 0f)
                yield return new WaitForSeconds(comp.CastTime);

            // 施法狀態旗標：通知 UI 詠唱結束
            // hudSkillStats.SetSkillCasting(skillIndex, false); // <--- 使用 skillIndex

            // [資料應用] 根據 SkillData 的 HitType 決定執行哪種物理判定
            if (data.HitType == HitType.Area)
                DoArea2D(data, comp);
            else if (data.HitType == HitType.Cone) // ★ 新增：扇形
                DoCone2D(data, comp);
            else
                DoSingle2D(data, comp);


            isCasting = false;
        }

        // 單體攻擊邏輯 (投射物或射線)
        void DoSingle2D(SkillData data, SkillComputed comp)
        {
            var origin = firePoint ? firePoint.position : Owner.position;
            var dir = GetDir();

            // [資料應用] 判斷是否使用投射物 Prefab
            if (data.UseProjectile && data.ProjectilePrefab)
            {
                var spawnPos = origin + (Vector3)(dir * spawnInset);

                // ⭐ 將 Instantiate 替換為 ObjectPool.Instance.Spawn ⭐
                GameObject obj;
                if (ObjectPool.Instance != null)
                {
                    // 使用物件池生成
                    obj = ObjectPool.Instance.Spawn(data.ProjectilePrefab.gameObject, spawnPos, Quaternion.identity);
                }
                else
                {
                    // 備用：如果沒有物件池，則傳統生成
                    obj = Instantiate(data.ProjectilePrefab.gameObject, spawnPos, Quaternion.identity);
                }

                // 獲取 Projectile2D 組件
                var proj = obj.GetComponent<Projectile2D>();

                // 初始化投射物，將 SkillData 和計算結果 (comp) 傳給投射物
                if (proj != null)
                {
                    proj.Init(Owner, dir, data, comp, enemyMask, obstacleMask);
                }
            }
            else
            {
                // 執行射線判定
                DoSingle2D_LegacyRay(data, comp);
            }
        }

        // 射線偵測傷害邏輯
        void DoSingle2D_LegacyRay(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : Owner.position;
            Vector2 dir = GetDir();
            float dist = Mathf.Max(0.1f, data.BaseRange); // 使用 SkillData 的基礎射程
            int mask = enemyMask | obstacleMask;

            RaycastHit2D hit = Physics2D.Raycast(origin3, dir, dist, mask);

            // 視覺效果
            if (hit.collider != null)
            {
                FlashLine(origin3, hit.point);
                return;
            }

            FlashLine(origin3, origin3 + (Vector3)(dir * dist));
        }

        // 範圍攻擊邏輯 (AoE)
        void DoArea2D(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : Owner.position;
            Vector2 dir = GetDir();
            float dist = Mathf.Max(0.1f, data.BaseRange);

            Vector2 center = (Vector2)origin3 + dir * dist;
            RaycastHit2D hit = Physics2D.Raycast(origin3, dir, dist, enemyMask | obstacleMask);
            if (hit.collider != null) center = hit.point;

            // [資料應用] 使用計算後的範圍 comp.AreaRadius
            float radius = Mathf.Max(0.05f, comp.AreaRadius);
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyMask);

            // 遍歷所有命中的目標，應用傷害
            foreach (var c in hits)
            {
                // 傷害處理邏輯 (使用計算後的 comp.Damage)
                if (EffectApplier.TryResolveOwner(c, out var ownerApplier, out var hitLayer))
                {
                    if (data.TargetLayer == hitLayer) // 檢查目標圖層
                        ownerApplier.ApplyIncomingRaw(comp.Damage);
                }
            }

            // 顯示圓圈特效
            if (areaRing)
                StartCoroutine(FlashCircle(areaRing, center, radius, tracerDuration));
        }

        // 扇形攻擊邏輯 (Cone)
        void DoCone2D(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : Owner.position;
            Vector2 dir = GetDir();
            float dist = Mathf.Max(0.1f, data.BaseRange); // 扇形半徑
            float angle = comp.ConeAngle; // 扇形角度

            // 1. 執行 AoE 圓形檢測，獲取所有潛在目標
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin3, dist, enemyMask);

            // 2. 遍歷目標，檢查是否落在扇形角度內
            foreach (var c in hits)
            {
                if (c.transform.IsChildOf(Owner)) continue;

                Vector2 targetDir = (c.bounds.center - origin3); // 目標中心點到原點的方向
                if (targetDir.sqrMagnitude < 0.001f) continue; // 忽略太近的目標

                // 計算目標方向與施法方向之間的夾角
                float angleToTarget = Vector2.Angle(dir, targetDir);

                // 如果目標在扇形角度的一半範圍內，則命中
                if (angleToTarget <= angle * 0.5f)
                {
                    // 傷害處理邏輯 (使用計算後的 comp.Damage)
                    if (EffectApplier.TryResolveOwner(c, out var ownerApplier, out var hitLayer))
                    {
                        if (data.TargetLayer == hitLayer) // 檢查目標圖層
                            ownerApplier.ApplyIncomingRaw(comp.Damage);
                    }
                }
            }

            // 視覺效果（如果需要，可以在這裡加入臨時特效，例如粒子系統）
            // 由於 AimPreview2D 負責預覽，執行時我們通常使用一次性特效。
        }

        // 獲取瞄準方向
        Vector2 GetDir()
        {
            if (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f)
                return aimSource.AimDir;
            return Vector2.right; // 無瞄準方向時，預設朝右
        }

        // 畫線條（視覺輔助）
        void FlashLine(Vector3 a, Vector3 b)
        {
            if (!drawTracer || !tracer) return;
            tracer.positionCount = 2;
            tracer.SetPosition(0, a);
            tracer.SetPosition(1, b);
            StopCoroutine("LineRoutine");
            StartCoroutine("LineRoutine");
        }

        // 線條短暫顯示協程
        IEnumerator LineRoutine()
        {
            tracer.enabled = true;
            yield return new WaitForSeconds(tracerDuration);
            tracer.enabled = false;
        }

        // 畫圓圈協程
        IEnumerator FlashCircle(LineRenderer lr, Vector3 center, float radius, float dur)
        {
            if (!lr) yield break;
            if (lr.positionCount != areaSegments + 1)
                lr.positionCount = areaSegments + 1;

            float step = 2f * Mathf.PI / Mathf.Max(6, areaSegments);

            for (int i = 0; i <= areaSegments; i++)
            {
                float a = i * step;
                // 利用三角函數計算圓圈上的點
                lr.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(a) * radius,
                    center.y + Mathf.Sin(a) * radius,
                    0f));
            }

            lr.enabled = true;
            yield return new WaitForSeconds(dur);
            lr.enabled = false;
        }
    }
}