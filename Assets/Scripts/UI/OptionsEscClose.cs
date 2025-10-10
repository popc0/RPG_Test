using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsEscClose : MonoBehaviour
{
    [Header("場景判斷")]
    [Tooltip("主選單場景名稱（用來判斷目前是否在主選單）")]
    public string mainMenuSceneName = "MainMenuScene";

    [Header("熱鍵")]
    public KeyCode escKey = KeyCode.Escape;

    [Header("參考（兩個都可以填，腳本會自動判斷何時使用）")]
    [Tooltip("主選單用：關 Page_Options（先左→再下）")]
    public MainMenuController mainMenu;

    [Tooltip("遊戲內用：關子頁或先收整個面板再回主頁（實際狀態切換由 IngameMenuRouterB 內部負責）")]
    public IngameMenuRouterB ingameRouter;

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
            // 主選單：呼叫主選單版本（內部會切到 MainMenu）
            if (mainMenu != null) mainMenu.OnClickOptionsBack();
            return;
        }

        // 遊戲內：呼叫遊戲內版本（內部會切到 IngameMenu）
        if (ingameRouter != null) ingameRouter.OnClick_OptionsBack();
    }
}
