// Assets/Scripts/joy/YuPadReader.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class YuPadTester : MonoBehaviour
{
    void Update()
    {
        var dev = InputSystem.GetDevice("YuPadGamepad");
        if (dev == null) return;
        var x = dev.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("leftStick/x");
        var y = dev.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("leftStick/y");
        if (x != null && y != null)
        {
            var v = new Vector2(x.ReadValue(), y.ReadValue());
            if (v.sqrMagnitude > 0.000001f) Debug.Log($"YuPad leftStick {v}");
        }
    }
}
