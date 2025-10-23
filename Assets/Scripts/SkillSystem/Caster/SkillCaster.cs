using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RPG
{
    /// <summary>
    /// 純 2D 技能施放器（方向只讀 AimSource2D）：
    /// - 單體/直線：障礙物會擋住（Raycast 以 enemy|obstacle 為遮罩），並依 SkillData.TargetLayer 過濾 Body/Feet。
    /// - 範圍（HitType.Area）：不理會障礙物（OverlapCircleAll 只掃敵人），同樣過濾 Body/Feet。
    /// </summary>
    public class SkillCaster : MonoBehaviour
    {
        [Header("引用")]
        public MainPointComponent main;
        public PlayerStats playerStats;
        public AimSource2D aimSource;                 // ✅ 唯一方向來源

        [Header("技能")]
        public List<SkillData> Skills = new List<SkillData>();
        public int currentSkillIndex = 0;

        [Header("命中設定 (2D)")]
        public Transform firePoint;
        public float rayDistance = 12f;

        [Tooltip("敵人圖層（只含 Enemy）")]
        [SerializeField] private LayerMask enemyMask = 0;

        [Tooltip("障礙物圖層（只含 Obstacle/牆）")]
        [SerializeField] private LayerMask obstacleMask = 0;

        [Tooltip("測試用：忽略圖層過濾（會穿牆、打到全部）")]
        public bool ignoreLayerMaskForTest = false;

        [Header("可視化（短暫顯示）")]
        public bool drawTracer = true;
        public float tracerDuration = 0.08f;
        public float tracerWidth = 0.035f;
        public Color tracerColor = new Color(1f, 0.9f, 0.2f, 1f);
        public bool drawAreaFlash = true;
        public Color areaFlashColor = new Color(0.2f, 0.8f, 1f, 0.9f);
        public float areaFlashWidth = 0.03f;
        public int areaSegments = 48;

        private float[] cooldownTimers;
        private bool isCasting;
        private LineRenderer tracer;
        private LineRenderer areaRing;
        private static Material s_lineMat;


        void OnEnable() => EnsureCooldownArray();

        void Start()
        {
            if (!firePoint) firePoint = transform;

            if (drawTracer) tracer = NewLR("RayTracer2D", tracerColor, tracerWidth, false, 2);
            if (drawAreaFlash) areaRing = NewLR("AreaRing2D", areaFlashColor, areaFlashWidth, true, areaSegments + 1);

            if (playerStats && playerStats.MaxMP > 0 && playerStats.CurrentMP <= 0)
                playerStats.CurrentMP = playerStats.MaxMP;
        }

        void OnValidate() { EnsureCooldownArray(); }

        void Update()
        {
            if (cooldownTimers != null)
            {
                for (int i = 0; i < cooldownTimers.Length; i++)
                    if (cooldownTimers[i] > 0f)
                        cooldownTimers[i] = Mathf.Max(0f, cooldownTimers[i] - Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.Space))
                TryCastCurrentSkill();
        }

        void OnDestroy()
        {
            if (tracer) Destroy(tracer.gameObject);
            if (areaRing) Destroy(areaRing.gameObject);
        }

        private void EnsureCooldownArray()
        {
            int n = Mathf.Max(1, Skills?.Count ?? 0);
            if (cooldownTimers == null || cooldownTimers.Length != n)
                cooldownTimers = new float[n];
        }

        public void TryCastCurrentSkill()
        {
            if (isCasting) return;
            if (!main || !playerStats) { Debug.LogWarning("[SkillCaster2D] 缺 main 或 playerStats"); return; }
            if (Skills == null || Skills.Count == 0) { Debug.LogWarning("[SkillCaster2D] Skills 為空"); return; }
            if (currentSkillIndex < 0 || currentSkillIndex >= Skills.Count) { Debug.LogWarning("[SkillCaster2D] 索引越界"); return; }

            var data = Skills[currentSkillIndex];
            if (!data) { Debug.LogWarning("[SkillCaster2D] SkillData 為 null"); return; }
            if (cooldownTimers[currentSkillIndex] > 0f) { Debug.Log($"{data.SkillName} 冷卻中 ({cooldownTimers[currentSkillIndex]:F1}s)"); return; }

            // 取得條件
            if (!data.MeetsRequirement(main.MP))
            {
                Debug.Log($"{data.SkillName} 未達成屬性門檻，無法施放/學習");
                return;
            }

            var comp = SkillCalculator.Compute(data, main.MP);
            if (playerStats.CurrentMP < comp.MpCost) { Debug.Log($"MP不足 ({playerStats.CurrentMP:F1}/{comp.MpCost:F1})"); return; }

            playerStats.UseMP(comp.MpCost);
            cooldownTimers[currentSkillIndex] = comp.Cooldown;
            StartCoroutine(CastRoutine(data, comp));
        }

        private IEnumerator CastRoutine(SkillData data, SkillComputed comp)
        {
            isCasting = true;
            if (comp.CastTime > 0f) yield return new WaitForSeconds(comp.CastTime);

            bool isAreaSkill = (data != null && data.HitType == HitType.Area);
            if (isAreaSkill) DoArea2D(data, comp);
            else DoSingle2D(data, comp);

            isCasting = false;
        }

        // ====== 單體 / 直線：障礙物會擋住，且需通過 Body/Feet 過濾 ======
        private void DoSingle2D(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : transform.position;
            Vector2 dir = GetDir();
            float dist = rayDistance;

            int mask = ignoreLayerMaskForTest ? ~0 : (enemyMask | obstacleMask);

            // 1) Raycast：誰先擋到算誰
            RaycastHit2D hit2D = Physics2D.Raycast(origin3, dir, dist, mask);
            if (hit2D.collider != null)
            {
                FlashLine(origin3, hit2D.point);

                if (!ignoreLayerMaskForTest && IsInMask(hit2D.collider.gameObject.layer, obstacleMask))
                {
                    Debug.Log($"[{data.SkillName}] 被障礙物擋住：{hit2D.collider.name}");
                    return;
                }

                if (EffectApplier.TryResolveOwner(hit2D.collider, out var owner, out var hitLayer))
                {
                    if (data.TargetLayer == hitLayer)
                    {
                        owner.ApplyIncomingRaw(comp.Damage);
                        Debug.Log($"[{data.SkillName}] 命中 {owner.name}（{hitLayer}）");
                    }
                    else
                    {
                        Debug.Log($"[{data.SkillName}] 命中 {owner.name} 但區層不符（需要 {data.TargetLayer}，實際 {hitLayer}）");
                    }
                    return;
                }

                Debug.Log($"命中 {hit2D.collider.name}（非敵人/無 EffectApplier，請檢查設定）");
                return;
            }

            // 2) 近點補偵測：只找敵人，且確認中間沒有障礙物；同時過濾 Body/Feet
            Vector2 probeCenter = (Vector2)origin3 + dir * Mathf.Min(3f, dist * 0.25f);
            float probeRadius = 0.35f;
            int enemyOnlyMask = ignoreLayerMaskForTest ? ~0 : enemyMask;
            var hits = Physics2D.OverlapCircleAll(probeCenter, probeRadius, enemyOnlyMask);

            EffectApplier closest = null;
            InteractionLayer chosenLayer = InteractionLayer.Body;
            float closestSq = float.MaxValue;

            foreach (var c in hits)
            {
                if (!EffectApplier.TryResolveOwner(c, out var candidate, out var hLayer))
                    continue;

                if (data.TargetLayer != hLayer) continue;

                if (!ignoreLayerMaskForTest)
                {
                    Vector2 toTarget = (Vector2)(candidate.transform.position - origin3);
                    if (Physics2D.Raycast(origin3, toTarget.normalized, toTarget.magnitude, obstacleMask))
                        continue;
                }

                float sq = (candidate.transform.position - origin3).sqrMagnitude;
                if (sq < closestSq) { closestSq = sq; closest = candidate; chosenLayer = hLayer; }
            }

            if (closest != null)
            {
                Vector3 hitPos = closest.GetComponent<Collider2D>()
                    ? (Vector3)closest.GetComponent<Collider2D>().bounds.ClosestPoint(origin3)
                    : closest.transform.position;

                FlashLine(origin3, hitPos);
                closest.ApplyIncomingRaw(comp.Damage);
                Debug.Log($"[{data.SkillName}] 命中 {closest.name}（{chosenLayer}，ProbeCircle2D）");
                return;
            }

            // 3) 完全未命中 → 畫到最遠端
            FlashLine(origin3, origin3 + (Vector3)(dir * dist));
            Debug.Log($"[{data.SkillName}] 未命中任何目標（2D，含障礙判定/區層過濾）");
        }

        // ====== 範圍 AoE：不理會障礙物，但仍需 Body/Feet 過濾 ======
        private void DoArea2D(SkillData data, SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : transform.position;

            Vector2 dir = GetDir();
            Vector2 center = origin3 + (Vector3)(dir * Mathf.Clamp(rayDistance, 0.1f, rayDistance));

            float radius = Mathf.Max(0.05f, comp.AreaRadius);
            int maskEnemies = ignoreLayerMaskForTest ? ~0 : enemyMask;

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, maskEnemies);

            // 去重：同一受擊主體只吃一次
            var unique = new System.Collections.Generic.HashSet<EffectApplier>();

            foreach (var c in hits)
            {
                if (!EffectApplier.TryResolveOwner(c, out var owner, out var hitLayer)) continue;
                if (data.TargetLayer != hitLayer) continue;

                if (unique.Add(owner))
                {
                    owner.ApplyIncomingRaw(comp.Damage);
                    Debug.Log($"[{data.SkillName}] 範圍命中 {owner.name}（{hitLayer}）");
                }
            }

            if (drawAreaFlash && areaRing)
                StartCoroutine(FlashCircle(areaRing, center, radius, tracerDuration));
        }

        // ====== 方向取得（只讀 AimSource2D） ======
        private Vector2 GetDir()
        {
            if (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f)
                return aimSource.AimDir;
            return Vector2.right; // 保底
        }

        // ====== 小工具 ======
        private bool IsInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        private LineRenderer NewLR(string name, Color color, float width, bool loop, int posCount)
        {
            if (s_lineMat == null)
                s_lineMat = new Material(Shader.Find("Sprites/Default"));

            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.material = s_lineMat;
            lr.positionCount = posCount;
            lr.widthMultiplier = width;
            lr.startColor = lr.endColor = color;
            lr.loop = loop;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 4;
            return lr;
        }

        private void FlashLine(Vector3 a, Vector3 b)
        {
            if (!drawTracer || tracer == null) return;
            StopCoroutine(nameof(LineRoutine));
            StartCoroutine(LineRoutine(a, b));
        }

        private IEnumerator LineRoutine(Vector3 a, Vector3 b)
        {
            tracer.SetPosition(0, a);
            tracer.SetPosition(1, b);
            tracer.enabled = true;
            yield return new WaitForSeconds(tracerDuration);
            tracer.enabled = false;
        }

        private IEnumerator FlashCircle(LineRenderer lr, Vector3 center, float radius, float dur)
        {
            if (!lr) yield break;

            float step = 2f * Mathf.PI / Mathf.Max(6, areaSegments);
            for (int i = 0; i <= areaSegments; i++)
            {
                float a = i * step;
                Vector3 p = new Vector3(
                    center.x + Mathf.Cos(a) * radius,
                    center.y + Mathf.Sin(a) * radius,
                    0f
                );
                lr.SetPosition(i, p);
            }
            lr.enabled = true;
            yield return new WaitForSeconds(dur);
            lr.enabled = false;
        }
    }
}
