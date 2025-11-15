using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 統一輸入來源：
/// - Movement: InputActions(Vector2) + 動態觸控搖桿
/// - Interact / Menu / Attack: InputActions(Button)
/// 掛在 Player/Input 空物件上。
/// </summary>
public class UnifiedInputSource : MonoBehaviour, IInputSource
{
    [Header("Movement (Vector2) 例：WASD / YuPad / 手把左搖桿")]
    [Tooltip("綁定動作圖中的 Move (Value/Vector2)")]
    public InputActionReference move; // Value(Vector2)

    [Header("Dynamic Touch Joystick (可選)")]
    [Tooltip("指到場景上的 DynamicTouchJoystick。若為空，會自動在場景中找第一個。")]
    public DynamicTouchJoystick dynamicTouchJoystick;

    [Header("Interact (Button) 例：E / 手把按鍵")]
    public InputActionReference interact;

    [Header("Menu (Button) 例：Esc / 手把 Start")]
    public InputActionReference menu;

    [Header("Attack / Attack2 例：K / O / 手把按鍵")]
    public InputActionReference attack;
    public InputActionReference attack2;

    // ===== lifecycle =====

    private void Awake()
    {
        // 如果沒手動指定，嘗試在場景找一個 DynamicTouchJoystick（例如 HUD 底下）
        if (dynamicTouchJoystick == null)
        {
            dynamicTouchJoystick = FindFirstObjectByType<DynamicTouchJoystick>();
            if (dynamicTouchJoystick != null)
            {
                Debug.Log($"[UnifiedInputSource] Auto-bound DynamicTouchJoystick: {dynamicTouchJoystick.name}");
            }
        }
    }

    private void OnEnable()
    {
        Enable(move);
        Enable(interact);
        Enable(menu);
        Enable(attack);
        Enable(attack2);
    }

    private void OnDisable()
    {
        Disable(move);
        Disable(interact);
        Disable(menu);
        Disable(attack);
        Disable(attack2);
    }

    private static void Enable(InputActionReference r)
    {
        if (r != null && r.action != null && !r.action.enabled)
            r.action.Enable();
    }

    private static void Disable(InputActionReference r)
    {
        if (r != null && r.action != null && r.action.enabled)
            r.action.Disable();
    }

    // ===== IInputSource 實作 =====

    /// <summary>
    /// 取得移動向量：
    /// 1. 先讀 InputActions (鍵盤 + 手把)
    /// 2. 若有 DynamicTouchJoystick 且有明顯輸入，覆蓋前面的值
    /// </summary>
    public Vector2 GetMoveVector()
    {
        // 1) 先從 Input Actions 讀值（鍵盤 + 手把）
        Vector2 v = Vector2.zero;
        if (move != null && move.action != null)
            v = move.action.ReadValue<Vector2>();

        // 2) 再看 DynamicTouchJoystick（若有設置且有輸入，再覆蓋）
        if (dynamicTouchJoystick != null &&
            dynamicTouchJoystick.enabledOnThisPlatform)
        {
            Vector2 joy = dynamicTouchJoystick.Value;

            // 有明顯輸入才覆蓋（避免小抖動蓋掉硬體輸入）
            if (joy.sqrMagnitude > 0.001f)
                v = joy;
        }

        // 最後限制在長度 1 以內
        return Vector2.ClampMagnitude(v, 1f);
    }

    public bool InteractPressedThisFrame()
        => interact != null && interact.action != null && interact.action.WasPerformedThisFrame();

    public bool MenuPressedThisFrame()
        => menu != null && menu.action != null && menu.action.WasPerformedThisFrame();

    public bool AttackIsPressedThisFrame()
        => attack != null && attack.action != null && attack.action.IsPressed();

    public bool Attack2IsPressedThisFrame()
        => attack2 != null && attack2.action != null && attack2.action.IsPressed();
}

/// <summary>
/// Player 使用的統一輸入介面
/// </summary>
public interface IInputSource
{
    Vector2 GetMoveVector();
    bool InteractPressedThisFrame();
    bool MenuPressedThisFrame();
    bool AttackIsPressedThisFrame();
    bool Attack2IsPressedThisFrame();
}
