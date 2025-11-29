using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    //  新增：直接引用場景中已經存在的按鈕列表
    [Header("子頁面導航按鈕")]
    [Tooltip("將場景中已排版好的導航按鈕拖曳到此清單")]
    public List<Button> navButtons = new List<Button>();

    [Tooltip("容納所有子頁面內容的父物件")]
    [SerializeField] private RectTransform pageContentContainer;


    [System.Serializable]
    public class NavigablePage
    {
        [Tooltip("請將場景中已存在的子頁面 GameObject 拖曳到此處")]
        public GameObject pageInstance;

        [HideInInspector] public CanvasGroup pageCanvasGroup; // 運行時存儲 CanvasGroup
        [HideInInspector] public PageButtonVisuals buttonVisuals; // <-- 新增欄位
    }
    [Tooltip("所有可切換的子頁面清單")]
    public List<NavigablePage> navigablePages = new List<NavigablePage>();

    private int currentPageIndex = -1; // 當前啟用的頁面索引

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

        if (pageContentContainer == null)
            pageContentContainer = transform.Find("PageContentContainer") as RectTransform;

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

        //  步驟 1: 初始化導航結構（只執行一次）
        if (currentPageIndex == -1)
        {
            InitializeNavigation();
        }
        //  NEW: 載入上次的頁面索引
        // 我們假設 SaveManager.CurrentData 已經由 LoadNow/Awake 載入
        int lastIndex = SaveManager.CurrentData.pageMainLastPageIndex;

        // 確保載入的索引在有效範圍內 (防止遊戲更新後索引無效)
        if (lastIndex < 0 || lastIndex >= navigablePages.Count)
        {
            lastIndex = 0; // 預設為第一頁
        }

        // 修正 A: 強制將 currentPageIndex 設為一個無效值
        currentPageIndex = -1;

        // 修正 B: 呼叫 SwitchPage(lastIndex)
        SwitchPage(lastIndex); // <-- 呼叫上次的頁面索引

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
        SaveAndClose(); // 執行存檔和 PageMain.Close() 動畫

        TryFindSCC();
        if (scc != null) scc.CloseIngameMenu();// 通知 SystemCanvasController 關閉
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
        //  步驟 1: 存檔並執行 PageMain 關閉動畫
        SaveAndClose();

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

    // PageMain.cs (新增方法)

    // PageMain.cs (修改 InitializeNavigation 方法)
    void InitializeNavigation()
    {
        if (pageContentContainer == null)
        {
            Debug.LogError("[PageMain] 頁面內容容器遺失，無法初始化導航！");
            return;
        }

        // 檢查按鈕數量和頁面數量是否匹配 (避免出錯)
        if (navButtons.Count != navigablePages.Count)
        {
            Debug.LogError("[PageMain] 導航按鈕數量與子頁面數量不匹配！請檢查 Inspector。");
            return;
        }

        // 實例化所有子頁面，並為每個按鈕綁定切換功能
        for (int i = 0; i < navigablePages.Count; i++)
        {
            var pageData = navigablePages[i];
            var button = navButtons[i];

            //  新增：獲取按鈕上的視覺腳本
            pageData.buttonVisuals = button.GetComponent<PageButtonVisuals>();
            // 1. 實例化子頁面 (Instance)
            GameObject pageInstance = pageData.pageInstance; // 直接使用已連線的實例

            if (pageInstance == null)
            {
                Debug.LogError($"[PageMain] 索引 {i} 的子頁面實例 (Page Instance) 欄位為空，無法初始化。");
                continue;
            }

            // 獲取並儲存 CanvasGroup
            pageData.pageCanvasGroup = pageInstance.GetComponent<CanvasGroup>();
            if (pageData.pageCanvasGroup == null)
            {
                pageData.pageCanvasGroup = pageInstance.AddComponent<CanvasGroup>();
            }

            // 2.  綁定點擊事件 (這是核心！)
            int index = i;
            // 確保先清除舊的 Listener (防止重複綁定)
            button.onClick.RemoveAllListeners();
            // 綁定 SwitchPage 方法，並傳遞當前迴圈的索引
            button.onClick.AddListener(() => SwitchPage(index));

            // 3.預設關閉所有頁面
            SetPageActive(pageData, false);
        }
    }

    public void SwitchPage(int index)
    {
        // 檢查索引是否有效
        if (index < 0 || index >= navigablePages.Count) return;

        //  修正：如果切換到相同頁面，只執行視覺更新並返回
        if (index == currentPageIndex)
        {
            // 確保視覺被設為選中狀態
            navigablePages[index].buttonVisuals?.SetSelected(true);
            // 確保頁面是啟用的 (防止被意外禁用)
            SetPageActive(navigablePages[index], true);
            return;
        }

        // 1. 互斥控制：關閉舊頁面
        // 只有當 currentPageIndex >= 0 時才執行關閉邏輯
        if (currentPageIndex >= 0 && currentPageIndex < navigablePages.Count)
        {
            SetPageActive(navigablePages[currentPageIndex], false);

            // 舊按鈕：設為未選中
            navigablePages[currentPageIndex].buttonVisuals?.SetSelected(false);
        }

        // 2. 啟用新頁面
        SetPageActive(navigablePages[index], true);
        currentPageIndex = index;
        navigablePages[index].buttonVisuals?.SetSelected(true);        // 新按鈕：設為選中

        // 🚨 NEW: 核心：將當前頁面索引寫入 SaveManager 的靜態資料
        SaveManager.CurrentData.pageMainLastPageIndex = index;
    }

    // 輔助方法：使用 CanvasGroup 實現互斥效果
    void SetPageActive(NavigablePage pageData, bool active)
    {
        if (pageData.pageCanvasGroup == null) return;

        // 讓頁面在視覺上消失，且不能互動
        pageData.pageCanvasGroup.alpha = active ? 1f : 0f;
        pageData.pageCanvasGroup.interactable = active;
        pageData.pageCanvasGroup.blocksRaycasts = active;

        // 2.  新增：控制 GameObject 的啟用狀態 (節省 CPU 資源)
        // 只有當需要改變狀態時才呼叫 SetActive，避免重複呼叫
        if (pageData.pageInstance.activeSelf != active)
        {
            pageData.pageInstance.SetActive(active);
        }
    }
    public void SaveAndClose()
    {
        // 確保最新的頁面索引已經寫入 SaveManager.CurrentData (由 SwitchPage 完成)

        // 呼叫 SaveManager 寫入檔案（包含最新頁面索引和玩家狀態）
        SaveManager.Instance?.SaveNow();

        // 執行 PageMain 的關閉動畫和清理
        Close();
    }
}
