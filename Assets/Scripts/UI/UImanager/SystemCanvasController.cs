using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool ignoreHotkeyInMainMenu = true;

    private enum UiGroup { None, IngameMenu, Options }
    private UiGroup current = UiGroup.None;

    bool pendingNotifyClose = false;

    void Awake()
    {
        // 初始全部關（CanvasGroup 控制）
        SetGroupVisible(groupIngameMenu, false);
        SetGroupVisible(groupOptions, false);
        current = UiGroup.None;
        EnsureGameResumed();
    }

    void Update()
    {
        if (!enableToggleHotkey) return;
        if (ignoreHotkeyInMainMenu && SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        if (Input.GetKeyDown(toggleKey)) ToggleIngameMenu();
    }

    // ===== 外層 Canvas 事件 =====
    void EnsureSystemCanvasActive() => UIEvents.RaiseOpenSystemCanvas();
    void RequestNotifyRootIfNone()
    {
        if (!pendingNotifyClose) StartCoroutine(CoDelayedNotifyNone());
    }
    IEnumerator CoDelayedNotifyNone()
    {
        pendingNotifyClose = true;
        yield return null; // 等一幀讓切換完成（例如 Options→Main）
        if (!HasAnyUiOpen()) UIEvents.RaiseCloseActiveCanvas();
        pendingNotifyClose = false;
    }

    // ===== 群組層索引（先選群組，再開頁） =====
    void SetGroupIndex(UiGroup target)
    {
        current = target;

        // 只有索引指向的群組可見且可互動；其他關閉可見與互動
        SetGroupVisible(groupIngameMenu, target == UiGroup.IngameMenu);
        SetGroupVisible(groupOptions, target == UiGroup.Options);
    }

    // ===== 對外入口 =====
    public void ToggleIngameMenu()
    {
        if (!groupIngameMenu || !pageMain) return;

        if (current != UiGroup.IngameMenu)
        {
            EnsureSystemCanvasActive();
            SetGroupIndex(UiGroup.IngameMenu); // 先群組索引→可見可互動
            pageMain.Open();                   // 再頁面
            EnsureGamePaused();
        }
        else
        {
            pageMain.Close();                  // 先頁面關
            SetGroupIndex(UiGroup.None);       // 再群組索引回 None
            EnsureGameResumedIfNone();
            RequestNotifyRootIfNone();
        }
    }

    public void OpenIngameMenu()
    {
        if (!groupIngameMenu || !pageMain) return;
        EnsureSystemCanvasActive();
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
        RequestNotifyRootIfNone();
    }

    public void OpenOptionsFromMainMenu(GameObject callerPage)
    {
        if (!groupOptions || !pageOptions) return;
        EnsureSystemCanvasActive();
        SetGroupIndex(UiGroup.Options);
        pageOptions.Open(callerPage);
        EnsureGamePaused();
    }

    public void OpenOptionsFromIngame(GameObject callerPage)
    {
        if (!groupOptions || !pageOptions) return;
        EnsureSystemCanvasActive();
        SetGroupIndex(UiGroup.Options);
        pageOptions.Open(callerPage);
        EnsureGamePaused();
    }

    // 由 PageOptions 關閉完成後呼叫（Back）
    public void OnOptionsClosed()
    {
        // 若 PageMain 仍開著，就回到 IngameMenu；否則關到 None
        if (pageMain && pageMain.isActiveAndEnabled && pageMain.IsOpen)
            SetGroupIndex(UiGroup.IngameMenu); // 這一步會把 Group_IngameMenu 的互動恢復
        else
            SetGroupIndex(UiGroup.None);

        EnsureGameResumedIfNone();
        RequestNotifyRootIfNone();
    }

    // ===== 判定有沒有 UI 開著（群組或頁面任一成立即可） =====
    bool HasAnyUiOpen()
    {
        if (current != UiGroup.None) return true; // 有群組索引即視為開著
        if (pageMain && pageMain.isActiveAndEnabled && pageMain.IsOpen) return true;
        if (pageOptions && pageOptions.isActiveAndEnabled && pageOptions.IsOpen) return true;
        return false;
    }

    // ===== CanvasGroup 工具（只用 CG，不關物件） =====
    static void SetGroupVisible(CanvasGroup cg, bool visible)
    {
        if (!cg) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    // ===== 時間控制 =====
    void EnsureGamePaused() => Time.timeScale = 0f;
    void EnsureGameResumedIfNone() { if (!HasAnyUiOpen()) Time.timeScale = 1f; }
    void EnsureGameResumed() => Time.timeScale = 1f;
}
