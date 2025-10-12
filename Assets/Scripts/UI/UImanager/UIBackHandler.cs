using UnityEngine;

public class UIBackHandler : MonoBehaviour
{
    public KeyCode backKey = KeyCode.R; // 你之前把 ESC 改 R 就改這裡或用新 Input System 綁 Action

    void Update()
    {
        if (Input.GetKeyDown(backKey))
            UIOrchestrator.I?.Pop();
    }
}
