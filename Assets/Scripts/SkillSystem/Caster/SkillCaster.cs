using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;          // ★ 新 InputSystem
using UnityEngine.InputSystem.Utilities;

namespace RPG
{
    [DisallowMultipleComponent]
    public class SkillCaster : MonoBehaviour
    {
        [Header("Owner/發射者")]
        public Transform owner;
        public Transform Owner => owner ? owner : transform;

        [Header("引用")]
        public MainPointComponent main;
        public PlayerStats playerStats;
        public AimSource2D aimSource;

        [Header("技能清單")]
        public List<SkillData> Skills = new List<SkillData>();
        public int currentSkillIndex = 0;

        [Header("命中設定 (2D)")]
        public Transform firePoint;
        [SerializeField] private LayerMask enemyMask = 0;
        [SerializeField] private LayerMask obstacleMask = 0;
        [SerializeField] private float spawnInset = 0.05f;

        [Header("可視化（引用既有 LR）")]
        public bool drawTracer = true;
        public float tracerDuration = 0.08f;
        public LineRenderer tracer;
        public LineRenderer areaRing;
        public int areaSegments = 48;

        // =============================
        // ★ 改成新版 Input System 動作
        // =============================
        [Header("Input (Input System)")]
        [SerializeField] private InputActionReference castAction; // 原本就有的

        [Header("HUD 冷卻顯示")]
        [SerializeField] private HUDSkillStats hudSkillStats;   // ✅ Inspector 可選填
        private bool _hudAutoBound = false;                    // ✅ 已經嘗試自動綁定過？

        /// <summary>
        /// cooldownTimers[i] ＝ 第 i 個技能「剩餘冷卻秒數」
        /// cooldownMax[i]    ＝ 第 i 個技能「這次施放的冷卻總秒數」（當成 UI 的 max）
        /// </summary>
        private float[] cooldownTimers;
        private float[] cooldownMax;
        private bool isCasting;

        void OnValidate()
        {
            EnsureOwnerAndFirePoint();
            EnsureCooldownArray();
        }

        void OnEnable()
        {
            EnsureOwnerAndFirePoint();
            EnsureCooldownArray();

            // ★ 啟用動作
            if (castAction != null && castAction.action != null)
                castAction.action.Enable();

            // ✅ 遊戲一開始先嘗試自動綁一次
            TryAutoBindHud();
        }

        void OnDisable()
        {
            if (castAction != null && castAction.action != null)
                castAction.action.Disable();
        }

        void EnsureOwnerAndFirePoint()
        {
            if (!owner) owner = transform;
            if (!firePoint) firePoint = owner;
        }

        void Start()
        {
            if (playerStats && playerStats.MaxMP > 0 && playerStats.CurrentMP <= 0)
                playerStats.CurrentMP = playerStats.MaxMP;
        }

        void Update()
        {
            if (hudSkillStats == null && !_hudAutoBound)
            {
                TryAutoBindHud();
            }
            // ===========================
            // 1. 冷卻時間減少 + 同步到 HUD
            // ===========================
            if (cooldownTimers != null)
            {
                for (int i = 0; i < cooldownTimers.Length; i++)
                {
                    if (cooldownTimers[i] > 0f)
                    {
                        cooldownTimers[i] = Mathf.Max(0f, cooldownTimers[i] - Time.deltaTime);
                    }

                    // ★ 有 HUDSkillStats 的話，把這格技能的冷卻狀態丟過去
                    if (hudSkillStats != null && cooldownMax != null && i < cooldownMax.Length && cooldownMax[i] > 0f)
                    {
                        // current = 剩餘冷卻, max = 這次施放的總冷卻（當成填滿狀態）
                        hudSkillStats.SetSkillCooldown(i, cooldownTimers[i], cooldownMax[i]);
                    }
                }
            }

            // =======================================
            // 2. 用 InputActionReference 觸發技能
            // =======================================
            if (castAction != null && castAction.action.WasPerformedThisFrame())
            {
                TryCastCurrentSkill();
            }
        }

        /// <summary>
        /// 嘗試找到場景中的 HUDSkillStats，只會成功一次。
        /// </summary>
        void TryAutoBindHud()
        {
            // Inspector 已經手動綁好的話，就當作完成
            if (hudSkillStats != null)
            {
                _hudAutoBound = true;
                return;
            }

            // 在場景中找一次 HUDSkillStats
            hudSkillStats = FindObjectOfType<HUDSkillStats>();

            if (hudSkillStats != null)
            {
                _hudAutoBound = true;
                Debug.Log($"[SkillCaster] Auto-bound HUDSkillStats: {hudSkillStats.name}");
            }
            // 找不到就什麼都不做，Update 會在下一幀再試
        }
        void EnsureCooldownArray()
        {
            int n = Mathf.Max(1, Skills?.Count ?? 0);

            if (cooldownTimers == null || cooldownTimers.Length != n)
                cooldownTimers = new float[n];

            if (cooldownMax == null || cooldownMax.Length != n)
                cooldownMax = new float[n];
        }

