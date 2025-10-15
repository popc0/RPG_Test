using UnityEngine;

public class JoystickValueViewer : MonoBehaviour
{
    void Update()
    {
        float a1 = Input.GetAxisRaw("Joystick Axis 1");
        float a2 = Input.GetAxisRaw("Joystick Axis 2");
        float a3 = Input.GetAxisRaw("Joystick Axis 3");
        float a4 = Input.GetAxisRaw("Joystick Axis 4");
        float a5 = Input.GetAxisRaw("Joystick Axis 5");
        float a6 = Input.GetAxisRaw("Joystick Axis 6");
        float a7 = Input.GetAxisRaw("Joystick Axis 7");
        float a8 = Input.GetAxisRaw("Joystick Axis 8");

        Debug.Log($"[Joystick Axes] A1={a1:F2}, A2={a2:F2}, A3={a3:F2}, A4={a4:F2}, A5={a5:F2}, A6={a6:F2}, A7={a7:F2}, A8={a8:F2}");
    }
}
