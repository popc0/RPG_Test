using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    [DisallowMultipleComponent]
    public class Projectile2D : MonoBehaviour
    {
        public enum FacingAxis { Right, Up }

        [Header("執行參數（由 SkillCaster 初始化）")]
        public Transform owner;
        public float speed;                 // from SkillData.ProjectileSpeed
        public float maxDistance;           // from SkillData.BaseRange
        public float damage;
        public InteractionLayer targetLayer;
        public LayerMask enemyMask;
        public LayerMask obstacleMask;

        [Header("朝向與步進")]
        public bool alignRotation = true;
        public FacingAxis modelForward = FacingAxis.Right;
        public float rotationOffsetDeg = 0f;
        [Tooltip("命中點向前微移以避免重複命中（公尺）")]
        public float skin = 0.01f;
        [Tooltip("每幀最多連續 Cast 次數")]
        public int maxCastsPerStep = 4;

        [Header("模型設定 (透視修正用)")]
        [Tooltip("若指定此欄位，旋轉時只會轉動此子物件，父物件保持不動以維持縮放。")]
        public Transform modelTransform;

        // 內部
        Collider2D _col;                    // 允許在子物件上
        Vector2 _dir = Vector2.right;
        float _traveled;
        bool _stopped;
        readonly RaycastHit2D[] _hitsBuf = new RaycastHit2D[8];
        ContactFilter2D _filter;

        public Vector2 Direction => _dir;
        public float Traveled => _traveled;

        public void Init(Transform owner, Vector2 dir, SkillData data, SkillComputed comp,
                         LayerMask enemyMask, LayerMask obstacleMask)
        {
            // ★ 新增：初始化時強制套用全域透視縮放
            // 這樣就不需要 SkillExecutor 去設定 transform.localScale 了
            transform.localScale = PerspectiveUtils.GlobalScale;

            // ... (原本的 Init 邏輯: 抓 Collider, 設定變數...)
            _col = GetComponent<Collider2D>();
            if (!_col) _col = GetComponentInChildren<Collider2D>();

            this.owner = owner;
            _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            speed = data.ProjectileSpeed;
            maxDistance = Mathf.Max(0.1f, data.BaseRange);
            damage = comp.Damage;
            targetLayer = data.TargetLayer;
            this.enemyMask = enemyMask;
            this.obstacleMask = obstacleMask;

            _traveled = 0f;
            _stopped = false;

            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = enemyMask | obstacleMask,
                useTriggers = true
            };

            ApplyFacingRotation();
        }

        void Update()
        {
            if (_stopped || speed <= 0f) return;

            // 1. 取得透視倍率
            // _dir 已經是視覺方向 (例如 26度)，我們算出這個角度應有的速度折損率 (例如 0.8)
            float scaleFactor = PerspectiveUtils.GetVisualScaleFactor(_dir);

            // 2. 計算這一幀的位移 (速度 * 倍率 * 時間)
            float step = speed * scaleFactor * Time.deltaTime;

            float remaining = step;
            int guard = 0;

            while (remaining > 0f && guard++ < maxCastsPerStep && !_stopped)
            {
                // 直接用 _dir (視覺方向) 檢測
                int count = _col.Cast(_dir, _filter, _hitsBuf, remaining);

                // 取最近，但只考慮「障礙物」與「正確目標部位」
                int best = -1;
                float bestDist = float.PositiveInfinity;

                for (int i = 0; i < count; i++)
                {
                    var h = _hitsBuf[i];
                    if (!h.collider) continue;
                    if (owner && h.collider.transform.IsChildOf(owner)) continue;

                    int maskBitHit = 1 << h.collider.gameObject.layer;
                    bool isObstacle = (obstacleMask.value & maskBitHit) != 0;
                    bool isEnemy = (enemyMask.value & maskBitHit) != 0;

                    // 只處理障礙物與敵人，其餘忽略
                    if (!isObstacle && !isEnemy) continue;

                    if (isEnemy)
                    {
                        // 先檢查是不是正確部位，不是的話整個當透明忽略
                        if (EffectApplier.TryResolveOwner(h.collider, out var tmpTarget, out var tmpLayer))
                        {
                            if (tmpLayer != targetLayer)
                                continue; // 錯誤部位 → 視為不存在
                        }
                        else
                        {
                            // 找不到擁有者時，保守起見交給後面的流程去決定（視為可命中）
                        }
                    }

                    if (h.distance < bestDist)
                    {
                        bestDist = h.distance;
                        best = i;
                    }
                }

                // 下面是 Hit 處理的修改 (還原物理距離)
                if (best == -1)
                {
                    transform.position += (Vector3)(_dir * remaining);

                    // ★ 距離累加：要把「視覺距離」還原成「物理距離」來判斷射程
                    // 物理距離 = 視覺距離 / 倍率
                    _traveled += remaining / scaleFactor;

                    remaining = 0f;
                    break;
                }

                var hit = _hitsBuf[best];
                float move = Mathf.Max(0f, hit.distance);
                transform.position += (Vector3)(_dir * move);

                // ★ 累加
                _traveled += move / scaleFactor;

                remaining -= move;

                int maskBit = 1 << hit.collider.gameObject.layer;

                // 障礙物 → 直接停
                if ((obstacleMask.value & maskBit) != 0) { StopProjectile(); return; }
                if ((enemyMask.value & maskBit) != 0)
                {
                    if (EffectApplier.TryResolveOwner(hit.collider, out var target, out var hitLayer) && hitLayer == targetLayer)
                    { target.ApplyIncomingRaw(damage); StopProjectile(); return; }
                    else { StopProjectile(); return; }
                }

                // 微移
                float advance = Mathf.Min(remaining, skin);
                transform.position += (Vector3)(_dir * advance);
                // _traveled += advance / scaleFactor;
                remaining -= advance;
            }

            if (_traveled >= maxDistance)
            {
                StopProjectile();
                return;
            }

            if (alignRotation)
                ApplyFacingRotation();
        }

        void ApplyFacingRotation()
        {
            if (_dir.sqrMagnitude < 1e-6f) return;

            float deg = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg; // 以 +X 為前
            if (modelForward == FacingAxis.Up) deg -= 90f;
            deg += rotationOffsetDeg;

            Quaternion targetRot = Quaternion.AngleAxis(deg, Vector3.forward);

            if (modelTransform)
            {
                // 有指定模型：轉模型，父物件不動 (保持世界座標的 Scale 軸向)
                modelTransform.localRotation = targetRot;
                transform.rotation = Quaternion.identity; // 確保父物件歸零
            }
            else
            {
                // 沒指定：轉自己 (舊邏輯)
                transform.rotation = targetRot;
            }
        }

        void StopProjectile()
        {
            if (_stopped) return;
            _stopped = true;

            // 將原有的 Destroy(gameObject);
            // 改為呼叫物件池的回收方法

            // ⭐ 新的物件池回收邏輯 ⭐
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Despawn(gameObject);
            }
            else
            {
                // 作為備用（如果沒有物件池實例，就使用傳統銷毀）
                Destroy(gameObject);
            }

            // 注意：這裡不需要額外設置 _stopped=false，因為當物件被回收並再次 Spawn 時，
            // Init() 方法會被調用並重置所有內部狀態。
        }

        // === 權威寬度：由投射物回報自身 Collider 寬度（沿飛行方向的垂直厚度） ===
        public bool TryGetColliderDiameter(Vector2 dir, out float diameter)
        {
            diameter = 0f;
            if (!_col) _col = GetComponent<Collider2D>();
            if (!_col) _col = GetComponentInChildren<Collider2D>();
            if (!_col) return false;

            dir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;
            Vector2 n = new Vector2(Mathf.Abs(-dir.y), Mathf.Abs(dir.x)); // 法向
            Transform t = _col.transform;
            Vector3 s = t.lossyScale;

            switch (_col)
            {
                case CircleCollider2D c:
                    diameter = 2f * c.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
                    return true;

                case BoxCollider2D b:
                    Vector2 size = b.size;
                    bool horiz = Mathf.Abs(n.x) > Mathf.Abs(n.y);
                    diameter = horiz ? size.y * Mathf.Abs(s.y) : size.x * Mathf.Abs(s.x);
                    return true;

                case CapsuleCollider2D cap:
                    Vector2 cs = cap.size;
                    diameter = (cap.direction == CapsuleDirection2D.Horizontal)
                        ? cs.y * Mathf.Abs(s.y)
                        : cs.x * Mathf.Abs(s.x);
                    return true;

                case PolygonCollider2D poly:
                    var pts = poly.points;
                    if (pts == null || pts.Length == 0) return false;
                    float minProj = float.PositiveInfinity, maxProj = float.NegativeInfinity;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        Vector2 wp = t.TransformPoint(pts[i]);
                        float proj = wp.x * n.x + wp.y * n.y;
                        if (proj < minProj) minProj = proj;
                        if (proj > maxProj) maxProj = proj;
                    }
                    diameter = Mathf.Max(0f, maxProj - minProj);
                    return diameter > 0f;

                default:
                    var bnd = _col.bounds;
                    var ext = bnd.extents;
                    float r = ext.x * n.x + ext.y * n.y;
                    diameter = Mathf.Max(0f, 2f * r);
                    return diameter > 0f;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            //  修正：如果自己身上沒有，就去子物件找
            if (!_col) _col = GetComponent<Collider2D>();
            if (!_col) _col = GetComponentInChildren<Collider2D>();
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            if (_col) Gizmos.DrawWireCube(_col.bounds.center, _col.bounds.size);

            Gizmos.color = Color.cyan;
            var tip = (Vector3)Direction.normalized * 0.6f;
            Gizmos.DrawLine(transform.position, transform.position + tip);
        }
#endif
    }
}