        public void TryCastCurrentSkill()
        {
            if (isCasting) return;
            if (!main || !playerStats) return;
            if (Skills == null || Skills.Count == 0) return;
            if (currentSkillIndex < 0 || currentSkillIndex >= Skills.Count) return;

            var data = Skills[currentSkillIndex];
            if (!data) return;

            // 還在冷卻中就不能施放
            if (cooldownTimers != null &&
                currentSkillIndex < cooldownTimers.Length &&
                cooldownTimers[currentSkillIndex] > 0f)
                return;

            var comp = SkillCalculator.Compute(data, main.MP);

            // 魔力不足
            if (playerStats.CurrentMP < comp.MpCost) return;

            // 扣 MP
            playerStats.UseMP(comp.MpCost);

            // ===========================
            // ★ 設定這次施放的冷卻時間
            // ===========================
            EnsureCooldownArray(); // 保險
            cooldownTimers[currentSkillIndex] = comp.Cooldown;
            cooldownMax[currentSkillIndex] = comp.Cooldown;

            // 立刻同步一次 HUD（避免一開始看到 0）
            if (hudSkillStats != null)
            {
                hudSkillStats.SetSkillCooldown(currentSkillIndex,
                                               cooldownTimers[currentSkillIndex],
                                               cooldownMax[currentSkillIndex]);
            }

            // 進入施法流程
            StartCoroutine(CastRoutine(data, comp));
        }

        IEnumerator CastRoutine(SkillData data, SkillComputed comp)
        {
            isCasting = true;
            if (comp.CastTime > 0f)
                yield return new WaitForSeconds(comp.CastTime);

            if (data.HitType == HitType.Area)
                DoArea2D(data, comp);
            else
                DoSingle2D(data, comp);

            isCasting = false;
        }

        // ====== 單體 ======
        void DoSingle2D(SkillData data, SkillComputed comp)
        {
            var origin = firePoint ? firePoint.position : Owner.position;
            var dir = GetDir();

            if (data.UseProjectile && data.ProjectilePrefab)
            {
                var spawnPos = origin + (Vector3)(dir * spawnInset);
                var proj = Instantiate(data.ProjectilePrefab, spawnPos, Quaternion.identity);
                proj.Init(Owner, dir, data, comp, enemyMask, obstacleMask);
            }
            else
            {
                DoSingle2D_LegacyRay(data, comp);
            }
        }

        void DoSingle2D_LegacyRay(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : Owner.position;
            Vector2 dir = GetDir();
            float dist = Mathf.Max(0.1f, data.BaseRange);
            int mask = enemyMask | obstacleMask;

            RaycastHit2D hit = Physics2D.Raycast(origin3, dir, dist, mask);

            if (hit.collider != null)
            {
                FlashLine(origin3, hit.point);
                return;
            }

            FlashLine(origin3, origin3 + (Vector3)(dir * dist));
        }

        // ====== 範圍 ======
        void DoArea2D(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : Owner.position;
            Vector2 dir = GetDir();
            float dist = Mathf.Max(0.1f, data.BaseRange);

            Vector2 center = (Vector2)origin3 + dir * dist;
            RaycastHit2D hit = Physics2D.Raycast(origin3, dir, dist, enemyMask | obstacleMask);
            if (hit.collider != null) center = hit.point;

            float radius = Mathf.Max(0.05f, comp.AreaRadius);
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyMask);

            foreach (var c in hits)
            {
                if (EffectApplier.TryResolveOwner(c, out var ownerApplier, out var hitLayer))
                {
                    if (data.TargetLayer == hitLayer)
                        ownerApplier.ApplyIncomingRaw(comp.Damage);
                }
            }

            if (areaRing)
                StartCoroutine(FlashCircle(areaRing, center, radius, tracerDuration));
        }

        Vector2 GetDir()
        {
            if (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f)
                return aimSource.AimDir;

            return Vector2.right;
        }

        void FlashLine(Vector3 a, Vector3 b)
        {
            if (!drawTracer || !tracer) return;
            tracer.positionCount = 2;
            tracer.SetPosition(0, a);
            tracer.SetPosition(1, b);
            StopCoroutine("LineRoutine");
            StartCoroutine("LineRoutine");
        }

        IEnumerator LineRoutine()
        {
            tracer.enabled = true;
            yield return new WaitForSeconds(tracerDuration);
            tracer.enabled = false;
        }

        IEnumerator FlashCircle(LineRenderer lr, Vector3 center, float radius, float dur)
        {
            if (!lr) yield break;
            if (lr.positionCount != areaSegments + 1)
                lr.positionCount = areaSegments + 1;

            float step = 2f * Mathf.PI / Mathf.Max(6, areaSegments);

            for (int i = 0; i <= areaSegments; i++)
            {
                float a = i * step;
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
