using UnityEngine;

public class GamepadDebugger : MonoBehaviour
{
    [Tooltip("多久檢查一次 (秒)")]
    public float checkInterval = 0.5f;
    private float timer = 0f;

    private string[] testAxes = new string[]
    {
        "Horizontal", "Vertical",
        "JoyX", "JoyY",        // 你可能在 Input Manager 自訂的軸名
        "Joystick X", "Joystick Y"
    };

    void Start()
    {
        Debug.Log("=== [YuPad Debugger 啟動] ===");
        Debug.Log("請移動搖桿或按 D-pad 檢查輸入值是否變動");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        bool anyInput = false;
        foreach (var axis in testAxes)
        {
            float v = 0;
            try { v = Input.GetAxis(axis); }
            catch { continue; }

            if (Mathf.Abs(v) > 0.01f)
            {
                anyInput = true;
                Debug.Log($"[Input] {axis} = {v:F2}");
            }
        }

        // 測試按鍵（D-pad、按鈕）
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKey($"joystick button {i}"))
            {
                anyInput = true;
                Debug.Log($"[Button] joystick button {i} = DOWN");
            }
        }

        if (!anyInput)
        {
            Debug.Log("(No input detected — YuPad 可能未連線或軸名不符)");
        }
    }
}
