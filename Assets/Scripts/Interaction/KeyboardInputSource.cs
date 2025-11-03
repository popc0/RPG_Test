using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardInputSource : MonoBehaviour, IInputSource
{
    [Header("Interact Key (Input System)")]
    public Key interactKey = Key.E;

    public bool InteractPressedThisFrame()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        return kb[interactKey].wasPressedThisFrame;
    }
}
