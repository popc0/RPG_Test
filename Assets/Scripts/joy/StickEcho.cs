using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class StickEcho : MonoBehaviour
{
    private InputAction move;

    private void OnEnable()
    {
        var actions = new Controls(); // 由 inputactions 產生的 C# wrapper（在 Import 時會自動生成）
        actions.Enable();
        move = actions.Gameplay.Move;
    }

    private void Update()
    {
        Vector2 v = move.ReadValue<Vector2>();
        // 只要 ESP32 有連上且被辨識為 Gamepad，就會有 leftStick 的值
        if (v.sqrMagnitude > 0.0001f)
            Debug.Log($"Stick {v}");
    }
}
