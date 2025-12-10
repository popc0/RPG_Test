using UnityEngine;

namespace RPG
{
    public static class PerspectiveUtils
    {
        // ★ 修正：改為 30度視角標準 (Tan(30) = 0.57735)
        // 這樣 (1, 1) 的物理輸入會轉換成 (1, 0.577) 的視覺向量 -> 剛好 30 度
        public static readonly Vector3 GlobalScale = new Vector3(1f, 0.57735f, 1f);

        /// <summary>
        /// ★ 核心公式：給定一個「視覺方向」(已正規化)，回傳該方向應有的「透視長度倍率」。
        /// </summary>
        public static float GetVisualScaleFactor(Vector2 normalizedVisualDir)
        {
            // 公式推導：L = 1 / sqrt(x^2 + (y/scaleY)^2)
            // 這裡 scaleY = 0.57735
            // (1 / 0.57735)^2 = (1.732)^2 = 3.0
            // 所以分母係數從 4.0 變成 3.0

            float x2 = normalizedVisualDir.x * normalizedVisualDir.x;
            float y2 = normalizedVisualDir.y * normalizedVisualDir.y;

            // ★ 修正係數： (1 / 0.57735f)^2 ≈ 3.0f
            float denom = Mathf.Sqrt(x2 + 3.0f * y2);

            return 1f / denom;
        }
        // 用於將物理向量轉為視覺向量 (UnifiedInputSource 用)
        public static Vector3 GetVisualVector(Vector2 physicalDir, float physicalLength)
        {
            return new Vector3(
                physicalDir.x * GlobalScale.x,
                physicalDir.y * GlobalScale.y,
                0f
            ) * physicalLength;
        }
        public static Quaternion GetFacingRotation(Vector2 dir, float offsetDeg = 0f, bool isUpAxis = false)
        {
            if (dir.sqrMagnitude < 1e-6f) return Quaternion.identity;
            float deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (isUpAxis) deg -= 90f;
            deg += offsetDeg;
            return Quaternion.AngleAxis(deg, Vector3.forward);
        }

        // 舊方法可以保留或標記過時
        public static float PhysicalToVisualDistance(float physicalDist, Vector2 dir)
        {
            // 這裡可以改用新公式，確保邏輯統一
            return physicalDist * GetVisualScaleFactor(dir.normalized);
        }
    }
}