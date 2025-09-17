using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IngameMenuRouterB : MonoBehaviour
{
    [Header("Slides")]
    public IngameMenuSlide menuSlide;    // Panel_IngameMenu（垂直上下）
    public IngameMenuSlide optionsSlide; // Page_Options（水平左右）

    [Header("Pages")]
    public GameObject pageMain;          // Page_Main
    public GameObject pageOptions;       // Page_Options（內容容器）

    [Header("Focus (Optional)")]
    public Selectable focusMain;         // 主頁預設聚焦（例如 繼續）
    public Selectable focusOptions;      // 選項頁預設聚焦（例如 返回）

    [Header("（可選）主頁互動控制")]
    [Tooltip("如果指定，開啟 Page_Options 時會暫時停用主頁的互動/射線，關閉後再恢復")]
    public CanvasGroup mainPageGroup;    // 指到 Page_Main 的根或其父容器上的 CanvasGroup

    [Header("ESC")]
    public KeyCode escKey = KeyCode.Escape;

    [Header("Main Menu Scene Name")]
    public string mainMenuSceneName = "MainMenu";

    // 旗標：用來等 Panel 關閉後再回主頁
    private bool pendingReturnToMainAfterClose = false;

    void Awake()
    {
        if (pageMain) pageMain.SetActive(true);
        if (pageOptions) pageOptions.SetActive(true); // slide 需要啟用才會動

        if (optionsSlide != null) optionsSlide.Close();
        SetFocus(focusMain);

        // 監聽 Panel 關閉事件，用於「先下收，再回主頁」
        if (menuSlide != null)
        {
            menuSlide.Closed.RemoveListener(OnMenuClosedThenReturnMain);
            menuSlide.Closed.AddListener(OnMenuClosedThenReturnMain);
        }

        //  自動綁定 Page_Main/Btn_Options → OnClick_Options（避免忘了接線）
        AutoBindMainOptionsButton();
    }

    void Update()
    {
        // 主選單場景：停用 ESC 控制 Panel_IngameMenu
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            return;

        if (Input.GetKeyDown(escKey))
        {
            // 先關選項子面板；否則才切 Panel
            if (optionsSlide != null && optionsSlide.IsOpen)
            {
                CloseOptions();
                return;
            }
            if (menuSlide != null) menuSlide.Toggle();
        }
    }

    // ===== 主頁按鈕 =====
    public void OnClick_Continue()
    {
        if (menuSlide) menuSlide.Close();
    }

    public void OnClick_Options()
    {
        // 從主頁點進來時，保險起見：確保整個面板是打開的（上升狀態）
        if (menuSlide != null && !menuSlide.IsOpen)
            menuSlide.Open();

        OpenOptions();
    }

    public void OnClick_BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // 你要的：「返回主頁面」＝ 先把 Panel 下收，再回主頁（關子頁）
    public void OnClick_ReturnToMainAndHidePanel()
    {
        if (menuSlide == null) return;

        // 設旗標：等 Panel 關閉動畫完成再回主頁
        pendingReturnToMainAfterClose = true;
        menuSlide.Close();
    }

    // ===== 選項頁按鈕 =====
    public void OnClick_OptionsBack()
    {
        CloseOptions();
    }

    // ===== 內部 =====
    void OpenOptions()
    {
        // Page_Main 維持啟用以便 CanvasGroup 控制；互動由 mainPageGroup 來禁用/恢復
        if (pageMain) pageMain.SetActive(true);

        // 強制啟用 Page_Options 並送到最上層，避免被擋住或尚未啟用
        if (pageOptions != null)
        {
            if (!pageOptions.activeSelf) pageOptions.SetActive(true);
            pageOptions.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning("[IngameMenuRouterB] pageOptions root is null.");
        }

        // 暫時關閉主頁互動（避免底層被點到）
        SetMainPageRaycast(false);

        if (optionsSlide != null)
            optionsSlide.Open();
        else
            Debug.LogWarning("[IngameMenuRouterB] optionsSlide is null.");

        SetFocus(focusOptions);
    }

    void CloseOptions()
    {
        if (optionsSlide != null)
        {
            // 等關閉動畫結束後再恢復主頁互動，避免邊關邊點擊
            optionsSlide.Closed.RemoveListener(RestoreMainPageRaycast);
            optionsSlide.Closed.AddListener(RestoreMainPageRaycast);
            optionsSlide.Close();
        }
        else
        {
            RestoreMainPageRaycast();
        }

        SetFocus(focusMain);
    }

    void RestoreMainPageRaycast()
    {
        SetMainPageRaycast(true);

        if (optionsSlide != null)
            optionsSlide.Closed.RemoveListener(RestoreMainPageRaycast);
    }

    void SetMainPageRaycast(bool on)
    {
        if (mainPageGroup == null) return;
        mainPageGroup.blocksRaycasts = on;
        mainPageGroup.interactable = on;
    }

    void SetFocus(Selectable s)
    {
        if (s == null || EventSystem.current == null) return;
        if (!s.gameObject.activeInHierarchy) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    // —— Panel 關閉後才回主頁（對應 OnClick_ReturnToMainAndHidePanel）——
    void OnMenuClosedThenReturnMain()
    {
        if (!pendingReturnToMainAfterClose) return;
        pendingReturnToMainAfterClose = false;

        // 確保關閉子頁、回主頁、設焦點
        if (optionsSlide && optionsSlide.IsOpen) optionsSlide.Close();
        if (pageMain) pageMain.SetActive(true);
        SetFocus(focusMain);

        // 關面板後恢復主頁互動（以防面板開啟時關掉過）
        RestoreMainPageRaycast();
    }

    // ------- 自動綁定主頁的 Btn_Options -------
    void AutoBindMainOptionsButton()
    {
        if (pageMain == null) return;

        // 先直接找同名物件
        Transform t = pageMain.transform.Find("Btn_Options");
        if (t == null)
        {
            // 找不到就遍歷搜尋第一個 Button 名稱包含 "Options"
            foreach (var btn in pageMain.GetComponentsInChildren<Button>(true))
            {
                if (btn.name.Contains("Options"))
                {
                    t = btn.transform;
                    break;
                }
            }
        }
        if (t == null) return;

        var button = t.GetComponent<Button>();
        if (button == null) return;

        // 檢查是否已經綁過 OnClick_Options，避免重複
        bool alreadyBound = false;
        foreach (var ev in button.onClick.GetPersistentEventCount() > 0 ? null : new object[0]) { } // 占位，以下用簡單方式處理
        // 最保險作法：先移除再加一次（避免場景反覆進出造成重複）
        button.onClick.RemoveListener(OnClick_Options);
        button.onClick.AddListener(OnClick_Options);
    }
}
