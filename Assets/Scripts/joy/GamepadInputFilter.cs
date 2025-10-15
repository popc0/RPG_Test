using UnityEngine;

/// <summary>
/// 類比搖桿輸入整形（不做 4/8 向吸附）
/// 1) 圓形死區 + 外圈裁切
/// 2) 自動偏移校正 (bias) → 去漂移（不改板子）
/// 3) 卡極值保護（長時間-1/1 無變化當作故障，緩慢回中）
/// 4) 平滑 (可關)
/// 5) 鍵盤與搖桿自動擇強
/// 6) 熱鍵重置中心（預設：R）
/// </summary>
public class GamepadInputFilter : MonoBehaviour
{
    [Header("Axis 名稱 (Input Manager)")]
    public string axisX = "Horizontal";
    public string axisY = "Vertical";

    [Header("死區/外圈")]
    [Range(0f, 0.6f)] public float innerDeadzone = 0.18f;   // 中心死區（圓形）
    [Range(0.8f, 1.2f)] public float outerCut = 1.00f;       // 外圈裁切（<1 可削掉邊緣飽和）

    [Header("自動偏移校正 (Anti-Drift)")]
    [Tooltip("當輸入幅度小於此值時，視為可能在漂移，學習偏移量")]
    [Range(0.05f, 0.8f)] public float biasCaptureMax = 0.65f;
    [Tooltip("偏移量學習速度，越大收斂越快")]
    [Range(0f, 8f)] public float biasLearnSpeed = 1.8f;
    [Tooltip("熱鍵：手動重置偏移量")]
    public KeyCode recenterKey = KeyCode.R;

    [Header("卡極值保護")]
    [Tooltip("連續卡在 |x| 或 |y| >= 此門檻且變化極小，判定『卡極值』")]
    [Range(0.9f, 1.0f)] public float stuckThreshold = 0.98f;
    [Tooltip("持續幾秒以上視為卡極值")]
    [Range(0.05f, 2f)] public float stuckTime = 0.35f;
    [Tooltip("卡極值時往中心回彈速度")]
    [Range(0f, 30f)] public float unstuckSpeed = 10f;
    [Tooltip("判斷『變化極小』的 Δ 門檻")]
    [Range(0f, 0.1f)] public float tinyDelta = 0.02f;

    [Header("平滑")]
    [Range(0f, 30f)] public float followSpeed = 12f; // 0=關閉平滑

    [Header("鍵盤 (WASD)")]
    public KeyCode keyLeft = KeyCode.A;
    public KeyCode keyRight = KeyCode.D;
    public KeyCode keyUp = KeyCode.W;
    public KeyCode keyDown = KeyCode.S;

    [Header("除錯")]
    public bool logOncePerSecond = false;

    // 內部狀態
    private Vector2 _bias;        // 自動學習的偏移量
    private Vector2 _prevRaw;     // 前一禎原始（擇強後）的值
    private float _stuckTimerX;
    private float _stuckTimerY;
    private Vector2 _smoothed;
    private float _logTimer;

    public Vector2 GetMove()
    {
        // 1) 讀鍵盤
        float kx = (Input.GetKey(keyRight) ? 1f : 0f) - (Input.GetKey(keyLeft) ? 1f : 0f);
        float ky = (Input.GetKey(keyUp) ? 1f : 0f) - (Input.GetKey(keyDown) ? 1f : 0f);
        Vector2 k = new Vector2(kx, ky);

        // 2) 讀搖桿（Raw 比較不會被 Unity 平滑污染）
        float jx = SafeGetAxisRaw(axisX);
        float jy = SafeGetAxisRaw(axisY);
        Vector2 j = new Vector2(jx, jy);

        // 3) 擇強（誰幅度大用誰）
        Vector2 raw = (j.sqrMagnitude >= k.sqrMagnitude) ? j : k;

        // 4) 自動偏移校正（只在幅度小時學習 → 去漂移）
        if (raw.magnitude <= biasCaptureMax)
        {
            _bias = Vector2.Lerp(_bias, raw, 1f - Mathf.Exp(-biasLearnSpeed * Time.unscaledDeltaTime));
        }
        if (Input.GetKeyDown(recenterKey)) _bias = Vector2.zero;

        Vector2 unbiased = raw - _bias;

        // 5) 圓形死區 + 外圈裁切 + 重新映射
        Vector2 shaped = ShapeCircular(unbiased, innerDeadzone, outerCut);

        // 6) 卡極值保護：若長時間卡在 ±1 且幾乎沒變化，視為故障→往 0 回彈
        shaped = ApplyStuckGuard(shaped);

        // 7) 平滑
        if (followSpeed > 0f)
            _smoothed = Vector2.Lerp(_smoothed, shaped, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        else
            _smoothed = shaped;

        // 等速（避免斜向過快）
        if (_smoothed.sqrMagnitude > 1f) _smoothed.Normalize();

        // 除錯
        if (logOncePerSecond)
        {
            _logTimer += Time.unscaledDeltaTime;
            if (_logTimer >= 1f)
            {
                _logTimer = 0f;
                Debug.Log($"[Filter] raw={raw} bias={_bias} shaped={shaped} out={_smoothed}");
            }
        }

        _prevRaw = raw;
        return _smoothed;
    }

    // ========== helpers ==========
    float SafeGetAxisRaw(string name)
    {
        try { return Input.GetAxisRaw(name); }
        catch { return 0f; }
    }

    Vector2 ShapeCircular(Vector2 v, float inner, float outer)
    {
        float mag = v.magnitude;
        if (mag <= inner) return Vector2.zero;

        float max = Mathf.Max(outer, 0.0001f);
        mag = Mathf.Min(mag, max);

        float t = (mag - inner) / (max - inner); // 映射到 0~1
        return (v / (mag > 0 ? mag : 1f)) * t;
    }

    Vector2 ApplyStuckGuard(Vector2 v)
    {
        // X 軸
        if (Mathf.Abs(v.x) >= stuckThreshold && Mathf.Abs(_prevRaw.x - v.x) < tinyDelta)
            _stuckTimerX += Time.unscaledDeltaTime;
        else
            _stuckTimerX = 0f;

        // Y 軸
        if (Mathf.Abs(v.y) >= stuckThreshold && Mathf.Abs(_prevRaw.y - v.y) < tinyDelta)
            _stuckTimerY += Time.unscaledDeltaTime;
        else
            _stuckTimerY = 0f;

        if (_stuckTimerX >= stuckTime)
            v.x = Mathf.Lerp(v.x, 0f, 1f - Mathf.Exp(-unstuckSpeed * Time.deltaTime));
        if (_stuckTimerY >= stuckTime)
            v.y = Mathf.Lerp(v.y, 0f, 1f - Mathf.Exp(-unstuckSpeed * Time.deltaTime));

        return v;
    }
}
