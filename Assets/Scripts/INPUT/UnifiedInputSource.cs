using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 統一的輸入來源：把鍵盤/手把改成走 Input Actions，亦可切換使用 DynamicTouchJoystick。
/// 掛在 Player/Input 空物件上。
/// </summary>
public class UnifiedInputSource : MonoBehaviour, IInputSource
{
    [Header("Movement (Vector2) 例：WASD / 左搖桿 (Input Actions)")]
    public InputActionReference move; // Value(Vector2)

    [Header("是否改用動態觸控搖桿 (如手機)")]
    public bool useDynamicTouchJoystick = false;
    [Tooltip("指到場景上的 DynamicTouchJoystick 腳本。只會嘗試讀取公開的 Vector2 屬性/方法，如 Value/Direction/Input...")]
    public MonoBehaviour dynamicTouchJoystick;

    [Header("Interact (Button) 例：E / 手把")]
    public InputActionReference interact;

    [Header("Menu (Button) 例：Esc / 手把 Start")]
    public InputActionReference menu;

    [Header("Attack / Attack2 例：K / O")]
    public InputActionReference attack;
    public InputActionReference attack2;

    // ===== lifecycle =====
    void OnEnable()
    {
        Enable(move); Enable(interact); Enable(menu); Enable(attack); Enable(attack2);
    }
    void OnDisable()
    {
        Disable(move); Disable(interact); Disable(menu); Disable(attack); Disable(attack2);
    }

    static void Enable(InputActionReference r) { if (r && r.action != null && !r.action.enabled) r.action.Enable(); }
    static void Disable(InputActionReference r) { if (r && r.action != null && r.action.enabled) r.action.Disable(); }

    // ===== IInputSource =====
    public Vector2 GetMoveVector()
    {
        // 1) 觸控搖桿（若有勾選且可讀取）
        if (useDynamicTouchJoystick && dynamicTouchJoystick != null)
        {
            if (_dynGetter == null) _dynGetter = BuildDynGetter(dynamicTouchJoystick);
            if (_dynGetter != null) return Vector2.ClampMagnitude(_dynGetter(), 1f);
        }
        // 2) Input Actions
        return move != null ? Vector2.ClampMagnitude(move.action.ReadValue<Vector2>(), 1f) : Vector2.zero;
    }
    public bool InteractPressedThisFrame() => interact && interact.action.WasPerformedThisFrame();
    public bool MenuPressedThisFrame() => menu && menu.action.WasPerformedThisFrame();
    public bool AttackIsPressedThisFrame() => attack && attack.action.IsPressed();
    public bool Attack2IsPressedThisFrame() => attack2 && attack2.action.IsPressed();

    // ---- DynamicTouchJoystick 反射存取 ----
    private System.Func<Vector2> _dynGetter;
    private static System.Func<Vector2> BuildDynGetter(MonoBehaviour b)
    {
        var t = b.GetType();
        // 常見 Vector2 屬性
        foreach (var name in new[] { "Value", "Direction", "Input", "Delta", "Axis", "Current", "Vector" })
        {
            var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(Vector2) && p.CanRead)
                return () => (Vector2)p.GetValue(b, null);
        }
        // 常見 Vector2 無參方法
        foreach (var m in new[] { "Read", "GetValue", "GetDirection", "GetVector", "GetAxis" })
        {
            var mi = t.GetMethod(m, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, System.Type.EmptyTypes, null);
            if (mi != null && mi.ReturnType == typeof(Vector2))
                return () => (Vector2)mi.Invoke(b, null);
        }
        Debug.LogWarning($"[UnifiedInputSource] 找不到可讀 Vector2 的屬性/方法於 {t.Name}，請確認 DynamicTouchJoystick 暴露的 API。");
        return null;
    }
}

public interface IInputSource
{
    Vector2 GetMoveVector();
    bool InteractPressedThisFrame();
    bool MenuPressedThisFrame();
    bool AttackIsPressedThisFrame();
    bool Attack2IsPressedThisFrame();
}