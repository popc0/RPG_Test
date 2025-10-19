using System.Collections;
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

    private enum UiGroup { None, IngameMenu, Options }
    private UiGroup current = UiGroup.None;

    // 新增：SystemCanvas 自身的 CanvasGroup
    private CanvasGroup rootCanvasGroup;

    bool pendingNotifyClose = false;

    void Awake()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
        if (!rootCanvasGroup)
        {
            rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("[SCC] Root CanvasGroup added automatically.");
        }

        SetGroupVisible(groupIngameMenu, false);
        SetGroupVisible(groupOptions, false);
        current = UiGroup.None;

        // 開場確保時間為正常
        EnsureGameResumed();

        // 開場預設將 System 根關互動，等真的有頁面開啟再打開
        SetRootActive(false);

        Debug.Log("[SCC] Awake. Groups bound: Ingame=" + (groupIngameMenu != null) + " Options=" + (groupOptions != null));
    }

    void Update()
    {
        if (!enableToggleHotkey) return;
        if (ignoreHotkeyInMainMenu && SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log("[SCC] Toggle hotkey pressed: " + toggleKey);
            ToggleIngameMenu();
        }
    }

    // 對外告知外層目前前景為 system
    void OpenSystemOnTop()
    {
        Debug.Log("[SCC] RaiseOpenCanvas(system)");
        UIEvents.RaiseOpenCanvas("system");
    }

    // 新增：控制 SystemCanvas 根層的互動與能見度
    void SetRootActive(bool active)
    {
        if (!rootCanvasGroup) return;
        rootCanvasGroup.interactable = active;
        rootCanvasGroup.blocksRaycasts = active;
        rootCanvasGroup.alpha = active ? 1f : 0f;
        Debug.Log("[SCC] SetRootActive = " + active);
    }

    void RequestNotifyRootIfNone()
    {
        if (!pendingNotifyClose) StartCoroutine(CoDelayedNotifyNone());
    }

    IEnumerator CoDelayedNotifyNone()
    {
        pendingNotifyClose = true;
        yield return null; // 等一幀讓切換完成（例如 Options → Main）
        if (!HasAnyUiOpen())
        {
            Debug.Log("[SCC] None UI open. RaiseCloseActiveCanvas()");
            UIEvents.RaiseCloseActiveCanvas();
        }
        pendingNotifyClose = false;
    }

    // 群組層索引（先選群組，再開頁）
    void SetGroupIndex(UiGroup target)
    {
        current = target;
        SetGroupVisible(groupIngameMenu, target == UiGroup.IngameMenu);
        SetGroupVisible(groupOptions, target == UiGroup.Options);

        Debug.Log("[SCC] SetGroupIndex -> " + target);
    }

    // 對外入口
    public void ToggleIngameMenu()
    {
        if (!groupIngameMenu || !pageMain) { Debug.LogWarning("[SCC] ToggleIngameMenu missing refs"); return; }

        if (current != UiGroup.IngameMenu)
        {
            OpenSystemOnTop();
            SetRootActive(true);                 // 先打開 System 根
            SetGroupIndex(UiGroup.IngameMenu);
            pageMain.Open();
            Debug.Log("[SCC] Open IngameMenu");
            EnsureGamePaused();
        }
        else
        {
            pageMain.Close();
            SetGroupIndex(UiGroup.None);
            Debug.Log("[SCC] Close IngameMenu");
            EnsureGameResumedIfNone();

            if (!HasAnyUiOpen())
                SetRootActive(false);           // 所有頁面都關才關 System 根

            RequestNotifyRootIfNone();
        }
    }

    public void OpenIngameMenu()
    {
        if (!groupIngameMenu || !pageMain) { Debug.LogWarning("[SCC] OpenIngameMenu missing refs"); return; }

        OpenSystemOnTop();
        SetRootActive(true);                     // 先打開 System 根
        SetGroupIndex(UiGroup.IngameMenu);
        pageMain.Open();
        Debug.Log("[SCC] Open IngameMenu (direct)");
        EnsureGamePaused();
    }

    public void CloseIngameMenu()
    {
        if (!groupIngameMenu || !pageMain) { Debug.LogWarning("[SCC] CloseIngameMenu missing refs"); return; }

        pageMain.Close();
        SetGroupIndex(UiGroup.None);
        Debug.Log("[SCC] Close IngameMenu (direct)");
        EnsureGameResumedIfNone();

        if (!HasAnyUiOpen())
            SetRootActive(false);               // 所有頁面都關才關 System 根

        RequestNotifyRootIfNone();
    }

    public void OpenOptionsFromMainMenu(GameObject callerPage)
    {
        if (!groupOptions || !pageOptions) { Debug.LogWarning("[SCC] OpenOptionsFromMainMenu missing refs"); return; }

        OpenSystemOnTop();
        SetRootActive(true);                     // 先打開 System 根
        SetGroupIndex(UiGroup.Options);
        pageOptions.Open(callerPage);
        Debug.Log("[SCC] Open Options from MainMenu");
        EnsureGamePaused();
    }

    public void OpenOptionsFromIngame(GameObject callerPage)
    {
        if (!groupOptions || !pageOptions) { Debug.LogWarning("[SCC] OpenOptionsFromIngame missing refs"); return; }

        OpenSystemOnTop();
        SetRootActive(true);                     // 先打開 System 根
        SetGroupIndex(UiGroup.Options);
        pageOptions.Open(callerPage);
        Debug.Log("[SCC] Open Options from Ingame");
        EnsureGamePaused();
    }

    // 由 PageOptions 關閉完成後呼叫（Back）
    public void OnOptionsClosed()
    {
        if (pageMain && pageMain.isActiveAndEnabled && pageMain.IsOpen)
        {
            SetGroupIndex(UiGroup.IngameMenu);
            Debug.Log("[SCC] Options closed -> back to IngameMenu");
            // 保持 System 根為開狀態
            SetRootActive(true);
        }
        else
        {
            SetGroupIndex(UiGroup.None);
            Debug.Log("[SCC] Options closed -> none");
            EnsureGameResumedIfNone();

            if (!HasAnyUiOpen())
                SetRootActive(false);           // 沒有任何頁面時關閉 System 根
        }

        RequestNotifyRootIfNone();
    }

    // 判定有沒有 UI 開著（群組或頁面任一成立即可）
    bool HasAnyUiOpen()
    {
        if (current != UiGroup.None) return true;
        if (pageMain && pageMain.isActiveAndEnabled && pageMain.IsOpen) return true;
        if (pageOptions && pageOptions.isActiveAndEnabled && pageOptions.IsOpen) return true;
        return false;
    }

    // CanvasGroup 工具（只用 CG，不關物件）
    static void SetGroupVisible(CanvasGroup cg, bool visible)
    {
        if (!cg) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    // 時間控制
    void EnsureGamePaused() => Time.timeScale = 0f;

    void EnsureGameResumedIfNone()
    {
        if (!HasAnyUiOpen()) Time.timeScale = 1f;
    }

    void EnsureGameResumed() => Time.timeScale = 1f;
}
