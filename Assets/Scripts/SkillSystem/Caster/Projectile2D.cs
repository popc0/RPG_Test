using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
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
        [Tooltip("命中點向前微移以避免重複命中（公尺）")] public float skin = 0.01f;
        [Tooltip("每幀最多連續 Cast 次數")] public int maxCastsPerStep = 4;

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

            _traveled = 0f; _stopped = false;

            _filter = new ContactFilter2D { useLayerMask = true, layerMask = enemyMask | obstacleMask, useTriggers = true };
            ApplyFacingRotation();
        }

        void Update()
        {
            if (_stopped || speed <= 0f) return;

            float step = speed * Time.deltaTime;
            float remaining = step;
            int guard = 0;

            while (remaining > 0f && guard++ < maxCastsPerStep && !_stopped)
            {
                int count = _col.Cast(_dir, _filter, _hitsBuf, remaining);
                // 取最近，忽略自己
                int best = -1; float bestDist = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    var h = _hitsBuf[i];
                    if (!h.collider) continue;
                    if (owner && h.collider.transform.IsChildOf(owner)) continue;
                    if (h.distance < bestDist) { bestDist = h.distance; best = i; }
                }

                if (best == -1)
                {
                    // 無命中 → 前進
                    transform.position += (Vector3)(_dir * remaining);
                    _traveled += remaining;
                    remaining = 0f;
                    break;
                }

                var hit = _hitsBuf[best];
                float move = Mathf.Max(0f, hit.distance);
                transform.position += (Vector3)(_dir * move);
                _traveled += move;
                remaining -= move;

                int maskBit = 1 << hit.collider.gameObject.layer;
                // 障礙物 → 直接停
                if ((obstacleMask.value & maskBit) != 0)
                { StopProjectile(); return; }

                // 敵人 → 檢查部位
                if ((enemyMask.value & maskBit) != 0)
                {
                    if (EffectApplier.TryResolveOwner(hit.collider, out var target, out var hitLayer))
                    {
                        if (hitLayer == targetLayer)
                        { target.ApplyIncomingRaw(damage); StopProjectile(); return; }
                        else
                        {
                            // 部位不符 → 當空氣，微移後繼續
                            float adv = Mathf.Min(remaining, skin);
                            transform.position += (Vector3)(_dir * adv);
                            _traveled += adv; remaining -= adv; continue;
                        }
                    }
                    else { StopProjectile(); return; }
                }

                // 其他 → 微移繼續
                float advance = Mathf.Min(remaining, skin);
                transform.position += (Vector3)(_dir * advance);
                _traveled += advance; remaining -= advance;
            }

            if (_traveled >= maxDistance) { StopProjectile(); return; }
            if (alignRotation) ApplyFacingRotation();
        }

        void ApplyFacingRotation()
        {
            if (_dir.sqrMagnitude < 1e-6f) return;
            float deg = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg; // 以 +X 為前
            if (modelForward == FacingAxis.Up) deg -= 90f;
            deg += rotationOffsetDeg;
            transform.rotation = Quaternion.AngleAxis(deg, Vector3.forward);
        }

        void StopProjectile()
        {
            if (_stopped) return; _stopped = true;
            Destroy(gameObject); // 可改成物件池
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
            Transform t = _col.transform; Vector3 s = t.lossyScale;

            switch (_col)
            {
                case CircleCollider2D c:
                    diameter = 2f * c.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
                    return true;
                case BoxCollider2D b:
                    Vector2 size = b.size; bool horiz = Mathf.Abs(n.x) > Mathf.Abs(n.y);
                    diameter = horiz ? size.y * Mathf.Abs(s.y) : size.x * Mathf.Abs(s.x);
                    return true;
                case CapsuleCollider2D cap:
                    Vector2 cs = cap.size;
                    diameter = (cap.direction == CapsuleDirection2D.Horizontal) ? cs.y * Mathf.Abs(s.y) : cs.x * Mathf.Abs(s.x);
                    return true;
                case PolygonCollider2D poly:
                    var pts = poly.points; if (pts == null || pts.Length == 0) return false;
                    float minProj = float.PositiveInfinity, maxProj = float.NegativeInfinity;
                    for (int i = 0; i < pts.Length; i++)
                    { Vector2 wp = t.TransformPoint(pts[i]); float proj = wp.x * n.x + wp.y * n.y; if (proj < minProj) minProj = proj; if (proj > maxProj) maxProj = proj; }
                    diameter = Mathf.Max(0f, maxProj - minProj); return diameter > 0f;
                default:
                    var bnd = _col.bounds; var ext = bnd.extents; float r = ext.x * n.x + ext.y * n.y; diameter = Mathf.Max(0f, 2f * r); return diameter > 0f;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!_col) _col = GetComponent<Collider2D>();
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            if (_col) Gizmos.DrawWireCube(_col.bounds.center, _col.bounds.size);
            Gizmos.color = Color.cyan;
            var tip = (Vector3)Direction.normalized * 0.6f;
            Gizmos.DrawLine(transform.position, transform.position + tip);
        }
#endif
    }
}
