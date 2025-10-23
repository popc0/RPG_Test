using UnityEngine;

/// <summary>
/// 2D 瞄準輸入來源：
///  - 滑鼠優先：由 origin 到滑鼠世界座標的向量；距離太近視為無效。
///  - 備援：舊 Input Manager 的 Horizontal/Vertical。
///  - 無有效輸入時維持最後一次方向。
/// NOTE: SkillCaster / AimPreview2D 只要讀 AimDir 即可，不需再處理滑鼠。
/// </summary>
public class AimSource2D : MonoBehaviour
{
    [Header("參考點（不設則用自身）")]
    public Transform origin;

    [Header("舊 Input Manager 備援軸")]
    public string moveX = "Horizontal";
    public string moveY = "Vertical";
    public bool invertY = false;

    [Header("靈敏度")]
    [Tooltip("滑鼠相對 origin 的最小有效距離")]
    public float mouseDeadRadius = 0.05f;
    [Range(0f, 1f)] public float stickDeadZone = 0.25f;
    public bool normalizeOutput = true;

    Camera _cam;

    /// <summary>最終的瞄準方向（單位向量）</summary>
    public Vector2 AimDir { get; private set; } = Vector2.right;

    Vector2 _lastDir = Vector2.right;

    void Reset()
    {
        if (!origin) origin = transform;
        _cam = Camera.main;
    }

    void Awake()
    {
        if (!origin) origin = transform;
        _cam = Camera.main;
    }

    void Update()
    {
        Vector2 dir = ReadMouseDir();
        if (dir.sqrMagnitude < mouseDeadRadius * mouseDeadRadius)
            dir = ReadAxesDir();

        if (dir.sqrMagnitude >= 1e-6f)
        {
            _lastDir = normalizeOutput ? dir.normalized : dir;
            AimDir = _lastDir;
        }
        else
        {
            AimDir = _lastDir; // 維持最後方向
        }
    }

Vector2 ReadMouseDir()
{
    if (!_cam)
    {
        _cam = Camera.main;
        if (!_cam) return Vector2.zero;
    }
    Vector3 o = origin ? origin.position : transform.position;
    Vector3 m = _cam.ScreenToWorldPoint(Input.mousePosition);
    m.z = 0f;
    return (Vector2)(m - o);
}


    Vector2 ReadAxesDir()
    {
        float x = 0f, y = 0f;
        try { x = Input.GetAxisRaw(moveX); } catch { }
        try { y = Input.GetAxisRaw(moveY); } catch { }
        if (invertY) y = -y;

        Vector2 v = new Vector2(x, y);
        float mag = v.magnitude;
        if (mag <= stickDeadZone) return Vector2.zero;

        // 讓小幅度輸入更平順
        float t = Mathf.InverseLerp(stickDeadZone, 1f, mag);
        t = Mathf.SmoothStep(0f, 1f, t);
        return v.normalized * t;
    }
}
