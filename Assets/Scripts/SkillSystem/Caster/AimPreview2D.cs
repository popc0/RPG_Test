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

        [Header("顯示（Slot 0: 固定普攻）")]
        public MeshFilter meshFilter1;
        public MeshRenderer meshRenderer1;

        [Header("顯示（Slot 1: 切換普攻）")]
        public MeshFilter meshFilter2;
        public MeshRenderer meshRenderer2;

        [Header("網格設定")]
        public int circleSegments = 48;

        Mesh _mesh1; // Slot 0
        Mesh _mesh2; // Slot 1

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
            if (meshFilter1) meshFilter1.sharedMesh = null;
            if (meshFilter2) meshFilter2.sharedMesh = null;
        }

        void LateUpdate()
        {
            if (!caster) return;

            var skills = caster.Skills;

            // 防呆
            if (skills == null || skills.Count < 2)
            {
                DrawPreviewForSlot(null, meshFilter1, meshRenderer1, _mesh1);
                DrawPreviewForSlot(null, meshFilter2, meshRenderer2, _mesh2);
                return;
            }

            // Slot 0 & 1
            DrawPreviewForSlot(skills[0], meshFilter1, meshRenderer1, _mesh1);
            DrawPreviewForSlot(skills[1], meshFilter2, meshRenderer2, _mesh2);
        }

        void DrawPreviewForSlot(SkillData data, MeshFilter mf, MeshRenderer mr, Mesh mesh)
        {
            if (!data || !mf) { if (mf) mf.sharedMesh = null; return; }

            // 1. 準備數據
            var comp = SkillCalculator.Compute(data, main ? main.MP : MainPoint.Zero);
            Vector3 origin = transform.position;
            if (caster.executor != null && caster.executor.firePoint != null)
                origin = caster.executor.firePoint.position;

            Vector2 dir = (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f) ? aimSource.AimDir : Vector2.right;
            dir.Normalize(); // 確保是單位向量

            // 1. 取得透視倍率
            float scaleFactor = PerspectiveUtils.GetVisualScaleFactor(dir);

            // ============================================================
            // ★ 修改：關閉寬度的透視計算
            // ============================================================
            // [原版] Vector3 scale = PerspectiveUtils.GlobalScale; // 會導致線條被壓扁

            // [修改] 改用 Vector3.one (1, 1, 1)
            // 這樣傳進去 BuildFlatStrip 的時候，就不會對寬度做額外的縮放了
            Vector3 scale = Vector3.one;

            // 2. 計算視覺終點
            // 物理射程 10 * 倍率 0.5 = 視覺射程 5 (往上)
            float visualDist = Mathf.Max(0.1f, data.BaseRange * scaleFactor);
            Vector3 end = origin + (Vector3)(dir * visualDist);

            float radius = GetProjectileRadiusForPreview(data, dir);

            // 障礙物判定
            if (useObstacleBlock)
            {
                RaycastHit2D hit = Physics2D.CircleCast(origin, radius, dir, visualDist, obstacleMask);
                if (hit.collider != null)
                {
                    end = origin + (Vector3)(dir * Mathf.Max(0f, hit.distance));
                }
            }

            // 3. 建構 Mesh (傳入 scale 參數來做變形)
            var mft = mf.transform;
            bool drewMesh = false;

            // 轉換為本地座標 (Mesh 是畫在 Local Space)
            Vector3 startLocal = mft.InverseTransformPoint(origin);
            Vector3 endLocal = mft.InverseTransformPoint(end);

            if (data.HitType == HitType.Single)
            {
                // 直線：使用 Start/End 和 半徑，並套用 Scale 壓扁寬度
                // 注意：startLocal 到 endLocal 的長度已經是 visualDist (縮放過的)
                // 我們只需要處理「寬度」的縮放
                BuildFlatStrip(mesh, startLocal, endLocal, radius, scale);
                drewMesh = true;
            }
            else if (data.HitType == HitType.Area)
            {
                // 圓形 AoE：變成橢圓 (Apply Scale)
                float r = Mathf.Max(0.05f, comp.AreaRadius);
                BuildCircleMesh(mesh, endLocal, r, scale);
                drewMesh = true;
            }
            else if (data.HitType == HitType.Cone)
            {
                // 扇形：套用 Scale 變成壓扁的扇形
                // 這裡我們傳入原始物理 range，在 Build 裡面用 Scale 轉成視覺頂點
                float physicalRange = Mathf.Max(0.1f, data.BaseRange);
                BuildConeMesh(mesh, startLocal, dir, physicalRange, comp.ConeAngle, scale);
                drewMesh = true;
            }

            if (drewMesh) mf.sharedMesh = mesh;
            else mf.sharedMesh = null;
        }

        // === 輔助方法 ===

        float GetProjectileRadiusForPreview(SkillData data, Vector2 dir)
        {
            // [修改] 預設給一個最小值，不再依賴 data.ProjectileRadius
            float radius = 0.1f;

            // [修改] 不再檢查 UseProjectile，只看有沒有 Prefab
            if (data.ProjectilePrefab != null)
            {
                // 嘗試從 Prefab 取得實際寬度
                if (data.ProjectilePrefab.TryGetColliderDiameter(dir, out float diameter))
                {
                    radius = Mathf.Max(radius, diameter * 0.5f);
                }
            }

            return radius;
        }

        // 1. 長條 (Strip)
        void BuildFlatStrip(Mesh m, Vector3 start, Vector3 end, float radius, Vector3 scale)
        {
            m.Clear();
            Vector3 diff = end - start;
            if (diff.sqrMagnitude < 1e-6f) return;

            // 計算垂直向量 (未縮放的寬度方向)
            Vector2 dir = new Vector2(diff.x, diff.y).normalized;
            Vector2 n = new Vector2(-dir.y, dir.x); // 法向量

            // ★ 關鍵：將法向量乘上 Scale (讓寬度也受到透視影響：垂直時變粗，水平時變細)
            // 其實如果是單體投射物，通常它的寬度是由 ProjectilePrefab 的旋轉決定的
            // 這裡我們簡單處理：把寬度向量直接乘上全域縮放
            // 這樣往上打時，Scale.x (1) 作用在寬度上 (維持寬度)
            // 往右打時，Scale.y (0.5) 作用在寬度上 (變細) -> 這符合 2.5D 透視
            Vector3 offset = new Vector3(n.x * scale.x, n.y * scale.y, 0f) * radius;

            // 如果你不希望「預覽線的粗細」受透視影響，只希望「長度」受影響，
            // 請改用： Vector3 offset = (Vector3)n * radius; (不乘 scale)

            Vector3 p0 = start + offset;
            Vector3 p1 = end + offset;
            Vector3 p2 = end - offset;
            Vector3 p3 = start - offset;

            m.SetVertices(new List<Vector3> { p0, p1, p2, p3 });
            m.SetTriangles(new List<int> { 0, 1, 2, 0, 2, 3 }, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
        }

        // 2. 圓形 (Circle -> Ellipse)
        void BuildCircleMesh(Mesh m, Vector3 center, float radius, Vector3 scale)
        {
            m.Clear();
            int segs = Mathf.Max(12, circleSegments);
            var vertices = new List<Vector3>(segs + 1);
            var triangles = new List<int>(segs * 3);

            vertices.Add(center);
            float step = 2f * Mathf.PI / segs;

            for (int i = 0; i < segs; i++)
            {
                float a = i * step;
                // 原始圓形座標
                float x = Mathf.Cos(a) * radius;
                float y = Mathf.Sin(a) * radius;

                // ★ 套用透視縮放：變成橢圓
                Vector3 pos = new Vector3(
                    center.x + x * scale.x,
                    center.y + y * scale.y,
                    center.z
                );
                vertices.Add(pos);
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

        // 3. 扇形 (Cone -> Squashed Cone)
        void BuildConeMesh(Mesh m, Vector3 center, Vector2 direction, float physicalRange, float angle, Vector3 scale)
        {
            m.Clear();
            int segs = Mathf.Max(3, Mathf.RoundToInt(angle / 5f));
            var vertices = new List<Vector3>(segs + 2);
            var triangles = new List<int>(segs * 3);

            vertices.Add(center);

            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float startAngle = baseAngle - angle * 0.5f;

            for (int i = 0; i <= segs; i++)
            {
                float currentAngle = (startAngle + (angle / segs) * i) * Mathf.Deg2Rad;

                // 原始扇形座標 (物理空間)
                float rawX = Mathf.Cos(currentAngle) * physicalRange;
                float rawY = Mathf.Sin(currentAngle) * physicalRange;

                // ★ 套用透視縮放 (變成壓扁的扇形)
                Vector3 pos = new Vector3(
                    center.x + rawX * scale.x,
                    center.y + rawY * scale.y,
                    center.z
                );
                vertices.Add(pos);
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