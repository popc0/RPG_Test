using UnityEngine;

public class GamepadDebugger : MonoBehaviour
{
    [Header("更新間隔 (秒)")]
    public float interval = 0.2f;
    private float timer;

    // 你想觀察的軸名稱
    private string[] axes = new string[]
    {
        "Horizontal", "Vertical",
        "JoyX", "JoyY",
        "Joystick X", "Joystick Y"
    };

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            PrintInputValues();
        }
    }

    private void PrintInputValues()
    {
        Debug.Log("===== [Gamepad Debugger] =====");

        // 顯示所有軸的值
        foreach (var axis in axes)
        {
            try
            {
                float v = Input.GetAxisRaw(axis);
                if (Mathf.Abs(v) > 0.01f)
                    Debug.Log($"{axis} = {v:F3}");
            }
            catch
            {
                // 忽略不存在的軸
            }
        }

        // 顯示按鈕狀態（前 10 個）
        for (int i = 0; i < 10; i++)
        {
            string btn = $"joystick button {i}";
            if (Input.GetKey(btn))
                Debug.Log($"{btn} = DOWN");
        }
    }
}
