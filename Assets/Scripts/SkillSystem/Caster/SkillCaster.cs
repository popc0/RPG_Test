using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RPG
{
    [DisallowMultipleComponent]
    public class SkillCaster : MonoBehaviour
    {
        [Header("Owner/發射者")]
        public Transform owner;                               // 預設取 transform
        public Transform Owner => owner ? owner : transform;

        [Header("引用")]
        public MainPointComponent main;
        public PlayerStats playerStats;
        public AimSource2D aimSource;                         // 唯一方向來源

        [Header("技能清單")]
        public List<SkillData> Skills = new List<SkillData>();
        public int currentSkillIndex = 0;

        [Header("命中設定 (2D)")]
        public Transform firePoint;                           // 發射點（可為 owner 子物件）
        [SerializeField] private LayerMask enemyMask = 0;     // 敵層
        [SerializeField] private LayerMask obstacleMask = 0;  // 牆層
        [SerializeField] private float spawnInset = 0.05f;    // 生成時沿方向微內縮，避免一出生就卡在自己碰撞上

        [Header("可視化（引用既有 LR，不動態生成）")]
        public bool drawTracer = true; public float tracerDuration = 0.08f;
        public LineRenderer tracer;        // 在 Inspector 指定（放在 Player 子物件）
        public LineRenderer areaRing;      // 同上，用於 AoE 閃圈
        public int areaSegments = 48;

        private float[] cooldownTimers; private bool isCasting;

        void OnValidate() { EnsureOwnerAndFirePoint(); EnsureCooldownArray(); }
        void OnEnable() { EnsureOwnerAndFirePoint(); EnsureCooldownArray(); }

        void EnsureOwnerAndFirePoint()
        { if (!owner) owner = transform; if (!firePoint) firePoint = owner; }

        void Start()
        {
            if (playerStats && playerStats.MaxMP > 0 && playerStats.CurrentMP <= 0)
                playerStats.CurrentMP = playerStats.MaxMP;
        }

        void Update()
        {
            if (cooldownTimers != null)
                for (int i = 0; i < cooldownTimers.Length; i++)
                    if (cooldownTimers[i] > 0f) cooldownTimers[i] = Mathf.Max(0f, cooldownTimers[i] - Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space)) TryCastCurrentSkill();
        }

        void EnsureCooldownArray()
        { int n = Mathf.Max(1, Skills?.Count ?? 0); if (cooldownTimers == null || cooldownTimers.Length != n) cooldownTimers = new float[n]; }

        public void TryCastCurrentSkill()
        {
            if (isCasting) return;
            if (!main || !playerStats) { Debug.LogWarning("[SkillCaster] 缺 main 或 playerStats"); return; }
            if (Skills == null || Skills.Count == 0) { Debug.LogWarning("[SkillCaster] Skills 為空"); return; }
            if (currentSkillIndex < 0 || currentSkillIndex >= Skills.Count) { Debug.LogWarning("[SkillCaster] 索引越界"); return; }

            var data = Skills[currentSkillIndex]; if (!data) { Debug.LogWarning("[SkillCaster] SkillData 為 null"); return; }
            if (cooldownTimers[currentSkillIndex] > 0f) { Debug.Log($"{data.SkillName} 冷卻中 ({cooldownTimers[currentSkillIndex]:F1}s)"); return; }
            if (!data.MeetsRequirement(main.MP)) { Debug.Log($"{data.SkillName} 未達成屬性門檻"); return; }

            var comp = SkillCalculator.Compute(data, main.MP);
            if (playerStats.CurrentMP < comp.MpCost) { Debug.Log($"MP不足 ({playerStats.CurrentMP:F1}/{comp.MpCost:F1})"); return; }

            playerStats.UseMP(comp.MpCost);
            cooldownTimers[currentSkillIndex] = comp.Cooldown;
            StartCoroutine(CastRoutine(data, comp));
        }

        IEnumerator CastRoutine(SkillData data, SkillComputed comp)
        {
            isCasting = true; if (comp.CastTime > 0f) yield return new WaitForSeconds(comp.CastTime);
            if (data.HitType == HitType.Area) DoArea2D(data, comp); else DoSingle2D(data, comp);
            isCasting = false;
        }

        // ====== 單體 ======
        void DoSingle2D(SkillData data, SkillComputed comp)
        {
            var origin = firePoint ? firePoint.position : Owner.position;
            var dir = GetDir();

            if (data.UseProjectile && data.ProjectilePrefab)
            {
                // 生成時沿方向微內縮，避免一出生就與自身 Collider 重疊
                var spawnPos = origin + (Vector3)(dir * spawnInset);
                var proj = Instantiate(data.ProjectilePrefab, spawnPos, Quaternion.identity);
                proj.Init(Owner, dir, data, comp, enemyMask, obstacleMask);
            }
            else
            {
                DoSingle2D_LegacyRay(data, comp);
            }
        }

        // 備援：舊射線命中（牆擋、Body/Feet 過濾）
        void DoSingle2D_LegacyRay(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : Owner.position;
            Vector2 dir = GetDir(); float dist = Mathf.Max(0.1f, data.BaseRange);
            int mask = enemyMask | obstacleMask; RaycastHit2D hit2D = Physics2D.Raycast(origin3, dir, dist, mask);

            if (hit2D.collider != null)
            {
                FlashLine(origin3, hit2D.point);
                if (((1 << hit2D.collider.gameObject.layer) & obstacleMask.value) != 0) return; // 被牆擋住

                if (EffectApplier.TryResolveOwner(hit2D.collider, out var ownerApplier, out var hitLayer))
                { if (data.TargetLayer == hitLayer) ownerApplier.ApplyIncomingRaw(comp.Damage); return; }
                return;
            }
            FlashLine(origin3, origin3 + (Vector3)(dir * dist));
        }

        // ====== 範圍 ======
        void DoArea2D(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : Owner.position; Vector2 dir = GetDir();
            float dist = Mathf.Max(0.1f, data.BaseRange); int blockMask = enemyMask | obstacleMask; Vector2 center = (Vector2)origin3 + dir * dist;
            RaycastHit2D hit = Physics2D.Raycast(origin3, dir, dist, blockMask); if (hit.collider != null) center = hit.point;

            float radius = Mathf.Max(0.05f, comp.AreaRadius); Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyMask);
            var unique = new HashSet<EffectApplier>();
            foreach (var c in hits)
            { if (!EffectApplier.TryResolveOwner(c, out var ownerApplier, out var hitLayer)) continue; if (data.TargetLayer != hitLayer) continue; if (unique.Add(ownerApplier)) ownerApplier.ApplyIncomingRaw(comp.Damage); }

            if (areaRing) StartCoroutine(FlashCircle(areaRing, center, radius, tracerDuration));
        }

        Vector2 GetDir()
        { if (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f) return aimSource.AimDir; return Vector2.right; }

        void FlashLine(Vector3 a, Vector3 b)
        { if (!drawTracer || !tracer) return; tracer.positionCount = 2; tracer.SetPosition(0, a); tracer.SetPosition(1, b); StopCoroutine(nameof(LineRoutine)); StartCoroutine(LineRoutine()); }
        IEnumerator LineRoutine() { tracer.enabled = true; yield return new WaitForSeconds(tracerDuration); tracer.enabled = false; }

        IEnumerator FlashCircle(LineRenderer lr, Vector3 center, float radius, float dur)
        {
            if (!lr) yield break; if (lr.positionCount != areaSegments + 1) lr.positionCount = areaSegments + 1;
            float step = 2f * Mathf.PI / Mathf.Max(6, areaSegments);
            for (int i = 0; i <= areaSegments; i++) { float a = i * step; Vector3 p = new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0f); lr.SetPosition(i, p); }
            lr.enabled = true; yield return new WaitForSeconds(dur); lr.enabled = false;
        }
    }
}
