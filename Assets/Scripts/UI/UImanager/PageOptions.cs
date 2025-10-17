using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PageOptions : MonoBehaviour
{
    [Header("入場出場動畫")]
    [SerializeField] float duration = 0.25f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("預設聚焦按鈕")]
    [SerializeField] Selectable defaultFocus;

    [Header("關閉快捷鍵")]
    [SerializeField] KeyCode closeKey = KeyCode.B;

    RectTransform rt;
    CanvasGroup cg;
    Coroutine tweenCo;
    Vector2 startPosHidden;
    Vector2 startPosShown;
    bool isOpen;

    GameObject callerPageRoot; // 從誰開的

    SystemCanvasController scc;

    public bool IsOpen => isOpen;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        startPosShown = Vector2.zero;
        startPosHidden = new Vector2(rt.rect.width, 0); // 從右滑入

        // 初始為關閉狀態，但不關物件
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        rt.anchoredPosition = startPosHidden;

        TryFindSCC();
    }

    void Update()
    {
        if (!isOpen) return;
        if (Input.GetKeyDown(closeKey))
        {
            CloseRequested();
        }
    }

    void StopTween()
    {
        if (tweenCo != null) StopCoroutine(tweenCo);
        tweenCo = null;
    }

    public void Open(GameObject caller)
    {
        callerPageRoot = caller;

        if (UIPageStack.Instance != null && callerPageRoot != null)
            UIPageStack.Instance.Push(callerPageRoot);

        if (isOpen) return;
        isOpen = true;

        StopTween();

        // 顯示並可互動（不使用 SetActive）
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        // 先清焦點，下一幀再設
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(SetFocusNextFrame());
        }

        tweenCo = StartCoroutine(TweenPos(rt.anchoredPosition, startPosShown, true));
    }

    public void CloseRequested()
    {
        if (!isOpen) return;

        isOpen = false;
        StopTween();

        // 立刻關互動，開始滑出
        cg.blocksRaycasts = false;
        cg.interactable = false;

        tweenCo = StartCoroutine(TweenPos(rt.anchoredPosition, startPosHidden, false, NotifyControllerClosed));
    }

    // Back 按鈕請直接綁這個
    public void OnClick_Back()
    {
        CloseRequested();
    }

    IEnumerator TweenPos(Vector2 from, Vector2 to, bool opening, System.Action onComplete = null)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            yield return null;
        }
        rt.anchoredPosition = to;

        // 關閉後保持 alpha 0、不可互動；不做 SetActive(false)
        if (!opening)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        tweenCo = null;
        onComplete?.Invoke();
    }

    IEnumerator SetFocusNextFrame()
    {
        yield return null;
        if (defaultFocus != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(defaultFocus.gameObject);
    }

    void NotifyControllerClosed()
    {
        TryFindSCC();
        if (scc != null) scc.OnOptionsClosed();

        if (UIPageStack.Instance != null)
            UIPageStack.Instance.PopAndFocus();
    }

    void TryFindSCC()
    {
        if (scc != null) return;
        scc = FindObjectOfType<SystemCanvasController>();
        if (scc == null)
            Debug.LogWarning("[PageOptions] 找不到 SystemCanvasController。請確認它掛在 SystemCanvas。");
    }
}
