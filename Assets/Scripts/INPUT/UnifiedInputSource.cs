using RPG;
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
    public InputActionReference switchSkillGroup; // 切換技能組

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
    public void ResetAllInputStates()
    {
        // 對所有輸入 Action 呼叫 .Reset()
        if (move?.action != null) move.action.Reset();
        if (interact?.action != null) interact.action.Reset();
        if (attack?.action != null) attack.action.Reset();
        if (attack2?.action != null) attack2.action.Reset();
        if (switchSkillGroup?.action != null) switchSkillGroup.action.Reset();
    }
    private void OnEnable()
    {
        //Enable(move);
        //Enable(interact);
        //Enable(menu);
        //Enable(attack);
        //Enable(attack2);
        //Enable(switchSkillGroup);
    }

    private void OnDisable()
    {
        //Disable(move);
        //Disable(interact);
        //Disable(menu);
        //Disable(attack);
        //Disable(attack2);
        //Disable(switchSkillGroup);
    }
    public void DisableAllInputActions()
    {
        // 呼叫原本的 Disable 靜態方法，禁用所有相關動作
        Disable(move);
        Disable(interact);
        Disable(attack);
        Disable(attack2);
        Disable(switchSkillGroup);
        // ... 確保所有您在 OnEnable 中啟用的動作都被禁用
    }
    public void EnableAllInputActions()
    {
        // 重新啟用所有動作
        Enable(move);
        Enable(interact);
        Enable(attack);
        Enable(attack2);
        Enable(switchSkillGroup);
        // ...
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
        // 1. 取得原始輸入 (物理空間 Raw Input)
        // 這裡拿到的還是 45 度、長度 1 的原始向量
        Vector2 rawInput = Vector2.zero;

        if (move != null && move.action != null)
            rawInput = move.action.ReadValue<Vector2>();

        if (dynamicTouchJoystick != null && dynamicTouchJoystick.enabledOnThisPlatform)
        {
            Vector2 joy = dynamicTouchJoystick.Value;
            // 覆蓋邏輯：如果有搖桿輸入，就以此為準
            if (joy.sqrMagnitude > 0.001f)
                rawInput = joy;
        }

        // 防呆：如果沒有輸入就直接回傳零
        if (rawInput.sqrMagnitude < 1e-6f) return Vector2.zero;

        // 2. ★ 核心修改：在此處統一進行透視轉換
        // 把 "物理輸入" (例如搖桿推到底) 轉換成 "視覺位移"
        // 假設 PerspectiveUtils.GlobalScale 是 (1, 0.5)
        // 往上推 (0,1) -> 變成 (0, 0.5)
        // 往右推 (1,0) -> 變成 (1, 0)
        // 往斜推 (0.7, 0.7) -> 變成 (0.7, 0.35) -> 角度變平

        Vector3 visualVec3 = PerspectiveUtils.GetVisualVector(rawInput.normalized, rawInput.magnitude);

        // 轉回 Vector2
        Vector2 finalOutput = (Vector2)visualVec3;

        // 3. 限制最大長度
        // 因為壓扁通常是變小，所以不太會超過 1，但為了保險還是 Clamp 一下
        return Vector2.ClampMagnitude(finalOutput, 1f);
    }

    public bool InteractPressedThisFrame()
        => interact != null && interact.action != null && interact.action.WasPerformedThisFrame();

    public bool MenuPressedThisFrame()
        => menu != null && menu.action != null && menu.action.WasPerformedThisFrame();

    public bool AttackPressedThisFrame()
        => attack != null && attack.action != null && attack.action.WasPerformedThisFrame();
    public bool Attack2PressedThisFrame()
       => attack2 != null && attack2.action != null && attack2.action.WasPerformedThisFrame();

    public bool SwitchSkillGroupPressedThisFrame()
    => switchSkillGroup != null && switchSkillGroup.action != null && switchSkillGroup.action.WasPerformedThisFrame();
}

/// <summary>
/// Player 使用的統一輸入介面
/// </summary>
public interface IInputSource
{
    Vector2 GetMoveVector();
    bool InteractPressedThisFrame();
    bool MenuPressedThisFrame();
    bool AttackPressedThisFrame();
    bool Attack2PressedThisFrame();
    bool SwitchSkillGroupPressedThisFrame();
}


