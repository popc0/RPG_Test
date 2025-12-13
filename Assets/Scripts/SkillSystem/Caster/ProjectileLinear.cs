using UnityEngine;
using System.Linq;

namespace RPG
{
    public class ProjectileLinear : ProjectileBase
    {
        [Header("Linear 設定")]
        public bool alignRotation = true;
        // ★ 新增：允許調整發射點 (例如某些地波技能可能想設為 Feet)
        public SpawnAnchorType spawnAnchor = SpawnAnchorType.Body;

        public FacingAxis modelForward = FacingAxis.Right;
        public float rotationOffsetDeg = 0f;
        public Transform modelTransform;

        private Vector2 _moveDir;
        private readonly RaycastHit2D[] _hitsBuf = new RaycastHit2D[16];

        // ★ 實作父類別的虛擬屬性，連結到自己的變數
        protected override FacingAxis ModelForward => modelForward;
        protected override float RotationOffsetDeg => rotationOffsetDeg;
        public override SpawnAnchorType AnchorType => spawnAnchor;

        public override void Init(Transform owner, Vector2 dir, SkillData data, SkillComputed comp, LayerMask targetMask, LayerMask obstacleMask)
        {
            base.Init(owner, dir, data, comp, targetMask, obstacleMask);

            // Linear 專屬初始化
            _moveDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;
            if (alignRotation) ApplyFacingRotation();
        }

        protected override void OnUpdateMovement()
        {
            if (speed <= 0f) return;

            float scaleFactor = PerspectiveUtils.GetVisualScaleFactor(_moveDir);
            float step = speed * scaleFactor * Time.deltaTime;

            // 射線檢測
            int count = col.Cast(_moveDir, filter, _hitsBuf, step);
            if (count > 0)
            {
                // 取最近的
                var hit = _hitsBuf.Take(count).OrderBy(h => h.distance).FirstOrDefault();
                if (hit.collider != null)
                {
                    ResolveHit(hit.collider);
                    if (isStopped)
                    {
                        // 稍微補一點位移讓它看起來撞在面上
                        transform.position += (Vector3)(_moveDir * hit.distance);
                        return;
                    }
                }
            }

            // 移動
            transform.position += (Vector3)(_moveDir * step);

            if (alignRotation) ApplyFacingRotation();
        }

        protected override bool OnHitObstacle()
        {
            return true; // 撞牆自殺
        }
        // ============================================================
        // ★ 修改：ApplyFacingRotation (加入透視還原)
        // ============================================================
        private void ApplyFacingRotation()
        {
            // 1. 取得當前的飛行方向 (這是視覺向量，例如 30度)
            Vector2 visualDir = _moveDir;

            if (visualDir.sqrMagnitude < 1e-6f) return;

            // 2. ★ 關鍵修正：還原成邏輯向量 (Unsquash)
            // 因為父物件有 Scale (1, 0.577, 1)，如果直接轉視覺角度，圖片會被壓得更扁(歪掉)
            // 所以我們要先把它 "放大回去" 算邏輯角度
            float logicY = visualDir.y / PerspectiveUtils.GlobalScale.y;
            float logicX = visualDir.x;

            // 3. 計算邏輯角度
            float deg = Mathf.Atan2(logicY, logicX) * Mathf.Rad2Deg;

            // 4. 處理圖片原始朝向修正
            if (modelForward == FacingAxis.Up) deg -= 90f;
            deg += rotationOffsetDeg;

            // 5. 套用旋轉
            Quaternion rot = Quaternion.AngleAxis(deg, Vector3.forward);

            if (modelTransform)
            {
                modelTransform.localRotation = rot;
                transform.rotation = Quaternion.identity;
            }
            else
            {
                // 如果沒有 modelTransform，直接轉 Root (這會導致 Scale 軸向旋轉，通常不建議用於 Linear)
                // 但為了兼容性還是保留
                transform.rotation = rot;
            }
        }
        /*
        private void ApplyFacingRotation()
        {
            float deg = Mathf.Atan2(_moveDir.y, _moveDir.x) * Mathf.Rad2Deg;
            if (modelForward == FacingAxis.Up) deg -= 90f;
            deg += rotationOffsetDeg;

            Quaternion rot = Quaternion.AngleAxis(deg, Vector3.forward);
            if (modelTransform) { modelTransform.localRotation = rot; transform.rotation = Quaternion.identity; }
            else transform.rotation = rot;
        }
        */
    }
}