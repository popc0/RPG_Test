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

        [Header("顯示（技能槽 1）")] // <--- 標記為槽位 1
        public LineRenderer circle1;               // 槽位 1 終端或範圍圈
        public MeshFilter meshFilter1;             // 槽位 1 膠囊 Mesh
        public MeshRenderer meshRenderer1;         // 槽位 1 外觀材質

        [Header("顯示（技能槽 2）")] // <--- 標記為槽位 2 (新增)
        public LineRenderer circle2;               // 槽位 2 終端或範圍圈
        public MeshFilter meshFilter2;             // 槽位 2 膠囊 Mesh
        public MeshRenderer meshRenderer2;         // 槽位 2 外觀材質

        [ Header("線條設定")]
        public int circleSegments=48;

        Mesh _mesh1; // 對應槽位 1 的 Mesh
        Mesh _mesh2; // 對應槽位 2 的 Mesh (新增)

        void Awake()
        {
            // ... 引用獲取不變 ...

            // 初始化槽位 1 的 Mesh
            if (meshFilter1 && _mesh1 == null)
            {
                _mesh1 = new Mesh { name = "AimPreview_Strip_1" };
                meshFilter1.sharedMesh = _mesh1;
            }
            // 初始化槽位 2 的 Mesh (新增)
            if (meshFilter2 && _mesh2 == null)
            {
                _mesh2 = new Mesh { name = "AimPreview_Strip_2" };
                meshFilter2.sharedMesh = _mesh2;
            }
        }

        void OnDisable()
        {
            // 清理槽位 1
            if (circle1) circle1.enabled = false;
            if (meshFilter1) meshFilter1.sharedMesh = null;

            // 清理槽位 2 (新增)
            if (circle2) circle2.enabled = false;
            if (meshFilter2) meshFilter2.sharedMesh = null;
        }

        // 核心邏輯：將 LateUpdate 中的代碼封裝到這個新方法中
        void DrawPreviewForSlot(
            SkillData data,
            MeshFilter mf,
            MeshRenderer mr,
            LineRenderer lr_circle,
            Mesh mesh) // 接收要繪製的視覺組件和 Mesh 實例
        {
            // 如果沒有資料或沒有視覺組件，則跳過並清理
            if (!data || !mf)
            {
                if (lr_circle) lr_circle.enabled = false;
                if (mf) mf.sharedMesh = null;
                return;
            }

            // --- 以下是原 LateUpdate 內的所有計算和繪圖邏輯 ---

            var comp = SkillCalculator.Compute(data, main ? main.MP : MainPoint.Zero);
            Vector3 origin = caster.firePoint ? (Vector3)caster.firePoint.position : transform.position;

            // 方向 (AimSource2D 只需要讀取一次)
            Vector2 dir = (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f) ? aimSource.AimDir : Vector2.right;
            dir.Normalize();

            // 投射物半徑（遮蔽與線寬）
            float previewRadius = GetProjectileRadiusForPreview(data, dir);

            // 射程與終點（考慮障礙物與子彈寬度）
            float dist = Mathf.Max(0.1f, data.BaseRange);
            Vector3 end = origin + (Vector3)(dir * dist);
            if (useObstacleBlock)
            {
                // 注意：這裡使用 previewRadius 進行 CircleCast
                RaycastHit2D hit = Physics2D.CircleCast(origin, previewRadius, dir, dist, obstacleMask);
                if (hit.collider != null)
                {
                    float d = Mathf.Clamp(hit.distance, 0f, dist);
                    end = origin + (Vector3)(dir * d);
                }
            }

            // ===== 中段 Mesh（無端帽）=====
            bool drewMesh = false;
            if (mf && mr && data.HitType == HitType.Single && data.UseProjectile && data.ProjectilePrefab)
            {
                var mft = mf.transform;
                Vector3 aLocal = mft.InverseTransformPoint(origin);
                Vector3 bLocal = mft.InverseTransformPoint(end);

                BuildFlatStrip(mesh, aLocal, bLocal, previewRadius);
                mf.sharedMesh = mesh;
                drewMesh = true;
            }
            if (!drewMesh && mf) mf.sharedMesh = null;

            // ===== 主線條預覽 =====
            /*
            if (lr_line) // <--- 使用傳入的 lr_line
            {
                lr_line.enabled = true; lr_line.useWorldSpace = true; lr_line.positionCount = 2;
                lr_line.SetPosition(0, origin); lr_line.SetPosition(1, end);
                float width = fallbackLineWidth;
                if (data.HitType == HitType.Single && data.UseProjectile)
                {
                    float d = 2f * Mathf.Max(0.0f, previewRadius);
                    width = Mathf.Clamp(d, 0.0f, maxPreviewWidth);
                }
                lr_line.startWidth = width; lr_line.endWidth = width;
            }
            */
            // ===== 終端／範圍圈 =====
            if (lr_circle) // <--- 使用傳入的 lr_circle
            {
                if (data.HitType == HitType.Area)
                {
                    // 範圍技能 → 畫 AoE 圓圈
                    float r = Mathf.Max(0.05f, comp.AreaRadius);
                    DrawCircle(lr_circle, end, r); // <--- 使用傳入的 lr_circle
                }
                else if (data.HitType == HitType.Single && data.UseProjectile)
                {
                    // 單體投射 → 畫小終端圈
                    float r = Mathf.Max(0.02f, previewRadius);
                    DrawCircle(lr_circle, end, r); // <--- 使用傳入的 lr_circle
                }
                else
                {
                    lr_circle.enabled = false;
                }
            }
        }
        void LateUpdate()
        {
            if (!caster || caster.Skills == null || caster.Skills.Count == 0)
            {
                // 如果沒有技能，確保所有預覽都關閉
                DrawPreviewForSlot(null, meshFilter1, meshRenderer1, circle1, _mesh1);
                DrawPreviewForSlot(null, meshFilter2, meshRenderer2, circle2, _mesh2);
                return;
            }

            var skills = caster.Skills; // 獲取當前技能組清單
            int maxIndex = skills.Count - 1;

            // --- 處理技能槽位 1 ---
            int index1 = Mathf.Clamp(caster.skillSlotIndex1, 0, maxIndex);
            SkillData data1 = skills[index1];

            DrawPreviewForSlot(
                data1,
                meshFilter1, meshRenderer1,
                circle1,
                _mesh1); // 傳入 slot 1 的資料和視覺組件

            // --- 處理技能槽位 2 ---
            int index2 = Mathf.Clamp(caster.skillSlotIndex2, 0, maxIndex);
            SkillData data2 = skills[index2];

            DrawPreviewForSlot(
                data2,
                meshFilter2, meshRenderer2,
                circle2,
                _mesh2); // 傳入 slot 2 的資料和視覺組件
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
