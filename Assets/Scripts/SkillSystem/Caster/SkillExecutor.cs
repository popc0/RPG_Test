using UnityEngine;
using System.Collections;
using RPG; // 引用您的命名空間

namespace RPG
{
    /// <summary>
    /// 負責技能的「物理執行」與「視覺表現」。
    /// 接受 SkillCaster 的命令，執行具體的發射、判定與特效。
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        [Header("位置與物理設定")]
        public Transform owner;      // 誰發射的 (通常是 Player)
        public Transform firePoint;  // 發射點 (槍口/手部)

        [Header("判定層級")]
        public LayerMask enemyMask = 0;
        public LayerMask obstacleMask = 0;
        [SerializeField] private float spawnInset = 0.05f; // 生成投射物的微偏移

        // 確保有 Owner 與 FirePoint
        void Awake()
        {
            if (!owner) owner = transform;
            if (!firePoint) firePoint = owner;
        }

        // ============================================================
        //  公開介面：SkillCaster 呼叫這個方法
        // ============================================================
        public void ExecuteSkill(SkillData data, SkillComputed comp, Vector3 origin, Vector2 aimDir)
        {
            if (!data) return;

            // 確保方向正確 (防呆)
            if (aimDir.sqrMagnitude < 0.0001f) aimDir = Vector2.right;
            aimDir.Normalize();

            // 根據類型分派工作
            switch (data.HitType)
            {
                case HitType.Area:
                    DoArea2D(data, comp, origin, aimDir);
                    break;
                case HitType.Cone:
                    DoCone2D(data, comp, origin, aimDir);
                    break;
                case HitType.Single:
                default:
                    DoSingle2D(data, comp, origin, aimDir);
                    break;
            }
        }

        // ============================================================
        //  內部物理邏輯 (從原 SkillCaster 搬過來的)
        // ============================================================

        void DoSingle2D(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            if (data.UseProjectile && data.ProjectilePrefab)
            {
                // 生成投射物
                var spawnPos = origin + (Vector3)(dir * spawnInset);
                GameObject obj;

                // 支援 ObjectPool
                if (ObjectPool.Instance != null)
                    obj = ObjectPool.Instance.Spawn(data.ProjectilePrefab.gameObject, spawnPos, Quaternion.identity);
                else
                    obj = Instantiate(data.ProjectilePrefab.gameObject, spawnPos, Quaternion.identity);

                var proj = obj.GetComponent<Projectile2D>();
                if (proj) proj.Init(owner, dir, data, comp, enemyMask, obstacleMask);
            }
            else
            {
                // 立即射線 (Hitscan)
                DoSingle2D_LegacyRay(data, comp, origin, dir);
            }
        }

        void DoSingle2D_LegacyRay(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            float dist = Mathf.Max(0.1f, data.BaseRange);
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, enemyMask | obstacleMask);

            Vector3 endPos = origin + (Vector3)(dir * dist);

            if (hit.collider != null)
            {
                endPos = hit.point;
                // 嘗試取得受擊者並扣血
                if (EffectApplier.TryResolveOwner(hit.collider, out var target, out var layer))
                {
                    if (data.TargetLayer == layer)
                        target.ApplyIncomingRaw(comp.Damage);
                }
            }
        }

        void DoArea2D(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            Vector2 center = (Vector2)origin + dir * Mathf.Max(0.1f, data.BaseRange);
            float r = Mathf.Max(0.05f, comp.AreaRadius);

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, r, enemyMask);
            foreach (var h in hits)
            {
                if (EffectApplier.TryResolveOwner(h, out var target, out var layer))
                    if (data.TargetLayer == layer) target.ApplyIncomingRaw(comp.Damage);
            }
        }

        void DoCone2D(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            float dist = Mathf.Max(0.1f, data.BaseRange);
            float angle = comp.ConeAngle;

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, dist, enemyMask);
            foreach (var h in hits)
            {
                if (h.transform == owner || h.transform.IsChildOf(owner)) continue;

                Vector2 tDir = ((Vector2)h.bounds.center - (Vector2)origin);
                if (Vector2.Angle(dir, tDir) <= angle * 0.5f)
                {
                    if (EffectApplier.TryResolveOwner(h, out var target, out var layer))
                        if (data.TargetLayer == layer) target.ApplyIncomingRaw(comp.Damage);
                }
            }
        }
    }
}