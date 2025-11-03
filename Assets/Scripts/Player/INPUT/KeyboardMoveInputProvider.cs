using UnityEngine;
using UnityEngine.InputSystem; // New Input System

/// <summary>
/// 以鍵盤提供 8 方向移動（WASD / 方向鍵）
/// </summary>
public class KeyboardMoveInputProvider : MonoBehaviour, IMoveInputProvider
{
    [Header("允許對角線（歸一化）")]
    public bool normalize = true;

    public Vector2 ReadMove()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        int x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0)
              - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
        int y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1 : 0)
              - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);

        Vector2 v = new Vector2(x, y);
        if (normalize && v != Vector2.zero) v.Normalize();
        return v;
    }
}
