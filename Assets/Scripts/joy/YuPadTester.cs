using UnityEngine;
using UnityEngine.InputSystem;

public class YuPadTester : MonoBehaviour
{
    private YuPadGamepad pad;
    private Vector2 lastLS;
    private double lastPrint;

    void Update()
    {
        // 只抓我們剛註冊的 YuPadGamepad；若找不到就抓一般 Gamepad（容錯）
        if (pad == null)
            pad = InputSystem.GetDevice<YuPadGamepad>();

        var gamepad = (InputSystem.GetDevice<YuPadGamepad>() as Gamepad) ?? Gamepad.current;
        if (gamepad == null) return;

        // 左搖桿
        Vector2 ls = gamepad.leftStick.ReadValue();

        // 每 0.2 秒列印一次（避免刷爆 Console）
        if (Time.unscaledTimeAsDouble - lastPrint > 0.2)
        {
            lastPrint = Time.unscaledTimeAsDouble;
            Debug.Log($"[YuPad] LS={ls}  A={gamepad.buttonSouth.isPressed}  B={gamepad.buttonEast.isPressed}  X={gamepad.buttonWest.isPressed}  Y={gamepad.buttonNorth.isPressed}");
        }

        // 畫在螢幕左上角
        lastLS = ls;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 500, 30), $"YuPad LS: {lastLS}");
        var gp = Gamepad.current;
        if (gp != null)
        {
            GUI.Label(new Rect(10, 30, 500, 30),
                $"A:{gp.buttonSouth.isPressed}  B:{gp.buttonEast.isPressed}  X:{gp.buttonWest.isPressed}  Y:{gp.buttonNorth.isPressed}");
        }
    }
}
