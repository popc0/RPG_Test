#if ENABLE_INPUT_SYSTEM && UNITY_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

public static class YuPadLayoutJson
{
    // 依你的量測：
    // X 中心大約在 0xF5xx → 有號為 -2700 左右
    // Y 中心大約在 0x0Axx → 有號為 +2720 左右
    // 兩端近似 -32768..+32767，因此用 SHRT + normalizeMin/Max + normalizeZero 校正到 [-1..1] 中心 0。
    private const string Json = @"
{
  ""name"": ""YuPadGamepadHID16"",
  ""extend"": ""HID"",
  ""format"": ""HID"",
  ""displayName"": ""YuPad (HID 16-bit XY + Button)"",
  ""controls"": [

    { ""name"": ""leftStick"",
      ""layout"": ""Stick"",
      ""offset"": 0,
      ""displayName"": ""Left Stick"",
      ""synthetic"": true,
      ""usages"": [""Primary2DMotion""] },

    { ""name"": ""buttonSouth"",
      ""layout"": ""Button"",
      ""offset"": 1,
      ""bit"": 0,
      ""sizeInBits"": 1,
      ""format"": ""BIT "",
      ""usages"": [ ""PrimaryAction"" ],
      ""displayName"": ""A"" },

    { ""name"": ""leftStick/x"",
      ""layout"": ""Axis"",
      ""format"": ""SHRT"",
      ""offset"": 2,
      ""sizeInBits"": 16,
      ""processors"": ""axisDeadzone"" },

    { ""name"": ""leftStick/y"",
      ""layout"": ""Axis"",
      ""format"": ""SHRT"",
      ""offset"": 4,
      ""sizeInBits"": 16,
      ""processors"": ""axisDeadzone"" }

  ]
}";


    private static bool s_done;

#if UNITY_EDITOR
    // 讓「一開專案/編譯重載」就註冊（不需要按 Play）
    [UnityEditor.InitializeOnLoadMethod]
    private static void EditorRegister()
    {
        TryRegister("editor-load");
    }
#endif

    // 讓執行時也註冊（保險）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void RuntimeRegister()
    {
        TryRegister("runtime");
    }

    private static void TryRegister(string tag)
    {
        if (s_done) return;
        s_done = true;

        var matcher = new InputDeviceMatcher()
            .WithInterface("HID")
            .WithCapability("vendorId", 58626)   // 0xE502
            .WithCapability("productId", 48043)  // 0xBBAB
            .WithCapability("usagePage", 0x01)   // Generic Desktop
            .WithCapability("usage", 0x05);      // Gamepad

        // 註冊 JSON 版型
        InputSystem.RegisterLayout(json: Json, name: "YuPadGamepadHID16", matches: matcher);
        Debug.Log($"[YuPad] JSON layout 'YuPadGamepadHID16' registered at {tag}.");
    }
}
#endif
