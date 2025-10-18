using System;
using UnityEngine;

public static class UIEvents
{
    // 外層 Canvas 註冊（避免跨場景接線斷掉）
    public static event Action<string, CanvasGroup> OnRegisterCanvas;
    public static event Action<string> OnUnregisterCanvas;
    public static void RegisterCanvas(string key, CanvasGroup cg) => OnRegisterCanvas?.Invoke(key, cg);
    public static void UnregisterCanvas(string key) => OnUnregisterCanvas?.Invoke(key);

    // 外層前景切換（只管 MainMenu / System）
    public static event Action<string> OnOpenCanvas;   // key: "mainmenu" or "system"
    public static event Action OnCloseActiveCanvas;    // 外層前景關閉（恢復預設）
    public static void RaiseOpenCanvas(string key) => OnOpenCanvas?.Invoke(key);
    public static void RaiseCloseActiveCanvas() => OnCloseActiveCanvas?.Invoke();
}
