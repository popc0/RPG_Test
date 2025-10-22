using UnityEngine;

/// <summary>
/// 2D 瞄準輸入來源（支援鍵盤 + 手把舊 Input Manager）
/// 可讀左搖桿、右搖桿或鍵盤 WASD/方向鍵，
/// 當無輸入時自動保持上次朝向。
/// </summary>
public class AimSource2D : MonoBehaviour
{
    [Header("來源優先序")]
    [Tooltip("若有右搖桿，優先用來瞄準；否則用左搖桿/鍵盤。")]
    public bool preferRightStickForAim = true;

    [Header("軸名稱設定（舊 Input Manager）")]
    [Tooltip("左搖桿或鍵盤 X 軸名稱（建議: Horizontal 或 JoyX）")]
    public string moveX = "Horizontal";
    [Tooltip("左搖桿或鍵盤 Y 軸名稱（建議: Vertical 或 JoyY）")]
    public string moveY = "Vertical";
    [Tooltip("右搖桿 X 軸名稱（若未設可留空）")]
    public string aimX = "AimX";
    [Tooltip("右搖桿 Y 軸名稱（若未設可留空）")]
    public string aimY = "AimY";
    [Tooltip("Y 軸是否反向（通常不用）")]
    public bool invertY = false;

    [Header("一般參數")]
    [Range(0f, 1f)] public float deadZone = 0.25f;
    public bool normalize = true;

    /// <summary>最終輸出的瞄準方向</summary>
    public Vector2 AimDir { get; private set; } = Vector2.right;

    private Vector2 lastDir = Vector2.right; // ⬅️ 記錄最後一次有效方向

    void Update()
    {
        Vector2 dir = Vector2.zero;

        // 優先右搖桿
        if (preferRightStickForAim)
            dir = ReadAxes(aimX, aimY);

        // 若沒有，改用左搖桿/鍵盤
        if (dir.sqrMagnitude < deadZone * deadZone)
            dir = ReadAxes(moveX, moveY);

        // 有輸入 → 更新方向
        if (dir.sqrMagnitude >= deadZone * deadZone)
        {
            lastDir = normalize ? dir.normalized : dir;
            AimDir = lastDir;
        }
        // 無輸入 → 保留最後方向
        else
        {
            AimDir = lastDir;
        }
    }

    Vector2 ReadAxes(string ax, string ay)
    {
        if (string.IsNullOrEmpty(ax) || string.IsNullOrEmpty(ay))
            return Vector2.zero;

        float x = 0f, y = 0f;
        try { x = Input.GetAxisRaw(ax); } catch { }
        try { y = Input.GetAxisRaw(ay); } catch { }

        if (invertY) y = -y;

        Vector2 v = new Vector2(x, y);
        if (v.magnitude <= deadZone) return Vector2.zero;

        // 線性映射 + 平滑處理
        float t = Mathf.InverseLerp(deadZone, 1f, v.magnitude);
        t = Mathf.SmoothStep(0f, 1f, t);
        return v.normalized * t;
    }
}
