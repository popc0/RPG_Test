using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 遊戲內主選單路由（極薄）
/// - R 鍵：呼叫 SystemCanvasController.ToggleIngameMenu()
/// - Page_Main/Options：呼叫 SystemCanvasController.OpenOptionsFromIngame(pageMainRoot)
/// - Continue：呼叫 SystemCanvasController.CloseIngameMenu()
/// </summary>
public class IngameMenuRouterB : MonoBehaviour
{
    [Header("Page_Main 根物件（作為 Options 的 caller）")]
    [SerializeField] private GameObject pageMainRoot; // SystemCanvas/Group_IngameMenu/Page_Main

    [Header("主選單場景名稱（在主選單停用 R）")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("切換鍵（原 ESC，改為 R）")]
    [SerializeField] private KeyCode toggleKey = KeyCode.R;

    [Header("（可選）進入 IngameMenu 時聚焦的按鈕")]
    [SerializeField] private Selectable defaultFocusIngame;

    private SystemCanvasController scc;

    void Awake() => TryFindSCC();

    void Update()
    {
        // 主選單場景不處理 R
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;
        if (!Input.GetKeyDown(toggleKey)) return;

        TryFindSCC();
        if (scc != null) scc.ToggleIngameMenu();
    }

    // ===== Page_Main 內按鈕 =====
    public void OnClick_Continue()
    {
        TryFindSCC();
        if (scc != null) scc.CloseIngameMenu();
    }

    public void OnClick_Options()
    {
        TryFindSCC();
        if (scc != null && pageMainRoot != null)
            scc.OpenOptionsFromIngame(pageMainRoot);
    }

    public void OnClick_BackToMainMenu()
    {
        // 這裡照你原本「回主選單」流程（存檔→載入主選單）
        TryFindSCC();
        if (scc != null) scc.CloseIngameMenu();
        // SaveManager.Instance?.SaveNow();
        // SceneManager.LoadScene(mainMenuSceneName);
    }

    private void TryFindSCC()
    {
        if (scc != null) return;
        scc = FindObjectOfType<SystemCanvasController>();
        if (scc == null)
            Debug.LogWarning("[IngameMenuRouterB] 找不到 SystemCanvasController。請確認 SystemCanvasController 有掛在 SystemCanvas。");
    }
}
