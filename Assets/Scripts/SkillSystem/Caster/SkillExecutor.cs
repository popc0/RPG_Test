using UnityEngine;
using System.Collections;
using RPG;

namespace RPG
{
    public class SkillExecutor : MonoBehaviour
    {
        [Header("位置與物理設定")]
        public Transform owner;
        public Transform firePoint;

        [Header("判定層級")]
        public LayerMask enemyMask = 0;
        public LayerMask allyMask = 0;
        public LayerMask obstacleMask = 0;
        [SerializeField] private float spawnInset = 0.05f;

        void Awake()
        {
            if (!owner) owner = transform;
            if (!firePoint) firePoint = owner;
        }

        public void ExecuteSkill(SkillData data, SkillComputed comp, Vector3 origin, Vector2 aimDir)
        {
            if (!data) return;

            if (data.Target == TargetType.Self)
            {
                ApplyToSelf(data, comp);
                return;
            }

            if (aimDir.sqrMagnitude < 0.0001f) aimDir = Vector2.right;
            aimDir.Normalize();

            LayerMask targetMask = (data.Target == TargetType.Ally) ? allyMask : enemyMask;

            Vector3 spawnPos = CalculateSpawnPosition(data, origin, aimDir);
            SpawnProjectile(data, comp, spawnPos, aimDir, targetMask);
        }

        Vector3 CalculateSpawnPosition(SkillData data, Vector3 origin, Vector2 dir)
        {
            if (data.HitType == HitType.Area)
            {
                float visualDist = PerspectiveUtils.PhysicalToVisualDistance(data.BaseRange, dir);
                RaycastHit2D hit = Physics2D.Raycast(origin, dir, visualDist, obstacleMask);
                float dist = (hit.collider != null) ? hit.distance : visualDist;
                return origin + (Vector3)(dir * Mathf.Max(0f, dist));
            }
            else
            {
                return origin + (Vector3)(dir * spawnInset);
            }
        }

        void SpawnProjectile(SkillData data, SkillComputed comp, Vector3 pos, Vector2 dir, LayerMask targetMask)
        {
            if (data.ProjectilePrefab == null)
            {
                // 保留這唯一的 Legacy 用法給沒有 Prefab 的技能 (Hitscan)
                if (data.HitType == HitType.Single) DoSingle2D_LegacyRay(data, comp, pos, dir, targetMask);
                return;
            }

            GameObject obj;
            if (ObjectPool.Instance != null)
                obj = ObjectPool.Instance.Spawn(data.ProjectilePrefab.gameObject, pos, Quaternion.identity);
            else
                obj = Instantiate(data.ProjectilePrefab.gameObject, pos, Quaternion.identity);

            var proj = obj.GetComponent<ProjectileBase>();
            if (proj)
            {
                proj.Init(owner, dir, data, comp, targetMask, obstacleMask);
            }
        }

        void ApplyToSelf(SkillData data, SkillComputed comp)
        {
            var target = owner.GetComponent<EffectApplier>();
            if (!target) target = owner.GetComponentInChildren<EffectApplier>();
            if (target) target.ApplyIncomingRaw(comp.Damage);
        }

        // 這是唯一保留的舊邏輯 (給沒 Prefab 的瞬發技能用)
        void DoSingle2D_LegacyRay(SkillData data, SkillComputed comp, Vector3 origin, Vector2 dir, LayerMask targetMask)
        {
            float dist = Mathf.Max(0.1f, data.BaseRange);
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, targetMask | obstacleMask);
            if (hit.collider != null)
            {
                if (EffectApplier.TryResolveOwner(hit.collider, out var target, out var layer))
                {
                    if (data.TargetLayer == layer) target.ApplyIncomingRaw(comp.Damage);
                }
            }
        }
    }
}