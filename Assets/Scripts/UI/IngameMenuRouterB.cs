using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IngameMenuRouterB : MonoBehaviour
{
    [Header("面板（整體上下：Panel_IngameMenu）")]
    public IngameMenuSlide menuSlide;            // 垂直：整個 Ingame 面板上下

    [Header("Options 兩段式（與主選單一致）")]
    public IngameMenuSlide optionsWrapperSlide;  // 垂直：PageOptionsWrapper
    public IngameMenuSlide optionsSlide;         // 水平：Page_Options

    [Header("頁面根物件")]
    public GameObject pageMain;                  // Panel_IngameMenu/Page_Main
    public GameObject pageOptions;               // SystemCanvas/PageOptionsWrapper/Page_Options

    [Header("預設聚焦")]
    public Selectable focusMain;                 // 回主頁聚焦的按鈕
    public Selectable focusOptions;              // 進 Options 聚焦的按鈕

    [Header("鍵位")]
    public KeyCode escKey = KeyCode.Escape;

    [Header("主選單場景名稱（在主選單下停用 ESC）")]
    public string mainMenuSceneName = "MainMenu";

    // 旗標
    private bool pendingReturnToMainAfterClose = false;   // 返回主頁面：先下收 Panel 再回主頁
    private bool pendingGoToMainMenuAfterClose = false;   // 回主選單：先下收 Panel 再存檔切場景

    void Awake()
    {
        if (pageMain) pageMain.SetActive(true);
        if (pageOptions) pageOptions.SetActive(true);

        if (optionsSlide != null) optionsSlide.Close();
        if (optionsWrapperSlide != null) optionsWrapperSlide.Close();

        SetFocus(focusMain);

        if (menuSlide != null)
        {
            menuSlide.Closed.RemoveListener(OnMenuClosed);
            menuSlide.Closed.AddListener(OnMenuClosed);
        }
    }

    void Update()
    {
        // 主選單場景不處理 ESC（避免誤開 Ingame 面板）
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            return;

        if (!Input.GetKeyDown(escKey)) return;

        // 先處理 Options：若開著就先關（先左→再下）
        if (optionsSlide != null && optionsSlide.IsOpen)
        {
            OnClick_OptionsBack();
            return;
        }

        // 否則切換整體面板（Page_Main 所在的 Panel）
        if (menuSlide != null)
            menuSlide.Toggle();
    }

    // ===== Page_Main 內按鈕 =====

    public void OnClick_Continue()
    {
        if (menuSlide != null)
            menuSlide.Close();
    }

    /// <summary>打開 Options（兩段式：先上 Wrapper → 再左 Options）</summary>
    public void OnClick_Options()
    {
        if (optionsWrapperSlide == null || optionsSlide == null)
            return;

        optionsWrapperSlide.Opened.RemoveListener(OpenOptionsHorizontal);
        optionsWrapperSlide.Opened.AddListener(OpenOptionsHorizontal);
        optionsWrapperSlide.Open();
    }

    /// <summary>回主選單：先收面板 → 存檔 → 切場景</summary>
    public void OnClick_BackToMainMenu()
    {
        if (menuSlide == null)
        {
            SaveAndLoadMainMenu();
            return;
        }

        pendingGoToMainMenuAfterClose = true;
        menuSlide.Close();
    }

    /// <summary>
    /// 返回主頁面：先把面板收回去，再回主頁。
    /// 同時強制讓 Page_Options / PageOptionsWrapper 歸位到隱藏狀態。
    /// </summary>
    public void OnClick_ReturnToMainAndHidePanel()
    {
        if (menuSlide == null) return;

        // 先把 Options 兩段式強制歸位，避免面板動畫中看見殘影
        ForceHideOptionsInstant();

        pendingReturnToMainAfterClose = true;
        menuSlide.Close();
    }

    // ===== Page_Options 內按鈕 =====

    /// <summary>關閉 Options（兩段式：先左 Options → 關完後再下 Wrapper）</summary>
    public void OnClick_OptionsBack()
    {
        if (optionsSlide == null || optionsWrapperSlide == null)
            return;

        optionsSlide.Closed.RemoveListener(CloseWrapperAfterOptions);
        optionsSlide.Closed.AddListener(CloseWrapperAfterOptions);
        optionsSlide.Close();

        SetFocus(focusMain);
    }

    // ===== 內部串接 =====

    // 垂直開完後，打開水平（先上→再左）
    void OpenOptionsHorizontal()
    {
        if (optionsWrapperSlide != null)
            optionsWrapperSlide.Opened.RemoveListener(OpenOptionsHorizontal);

        if (pageOptions != null && !pageOptions.activeSelf)
            pageOptions.SetActive(true);

        if (optionsSlide != null)
            optionsSlide.Open();

        SetFocus(focusOptions);
    }

    // 水平關完後，關垂直（先左→再下）
    void CloseWrapperAfterOptions()
    {
        if (optionsSlide != null)
            optionsSlide.Closed.RemoveListener(CloseWrapperAfterOptions);

        if (optionsWrapperSlide != null)
            optionsWrapperSlide.Close();
    }

    // 面板關閉後的統一回呼（處理兩種旗標）
    void OnMenuClosed()
    {
        // 回主選單：關面板 → 存檔 → 切場景
        if (pendingGoToMainMenuAfterClose)
        {
            pendingGoToMainMenuAfterClose = false;
            SaveAndLoadMainMenu();
            return;
        }

        // 返回主頁面：關面板 → 回主頁（並確保 Options 兩段式已完全歸位）
        if (pendingReturnToMainAfterClose)
        {
            pendingReturnToMainAfterClose = false;

            // 無論是否 IsOpen，一律強制回 Hidden，避免殘留位置
            ForceHideOptionsInstant();

            if (pageMain) pageMain.SetActive(true);
            SetFocus(focusMain);
        }
    }

    // 存檔並載入主選單（照你現有 SaveManager 的 API）
    void SaveAndLoadMainMenu()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveNow();

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ===== 小工具 =====

    /// <summary>
    /// 直接把 Page_Options（水平）與 PageOptionsWrapper（垂直）「瞬間」收回到 Hidden。
    /// 用於返回主頁或切面板前的狀態清理，避免下一次開啟位置錯亂或殘影。
    /// </summary>
    void ForceHideOptionsInstant()
    {
        if (optionsSlide != null)
            optionsSlide.SnapClosed();          // 水平：瞬間回 Hidden

        if (optionsWrapperSlide != null)
            optionsWrapperSlide.SnapClosed();   // 垂直：瞬間回 Hidden
    }

    void SetFocus(Selectable s)
    {
        if (s == null || EventSystem.current == null) return;
        if (!s.gameObject.activeInHierarchy) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}
