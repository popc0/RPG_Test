using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PageMain : MonoBehaviour
{
    [Header("滑入方向 = 從下滑入")]
    [SerializeField] float duration = 0.25f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Header("預設聚焦按鈕")]
    [SerializeField] Selectable defaultFocus;

    RectTransform rt;
    CanvasGroup cg;
    Vector2 onPos;      // 顯示位置
    Vector2 offPos;     // 畫面外位置（下方）
    bool isOpen;
    Coroutine tweenCo;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        onPos = rt.anchoredPosition;

        var parent = rt.parent as RectTransform;
        float height = parent != null ? parent.rect.height : Screen.height;
        offPos = onPos + new Vector2(0, -height); // 從下方滑進

        SnapHidden();
    }

    public void Open()
    {
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

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        StopTween();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        tweenCo = StartCoroutine(Slide(rt.anchoredPosition, offPos, duration, ease, () =>
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
            cg.alpha = 0f;
            gameObject.SetActive(false);
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
            elapsed += Time.unscaledDeltaTime; // 不受暫停影響
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
