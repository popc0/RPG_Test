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
            // 1. 計算中心點 (視覺上的終點)
            // 因為移動有透視，所以射程也要經過透視換算
            float visualDist = PerspectiveUtils.PhysicalToVisualDistance(data.BaseRange, dir);
            Vector2 center = (Vector2)origin + dir * Mathf.Max(0.1f, visualDist);

            // 2. 物理範圍半徑
            float r = Mathf.Max(0.05f, comp.AreaRadius);

            // 3. 第一階段：Unity 物理檢測 (抓一個足夠大的正圓，包含扁圓形)
            // 因為扁圓形的寬度是 1.0 (未縮放)，所以用原始半徑去抓一定抓得到
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, r, enemyMask);

            foreach (var h in hits)
            {
                // 4. 第二階段：透視過濾 (Narrow Phase)
                if (CheckHitAreaPerspective(center, h.bounds.center, r))
                {
                    // 命中！
                    if (EffectApplier.TryResolveOwner(h, out var target, out var layer))
                        if (data.TargetLayer == layer) target.ApplyIncomingRaw(comp.Damage);
                }
            }

            // (可選) 這裡可以生成一個視覺特效，記得設定 scale 為 (1, 0.5, 1)
            SpawnAreaVFX(center, r);
        }

        void DoCone2D(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir)
        {
            float dist = Mathf.Max(0.1f, data.BaseRange);
            float angle = comp.ConeAngle;

            // 1. 第一階段：抓大圓 (距離不用透視縮放，因為我們要檢查的是"還原後"的距離)
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, dist, enemyMask);

            foreach (var h in hits)
            {
                if (h.transform == owner || h.transform.IsChildOf(owner)) continue;

                // 2. 第二階段：透視過濾
                if (CheckHitConePerspective(origin, dir, h.bounds.center, dist, angle))
                {
                    if (EffectApplier.TryResolveOwner(h, out var target, out var layer))
                        if (data.TargetLayer == layer) target.ApplyIncomingRaw(comp.Damage);
                }
            }
        }
        // ============================================================
        // ★ 核心數學：透視判定演算法
        // ============================================================

        /// <summary>
        /// 檢查目標是否在「壓扁的圓形 (橢圓)」內
        /// </summary>
        bool CheckHitAreaPerspective(Vector2 center, Vector2 targetPos, float radius)
        {
            Vector2 diff = targetPos - center;

            // 逆向工程：把畫面上的距離「還原」成物理距離
            // 例如：Y 軸差了 0.5 (畫面)，除以 Scale.y (0.5) = 1.0 (物理)
            // 這樣就等於把橢圓拉回成正圓來比較
            float physicalX = diff.x / PerspectiveUtils.GlobalScale.x;
            float physicalY = diff.y / PerspectiveUtils.GlobalScale.y;

            // 計算還原後的距離平方
            float sqrDist = (physicalX * physicalX) + (physicalY * physicalY);

            // 比較半徑平方
            return sqrDist <= (radius * radius);
        }

        /// <summary>
        /// 檢查目標是否在「壓扁的扇形」內
        /// </summary>
        bool CheckHitConePerspective(Vector2 origin, Vector2 aimDir, Vector2 targetPos, float range, float angle)
        {
            // 1. 還原目標向量
            Vector2 diff = targetPos - origin;
            Vector2 physicalDiff = new Vector2(
                diff.x / PerspectiveUtils.GlobalScale.x,
                diff.y / PerspectiveUtils.GlobalScale.y
            );

            // 2. 檢查距離 (物理距離)
            if (physicalDiff.magnitude > range) return false;

            // 3. 還原瞄準方向 (這很重要！原本 (1, 0.5) 的方向其實代表 45 度)
            Vector2 physicalAim = new Vector2(
                aimDir.x / PerspectiveUtils.GlobalScale.x,
                aimDir.y / PerspectiveUtils.GlobalScale.y
            ).normalized;

            // 4. 檢查角度
            float angleToTarget = Vector2.Angle(physicalAim, physicalDiff);
            return angleToTarget <= angle * 0.5f;
        }

        // 簡單的特效生成範例
        void SpawnAreaVFX(Vector3 pos, float radius)
        {
            // 如果你有特效 Prefab，可以在這裡 Instantiate
            // 重點：obj.transform.localScale = new Vector3(radius, radius * 0.5f, 1f);
        }
    }
}