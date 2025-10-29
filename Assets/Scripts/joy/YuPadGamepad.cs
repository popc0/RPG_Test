using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

/// 把 E502:BBAB 這顆 BLE 裝置，直接視為「標準 Gamepad」
/// 這樣 Unity 會用 Gamepad 佈局解析，而不是 Unsupported HID。
[DisplayStringFormat("{displayName}")]
public class YuPadGamepad : Gamepad
{
    // 需要時可增加自訂控制（目前沿用 Gamepad 既有 mapping）
    protected override void FinishSetup()
    {
        base.FinishSetup();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        // 安全起見，先刪掉任何舊的相同註冊（避免重複）
        InputSystem.RemoveLayout(nameof(YuPadGamepad));

        // 註冊自訂 Layout：繼承 Gamepad
        InputSystem.RegisterLayout<YuPadGamepad>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithCapability("vendorId", 0xE502)   // 從你的 Log 抓到
                .WithCapability("productId", 0xBBAB)  // 從你的 Log 抓到
        );

        // （可選）若你曾被判成 UnsupportedHID，下面這行會把相同 VID/PID 也導去 Gamepad
        // 等同告訴 Unity：看到這顆裝置 → 用 YuPadGamepad 解析
        InputSystem.RegisterLayoutMatcher(nameof(Gamepad),
            new InputDeviceMatcher()
                .WithInterface("HID")
                .WithCapability("vendorId", 0xE502)
                .WithCapability("productId", 0xBBAB)
        );
    }
}
