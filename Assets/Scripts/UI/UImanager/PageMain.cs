using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PageMain : MonoBehaviour
{
    [Header("入場出場動畫")]
    [SerializeField] float duration = 0.25f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("預設聚焦按鈕")]
    [SerializeField] Selectable defaultFocus;

    [Header("主選單場景名稱")]
    [SerializeField] string mainMenuSceneName = "MainMenuScene";

    RectTransform rt;
    CanvasGroup cg;
    Coroutine tweenCo;
    Vector2 startPosHidden;
    Vector2 startPosShown;
    bool isOpen;

    SystemCanvasController scc;

    public bool IsOpen => isOpen;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        startPosShown = Vector2.zero;
        startPosHidden = new Vector2(0, -rt.rect.height); // 從下滑入

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        rt.anchoredPosition = startPosHidden;

        TryFindSCC();
    }

    void StopTween()
    {
        if (tweenCo != null) StopCoroutine(tweenCo);
        tweenCo = null;
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        StopTween();

        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(SetFocusNextFrame());
        }

        tweenCo = StartCoroutine(TweenPos(rt.anchoredPosition, startPosShown, true));
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        StopTween();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        tweenCo = StartCoroutine(TweenPos(rt.anchoredPosition, startPosHidden, false));
    }

    IEnumerator TweenPos(Vector2 from, Vector2 to, bool opening)
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

        if (!opening)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        tweenCo = null;
    }

    IEnumerator SetFocusNextFrame()
    {
        yield return null;
        if (defaultFocus != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(defaultFocus.gameObject);
    }

    void TryFindSCC()
    {
        if (scc != null) return;
        scc = FindObjectOfType<SystemCanvasController>();
        if (scc == null)
            Debug.LogWarning("[PageMain] 找不到 SystemCanvasController。請確認它掛在 SystemCanvas。");
    }

    // === 按鈕事件 ===

    public void OnClick_Continue()
    {
        TryFindSCC();
        if (scc != null) scc.CloseIngameMenu();
    }

    public void OnClick_Options()
    {
        TryFindSCC();
        if (scc == null) return;

        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            scc.OpenOptionsFromMainMenu(gameObject);
        else
            scc.OpenOptionsFromIngame(gameObject);
    }

    public void OnClick_BackToMainMenu()
    {
        TryFindSCC();
        if (scc != null) scc.CloseIngameMenu();

        // 恢復時間流動
        Time.timeScale = 1f;

        // 播放淡出與存檔流程
        StartCoroutine(ReturnToMainMenuAfterFade());
    }

    IEnumerator ReturnToMainMenuAfterFade()
    {
        // 模擬淡出動畫（可替換成你的實際黑幕控制）
        yield return new WaitForSecondsRealtime(0.5f);

        // 存檔（安全略過主選單）
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveNow();

        // 確保時間正常
        Time.timeScale = 1f;

        // 再等一點給檔案寫入時間
        yield return new WaitForSecondsRealtime(0.1f);

        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }
}
