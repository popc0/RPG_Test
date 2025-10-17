using System;

public static class UIEvents
{
    // Canvas ¼h
    public static event Action OnOpenSystemCanvas;
    public static event Action OnOpenStoryCanvas;
    public static event Action OnOpenTPHintCanvas;
    public static event Action OnCloseActiveCanvas;

    public static void RaiseOpenSystemCanvas() => OnOpenSystemCanvas?.Invoke();
    public static void RaiseOpenStoryCanvas() => OnOpenStoryCanvas?.Invoke();
    public static void RaiseOpenTPHintCanvas() => OnOpenTPHintCanvas?.Invoke();
    public static void RaiseCloseActiveCanvas() => OnCloseActiveCanvas?.Invoke();
}
