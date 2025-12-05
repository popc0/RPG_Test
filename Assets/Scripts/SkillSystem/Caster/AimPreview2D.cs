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

        // [修改] 標題改清楚一點，對應 Slot 0
        [Header("顯示（Slot 0: 固定普攻）")]
        public LineRenderer circle1;
        public MeshFilter meshFilter1;
        public MeshRenderer meshRenderer1;

        // [修改] 標題改清楚一點，對應 Slot 1
        [Header("顯示（Slot 1: 切換普攻）")]
        public LineRenderer circle2;
        public MeshFilter meshFilter2;
        public MeshRenderer meshRenderer2;

        [Header("線條設定")]
        public int circleSegments = 48;

        Mesh _mesh1; // 對應 Slot 0 的 Mesh
        Mesh _mesh2; // 對應 Slot 1 的 Mesh

        void Awake()
        {
            if (meshFilter1 && _mesh1 == null)
            {
                _mesh1 = new Mesh { name = "AimPreview_Strip_0" };
                meshFilter1.sharedMesh = _mesh1;
            }
            if (meshFilter2 && _mesh2 == null)
            {
                _mesh2 = new Mesh { name = "AimPreview_Strip_1" };
                meshFilter2.sharedMesh = _mesh2;
            }
        }

        void OnDisable()
        {
            if (circle1) circle1.enabled = false;
            if (meshFilter1) meshFilter1.sharedMesh = null;

            if (circle2) circle2.enabled = false;
            if (meshFilter2) meshFilter2.sharedMesh = null;
        }

        // [核心修改] 鎖定讀取 index 0 和 1
        void LateUpdate()
        {
            if (!caster) return;

            // 獲取目前的技能列表 (預期有 4 個: 0=Fixed, 1=Switch, 2=FixedUlt, 3=SwitchUlt)
            var skills = caster.Skills;

            // 防呆：如果技能列表還沒準備好或長度不夠
            if (skills == null || skills.Count < 2)
            {
                DrawPreviewForSlot(null, meshFilter1, meshRenderer1, circle1, _mesh1);
                DrawPreviewForSlot(null, meshFilter2, meshRenderer2, circle2, _mesh2);
                return;
            }

            // --- 顯示 Slot 0 (固定普攻) ---
            SkillData data0 = skills[0];
            DrawPreviewForSlot(
                data0,
                meshFilter1, meshRenderer1,
                circle1,
                _mesh1);

            // --- 顯示 Slot 1 (切換普攻) ---
            SkillData data1 = skills[1];
            DrawPreviewForSlot(
                data1,
                meshFilter2, meshRenderer2,
                circle2,
                _mesh2);
        }

        // ----------------------------------------------------------------------
        // 以下繪圖邏輯保持原樣 (DrawPreviewForSlot, Mesh 建構等)
        // ----------------------------------------------------------------------

        void DrawPreviewForSlot(
            SkillData data,
            MeshFilter mf,
            MeshRenderer mr,
            LineRenderer lr_circle,
            Mesh mesh)
        {
            if (!data || !mf)
            {
                if (lr_circle) lr_circle.enabled = false;
                if (mf) mf.sharedMesh = null;
                return;
            }

            var comp = SkillCalculator.Compute(data, main ? main.MP : MainPoint.Zero);
            Vector3 origin = caster.firePoint ? (Vector3)caster.firePoint.position : transform.position;

            Vector2 dir = (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f) ? aimSource.AimDir : Vector2.right;
            dir.Normalize();

            float previewRadius = GetProjectileRadiusForPreview(data, dir);
            float dist = Mathf.Max(0.1f, data.BaseRange);
            Vector3 end = origin + (Vector3)(dir * dist);

            if (useObstacleBlock)
            {
                RaycastHit2D hit = Physics2D.CircleCast(origin, previewRadius, dir, dist, obstacleMask);
                if (hit.collider != null)
                {
                    float d = Mathf.Clamp(hit.distance, 0f, dist);
                    end = origin + (Vector3)(dir * d);
                }
            }

            // 建構 Mesh (長條 / 圓形 / 扇形)
            bool drewMesh = false;
            if (mf && mr)
            {
                var mft = mf.transform;

                if (data.HitType == HitType.Single && data.UseProjectile && data.ProjectilePrefab)
                {
                    Vector3 aLocal = mft.InverseTransformPoint(origin);
                    Vector3 bLocal = mft.InverseTransformPoint(end);
                    BuildFlatStrip(mesh, aLocal, bLocal, previewRadius);
                    drewMesh = true;
                }
                else if (data.HitType == HitType.Area)
                {
                    Vector3 centerLocal = mft.InverseTransformPoint(end);
                    float r = Mathf.Max(0.05f, comp.AreaRadius);
                    BuildCircleMesh(mesh, centerLocal, r);
                    drewMesh = true;
                }
                else if (data.HitType == HitType.Cone)
                {
                    Vector3 centerLocal = mft.InverseTransformPoint(origin);
                    float r = dist;
                    float angle = comp.ConeAngle;
                    BuildConeMesh(mesh, centerLocal, dir, r, angle);
                    drewMesh = true;
                }
            }

            if (drewMesh) mf.sharedMesh = mesh;
            else if (mf) mf.sharedMesh = null;

            // 輔助圓圈 (LineRenderer)
            if (lr_circle)
            {
                if (data.HitType == HitType.Single && data.UseProjectile)
                {
                    float r = Mathf.Max(0.02f, previewRadius);
                    DrawCircle(lr_circle, end, r);
                }
                else
                {
                    lr_circle.enabled = false;
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

        void BuildFlatStrip(Mesh m, Vector3 a, Vector3 b, float radius)
        {
            if (m == null) return; m.Clear();
            Vector3 d3 = (b - a);
            if (d3.magnitude < 1e-5f || radius <= 1e-6f) return;

            Vector2 dir = new Vector2(d3.x, d3.y).normalized;
            Vector2 n = new Vector2(-dir.y, dir.x);

            Vector3 p0 = a + (Vector3)(n * radius);
            Vector3 p1 = b + (Vector3)(n * radius);
            Vector3 p2 = b - (Vector3)(n * radius);
            Vector3 p3 = a - (Vector3)(n * radius);

            m.SetVertices(new List<Vector3> { p0, p1, p2, p3 });
            m.SetTriangles(new List<int> { 0, 1, 2, 0, 2, 3 }, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
        }

        void BuildCircleMesh(Mesh m, Vector3 center, float radius)
        {
            if (m == null) return; m.Clear();
            if (radius <= 1e-6f) return;

            int segs = Mathf.Max(12, circleSegments);
            var vertices = new List<Vector3>(segs + 1);
            var triangles = new List<int>(segs * 3);

            vertices.Add(center);
            float step = 2f * Mathf.PI / segs;

            for (int i = 0; i < segs; i++)
            {
                float a = i * step;
                vertices.Add(new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, center.z));
            }

            for (int i = 1; i <= segs; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add((i % segs) + 1);
            }

            m.SetVertices(vertices);
            m.SetTriangles(triangles, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
        }

        void BuildConeMesh(Mesh m, Vector3 center, Vector2 direction, float range, float angle)
        {
            if (m == null) return; m.Clear();
            if (range <= 1e-6f || angle <= 1e-6f) return;

            int segs = Mathf.Max(3, Mathf.RoundToInt(angle / 5f));
            var vertices = new List<Vector3>(segs + 2);
            var triangles = new List<int>(segs * 3);

            vertices.Add(center);

            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float startAngle = baseAngle - angle * 0.5f;

            for (int i = 0; i <= segs; i++)
            {
                float currentAngle = startAngle + (angle / segs) * i;
                float rad = currentAngle * Mathf.Deg2Rad;
                vertices.Add(new Vector3(
                    center.x + Mathf.Cos(rad) * range,
                    center.y + Mathf.Sin(rad) * range,
                    center.z
                ));
            }

            for (int i = 1; i <= segs; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }

            m.SetVertices(vertices);
            m.SetTriangles(triangles, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
        }
    }
}