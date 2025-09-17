using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsEscClose : MonoBehaviour
{
    [Header("場景判斷")]
    [Tooltip("主選單場景名稱（用來判斷目前是否在主選單）")]
    public string mainMenuSceneName = "MainMenu";

    [Header("熱鍵")]
    public KeyCode escKey = KeyCode.Escape;

    [Header("參考（兩個都可以填，腳本會自動判斷何時使用）")]
    [Tooltip("主選單用：關 Page_Options（先左→再下）")]
    public MainMenuController mainMenu;

    [Tooltip("遊戲內用：關子頁或先收整個面板再回主頁")]
    public IngameMenuRouterB ingameRouter;

    [Header("遊戲內關閉行為")]
    [Tooltip("勾選：ESC 時先把 Panel_IngameMenu 收到畫面下方，再回主頁；不勾：只關 Page_Options 子頁")]
    public bool closeIngamePanelToMain = false;

    [Header("（可選）只在 Page_Options 啟用時響應 ESC")]
    [Tooltip("通常指定 Page_Options 的根物件；若留空則不做這項判斷")]
    public GameObject targetPageOptions;

    void Update()
    {
        if (!Input.GetKeyDown(escKey)) return;

        // 若指定了 Page_Options 根物件，僅在它啟用時才響應
        if (targetPageOptions != null && !targetPageOptions.activeInHierarchy)
            return;

        bool isMainMenu = SceneManager.GetActiveScene().name == mainMenuSceneName;

        if (isMainMenu)
        {
            // 主選單情境：走主選單的「先左→再下」收合
            if (mainMenu != null)
                mainMenu.OnClickOptionsBack();
            return;
        }

        // 遊戲內情境
        if (ingameRouter != null)
        {
            if (closeIngamePanelToMain)
                ingameRouter.OnClick_ReturnToMainAndHidePanel(); // 先把整個 Panel 收下，再回主頁
            else
                ingameRouter.OnClick_OptionsBack();              // 只關 Page_Options 子頁
        }
    }
}
