using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    // 1. 修改介面：多加一個 Vector3 endWorld
    public interface IPreviewDrawer
    {
        void Draw(AimPreview2D context, MeshFilter mf, SkillData data, Vector3 startLocal, Vector3 endLocal, Vector3 endWorld, Vector2 dir, float radius, Vector3 scale);
    }

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

        [Header("顯示設定 - Slot 0 (固定普攻)")]
        [Tooltip("請將負責『縮放/位置』的父物件拖曳到這裡")]
        public Transform squashRoot1;
        [Tooltip("請將負責『旋轉/顯示』的子物件(有MeshFilter)拖曳到這裡")]
        public MeshFilter meshFilter1;
        public MeshRenderer meshRenderer1;

        [Header("顯示設定 - Slot 1 (切換普攻)")]
        [Tooltip("請將負責『縮放/位置』的父物件拖曳到這裡")]
        public Transform squashRoot2;
        [Tooltip("請將負責『旋轉/顯示』的子物件(有MeshFilter)拖曳到這裡")]
        public MeshFilter meshFilter2;
        public MeshRenderer meshRenderer2;

        [Header("形狀測量設定")]
        [Tooltip("為了測量 PolygonCollider 形狀，會暫時生成一個物件。請指定一個看不到的空曠座標。")]
        public Vector3 measurePosition = new Vector3(0, -1000, 0);

        [Header("網格設定")]
        public int circleSegments = 48;

        Mesh _mesh1; // Slot 0
        Mesh _mesh2; // Slot 1

        // 策略字典
        private Dictionary<HitType, IPreviewDrawer> _drawers;

        // 公開 helper 讓 Drawer 呼叫
        public Dictionary<int, Mesh> ColliderMeshCache = new Dictionary<int, Mesh>();
        public Dictionary<MeshFilter, Transform> SquashRoots = new Dictionary<MeshFilter, Transform>();

        void Awake()
        {
            if (meshFilter1 && _mesh1 == null) { _mesh1 = new Mesh { name = "AimPreview_0" }; meshFilter1.sharedMesh = _mesh1; }
            if (meshFilter2 && _mesh2 == null) { _mesh2 = new Mesh { name = "AimPreview_1" }; meshFilter2.sharedMesh = _mesh2; }

            // ★ 修改：直接註冊你拖進來的父物件，不再動態生成
            if (meshFilter1 && squashRoot1) SquashRoots[meshFilter1] = squashRoot1;
            if (meshFilter2 && squashRoot2) SquashRoots[meshFilter2] = squashRoot2;
            // ============================================================
            // ★ 補上這一段：初始化 Drawer 策略字典！
            // ============================================================
            _drawers = new Dictionary<HitType, IPreviewDrawer>
            {
                { HitType.Single, new PreviewDrawerLinear() },
                { HitType.Area,   new PreviewDrawerArea() },
                { HitType.Cone,   new PreviewDrawerCone() }
            };
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

        void DrawPreviewForSlot(SkillData data, MeshFilter mf, MeshRenderer mr, Mesh dynamicMesh)
        {
            if (!data || !mf) { if (mf) mf.sharedMesh = null; return; }

            // 取得父物件
            if (!SquashRoots.TryGetValue(mf, out Transform squashRoot))
            {
                // 如果你在 Inspector 忘了拖 squashRoot，這裡暫時用自己當 root (雖然效果會錯，但至少不報錯)
                squashRoot = mf.transform;
            }
            // 1. 準備數據
            var comp = SkillCalculator.Compute(data, main ? main.MP : MainPoint.Zero);
            // ============================================================
            // ★ 修改：決定起點 (Origin)
            // ============================================================
            Vector3 origin = transform.position; // 預設

            if (caster != null)
            {
                // 問 Caster：這個技能的 Prefab 應該從哪裡發射？
                origin = caster.GetCurrentOrigin(data.ProjectilePrefab);
            }
            // ============================================================
            Vector2 dir = (aimSource && aimSource.AimDir.sqrMagnitude > 0.0001f) ? aimSource.AimDir : Vector2.right;
            dir.Normalize();

            // 1. 取得透視倍率
            float scaleFactor = PerspectiveUtils.GetVisualScaleFactor(dir);

            // 取得全域透視 (用於 Area 的整體壓縮)
            Vector3 globalScale = PerspectiveUtils.GlobalScale;

            // 2. 計算視覺終點
            float visualDist = Mathf.Max(0.1f, data.BaseRange * scaleFactor);

            // ★ 加入防呆：如果 dir 壞掉了，強制修復
            if (float.IsNaN(dir.x) || float.IsNaN(dir.y)) dir = Vector2.right;

            // LOS / 障礙物計算
            Vector3 end = origin + (Vector3)(dir * visualDist);
            float radius = GetProjectileRadiusForPreview(data, dir);

            // 障礙物判定 
            if (useObstacleBlock)
            {
                // 分流處理障礙物
                if (data.HitType == HitType.Area)
                {
                    RaycastHit2D hit = Physics2D.Raycast(origin, dir, visualDist, obstacleMask);
                    if (hit.collider != null) end = origin + (Vector3)(dir * Mathf.Max(0f, hit.distance));
                }
                else
                {
                    RaycastHit2D hit = Physics2D.CircleCast(origin, radius, dir, visualDist, obstacleMask);
                    if (hit.collider != null) end = origin + (Vector3)(dir * Mathf.Max(0f, hit.distance));
                }
            }

            // 轉 Local (給 Single/Cone 動態網格用，Area 不需要這個)
            Vector3 startLocal = mf.transform.InverseTransformPoint(origin);
            Vector3 endLocal = mf.transform.InverseTransformPoint(end);

            // ★ 核心：傳入 end (這是世界座標)
            if (_drawers.TryGetValue(data.HitType, out var drawer))
            {
                mf.sharedMesh = dynamicMesh;
                drawer.Draw(this, mf, data, startLocal, endLocal, end, dir, radius, globalScale);
            }
        }

        // ============================================================
        // 從 Prefab 提取形狀 (不含旋轉邏輯，純粹抓形狀)
        // ============================================================
        public Mesh GetMeshFromPrefab(ProjectileBase prefab)
        {
            if (prefab == null) return null;
            int id = prefab.GetInstanceID();

            if (ColliderMeshCache.TryGetValue(id, out Mesh cached)) return cached;

            // 為了讓 CreateMesh 成功，必須實例化
            Collider2D colPrefab = prefab.GetComponent<Collider2D>();
            if (!colPrefab) colPrefab = prefab.GetComponentInChildren<Collider2D>();
            if (!colPrefab) return null;

            Mesh newMesh = null;

            if (colPrefab is PolygonCollider2D)
            {
                GameObject tempObj = Instantiate(prefab.gameObject);
                tempObj.transform.position = measurePosition; // 使用較近的座標
                tempObj.SetActive(true);

                PolygonCollider2D tempPoly = tempObj.GetComponent<Collider2D>() as PolygonCollider2D;
                if (!tempPoly) tempPoly = tempObj.GetComponentInChildren<Collider2D>() as PolygonCollider2D;

                if (tempPoly)
                {
                    newMesh = tempPoly.CreateMesh(false, false);

                    // ★ 新增：強制重算邊界，防止生成出壞掉的 Bounds
                    if (newMesh != null)
                    {
                        newMesh.RecalculateBounds();
                    }
                }
                Destroy(tempObj);
            }
            else if (colPrefab is BoxCollider2D box)
            {
                newMesh = new Mesh();
                BuildQuadForBox(newMesh, box);
            }
            else if (colPrefab is CircleCollider2D circle)
            {
                newMesh = new Mesh();
                BuildCircleForCollider(newMesh, circle);
            }

            if (newMesh != null)
            {
                newMesh.name = $"Shape_{prefab.name}";
                ColliderMeshCache[id] = newMesh;
            }

            return newMesh;
        }

        // 輔助：畫方形 Mesh
        void BuildQuadForBox(Mesh m, BoxCollider2D box)
        {
            Vector2 size = box.size;
            Vector2 off = box.offset;
            Vector3[] verts = new Vector3[4];
            // 順序：左下 -> 右下 -> 右上 -> 左上
            verts[0] = off + new Vector2(-size.x, -size.y) * 0.5f;
            verts[1] = off + new Vector2(size.x, -size.y) * 0.5f;
            verts[2] = off + new Vector2(size.x, size.y) * 0.5f;
            verts[3] = off + new Vector2(-size.x, size.y) * 0.5f;

            m.vertices = verts;
            // 兩個三角形組成矩形
            m.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        }

        // 輔助：畫圓形 Mesh (用於 CircleCollider)
        void BuildCircleForCollider(Mesh m, CircleCollider2D circle)
        {
            int segs = 32;
            Vector3[] verts = new Vector3[segs + 1];
            int[] tris = new int[segs * 3];
            float r = circle.radius;
            Vector2 off = circle.offset;

            verts[0] = off; // 中心點
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / segs * Mathf.PI * 2f;
                verts[i + 1] = off + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }

            for (int i = 0; i < segs; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                // 連接到下一點，如果是最後一點則連回起點(1)
                tris[i * 3 + 2] = (i == segs - 1) ? 1 : i + 2;
            }
            m.vertices = verts;
            m.triangles = tris;
        }

        // === 原本的輔助方法 (保持不變) ===
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
        public void BuildFlatStrip(Mesh m, Vector3 start, Vector3 end, float radius, Vector3 scale)
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
        public void BuildCircleMesh(Mesh m, Vector3 center, float radius, Vector3 scale)
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
        public void BuildConeMesh(Mesh m, Vector3 center, Vector2 direction, float physicalRange, float angle, Vector3 scale)
        {
            m.Clear();
            int segs = Mathf.Max(3, Mathf.RoundToInt(angle / 5f));
            var vertices = new List<Vector3>(segs + 2);
            var triangles = new List<int>(segs * 3);

            vertices.Add(center);

            // ============================================================
            // ★ 修正：預覽線也要還原角度，才能對齊滑鼠
            // ============================================================
            float logicY = direction.y / scale.y;
            float logicX = direction.x / scale.x;
            float baseAngle = Mathf.Atan2(logicY, logicX) * Mathf.Rad2Deg;
            // ============================================================

            float startAngle = baseAngle - angle * 0.5f;

            for (int i = 0; i <= segs; i++)
            {
                float currentAngle = (startAngle + (angle / segs) * i) * Mathf.Deg2Rad;

                // 算出邏輯圓上的點 (未壓縮)
                float rawX = Mathf.Cos(currentAngle) * physicalRange;
                float rawY = Mathf.Sin(currentAngle) * physicalRange;

                // 乘上 scale 壓扁後，就會剛好變成正確的視覺角度
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
    // --- Drawer 實作 ---

    public class PreviewDrawerLinear : IPreviewDrawer
    {
        // ★ 參數 dir 改為 Vector2
        public void Draw(AimPreview2D ctx, MeshFilter mf, SkillData data, Vector3 start, Vector3 end, Vector3 endWorld, Vector2 dir, float radius, Vector3 scale)
        {
            if (ctx.SquashRoots.TryGetValue(mf, out var root))
            {
                root.localPosition = Vector3.zero;
                root.localScale = Vector3.one;
                root.localRotation = Quaternion.identity;
            }
            mf.transform.localPosition = Vector3.zero;
            mf.transform.localRotation = Quaternion.identity;
            mf.transform.localScale = Vector3.one;

            ctx.BuildFlatStrip(mf.sharedMesh, start, end, radius, scale);
        }
    }

    public class PreviewDrawerArea : IPreviewDrawer
    {
        // ★ 參數 dir 改為 Vector2
        public void Draw(AimPreview2D ctx, MeshFilter mf, SkillData data, Vector3 start, Vector3 end, Vector3 endWorld, Vector2 dir, float radius, Vector3 scale)
        {
            if (ctx.SquashRoots.TryGetValue(mf, out var root))
            {
                // ★★★ 關鍵修正：使用世界座標設定位置 ★★★
                // 原本寫 root.localPosition = end; (end是local) -> 導致遞迴亂跳
                // 現在改用 root.position = endWorld; -> 穩定鎖定目標點
                root.position = endWorld;

                root.localScale = scale;  // 壓扁
            }

            float angle = 0f;

            // ★ 因為 SkillData.ProjectilePrefab 現在是 ProjectileBase，所以可以用 is 判斷
            if (data.ProjectilePrefab is ProjectileArea areaProj && areaProj.alignRotation)
            {
                angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (areaProj.modelForward == FacingAxis.Up) angle -= 90f;
                angle += areaProj.rotationOffsetDeg;
            }
            // 兼容舊的 ProjectileLinear (如果有人拿它當 Area 用)
            else if (data.ProjectilePrefab is ProjectileLinear linProj && linProj.alignRotation)
            {
                angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (linProj.modelForward == FacingAxis.Up) angle -= 90f;
                angle += linProj.rotationOffsetDeg;
            }

            mf.transform.localRotation = Quaternion.Euler(0, 0, angle);
            mf.transform.localPosition = Vector3.zero;
            // 移除 "as Projectile2D"，直接傳入即可
            Mesh shape = ctx.GetMeshFromPrefab(data.ProjectilePrefab);
            if (shape) mf.sharedMesh = shape;
            else
            {
                float r = Mathf.Max(0.05f, 1f);
                // 嘗試獲取半徑
                if (data.ProjectilePrefab != null && data.ProjectilePrefab.TryGetColliderDiameter(Vector2.up, out float diameter))
                {
                    r = diameter * 0.5f;
                }
                ctx.BuildCircleMesh(mf.sharedMesh, Vector3.zero, r, Vector3.one);
            }
        }
    }

    public class PreviewDrawerCone : IPreviewDrawer
    {
        // ★ 參數 dir 改為 Vector2
        public void Draw(AimPreview2D ctx, MeshFilter mf, SkillData data, Vector3 start, Vector3 end, Vector3 endWorld, Vector2 dir, float radius, Vector3 scale)
        {
            // 1. 重置父物件 (Cone 是動態算點，不需要父層縮放)
            if (ctx.SquashRoots.TryGetValue(mf, out var root))
            {
                root.localPosition = Vector3.zero;
                root.localScale = Vector3.one; // 重置 Scale
                root.localRotation = Quaternion.identity;
            }

            // 2. 重置子物件
            mf.transform.localPosition = Vector3.zero;
            mf.transform.localRotation = Quaternion.identity;
            mf.transform.localScale = Vector3.one;

            // 3. 取得數值
            var comp = SkillCalculator.Compute(data, ctx.main ? ctx.main.MP : MainPoint.Zero);
            float angle = comp.ConeAngle;

            // ★ 修改：使用 ProjectileBase 的投影計算方法
            float physicalRange = 0f;
            if (data.ProjectilePrefab != null)
            {
                data.ProjectilePrefab.TryGetColliderRadius(out physicalRange);
            }
            // 如果 Prefab 沒設定 (例如是舊版)，才退回使用 BaseRange
            if (physicalRange <= 0.01f) physicalRange = Mathf.Max(0.1f, data.BaseRange);

            // 4. 畫圖 (傳入 GlobalScale 讓它變扁)
            ctx.BuildConeMesh(mf.sharedMesh, start, dir, physicalRange, angle, scale);
        }
    }
}