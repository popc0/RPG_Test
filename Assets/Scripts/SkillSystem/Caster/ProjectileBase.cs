using UnityEngine;
using System.Collections.Generic;
using RPG;

namespace RPG
{
    // 抽象類別：不能直接掛在物件上，必須被繼承
    public abstract class ProjectileBase : MonoBehaviour
    {
        [Header("基礎參數")]
        protected float speed;
        protected float damage;
        protected float maxDuration;
        protected InteractionLayer targetLayer;
        protected LayerMask targetMask;
        protected LayerMask obstacleMask;
        protected Transform owner;
        protected bool isPiercing;

        // 內部狀態
        protected float lifeTimer;
        protected Collider2D col;
        protected ContactFilter2D filter;
        protected HashSet<GameObject> hitHistory = new HashSet<GameObject>();
        protected bool isStopped = false;

        // ★ 抽象方法：子類別必須實作這兩個行為
        protected abstract void OnUpdateMovement(); // 每一格怎麼動？
        protected abstract bool OnHitObstacle();    // 撞牆要不要死？(true=死)

        // ★ 新增：定義這個投射物應該從哪裡生成
        public virtual SpawnAnchorType AnchorType => SpawnAnchorType.Body;

        public virtual void Init(Transform owner, Vector2 dir, SkillData data, SkillComputed comp, LayerMask targetMask, LayerMask obstacleMask)
        {
            this.owner = owner;
            this.targetMask = targetMask;
            this.obstacleMask = obstacleMask;

            this.damage = comp.Damage;
            this.maxDuration = data.MaxDuration;
            this.targetLayer = data.TargetLayer;
            this.isPiercing = data.IsPiercing;
            this.speed = data.ProjectileSpeed;

            col = GetComponent<Collider2D>();
            if (!col) col = GetComponentInChildren<Collider2D>();
            if (col) col.isTrigger = true;

            // 物理過濾器
            filter = new ContactFilter2D { useLayerMask = true, layerMask = targetMask | obstacleMask, useTriggers = true };

            // 強制透視縮放
            transform.localScale = PerspectiveUtils.GlobalScale;

            lifeTimer = 0f;
            isStopped = false;
            hitHistory.Clear();
        }

        protected virtual void Update()
        {
            if (isStopped) return;

            // 1. 生命週期
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= maxDuration)
            {
                StopProjectile();
                return;
            }

            // 2. 移動邏輯 (交給子類別)
            OnUpdateMovement();
        }

        // 共通碰撞判定
        protected void ResolveHit(Collider2D other)
        {
            if (!other || isStopped) return;
            if (owner && other.transform.IsChildOf(owner)) return;

            int layerVal = 1 << other.gameObject.layer;

            // A. 撞牆
            if ((obstacleMask.value & layerVal) != 0)
            {
                if (OnHitObstacle()) StopProjectile();
                return;
            }

            // B. 撞人
            if ((targetMask.value & layerVal) != 0)
            {
                if (EffectApplier.TryResolveOwner(other, out var applier, out var layer) && layer == targetLayer)
                {
                    if (!hitHistory.Contains(applier.gameObject))
                    {
                        applier.ApplyIncomingRaw(damage);
                        hitHistory.Add(applier.gameObject);

                        if (!isPiercing) StopProjectile();
                    }
                }
            }
        }

        public void StopProjectile()
        {
            isStopped = true;
            if (ObjectPool.Instance) ObjectPool.Instance.Despawn(gameObject);
            else Destroy(gameObject);
        }

