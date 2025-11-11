using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardInputSource : MonoBehaviour, IInputSource
{
    [Header("KEYE (Input Action)")]
    public InputActionReference keyE;  // 指到你在 InputActions 裡的 KEYE

    private void OnEnable()
    {
        if (keyE?.action != null && !keyE.action.enabled)
            keyE.action.Enable();
    }

    private void OnDisable()
    {
        if (keyE?.action != null && keyE.action.enabled)
            keyE.action.Disable();
    }

    public bool InteractPressedThisFrame()
    {
        // 回傳這幀是否觸發 KEYE（需在 Input Actions 設為 Press）
        return keyE != null && keyE.action.WasPerformedThisFrame();
    }
}
