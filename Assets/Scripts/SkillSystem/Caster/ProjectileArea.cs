using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    public class ProjectileArea : ProjectileBase
    {
        [Header("Area 設定")]
        public bool alignRotation = false; // Area 預設不轉，但可勾選
        // ★ 新增：Area 預設通常是腳下 (Feet)，但保留彈性
        public SpawnAnchorType spawnAnchor = SpawnAnchorType.Feet;

        public FacingAxis modelForward = FacingAxis.Right;
        public float rotationOffsetDeg = 0f;
        public Transform modelTransform; // 如果要支援旋轉，需要這個

        // ★ 實作父類別的虛擬屬性
        protected override FacingAxis ModelForward => modelForward;
        protected override float RotationOffsetDeg => rotationOffsetDeg;
        public override SpawnAnchorType AnchorType => spawnAnchor;
        public override void Init(Transform owner, Vector2 dir, SkillData data, SkillComputed comp, LayerMask targetMask, LayerMask obstacleMask)
        {
            base.Init(owner, dir, data, comp, targetMask, obstacleMask);
            speed = 0f; // 強制不動

            // 處理旋轉 (如果有的話)
            if (alignRotation && modelTransform)
            {
                float deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (modelForward == FacingAxis.Up) deg -= 90f;
                deg += rotationOffsetDeg;
                modelTransform.localRotation = Quaternion.Euler(0, 0, deg);
            }
        }

        protected override void OnUpdateMovement()
        {
            // 不移動，只做 Overlap 檢測
            List<Collider2D> results = new List<Collider2D>();
            col.OverlapCollider(filter, results);

            foreach (var c in results)
            {
                ResolveHit(c);
                if (isStopped) return;
            }
        }

        protected override bool OnHitObstacle()
        {
            return false; // 撞牆不死 (只會穿過去，視覺上被遮擋)
        }
    }
}