        // 這是為了讓 AimPreview 能算出寬度 (保持原本邏輯)
        // 為了讓子類別能覆寫或擴充，標記為 virtual
        public virtual bool TryGetColliderDiameter(Vector2 dir, out float diameter)
        {
            diameter = 0f;
            if (!col) col = GetComponent<Collider2D>();
            if (!col) col = GetComponentInChildren<Collider2D>();
            if (!col) return false;

            // 1. 決定測量軸向 (Measure Axis)
            // 預設：物件朝右(X)，所以側面是 Y 軸
            Vector2 measureAxis = Vector2.up;

            // ★ 注意：這裡需要存取子類別的設定 (modelForward, rotationOffsetDeg)
            // 因為這些變數是在子類別定義的，我們有兩種做法：
            // A. 在 ProjectileBase 定義虛擬屬性讓子類別實作 (最乾淨)
            // B. 透過轉型來讀取 (較髒但簡單)

            // 這裡採用 A 方案：定義虛擬屬性
            if (ModelForward == FacingAxis.Up) measureAxis = Vector2.right;

            if (Mathf.Abs(RotationOffsetDeg) > 0.001f)
            {
                measureAxis = Quaternion.Euler(0, 0, RotationOffsetDeg) * measureAxis;
            }

            // 2. 根據 Collider 類型計算投影寬度
            Transform t = col.transform;
            Vector3 s = t.lossyScale;

            if (col is CircleCollider2D circle)
            {
                float maxScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
                diameter = 2f * circle.radius * maxScale;
                return true;
            }
            else if (col is BoxCollider2D box)
            {
                Vector2 size = box.size;
                Vector2 offset = box.offset;
                Vector2[] corners = new Vector2[4];
                corners[0] = offset + new Vector2(-size.x, -size.y) * 0.5f;
                corners[1] = offset + new Vector2(size.x, -size.y) * 0.5f;
                corners[2] = offset + new Vector2(size.x, size.y) * 0.5f;
                corners[3] = offset + new Vector2(-size.x, size.y) * 0.5f;
                return CalculateProjectionWidth(corners, t, measureAxis, out diameter);
            }
            else if (col is PolygonCollider2D poly)
            {
                return CalculateProjectionWidth(poly.points, t, measureAxis, out diameter);
            }
            else if (col is CapsuleCollider2D cap)
            {
                Vector2 size = cap.size;
                float w = (cap.direction == CapsuleDirection2D.Horizontal) ? size.y : size.x;
                float axisScale = (cap.direction == CapsuleDirection2D.Horizontal) ? Mathf.Abs(s.y) : Mathf.Abs(s.x);
                diameter = w * axisScale;
                return true;
            }

            // Fallback: Bounds
            var b = col.bounds;
            Vector3 min = b.min; Vector3 max = b.max;
            Vector2[] bCorners = new Vector2[] { new Vector2(min.x, min.y), new Vector2(max.x, min.y), new Vector2(max.x, max.y), new Vector2(min.x, max.y) };
            return CalculateProjectionWidth(bCorners, null, measureAxis, out diameter);
        }

