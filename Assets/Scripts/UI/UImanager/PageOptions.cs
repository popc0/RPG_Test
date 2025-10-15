using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PageOptions : MonoBehaviour
{
    [Header("滑入方向 = 右 → 中")]
    [SerializeField] float duration = 0.25f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Header("預設聚焦（通常是返回）")]
    [SerializeField] Selectable defaultFocus;

    RectTransform rt;
    CanvasGroup cg;
    Vector2 onPos;
    Vector2 offPos;
    bool isOpen;
    Coroutine tweenCo;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        onPos = rt.anchoredPosition;

        var parent = rt.parent as RectTransform;
        float width = parent != null ? parent.rect.width : Screen.width;
        offPos = onPos + new Vector2(width, 0); // 右側畫面外

        SnapHidden();
    }

    /// <summary>開啟設定頁；caller 是「誰開我」（例如 Page_Main 的根物件）。</summary>
    public void Open(GameObject caller = null)
    {
        if (caller != null && UIPageStack.Instance != null)
            UIPageStack.Instance.Push(caller); // 記住從哪來

        if (isOpen) return;
        isOpen = true;

        StopTween();
        gameObject.SetActive(true);
        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.alpha = 1f;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        tweenCo = StartCoroutine(Slide(offPos, onPos, duration, ease, () =>
        {
            if (defaultFocus != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(defaultFocus.gameObject);
        }));
    }

    /// <summary>關閉設定頁；自動回到上一頁（若有）。</summary>
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        StopTween();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        var from = rt.anchoredPosition;
        tweenCo = StartCoroutine(Slide(from, offPos, duration, ease, () =>
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
            cg.alpha = 0f;
            gameObject.SetActive(false);

            // 回上一頁（偏好聚焦順序：Btn_Continue → Btn_Options）
            if (UIPageStack.Instance != null)
                UIPageStack.Instance.PopAndReturn("Btn_Continue", "Btn_Options");
        }));
    }

    void SnapHidden()
    {
        rt.anchoredPosition = offPos;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        gameObject.SetActive(false);
        isOpen = false;
    }

    IEnumerator Slide(Vector2 from, Vector2 to, float t, AnimationCurve curve, System.Action onDone)
    {
        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / t);
            float e = curve.Evaluate(k);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }
        rt.anchoredPosition = to;
        onDone?.Invoke();
        tweenCo = null;
    }

    void StopTween()
    {
        if (tweenCo != null) StopCoroutine(tweenCo);
        tweenCo = null;
    }
}
