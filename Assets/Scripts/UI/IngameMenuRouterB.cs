using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

public class IngameMenuRouterB : MonoBehaviour
{
    [Header("面板（整體上下：Panel_IngameMenu）")]
    public IngameMenuSlide menuSlide;

    [Header("Options 兩段式（與主選單一致）")]
    public IngameMenuSlide optionsWrapperSlide;  // 垂直：PageOptionsWrapper
    public IngameMenuSlide optionsSlide;         // 水平：Page_Options

    [Header("頁面根物件")]
    public GameObject pageMain;                  // Panel_IngameMenu/Page_Main
    public GameObject pageOptions;               // SystemCanvas/PageOptionsWrapper/Page_Options

    [Header("預設聚焦")]
    public Selectable focusMain;                 // 回主頁聚焦的按鈕
    public Selectable focusOptions;              // 進 Options 聚焦的按鈕

    [Header("切換鍵（原 ESC，改為 R）")]
    public KeyCode toggleKey = KeyCode.R;

    [Header("主選單場景名稱（在主選單下停用 R 切換）")]
    public string mainMenuSceneName = "MainMenuScene";

    [Header("Page_Main 的按鈕（把對應按鈕拖進來）")]
    public Button btnMain_Continue;
    public Button btnMain_Options;
    public Button btnMain_BackToMainMenu;

    [Header("Page_Options 的按鈕（把對應按鈕拖進來）")]
    public Button btnOptions_Back;

    private bool pendingReturnToMainAfterClose = false;
    private bool pendingGoToMainMenuAfterClose = false;
    private string currentSceneName;

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void Start()
    {
        // 起始不主動切狀態；由 OnSceneLoaded（載入完成）切一次
        if (pageMain) pageMain.SetActive(true);
        if (pageOptions) pageOptions.SetActive(true);

        if (optionsSlide != null) optionsSlide.Close();
        if (optionsWrapperSlide != null) optionsWrapperSlide.Close();

        BindButtons();
        FocusNowAndNext(focusMain);

        if (menuSlide != null)
        {
            menuSlide.Closed.RemoveListener(OnMenuClosed);
            menuSlide.Closed.AddListener(OnMenuClosed);
        }
    }

    void Update()
    {
        // 主選單場景不處理 R（避免誤開 Ingame 面板）
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        if (!Input.GetKeyDown(toggleKey)) return;

        // 若 Options 開著，先關（先左→再下）
        if (optionsSlide != null && optionsSlide.IsOpen)
        {
            OnClick_OptionsBack();
            return;
        }

        // 切換整體 Ingame 面板
        if (menuSlide != null)
        {
            menuSlide.Toggle();
            if (UIStateManager.Instance != null)
            {
                UIStateManager.Instance.SwitchUI(menuSlide.IsOpen ? UIState.IngameMenu : UIState.HUD);
            }
            FocusNowAndNext(menuSlide.IsOpen ? focusMain : null);
        }
    }

    // 場景切換完成後，僅此時切一次狀態
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        if (UIStateManager.Instance == null) return;

        if (scene.name == mainMenuSceneName) UIStateManager.Instance.SwitchUI(UIState.MainMenu);
        else UIStateManager.Instance.SwitchUI(UIState.HUD);

