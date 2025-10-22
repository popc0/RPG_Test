using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    /// <summary>
    /// 命中層：處理技能的實際命中邏輯（單體、範圍、投射物）。  
    /// SkillCaster 呼叫此類別來取得命中的目標清單。
    /// </summary>
    public static class ImpactResolver
    {
        /// <summary>
        /// 取得命中的目標清單
        /// </summary>
        public static List<EffectApplier> ResolveTargets(
            SkillComputed comp,
            Transform casterTransform,
            LayerMask targetMask)
        {
            List<EffectApplier> results = new List<EffectApplier>();

            switch (comp.IsArea)
            {
                case true:
                    ResolveArea(comp, casterTransform, targetMask, results);
                    break;
                default:
                    ResolveRaycast(comp, casterTransform, targetMask, results);
                    break;
            }

            return results;
        }

        private static void ResolveRaycast(
            SkillComputed comp,
            Transform casterTransform,
            LayerMask targetMask,
            List<EffectApplier> results)
        {
            Vector3 origin = casterTransform.position + Vector3.up * 1f;
            Vector3 dir = casterTransform.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, 12f, targetMask, QueryTriggerInteraction.Collide))
            {
                var target = hit.collider.GetComponent<EffectApplier>();
                if (target != null) results.Add(target);
            }
        }

        private static void ResolveArea(
            SkillComputed comp,
            Transform casterTransform,
            LayerMask targetMask,
            List<EffectApplier> results)
        {
            Vector3 center = casterTransform.position + casterTransform.forward * comp.AreaRadius;
            Collider[] hits = Physics.OverlapSphere(center, comp.AreaRadius, targetMask, QueryTriggerInteraction.Collide);

            foreach (Collider c in hits)
            {
                var target = c.GetComponent<EffectApplier>();
                if (target != null && !results.Contains(target))
                    results.Add(target);
            }

            // 在 Scene 顯示範圍圈（開發測試用）
            Debug.DrawLine(casterTransform.position, center, Color.yellow, 1f);
            Debug.DrawRay(center, Vector3.up * 0.5f, Color.red, 1f);
        }
    }
}
