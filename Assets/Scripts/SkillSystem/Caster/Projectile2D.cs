using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 用於排序
using RPG;

namespace RPG
{
    [DisallowMultipleComponent]
    public class Projectile2D : MonoBehaviour
    {
        #region 1. 參數與設定 (Settings)
        [Header("執行參數")]
        public Transform owner;
        public InteractionLayer targetLayer;
        public LayerMask targetMask;
        public LayerMask obstacleMask;

        [Header("數值")]
        public float speed;          // 飛行速度 或 旋轉速度(度/秒)
        public float damage;
        public float maxDuration;    // ★ 統一生死判斷標準

        [Header("行為開關")]
        public bool isPiercing = false;
        public bool alignRotation = true; 

        [Header("模型設定")]
        public FacingAxis modelForward = FacingAxis.Right; // 圖片原本朝哪？
        public float rotationOffsetDeg = 0f;               // 額外修正角度
        public Transform modelTransform;                   // 視覺子物件
        #endregion

        #region 2. 內部狀態 (State)
        // 元件
        private Collider2D _col;
        private ContactFilter2D _filter;
        private readonly RaycastHit2D[] _hitsBuf = new RaycastHit2D[16];

        // 運動狀態
        private Vector2 _dir = Vector2.right; // 飛行方向
        private float _lifeTimer = 0f;        // 存活計時
        private bool _stopped = false;

        // 防重複打擊 (穿透用)
        private HashSet<GameObject> _hitHistory = new HashSet<GameObject>();

        // 揮舞專用狀態
        private bool _isConeSweep = false;
        private float _currentAngleTraveled = 0f;
        private float _totalSweepAngle = 0f;
        private float _spinSign = 1f;
        #endregion

        #region 3. 初始化 (Init)
        public void Init(Transform owner, Vector2 dir, SkillData data, SkillComputed comp,
                         LayerMask targetMask, LayerMask obstacleMask)
        {
            // --- 基礎綁定 ---
            this.owner = owner;
            this.targetMask = targetMask;
            this.obstacleMask = obstacleMask;

            // 強制套用全域透視縮放
            transform.localScale = PerspectiveUtils.GlobalScale;

            // 抓取 Collider
            _col = GetComponent<Collider2D>();
            if (!_col) _col = GetComponentInChildren<Collider2D>();
            if (_col != null) _col.isTrigger = true;

            // 讀取數據
            if (data.HitType == HitType.Area)
            {
                speed = 0f;
            }
            else
            {
                speed = data.ProjectileSpeed;
            }

            damage = comp.Damage;
            targetLayer = data.TargetLayer;
            maxDuration = data.MaxDuration; // ★ 從 Data 讀取統一的時間
            isPiercing = data.IsPiercing;

            // 重置狀態
            _lifeTimer = 0f;
            _stopped = false;
            _hitHistory.Clear();

            // 設定物理過濾器
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = targetMask | obstacleMask,
                useTriggers = true
            };

            // --- 模式分流 ---
            if (data.HitType == HitType.Cone)
            {
                InitConeSweep(data, comp, dir);
            }
            else
            {
                InitStandardProjectile(dir);
            }
        }

        private void InitConeSweep(SkillData data, SkillComputed comp, Vector2 inputDir)
        {
            _isConeSweep = true;
            _totalSweepAngle = comp.ConeAngle;
            _currentAngleTraveled = 0f;
            _dir = Vector2.zero;

            _spinSign = (data.SwingDirection == SwingDir.LeftToRight) ? -1f : 1f;

            // 計算起始角度
            float aimAngle = Mathf.Atan2(inputDir.y, inputDir.x) * Mathf.Rad2Deg;
            float startOffset = -_spinSign * (_totalSweepAngle * 0.5f);
            float startAngle = aimAngle + startOffset;

            transform.rotation = Quaternion.Euler(0f, 0f, startAngle);

            // 修正子物件朝向
            if (modelTransform)
            {
                float modelCorrection = (modelForward == FacingAxis.Up) ? -90f : 0f;
                modelCorrection += rotationOffsetDeg;
                modelTransform.localRotation = Quaternion.Euler(0f, 0f, modelCorrection);
            }
        }

        private void InitStandardProjectile(Vector2 inputDir)
        {
            _isConeSweep = false;

            // 如果是 Area (速度=0)，且不轉向，我們通常希望它保持預設朝向 (Right/Up)
            // 但為了邏輯統一，這裡還是根據 inputDir 設定 _dir
            _dir = (inputDir.sqrMagnitude > 0.0001f) ? inputDir.normalized : Vector2.right;

            // ★ 修改：加入判斷，讓 Inspector 裡的勾選框生效
            // 這樣你就可以在 SkillData -> ProjectilePrefab 裡把 AlignRotation 勾掉，讓爆炸永遠朝正
            if (alignRotation)
            {
                ApplyFacingRotation();
            }
        }
        #endregion