        // 切完狀態補一次聚焦（下一幀）
        FocusNowAndNext(focusMain);
    }

    // ===== Page_Main 內按鈕 =====

    public void OnClick_Continue()
    {
        if (menuSlide != null)
        {
            menuSlide.Close();
            if (UIStateManager.Instance != null)
                UIStateManager.Instance.SwitchUI(UIState.HUD);
            FocusNowAndNext(null); // 回遊戲不一定需要 UI 焦點
        }
    }

    public void OnClick_Options()
    {
        if (optionsWrapperSlide == null || optionsSlide == null) return;

        optionsWrapperSlide.Opened.RemoveListener(OpenOptionsHorizontal);
        optionsWrapperSlide.Opened.AddListener(OpenOptionsHorizontal);
        optionsWrapperSlide.Open();

        if (UIStateManager.Instance != null)
            UIStateManager.Instance.SwitchUI(UIState.Options);

        FocusNowAndNext(focusOptions);
    }

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

    // ===== Page_Options 內按鈕 =====

    public void OnClick_OptionsBack()
    {
        if (optionsSlide == null || optionsWrapperSlide == null) return;

        optionsSlide.Closed.RemoveListener(CloseWrapperAfterOptions);
        optionsSlide.Closed.AddListener(CloseWrapperAfterOptions);
        optionsSlide.Close();

        bool isMainMenu = SceneManager.GetActiveScene().name == mainMenuSceneName;

        if (UIStateManager.Instance != null)
            UIStateManager.Instance.SwitchUI(isMainMenu ? UIState.MainMenu : UIState.IngameMenu);

        // 主選單/遊戲 皆回到 Page_Main 的預設聚焦，避免 Enter 打到舊目標
        FocusNowAndNext(focusMain);
    }

    // ===== 內部串接 =====

    void OpenOptionsHorizontal()
    {
        if (optionsWrapperSlide != null)
            optionsWrapperSlide.Opened.RemoveListener(OpenOptionsHorizontal);

        if (pageOptions != null && !pageOptions.activeSelf)
            pageOptions.SetActive(true);

        if (optionsSlide != null)
            optionsSlide.Open();

        if (UIStateManager.Instance != null)
            UIStateManager.Instance.SwitchUI(UIState.Options);

        FocusNowAndNext(focusOptions);
    }

    void CloseWrapperAfterOptions()
    {
        if (optionsSlide != null)
            optionsSlide.Closed.RemoveListener(CloseWrapperAfterOptions);

        // 只在主選單才收 Wrapper；遊戲內不要動 Wrapper（避免把 Page_Main 也收掉）
        bool isMainMenu = SceneManager.GetActiveScene().name == mainMenuSceneName;
        if (isMainMenu && optionsWrapperSlide != null)
            optionsWrapperSlide.Close();
    }

    void OnMenuClosed()
    {
        if (pendingGoToMainMenuAfterClose)
        {
            pendingGoToMainMenuAfterClose = false;
            SaveAndLoadMainMenu(); // 切場景；狀態等載入完成後再切
            return;
        }

        if (pendingReturnToMainAfterClose)
        {
            pendingReturnToMainAfterClose = false;
            ForceHideOptionsInstant();
            if (pageMain) pageMain.SetActive(true);
            if (UIStateManager.Instance != null)
                UIStateManager.Instance.SwitchUI(UIState.HUD);
        }
    }

    void SaveAndLoadMainMenu()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveNow();

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName); // 切場景；狀態在 OnSceneLoaded 切一次
    }

    void ForceHideOptionsInstant()
    {
        if (optionsSlide != null) optionsSlide.SnapClosed();
        if (optionsWrapperSlide != null) optionsWrapperSlide.SnapClosed();
    }

    // —— 聚焦工具（先即時、下一幀再補一次，避免被動畫/滑鼠覆蓋）——
    private Coroutine _coFocus;
    private void FocusNowAndNext(Selectable s)
    {
        if (s == null || EventSystem.current == null) return;
        if (!s.gameObject.activeInHierarchy) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);

        if (_coFocus != null) StopCoroutine(_coFocus);
        _coFocus = StartCoroutine(CoFocusNextFrame(s));
    }
    private System.Collections.IEnumerator CoFocusNextFrame(Selectable s)
    {
        yield return null;
        if (EventSystem.current == null || s == null) yield break;
        if (!s.gameObject.activeInHierarchy) yield break;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    void BindButtons()
    {
        Bind(btnMain_Continue, OnClick_Continue);
        Bind(btnMain_Options, OnClick_Options);
        Bind(btnMain_BackToMainMenu, OnClick_BackToMainMenu);
        Bind(btnOptions_Back, OnClick_OptionsBack);
    }

    void Bind(Button b, UnityAction action)
    {
        if (b == null || action == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(action);
    }
}
