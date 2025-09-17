using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class IngameMenuSlide : MonoBehaviour
{
    [Header("Target")]
    public RectTransform panel;
    public CanvasGroup canvasGroup;

    [Header("Positions (Anchored)")]
    public Vector2 shownAnchoredPos = Vector2.zero;      // 顯示時位置
    public Vector2 hiddenAnchoredPos = new Vector2(0, -800); // 隱藏時位置

    [Header("Animation")]
    public float duration = 0.25f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Focus（可選）")]
    public Selectable focusOnOpen;

    [Header("Pause（通常只有 Ingame 的根面板才會勾）")]
    public bool pauseOnOpen = true;

    [Header("Events")]
    public UnityEvent Opened;  // 動畫完成且為開啟狀態時觸發
    public UnityEvent Closed;  // 動畫完成且為關閉狀態時觸發

    public bool IsOpen { get; private set; }
    Coroutine animCo;

    void Reset()
    {
        panel = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        shownAnchoredPos = Vector2.zero;
        hiddenAnchoredPos = new Vector2(0, -800);
        duration = 0.25f;
        ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    void Awake()
    {
        if (!panel) panel = GetComponent<RectTransform>();
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        SnapClosed();
    }

    public void Toggle() { if (IsOpen) Close(); else Open(); }

    public void Open()
    {
        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(Animate(true));
    }

    public void Close()
    {
        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(Animate(false));
    }

    IEnumerator Animate(bool open)
    {
        if (!panel) yield break;
        IsOpen = open;

        if (pauseOnOpen) Time.timeScale = open ? 0f : 1f;

        if (canvasGroup)
        {
            canvasGroup.interactable = open;
            canvasGroup.blocksRaycasts = open;
        }

        float start = Time.unscaledTime;
        float end = start + Mathf.Max(0.0001f, duration);

        Vector2 from = panel.anchoredPosition;
        Vector2 to = open ? shownAnchoredPos : hiddenAnchoredPos;

        float a0 = canvasGroup ? canvasGroup.alpha : 1f;
        float a1 = canvasGroup ? (open ? 1f : 0f) : 1f;

        while (true)
        {
            float t = Mathf.InverseLerp(start, end, Time.unscaledTime);
            float k = ease.Evaluate(t);

            panel.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            if (canvasGroup) canvasGroup.alpha = Mathf.LerpUnclamped(a0, a1, k);

            if (t >= 1f) break;
            yield return null;
        }

        panel.anchoredPosition = to;
        if (canvasGroup) canvasGroup.alpha = a1;

        if (!open)
        {
            if (canvasGroup)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            Closed?.Invoke();
        }
        else
        {
            if (focusOnOpen && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(focusOnOpen.gameObject);
            }
            Opened?.Invoke();
        }

        animCo = null;
    }

    [ContextMenu("Snap Open")]
    public void SnapOpen()
    {
        IsOpen = true;
        if (pauseOnOpen) Time.timeScale = 0f;
        if (panel) panel.anchoredPosition = shownAnchoredPos;
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    [ContextMenu("Snap Closed")]
    public void SnapClosed()
    {
        IsOpen = false;
        if (pauseOnOpen) Time.timeScale = 1f;
        if (panel) panel.anchoredPosition = hiddenAnchoredPos;
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