        #region 4. 更新循環 (Update)
        void Update()
        {
            if (_stopped) return;

            // 1. 時間檢查 (統一由 MaxDuration 控制生死)
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= maxDuration)
            {
                // 對於揮舞(Cone)，確保最後一幀轉滿
                if (_isConeSweep) FinishConeSweep();

                StopProjectile();
                return;
            }

            // 2. 執行對應邏輯
            if (_isConeSweep) UpdateConeSweep();
            else UpdateStandardProjectile();
        }

        private void UpdateConeSweep()
        {
            // 這裡不需檢查 _currentAngleTraveled >= total，因為時間是同步的
            // 時間到了 Update 裡的 _lifeTimer 會負責殺掉它

            float rotateStep = speed * Time.deltaTime;
            transform.Rotate(0f, 0f, rotateStep * _spinSign);
            _currentAngleTraveled += rotateStep;

            CheckOverlapCollision();
        }

        private void FinishConeSweep()
        {
            // 補足最後一點角度 (視覺上完美)
            if (_currentAngleTraveled < _totalSweepAngle)
            {
                float remain = _totalSweepAngle - _currentAngleTraveled;
                transform.Rotate(0f, 0f, remain * _spinSign);
                CheckOverlapCollision();
            }
        }

        private void UpdateStandardProjectile()
        {
            if (speed <= 0f)
            {
                CheckCastCollision(0f, 1f);
                return;
            }

            float scaleFactor = PerspectiveUtils.GetVisualScaleFactor(_dir);
            float step = speed * scaleFactor * Time.deltaTime;

            CheckCastCollision(step, scaleFactor);

            if (!_stopped)
            {
                transform.position += (Vector3)(_dir * step);
                // 不再檢查 _traveled >= maxDistance，完全依賴 _lifeTimer
            }

            if (alignRotation && !_stopped) ApplyFacingRotation();
        }
        #endregion

        #region 5. 碰撞核心 (Collision Core)
        // ... (CheckCastCollision, CheckOverlapCollision, ResolveHit 保持不變) ...
        // ... (請參考上一則回應的實作，邏輯完全相同，只是拿掉了 maxDistance 檢查) ...

        private void CheckCastCollision(float distance, float scaleFactor)
        {
            int count = _col.Cast(_dir, _filter, _hitsBuf, distance);
            if (count == 0) return;

            var hits = _hitsBuf.Take(count).OrderBy(h => h.distance).ToList();

            foreach (var hit in hits)
            {
                if (ResolveHit(hit.collider))
                {
                    if (speed > 0)
                    {
                        transform.position += (Vector3)(_dir * hit.distance);
                    }
                    StopProjectile();
                    return;
                }
            }
        }

        private void CheckOverlapCollision()
        {
            List<Collider2D> results = new List<Collider2D>();
            int count = _col.OverlapCollider(_filter, results);
            foreach (var col in results)
            {
                if (ResolveHit(col)) { StopProjectile(); return; }
            }
        }

        private bool ResolveHit(Collider2D other)
        {
            if (!other) return false;
            if (owner && other.transform.IsChildOf(owner)) return false;

            int layerVal = 1 << other.gameObject.layer;

            // 撞牆
            if ((obstacleMask.value & layerVal) != 0)
            {
                // 1. 揮舞 (Cone) 不彈刀
                if (_isConeSweep) return false;

                // 2. ★ 新增：定點 (Area) 也不彈刀/不消失
                // Area 的速度在 Init 時被強制設為 0，可以用這個來判斷
                if (speed == 0f) return false;

                // 其他 (Single 飛行道具) -> 撞牆停下
                return true;
            }

            // 撞人
            if ((targetMask.value & layerVal) != 0)
            {
                if (EffectApplier.TryResolveOwner(other, out var applier, out var layer) && layer == targetLayer)
                {
                    if (!_hitHistory.Contains(applier.gameObject))
                    {
                        applier.ApplyIncomingRaw(damage);
                        _hitHistory.Add(applier.gameObject);
                        if (!isPiercing) return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region 6. 工具 (Utils)
        // ... (ApplyFacingRotation, StopProjectile, TryGetColliderDiameter 保持不變) ...

        private void ApplyFacingRotation()
        {
            if (_dir.sqrMagnitude < 1e-6f) return;
            float deg = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            if (modelForward == FacingAxis.Up) deg -= 90f;
            deg += rotationOffsetDeg;

            Quaternion rot = Quaternion.AngleAxis(deg, Vector3.forward);
            if (modelTransform) { modelTransform.localRotation = rot; transform.rotation = Quaternion.identity; }
            else transform.rotation = rot;
        }

        private void StopProjectile()
        {
            if (_stopped) return;
            _stopped = true;
            if (ObjectPool.Instance != null) ObjectPool.Instance.Despawn(gameObject);
            else Destroy(gameObject);
        }

        public bool TryGetColliderDiameter(Vector2 dir, out float diameter)
        {
            diameter = 0f;
            if (!_col) _col = GetComponent<Collider2D>();
            if (!_col) _col = GetComponentInChildren<Collider2D>();
            if (!_col) return false;

            // 1. 決定測量軸向 (Measure Axis)
            // 既然 Projectile 會自動旋轉對齊飛行方向 (alignRotation)，
            // 我們只需要知道這個物件在「未旋轉狀態(Prefab)」下的「側面」是哪一軸。

            // 預設：物件朝右(X)，所以側面是 Y 軸
            Vector2 measureAxis = Vector2.up;

            // 如果物件朝上(Y)，那側面就是 X 軸
            if (modelForward == FacingAxis.Up) measureAxis = Vector2.right;

            // 考慮額外旋轉 (RotationOffset)
            // 如果有設 offset，測量軸也要跟著轉
            if (Mathf.Abs(rotationOffsetDeg) > 0.001f)
            {
                measureAxis = Quaternion.Euler(0, 0, rotationOffsetDeg) * measureAxis;
            }

            // 2. 根據 Collider 類型計算投影寬度
            Transform t = _col.transform;
            Vector3 s = t.lossyScale;

            // 注意：我們是在 Local Space 或 World Space 投影到 measureAxis 上
            // 為了簡單且精確，我們直接拿 World Points (Prefab狀態) 投影到 MeasureAxis

            if (_col is CircleCollider2D circle)
            {
                // 圓形：半徑 * 縮放 * 2
                // 取 X/Y 縮放中較大者 (保守估計)
                float maxScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
                diameter = 2f * circle.radius * maxScale;
                return true;
            }
            else if (_col is BoxCollider2D box)
            {
                // 矩形：投影四個角
                Vector2 size = box.size;
                Vector2 offset = box.offset;

                // 建立四個角的 Local 座標
                Vector2[] corners = new Vector2[4];
                corners[0] = offset + new Vector2(-size.x, -size.y) * 0.5f;
                corners[1] = offset + new Vector2(size.x, -size.y) * 0.5f;
                corners[2] = offset + new Vector2(size.x, size.y) * 0.5f;
                corners[3] = offset + new Vector2(-size.x, size.y) * 0.5f;

                return CalculateProjectionWidth(corners, t, measureAxis, out diameter);
            }
            else if (_col is PolygonCollider2D poly)
            {
                // 多邊形：投影所有點
                return CalculateProjectionWidth(poly.points, t, measureAxis, out diameter);
            }
            else if (_col is CapsuleCollider2D cap)
            {
                // 膠囊：簡化為 Box 處理
                Vector2 size = cap.size;
                float w = (cap.direction == CapsuleDirection2D.Horizontal) ? size.y : size.x;
                // 乘上對應軸的 scale
                float axisScale = (cap.direction == CapsuleDirection2D.Horizontal) ? Mathf.Abs(s.y) : Mathf.Abs(s.x);
                diameter = w * axisScale;
                return true;
            }

            // Fallback: 使用 Bounds (最不準，但總比沒有好)
            // 投影 Bounds 的四個角
            var b = _col.bounds;
            Vector3 min = b.min;
            Vector3 max = b.max;
            Vector2[] bCorners = new Vector2[]
            {
                new Vector2(min.x, min.y), new Vector2(max.x, min.y),
                new Vector2(max.x, max.y), new Vector2(min.x, max.y)
            };

            // Bounds 已經是 World Space，所以 transform 傳 null (代表不需再轉 Local->World)
            return CalculateProjectionWidth(bCorners, null, measureAxis, out diameter);
        }
        // --- 核心運算：投影點集到軸上求寬度 ---
        private bool CalculateProjectionWidth(Vector2[] points, Transform pointSpace, Vector2 axis, out float width)
        {
            if (points == null || points.Length == 0)
            {
                width = 0f;
                return false;
            }

            float minProj = float.PositiveInfinity;
            float maxProj = float.NegativeInfinity;

            for (int i = 0; i < points.Length; i++)
            {
                // 1. 轉成世界座標 (如果有點空間)
                Vector2 worldPt = (pointSpace != null) ? (Vector2)pointSpace.TransformPoint(points[i]) : points[i];

                // 2. 投影到測量軸 (Dot Product)
                float proj = Vector2.Dot(worldPt, axis);

                if (proj < minProj) minProj = proj;
                if (proj > maxProj) maxProj = proj;
            }

            width = Mathf.Max(0f, maxProj - minProj);
            return width > 0f;
        }
        #endregion

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!_col) _col = GetComponent<Collider2D>();
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)_dir * 0.5f);
        }
#endif
    }
}