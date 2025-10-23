using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RPG
{
    /// <summary>
    /// 純 2D 技能施放器：
    /// - 單體/直線：障礙物會擋住（Raycast 以 enemy|obstacle 為遮罩）
    /// - 範圍（HitType.Area）：不理會障礙物（OverlapCircleAll 只掃敵人）
    /// - 攻擊方向：施放當下優先滑鼠，否則 AimSource2D
    /// </summary>
    public class SkillCaster : MonoBehaviour
    {
        [Header("引用")]
        public MainPointComponent main;
        public PlayerStats playerStats;
        public AimSource2D aimSource;

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

        [Tooltip("測試用：忽略圖層過濾（會穿牆）")]
        public bool ignoreLayerMaskForTest = false;

        [Header("方向來源")]
        [Tooltip("施放時是否用滑鼠指向；無滑鼠則用 AimSource2D")]
        public bool useMouseForAttack = true;

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

        void OnEnable()
        {
            EnsureCooldownArray();
        }

        void Start()
        {
            if (!firePoint) firePoint = transform;

            if (drawTracer)
            {
                tracer = NewLR("RayTracer2D", tracerColor, tracerWidth, false, 2);
            }
            if (drawAreaFlash)
            {
                areaRing = NewLR("AreaRing2D", areaFlashColor, areaFlashWidth, true, areaSegments + 1);
            }

            if (playerStats && playerStats.MaxMP > 0 && playerStats.CurrentMP <= 0)
                playerStats.CurrentMP = playerStats.MaxMP;
        }

        void Update()
        {
            if (cooldownTimers != null)
                for (int i = 0; i < cooldownTimers.Length; i++)
                    if (cooldownTimers[i] > 0f) cooldownTimers[i] -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Space))
                TryCastCurrentSkill();
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

            var comp = SkillCalculator.Compute(data, main.MP);
            if (playerStats.CurrentMP < comp.MpCost) { Debug.Log($"MP不足 ({playerStats.CurrentMP:F1}/{comp.MpCost:F1})"); return; }

            playerStats.UseMP(comp.MpCost);
            cooldownTimers[currentSkillIndex] = comp.Cooldown;
            StartCoroutine(CastRoutine( data, comp));
        }

        private IEnumerator CastRoutine(SkillData data, SkillComputed comp)
        {
            isCasting = true;
            if (comp.CastTime > 0f) yield return new WaitForSeconds(comp.CastTime);

            // Area 與 Single 分流
            bool isAreaSkill = (data != null && data.HitType == HitType.Area);
            if (isAreaSkill) DoArea2D(comp, data);
            else DoSingle2D(comp, data);

            isCasting = false;
        }

        // ====== 單體 / 直線：障礙物會擋住 ======
        private void DoSingle2D(SkillComputed comp, SkillData data)
        {
            Vector3 origin3 = firePoint ? firePoint.position : transform.position;
            Vector2 dir = GetAttackDirection2D(origin3);
            float dist = rayDistance;

            int mask = ignoreLayerMaskForTest ? ~0 : (enemyMask | obstacleMask);

            // 1) Raycast：誰先擋到算誰
            RaycastHit2D hit2D = Physics2D.Raycast(origin3, dir, dist, mask);
            if (hit2D.collider != null)
            {
                FlashLine(origin3, hit2D.point);

                if (!ignoreLayerMaskForTest && IsInMask(hit2D.collider.gameObject.layer, obstacleMask))
                {
                    Debug.Log($"[{comp.SkillName}] 被障礙物擋住：{hit2D.collider.name}");
                    return;
                }

                var target = hit2D.collider.GetComponentInParent<EffectApplier>();
                if (target != null)
                {
                    target.ApplyIncomingRaw(comp.Damage);
                    Debug.Log($"[{comp.SkillName}] 命中 {target.name}（Raycast2D）");
                    return;
                }

                Debug.Log($"命中 {hit2D.collider.name}（非敵人/障礙物圖層，請檢查 Layer）");
                return;
            }

            // 2) 近點補偵測：只找敵人，且確認中間沒有障礙物
            Vector2 probeCenter = (Vector2)origin3 + dir * Mathf.Min(3f, dist * 0.25f);
            float probeRadius = 0.35f;
            int enemyOnlyMask = ignoreLayerMaskForTest ? ~0 : enemyMask;
            var hits = Physics2D.OverlapCircleAll(probeCenter, probeRadius, enemyOnlyMask);

            EffectApplier closest = null;
            float closestSq = float.MaxValue;

            foreach (var c in hits)
            {
                var t = c.GetComponentInParent<EffectApplier>();
                if (t == null) continue;

                if (!ignoreLayerMaskForTest)
                {
                    Vector2 toTarget = (Vector2)(t.transform.position - origin3);
                    if (Physics2D.Raycast(origin3, toTarget.normalized, toTarget.magnitude, obstacleMask))
                        continue; // 有牆擋住
                }

                float sq = (t.transform.position - origin3).sqrMagnitude;
                if (sq < closestSq) { closestSq = sq; closest = t; }
            }

            if (closest != null)
            {
                Vector3 hitPos = closest.GetComponent<Collider2D>()
                    ? (Vector3)closest.GetComponent<Collider2D>().bounds.ClosestPoint(origin3)
                    : closest.transform.position;

                FlashLine(origin3, hitPos);
                closest.ApplyIncomingRaw(comp.Damage);
                Debug.Log($"[{comp.SkillName}] 命中 {closest.name}（ProbeCircle2D，無障礙）");
                return;
            }

            // 3) 完全未命中 → 畫到最遠端
            FlashLine(origin3, origin3 + (Vector3)(dir * dist));
            Debug.Log($"[{comp.SkillName}] 未命中任何目標（2D，含障礙判定）");
        }

        // ====== 範圍：不理會障礙物，直接在「瞄準點」爆炸 ======
        private void DoArea2D(SkillComputed comp, SkillData data)
        {
            Vector3 origin3 = firePoint ? firePoint.position : transform.position;

            // 瞄準點：滑鼠方向的點（距離上限 rayDistance）
            Vector2 dir = GetAttackDirection2D(origin3);
            Vector2 mousePoint = origin3 + (Vector3)(dir * rayDistance);

            if (useMouseForAttack && Camera.main != null)
            {
                Vector3 m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                m.z = 0f;
                Vector2 v = (Vector2)(m - origin3);
                if (v.sqrMagnitude > 0.0001f)
                    mousePoint = origin3 + (Vector3)Vector2.ClampMagnitude(v, rayDistance);
            }

            float radius = Mathf.Max(0.05f, comp.AreaRadius);
            int maskEnemies = ignoreLayerMaskForTest ? ~0 : enemyMask;

            // 掃描敵人（不檢查牆）
            Collider2D[] hits = Physics2D.OverlapCircleAll(mousePoint, radius, maskEnemies);
            foreach (var c in hits)
            {
                var t = c.GetComponentInParent<EffectApplier>();
                if (t != null)
                {
                    t.ApplyIncomingRaw(comp.Damage);
                    Debug.Log($"[{comp.SkillName}] 範圍命中 {t.name}");
                }
            }

            // 可視化：閃一下爆點圈
            if (drawAreaFlash && areaRing)
            {
                StartCoroutine(FlashCircle(areaRing, mousePoint, radius, tracerDuration));
            }
        }

        // ====== 方向取得 ======
        private Vector2 GetAttackDirection2D(Vector3 origin)
        {
            if (useMouseForAttack && Camera.main != null)
            {
                Vector3 m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                m.z = 0f;
                Vector2 v = ((Vector2)(m - origin));
                if (v.sqrMagnitude > 0.0001f) return v.normalized;
            }
            if (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f)
                return aimSource.AimDir;

            return Vector2.right;
        }

        // ====== 小工具 ======
        private bool IsInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        private LineRenderer NewLR(string name, Color color, float width, bool loop, int posCount)
        {
            var lr = new GameObject(name).AddComponent<LineRenderer>();
            lr.transform.SetParent(null);
            lr.useWorldSpace = true;
            lr.material = new Material(Shader.Find("Sprites/Default"));
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
