using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // ★ 新增 Input System

public class SystemCanvasController : MonoBehaviour
{
    [Header("Groups in SystemCanvas")]
    [SerializeField] private CanvasGroup groupIngameMenu;
    [SerializeField] private CanvasGroup groupOptions;

    [Header("Pages")]
    [SerializeField] private PageMain pageMain;
    [SerializeField] private PageOptions pageOptions;

    [Header("Hotkey (Input System)")]
    [SerializeField] private bool enableToggleHotkey = true;

    // ★ 用 Input Action 來當 R 鍵（KEYR），在 Inspector 指到你的「KEYR」動作
    [SerializeField] private InputActionReference toggleMenuAction;

    // ★ 備用：如果沒有設定 InputAction，就用鍵盤 R 當後備
    [SerializeField] private Key fallbackKey = Key.R;

    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private bool ignoreHotkeyInMainMenu = true;

    [Header("Auto bind on Awake")]
    [SerializeField] private bool autoFindOnAwake = true;

    private PlayerPauseAgent pauseAgent;
    private GameObject player;

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
    // ★ 啟用時順便 Enable InputAction
    void OnEnable()
    {
        if (toggleMenuAction != null)
        {
            try { toggleMenuAction.action.Enable(); }
            catch { /* 避免編輯器 domain reload 時噴例外 */ }
        }
    }

    void OnDisable()
    {
        if (toggleMenuAction != null)
        {
            try { toggleMenuAction.action.Disable(); }
            catch { }
        }
    }

    void Update()
    {
        if (!enableToggleHotkey) return;

        bool pressed = false;

        // 1️⃣ 優先用 Input Action (KEYR)
        if (toggleMenuAction != null)
        {
            var act = toggleMenuAction.action;
            if (act != null)
            {
                if (!act.enabled)
                    act.Enable();

                if (act.WasPressedThisFrame())
                    pressed = true;
            }
        }
        // 2️⃣ 沒有配 Action 的話，退回用鍵盤 R
        else
        {
            var kb = Keyboard.current;
            if (kb != null && kb[fallbackKey].wasPressedThisFrame)
                pressed = true;
        }

        if (pressed && ShouldHandleHotkey(out _))
            ToggleIngameMenu();
    }

    bool ShouldHandleHotkey(out string whyNot)
    {
        var scene = SceneManager.GetActiveScene().name;

        if (!isActiveAndEnabled) { whyNot = "disabled"; return false; }
        if (!gameObject.activeInHierarchy) { whyNot = "inactive"; return false; }
        if (ignoreHotkeyInMainMenu && scene == mainMenuSceneName) { whyNot = "mainmenu"; return false; }
        if (!groupIngameMenu || !pageMain) { whyNot = "missing"; return false; }

        // Options 開著時暫時不處理 R
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
            EnsureGamePaused();
            pageMain.Open();
        }
        else
        {
            if (pageMain)
            {
                pageMain.SaveAndClose(); // 執行存檔和 PageMain 的關閉動畫
            }

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
            EnsureGameResumed();
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

    void EnsureGamePaused()
    {

        // 自動找目前場上的玩家（跨場景安全）
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found;
        }

        if (player != null)
            pauseAgent = player.GetComponent<PlayerPauseAgent>();

        if (pauseAgent != null)
            pauseAgent.Pause();
        // 2. 暫停遊戲時間
        Time.timeScale = 0f;
    }
    void EnsureGameResumedIfNone()
    {
        if (!HasAnyUiOpen())
        {
            // 1. 恢復遊戲時間
            Time.timeScale = 1f;
            if (pauseAgent != null)
                pauseAgent.Resume();
        }
    }
    void EnsureGameResumed()
    {
        Time.timeScale = 1f;
        if (pauseAgent != null)
            pauseAgent.Resume();
    }

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
