using UnityEngine;
using System.Collections.Generic;

namespace RPG
{
    public class ProjectileCone : ProjectileBase
    {
        [Header("Cone 設定")]
        public SpawnAnchorType spawnAnchor = SpawnAnchorType.Body;

        [Header("視覺修正")]
        public FacingAxis modelForward = FacingAxis.Right;
        public float rotationOffsetDeg = 0f;
        public Transform modelTransform; // 這是 Pivot (子物件)

        // 內部狀態
        private float _currentAngleTraveled = 0f;
        private float _totalSweepAngle = 0f;
        private float _spinSign = 1f;
        private float _startAngle = 0f;

        // 實作屬性
        protected override FacingAxis ModelForward => modelForward;
        protected override float RotationOffsetDeg => rotationOffsetDeg;
        public override SpawnAnchorType AnchorType => spawnAnchor;

        public override void Init(Transform owner, Vector2 dir, SkillData data, SkillComputed comp, LayerMask targetMask, LayerMask obstacleMask)
        {
            // 1. 父類別初始化 (這會把 transform.localScale 設為 (1, 0.577, 1))
            base.Init(owner, dir, data, comp, targetMask, obstacleMask);

            // 2. 鎖定 Root 旋轉
            transform.rotation = Quaternion.identity;

            speed = data.ProjectileSpeed;
            _totalSweepAngle = comp.ConeAngle;
            _currentAngleTraveled = 0f;
            _spinSign = (data.SwingDirection == SwingDir.LeftToRight) ? -1f : 1f;

            // ============================================================
            // ★ 修正：角度還原 (Unsquash)
            // ============================================================
            // dir 是 "視覺方向" (例如 30度)。
            // 因為父物件有 Scale Y = 0.577，如果直接轉 30度，視覺上會變成 18度 (歪掉)。
            // 所以我們要先把它 "放大回去" 成邏輯方向 (45度)。

            float logicY = dir.y / PerspectiveUtils.GlobalScale.y; // 0.577 / 0.577 = 1
            float logicX = dir.x;                                  // 1

            // 算出邏輯角度 (45度)
            float logicAngle = Mathf.Atan2(logicY, logicX) * Mathf.Rad2Deg;

            // 3. 設定揮舞起點 (從中心往回推一半)
            float startOffset = -_spinSign * (_totalSweepAngle * 0.5f);

            // 儲存起始角度
            _startAngle = logicAngle + startOffset;

            // 4. 初次更新旋轉
            ApplyFacingRotation(_startAngle);
        }

        protected override void OnUpdateMovement()
        {
            // 推進角度
            float step = speed * Time.deltaTime;
            _currentAngleTraveled += step;

            // 計算當前絕對角度
            float currentAngle = _startAngle + (_currentAngleTraveled * _spinSign);

            // 更新旋轉
            ApplyFacingRotation(currentAngle);

            // 碰撞檢測 (Collider 跟著 modelTransform 轉，Unity 會自動處理被父物件壓扁後的形狀)
            CheckOverlapCollision();
        }

        // ============================================================
        // ★ 核心：與 ProjectileLinear 邏輯保持一致
        // ============================================================
        private void ApplyFacingRotation(float deg)
        {
            // 1. 修正圖片朝向 (如果圖片朝上，減90度讓它變朝右)
            if (modelForward == FacingAxis.Up) deg -= 90f;

            // 2. 加上額外修正
            deg += rotationOffsetDeg;

            // 3. 套用旋轉
            Quaternion rot = Quaternion.AngleAxis(deg, Vector3.forward);

            if (modelTransform)
            {
                // 有 Pivot/Visual 時：只轉 Pivot，Root 保持不動 (配合父物件壓縮)
                modelTransform.localRotation = rot;
                transform.rotation = Quaternion.identity;
            }
            else
            {
                // 沒有 Pivot 時：只能轉 Root (但這在 Cone 模式下通常不建議，因為會破壞壓縮效果)
                transform.rotation = rot;
            }
        }

        private void CheckOverlapCollision()
        {
            List<Collider2D> results = new List<Collider2D>();
            // 使用 OverlapCollider，它支援 Scale 變形後的碰撞體
            col.OverlapCollider(filter, results);

            foreach (var c in results)
            {
                ResolveHit(c);
                if (isStopped) return;
            }
        }

        protected override bool OnHitObstacle()
        {
            return false; // 揮砍不彈刀
        }
    }
}