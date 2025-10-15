using UnityEngine;

/// 自動校正藍牙搖桿中心；按 P 重新校正
public class GamepadInputFilter : MonoBehaviour
{
    [Header("Joystick Axes (ONLY joystick axes, not Horizontal/Vertical)")]
    public string axisX = "JoyX";
    public string axisY = "JoyY";
    public bool invertY = true;

    [Header("Deadzone / Clamp")]
    [Range(0f, 1f)] public float innerDeadzone = 0.25f;  // 內圈死區（小於此視為 0）
    [Range(0.5f, 1f)] public float outerCut = 0.98f;     // 外圈裁切（防抖 saturated）

    [Header("Calibrate Settings")]
    public int minSamples = 60;               // 至少要收幾筆「近中心」樣本
    public int maxFrames = 600;               // 最多取樣幀數（避免卡住）
    [Range(0f, 1f)] public float stillness = 0.40f; // 視為「近中心」的門檻 (|x|<=stillness & |y|<=stillness)
    public float startDelay = 1.0f;           // 進遊戲多久後自動校正
    public float maxOffsetClamp = 0.5f;       // 偏移量安全上限，避免 -1 被當作中心

    [Header("Debug (Read Only)")]
    public Vector2 rawInput;
    public Vector2 filteredInput;
    public Vector2 offset;    // 最終使用的校正偏移
    public bool calibrated;

    void Start()
    {
        Invoke(nameof(Calibrate), startDelay);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            Calibrate();

        // 讀取原始
        float x = Input.GetAxisRaw(axisX);
        float y = Input.GetAxisRaw(axisY);
        if (invertY) y = -y;
        rawInput = new Vector2(x, y);

        // 套用偏移 → 死區 → 外圈裁切
        Vector2 v = rawInput - offset;

        // 內圈死區（圓形）
        float mag = v.magnitude;
        if (mag < innerDeadzone)
            v = Vector2.zero;
        else
            v = v / Mathf.Max(mag, 1e-5f); // normalize

        // 外圈平滑（避免邊緣震盪）
        float t = Mathf.InverseLerp(innerDeadzone, 1f, mag);
        t = Mathf.SmoothStep(0f, 1f, t);
        v *= Mathf.Min(mag, outerCut); // 輕裁切

        filteredInput = v;
    }

    public Vector2 GetFilteredInput() => filteredInput;

    public void Calibrate()
    {
        StopAllCoroutines();
        StartCoroutine(CalibRoutine());
    }

    System.Collections.IEnumerator CalibRoutine()
    {
        calibrated = false;
        Debug.Log("[GamepadInputFilter] 開始校正… 請放開搖桿、不要按任何鍵。");

        int ok = 0;
        Vector2 sum = Vector2.zero;

        for (int f = 0; f < maxFrames && ok < minSamples; f++)
        {
            float x = Input.GetAxisRaw(axisX);
            float y = Input.GetAxisRaw(axisY);
            if (invertY) y = -y;

            // 只收「近中心」樣本；避免鍵盤/誤觸把均值拉到 -1
            if (Mathf.Abs(x) <= stillness && Mathf.Abs(y) <= stillness)
            {
                sum.x += x;
                sum.y += y;
                ok++;
            }
            yield return null;
        }

        if (ok < Mathf.Max(1, minSamples / 2))
        {
            Debug.LogWarning($"[GamepadInputFilter] 校正失敗：收集到的近中心樣本太少 (ok={ok})，保留舊 offset={offset}。請確認 axisX/axisY 指向『搖桿專用軸』且未被鍵盤混入。");
            yield break;
        }

        Vector2 avg = sum / ok;

        // 安全夾取，避免極端值（例如 -1,-1）
        avg.x = Mathf.Clamp(avg.x, -maxOffsetClamp, maxOffsetClamp);
        avg.y = Mathf.Clamp(avg.y, -maxOffsetClamp, maxOffsetClamp);

        offset = avg;
        calibrated = true;
        Debug.Log($"[GamepadInputFilter] 校正完成 offset=({offset.x:F3},{offset.y:F3})  (樣本:{ok})");
    }
}
