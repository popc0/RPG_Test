using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    [DefaultExecutionOrder(10)]
    public class AimPreview2D : MonoBehaviour
    {
        [Header("引用")]
        public SkillCaster caster;
        public MainPointComponent main;
        public AimSource2D aimSource;

        [Header("遮蔽")]
        public bool useObstacleBlock = true;
        public LayerMask obstacleMask = 0;

        [Header("顯示（中段用 Mesh，終端用線條)")]
        public LineRenderer line;                 // 主預覽線條（含終端）
        public LineRenderer circle;               // 終端或範圍圈
        public MeshFilter meshFilter;             // 膠囊 Mesh（中段，無端帽）
        public MeshRenderer meshRenderer;         // 外觀材質

        [Header("線條設定")]
        public float fallbackLineWidth = 0.06f;
        public float maxPreviewWidth = 2.0f;
        public int circleSegments = 48;

        Mesh _mesh;

        void Awake()
        {
            if (!caster) caster = GetComponent<SkillCaster>();
            if (!main) main = GetComponent<MainPointComponent>();
            if (!aimSource) aimSource = GetComponent<AimSource2D>();
            if (meshFilter && _mesh == null)
            {
                _mesh = new Mesh { name = "AimPreview_CapsuleStrip" };
                meshFilter.sharedMesh = _mesh;
            }
        }

        void OnDisable()
        {
            if (line) line.enabled = false;
            if (circle) circle.enabled = false;
            if (meshFilter) meshFilter.sharedMesh = null;
        }

        void LateUpdate()
        {
            if (!caster || caster.Skills == null || caster.Skills.Count == 0) return;

            var data = caster.Skills[Mathf.Clamp(caster.currentSkillIndex, 0, caster.Skills.Count - 1)];
            if (!data) return;

            var comp = SkillCalculator.Compute(data, main ? main.MP : MainPoint.Zero);
            Vector3 origin = caster.firePoint ? (Vector3)caster.firePoint.position : transform.position;

            // 方向
            Vector2 dir = (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f) ? aimSource.AimDir : Vector2.right;
            dir.Normalize();

            // 投射物半徑（遮蔽與線寬）
            float previewRadius = GetProjectileRadiusForPreview(data, dir);

            // 射程與終點（考慮障礙物與子彈寬度）
            float dist = Mathf.Max(0.1f, data.BaseRange);
            Vector3 end = origin + (Vector3)(dir * dist);
            if (useObstacleBlock)
            {
                RaycastHit2D hit = Physics2D.CircleCast(origin, previewRadius, dir, dist, obstacleMask);
                if (hit.collider != null)
                {
                    // 用「沿原始方向的距離」來決定終點，避免使用 hit.point 造成角度被拉向碰撞面的錯覺
                    float d = Mathf.Clamp(hit.distance, 0f, dist);
                    end = origin + (Vector3)(dir * d);
                }
            }

            // ===== 中段 Mesh（無端帽）=====
            bool drewMesh = false;
            if (meshFilter && meshRenderer && data.HitType == HitType.Single && data.UseProjectile && data.ProjectilePrefab)
            {
                var mf = meshFilter.transform;
                Vector3 aLocal = mf.InverseTransformPoint(origin);
                Vector3 bLocal = mf.InverseTransformPoint(end);

                BuildFlatStrip(_mesh, aLocal, bLocal, previewRadius);
                meshFilter.sharedMesh = _mesh;
                drewMesh = true;
            }
            if (!drewMesh && meshFilter) meshFilter.sharedMesh = null;

            // ===== 主線條預覽 =====
            if (line)
            {
                line.enabled = true; line.useWorldSpace = true; line.positionCount = 2;
                line.SetPosition(0, origin); line.SetPosition(1, end);
                float width = fallbackLineWidth;
                if (data.HitType == HitType.Single && data.UseProjectile)
                {
                    float d = 2f * Mathf.Max(0.0f, previewRadius);
                    width = Mathf.Clamp(d, 0.0f, maxPreviewWidth);
                }
                line.startWidth = width; line.endWidth = width;
            }

            // ===== 終端／範圍圈 =====
            if (circle)
            {
                if (data.HitType == HitType.Area)
                {
                    // 範圍技能 → 畫 AoE 圓圈
                    float r = Mathf.Max(0.05f, comp.AreaRadius);
                    DrawCircle(circle, end, r);
                }
                else if (data.HitType == HitType.Single && data.UseProjectile)
                {
                    // 單體投射 → 畫小終端圈
                    float r = Mathf.Max(0.02f, previewRadius);
                    DrawCircle(circle, end, r);
                }
                else
                {
                    circle.enabled = false;
                }
            }
        }

        float GetProjectileRadiusForPreview(SkillData data, Vector2 dir)
        {
            float radius = Mathf.Max(0.02f, data.ProjectileRadius);
            if (data.UseProjectile && data.ProjectilePrefab)
            {
                float diameter;
                if (data.ProjectilePrefab.TryGetColliderDiameter(dir, out diameter))
                {
                    radius = Mathf.Max(radius, diameter * 0.5f);
                }
            }
            return radius;
        }

        void DrawCircle(LineRenderer lr, Vector3 center, float radius)
        {
            if (!lr) return;
            int segs = Mathf.Max(12, circleSegments);
            if (lr.positionCount != segs + 1) lr.positionCount = segs + 1;
            float step = 2f * Mathf.PI / segs;
            for (int i = 0; i <= segs; i++)
            {
                float a = i * step;
                Vector3 p = new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0f);
                lr.SetPosition(i, p);
            }
            lr.enabled = true;
        }

        // 建立平頭長條（無端帽）
        void BuildFlatStrip(Mesh m, Vector3 a, Vector3 b, float radius)
        {
            if (m == null) return; m.Clear();

            Vector3 d3 = (b - a);
            float len = d3.magnitude;
            if (len < 1e-5f || radius <= 1e-6f) return;

            Vector2 dir = new Vector2(d3.x, d3.y).normalized;
            Vector2 n = new Vector2(-dir.y, dir.x);

            Vector3 p0 = a + (Vector3)(n * radius);
            Vector3 p1 = b + (Vector3)(n * radius);
            Vector3 p2 = b - (Vector3)(n * radius);
            Vector3 p3 = a - (Vector3)(n * radius);

            var vertices = new List<Vector3> { p0, p1, p2, p3 };
            var triangles = new List<int> { 0, 1, 2, 0, 2, 3 };

            m.SetVertices(vertices);
            m.SetTriangles(triangles, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
        }
    }
}
