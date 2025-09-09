using UnityEngine;

public class KeyboardInputSource : MonoBehaviour, IInputSource
{
    public KeyCode interactKey = KeyCode.E;
    public bool InteractPressedThisFrame() => Input.GetKeyDown(interactKey);
}
