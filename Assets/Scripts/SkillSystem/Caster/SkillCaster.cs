using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RPG
{
    /// <summary>純 2D 施放器：平常吃 AimSource2D；真正施放時可改用滑鼠方向。</summary>
    public class SkillCaster : MonoBehaviour
    {
        [Header("引用")]
        public MainPointComponent main;
        public PlayerStats playerStats;
        public AimSource2D aimSource;       // 鍵盤/手把方向來源（預覽或備援）

        [Header("技能")]
        public List<SkillData> Skills = new List<SkillData>();
        public int currentSkillIndex = 0;

        [Header("命中設定 (2D)")]
        public Transform firePoint;
        public float rayDistance = 12f;
        [SerializeField] private LayerMask targetMask = ~0;
        public bool ignoreLayerMaskForTest = true;

        [Header("方向來源")]
        [Tooltip("施放攻擊時是否使用滑鼠位置作為方向")]
        public bool useMouseForAttack = true;

        [Header("視覺化")]
        public bool drawTracer = true;
        public float tracerDuration = 0.08f;
        public float tracerWidth = 0.035f;
        public Color tracerColor = new Color(1f, 0.9f, 0.2f, 1f);

        private float[] cooldownTimers;
        private bool isCasting;
        private LineRenderer tracer;

        void OnEnable()
        {
            EnsureCooldownArray();
        }

        void Start()
        {
            if (!firePoint) firePoint = transform;

            if (drawTracer)
            {
                tracer = new GameObject("RayTracer2D").AddComponent<LineRenderer>();
                tracer.transform.SetParent(null);
                tracer.enabled = false;
                tracer.widthMultiplier = tracerWidth;
                tracer.positionCount = 2;
                tracer.useWorldSpace = true;
                tracer.material = new Material(Shader.Find("Sprites/Default"));
                tracer.startColor = tracerColor;
                tracer.endColor = tracerColor;
                tracer.numCornerVertices = 4;
                tracer.numCapVertices = 4;
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
            StartCoroutine(CastRoutine(comp));
        }

        private IEnumerator CastRoutine(SkillComputed comp)
        {
            isCasting = true;
            if (comp.CastTime > 0f) yield return new WaitForSeconds(comp.CastTime);
            DoHit2D(comp);
            isCasting = false;
        }

        private void DoHit2D(SkillComputed comp)
        {
            Vector3 origin3 = firePoint ? firePoint.position : transform.position;
            Vector2 dir = GetAttackDirection2D(origin3);  // ← 施放時決定方向

            float dist = rayDistance;
            int mask = ignoreLayerMaskForTest ? ~0 : targetMask;

            // Raycast2D
            RaycastHit2D hit2D = Physics2D.Raycast(origin3, dir, dist, mask);
            if (hit2D.collider != null)
            {
                DrawTracer(origin3, hit2D.point);
                var target = hit2D.collider.GetComponentInParent<EffectApplier>();
                if (target != null)
                {
                    target.ApplyIncomingRaw(comp.Damage);
                    Debug.Log($"[{comp.SkillName}] 命中 {target.name}（Raycast2D）");
                    return;
                }
                Debug.Log($"命中 {hit2D.collider.name}（無 EffectApplier）");
                return;
            }

            // 近點補偵測
            Vector2 probeCenter = (Vector2)origin3 + dir * Mathf.Min(3f, dist * 0.25f);
            float probeRadius = 0.35f;
            var hits = Physics2D.OverlapCircleAll(probeCenter, probeRadius, mask);
            foreach (var c in hits)
            {
                var t = c.GetComponentInParent<EffectApplier>();
                if (t != null)
                {
                    DrawTracer(origin3, c.bounds.ClosestPoint(origin3));
                    t.ApplyIncomingRaw(comp.Damage);
                    Debug.Log($"[{comp.SkillName}] 命中 {t.name}（ProbeCircle2D）");
                    return;
                }
            }

            // 未命中 → 畫到最遠點
            DrawTracer(origin3, origin3 + (Vector3)(dir * dist));
            Debug.Log($"[{comp.SkillName}] 未命中任何目標（2D）");
        }

        private Vector2 GetAttackDirection2D(Vector3 origin)
        {
            if (useMouseForAttack && Camera.main != null)
            {
                Vector3 m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                m.z = 0f;
                Vector2 v = ((Vector2)(m - origin));
                if (v.sqrMagnitude > 0.0001f) return v.normalized;
            }
            // 沒有滑鼠或距離太小 → 用 AimSource2D（鍵盤/手把）的方向
            if (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f)
                return aimSource.AimDir;

            return Vector2.right;
        }

        private void DrawTracer(Vector3 a, Vector3 b)
        {
            if (!drawTracer || tracer == null) return;
            StopAllCoroutines();
            StartCoroutine(FlashLine(a, b));
        }

        private IEnumerator FlashLine(Vector3 a, Vector3 b)
        {
            tracer.SetPosition(0, a);
            tracer.SetPosition(1, b);
            tracer.enabled = true;
            yield return new WaitForSeconds(tracerDuration);
            tracer.enabled = false;
        }
    }
}
