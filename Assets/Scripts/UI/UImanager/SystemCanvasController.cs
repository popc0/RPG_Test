using UnityEngine;
using UnityEngine.SceneManagement;

public class SystemCanvasController : MonoBehaviour
{
    [Header("Groups in SystemCanvas")]
    [SerializeField] private CanvasGroup groupIngameMenu;
    [SerializeField] private CanvasGroup groupOptions;

    [Header("Pages")]
    [SerializeField] private PageMain pageMain;
    [SerializeField] private PageOptions pageOptions;

    [Header("Hotkey")]
    [SerializeField] private bool enableToggleHotkey = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.R;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private bool ignoreHotkeyInMainMenu = true;

    [Header("Auto bind on Awake")]
    [SerializeField] private bool autoFindOnAwake = true;

    private enum UiGroup { None, IngameMenu, Options }
    private UiGroup current = UiGroup.None;
    private CanvasGroup rootCanvasGroup;

    void Awake()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
        if (!rootCanvasGroup)
            rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (autoFindOnAwake) AutoBindIfMissing();

        SetGroupVisible(groupIngameMenu, false);
        SetGroupVisible(groupOptions, false);
        current = UiGroup.None;

        EnsureGameResumed();
        SetRootActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;
        if (!ShouldHandleHotkey(out _)) return;
        ToggleIngameMenu();
    }

    bool ShouldHandleHotkey(out string whyNot)
    {
        var scene = SceneManager.GetActiveScene().name;

        if (!enableToggleHotkey) { whyNot = "disable"; return false; }
        if (!isActiveAndEnabled) { whyNot = "disabled"; return false; }
        if (!gameObject.activeInHierarchy) { whyNot = "inactive"; return false; }
        if (ignoreHotkeyInMainMenu && scene == mainMenuSceneName) { whyNot = "mainmenu"; return false; }
        if (!groupIngameMenu || !pageMain) { whyNot = "missing"; return false; }

        // 新增：Options 開啟時，忽略 R，避免把 Options 留在未正確關閉的狀態就切回主頁
        if (pageOptions && pageOptions.isActiveAndEnabled && pageOptions.IsOpen)
        {
            whyNot = "optionsOpen";
            return false;
        }

        whyNot = null;
        return true;
    }

    void OpenSystemOnTop() => UIEvents.RaiseOpenCanvas("system");

    void SetRootActive(bool active)
    {
        if (!rootCanvasGroup) return;
        rootCanvasGroup.interactable = active;
        rootCanvasGroup.blocksRaycasts = active;
        rootCanvasGroup.alpha = active ? 1f : 0f;
    }

    void NotifyMuxNone()
    {
        var mux = FindObjectOfType<SystemLayerMux>();
        if (mux) mux.NotifyChildClosed();
    }

    void SetGroupIndex(UiGroup target)
    {
        current = target;
        SetGroupVisible(groupIngameMenu, target == UiGroup.IngameMenu);
        SetGroupVisible(groupOptions, target == UiGroup.Options);
    }

    public void ToggleIngameMenu()
    {
        if (!groupIngameMenu || !pageMain) return;

        if (current != UiGroup.IngameMenu)
        {
            var mux = FindObjectOfType<SystemLayerMux>();
            if (mux) mux.OpenSystem();

            OpenSystemOnTop();
            SetRootActive(true);
            SetGroupIndex(UiGroup.IngameMenu);
            pageMain.Open();
            EnsureGamePaused();
        }
        else
        {
            pageMain.Close();
            SetGroupIndex(UiGroup.None);
            EnsureGameResumedIfNone();
            if (!HasAnyUiOpen()) SetRootActive(false);
            NotifyMuxNone();
        }
    }

    public void OpenIngameMenu()
    {
        if (!groupIngameMenu || !pageMain) return;

        var mux = FindObjectOfType<SystemLayerMux>();
        if (mux) mux.OpenSystem();

        OpenSystemOnTop();
        SetRootActive(true);
        SetGroupIndex(UiGroup.IngameMenu);
        pageMain.Open();
        EnsureGamePaused();
    }

    public void CloseIngameMenu()
    {
        if (!groupIngameMenu || !pageMain) return;

        pageMain.Close();
        SetGroupIndex(UiGroup.None);
        EnsureGameResumedIfNone();
        if (!HasAnyUiOpen()) SetRootActive(false);
        NotifyMuxNone();
    }

    public void OpenOptionsFromMainMenu(GameObject callerPage)
    {
        if (!groupOptions || !pageOptions) return;

        var mux = FindObjectOfType<SystemLayerMux>();
        if (mux) mux.OpenSystem();

        OpenSystemOnTop();
        SetRootActive(true);
        SetGroupIndex(UiGroup.Options);
        pageOptions.Open(callerPage);
        EnsureGamePaused();
    }

    public void OpenOptionsFromIngame(GameObject callerPage)
    {
        if (!groupOptions || !pageOptions) return;

        var mux = FindObjectOfType<SystemLayerMux>();
        if (mux) mux.OpenSystem();

        OpenSystemOnTop();
        SetRootActive(true);
        SetGroupIndex(UiGroup.Options);
        pageOptions.Open(callerPage);
        EnsureGamePaused();
    }

    public void OnOptionsClosed()
    {
        if (pageMain && pageMain.isActiveAndEnabled && pageMain.IsOpen)
        {
            SetGroupIndex(UiGroup.IngameMenu);
            SetRootActive(true);
        }
        else
        {
            SetGroupIndex(UiGroup.None);
            EnsureGameResumedIfNone();
            if (!HasAnyUiOpen()) SetRootActive(false);
            NotifyMuxNone();
        }
    }

    bool HasAnyUiOpen()
    {
        if (current != UiGroup.None) return true;
        if (pageMain && pageMain.isActiveAndEnabled && pageMain.IsOpen) return true;
        if (pageOptions && pageOptions.isActiveAndEnabled && pageOptions.IsOpen) return true;
        return false;
    }

    static void SetGroupVisible(CanvasGroup cg, bool visible)
    {
        if (!cg) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    void EnsureGamePaused() => Time.timeScale = 0f;
    void EnsureGameResumedIfNone() { if (!HasAnyUiOpen()) Time.timeScale = 1f; }
    void EnsureGameResumed() => Time.timeScale = 1f;

    void AutoBindIfMissing()
    {
        if (!groupIngameMenu)
        {
            var t = transform.Find("Group_IngameMenu");
            if (t) groupIngameMenu = t.GetComponent<CanvasGroup>();
        }
        if (!groupOptions)
        {
            var t = transform.Find("Group_Options");
            if (t) groupOptions = t.GetComponent<CanvasGroup>();
        }
        if (!pageMain && groupIngameMenu)
        {
            var t = groupIngameMenu.transform.Find("Page_Main");
            if (t) pageMain = t.GetComponent<PageMain>();
        }
        if (!pageOptions && groupOptions)
        {
            var t = groupOptions.transform.Find("Page_Options");
            if (t) pageOptions = t.GetComponent<PageOptions>();
        }
    }
}