        // 核心運算
        private bool CalculateProjectionWidth(Vector2[] points, Transform pointSpace, Vector2 axis, out float width)
        {
            if (points == null || points.Length == 0) { width = 0f; return false; }
            float minProj = float.PositiveInfinity;
            float maxProj = float.NegativeInfinity;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 worldPt = (pointSpace != null) ? (Vector2)pointSpace.TransformPoint(points[i]) : points[i];
                float proj = Vector2.Dot(worldPt, axis);
                if (proj < minProj) minProj = proj;
                if (proj > maxProj) maxProj = proj;
            }
            width = Mathf.Max(0f, maxProj - minProj);
            return width > 0f;
        }
        // ============================================================
        // ★ 新增：計算投射物長度/半徑 (給 Cone 預覽用)
        // ============================================================
        public virtual bool TryGetColliderRadius(out float radius)
        {
            radius = 0f;
            if (!col) col = GetComponent<Collider2D>();
            if (!col) col = GetComponentInChildren<Collider2D>();
            if (!col) return false;

            // 1. 決定測量軸向 (Forward Axis)
            // 寬度是量側面 (Up)，長度是量正面 (Right)
            Vector2 measureAxis = Vector2.right;

            if (ModelForward == FacingAxis.Up) measureAxis = Vector2.up;

            // 考慮額外旋轉
            if (Mathf.Abs(RotationOffsetDeg) > 0.001f)
            {
                measureAxis = Quaternion.Euler(0, 0, RotationOffsetDeg) * measureAxis;
            }

            // 2. 投影計算
            Transform t = col.transform;

            // 這裡邏輯跟寬度一樣，根據 Collider 類型抓點
            if (col is CircleCollider2D circle)
            {
                // 圓形的半徑 = Offset距離 + 半徑
                // 簡單估算：投影 Offset 到軸上 + 半徑 * Scale
                Vector2 worldCenter = t.TransformPoint(circle.offset);
                Vector3 s = t.lossyScale;
                // 投影中心點到測量軸的距離 (相對於 Pivot，假設 Pivot 在 0,0)
                // 但因為這是 Prefab 狀態，Pivot 就是 transform.position
                // 我們要算的是 "World Center - Pivot" 在 measureAxis 上的投影
                Vector2 relativeCenter = worldCenter - (Vector2)transform.position;
                float centerProj = Vector2.Dot(relativeCenter, measureAxis);

                // 加上半徑 (取對應軸的 scale)
                float axisScale = (ModelForward == FacingAxis.Up) ? Mathf.Abs(s.y) : Mathf.Abs(s.x);
                radius = centerProj + circle.radius * axisScale;
                return true;
            }
            else if (col is BoxCollider2D box)
            {
                Vector2 size = box.size; Vector2 offset = box.offset;
                Vector2[] corners = new Vector2[4];
                corners[0] = offset + new Vector2(-size.x, -size.y) * 0.5f;
                corners[1] = offset + new Vector2(size.x, -size.y) * 0.5f;
                corners[2] = offset + new Vector2(size.x, size.y) * 0.5f;
                corners[3] = offset + new Vector2(-size.x, size.y) * 0.5f;
                return CalculateProjectionLength(corners, t, measureAxis, out radius);
            }
            else if (col is PolygonCollider2D poly)
            {
                return CalculateProjectionLength(poly.points, t, measureAxis, out radius);
            }
            else if (col is CapsuleCollider2D cap)
            {
                // 膠囊簡化處理
                float h = (cap.direction == CapsuleDirection2D.Vertical) ? cap.size.y : cap.size.x;
                // 假設膠囊中心在 offset，長度延伸一半
                Vector2 worldCenter = t.TransformPoint(cap.offset);
                Vector2 relativeCenter = worldCenter - (Vector2)transform.position;
                float centerProj = Vector2.Dot(relativeCenter, measureAxis);

                // 加上一半長度 (粗略)
                Vector3 s = t.lossyScale;
                float axisScale = (cap.direction == CapsuleDirection2D.Vertical) ? Mathf.Abs(s.y) : Mathf.Abs(s.x);
                radius = centerProj + (h * 0.5f) * axisScale;
                return true;
            }

            return false;
        }

        // 專用投影：只取最大值 (Max Projection)，代表離 Pivot 最遠的距離
        private bool CalculateProjectionLength(Vector2[] points, Transform pointSpace, Vector2 axis, out float length)
        {
            if (points == null || points.Length == 0) { length = 0f; return false; }

            float maxProj = float.NegativeInfinity;

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 worldPt = (pointSpace != null) ? (Vector2)pointSpace.TransformPoint(points[i]) : points[i];

                // 算出相對於 Projectile Root (Pivot) 的向量
                Vector2 rel = worldPt - (Vector2)transform.position;

                // 投影到前方軸
                float proj = Vector2.Dot(rel, axis);

                if (proj > maxProj) maxProj = proj;
            }

            length = Mathf.Max(0f, maxProj); // 半徑不可能是負的
            return length > 0f;
        }

        // ============================================================
        // ★ 虛擬屬性 (讓子類別提供方向資訊)
        // ============================================================
        protected virtual FacingAxis ModelForward => FacingAxis.Right;
        protected virtual float RotationOffsetDeg => 0f;
    }
}