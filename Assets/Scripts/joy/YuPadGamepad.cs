/*

#if ENABLE_INPUT_SYSTEM && UNITY_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

[InputControlLayout(displayName = "YuPad Gamepad")]
public class YuPadGamepad : Joystick
{
    public new ButtonControl  trigger { get; private set; }

    protected override void FinishSetup()
    {
        base.FinishSetup();
        trigger = GetChildControl<ButtonControl>("trigger"); // 沒定義時為 null，不會拋錯
    }

    static bool s_Registered;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void EnsureRegistered()
    {
        if (s_Registered) return;
        s_Registered = true;

        // 只接你的裝置（58626 / 48043）。usage=0x05=Gamepad；如果你的描述是 Joystick 就改 0x04。
        var matcher = new InputDeviceMatcher()
            .WithInterface("HID")
            .WithCapability("vendorId",  58626) // 0xE502
            .WithCapability("productId", 48043) // 0xBBAB
            .WithCapability("usagePage", 0x01)  // Generic Desktop
            .WithCapability("usage",     0x05); // Gamepad (若是 Joystick 改成 0x04)

        // 這裡只註冊「裝置型別別名」，真正位元對應在下一支 JSON 內
        InputSystem.RegisterLayout<YuPadGamepad>(matches: matcher);

        Debug.Log("[YuPad] Custom device type 'YuPadGamepad' registered (VID=58626 PID=48043).");
    }
}
#endif
*/