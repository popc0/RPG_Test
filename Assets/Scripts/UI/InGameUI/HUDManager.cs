using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class HUDManager : MonoBehaviour
{
    [Header("主選單場景名稱")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("主選單場景是否隱藏 HUD")]
    [SerializeField] private bool hideInMainMenu = true;

    private CanvasGroup hudGroup;

    // 記錄遊戲中的預設互動（非主選單時恢復用）
    private bool defaultInteractable = true;
    private bool defaultBlocksRaycasts = true;

    // 外層目前前景（只關心 "system" 與 "mainmenu"；null 代表沒有任何外層 UI）
    private string currentTopKey = null;

    void Awake()
    {
        hudGroup = GetComponent<CanvasGroup>();
        if (hudGroup == null) hudGroup = gameObject.AddComponent<CanvasGroup>();

        defaultInteractable = hudGroup.interactable;
        defaultBlocksRaycasts = hudGroup.blocksRaycasts;

        ApplyByScene(SceneManager.GetActiveScene());

        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // 只在外層互斥層接收訊息（避免跨場景斷接）
        UIEvents.OnOpenCanvas += OnOpenCanvas;
        UIEvents.OnCloseActiveCanvas += OnCloseActiveCanvas;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        UIEvents.OnOpenCanvas -= OnOpenCanvas;
        UIEvents.OnCloseActiveCanvas -= OnCloseActiveCanvas;
    }

    // 進入/切換場景時套用可見與互動基準
    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        ApplyByScene(newScene);
    }

    private void ApplyByScene(Scene s)
    {
        bool isMainMenu = s.name == mainMenuSceneName;

        if (hideInMainMenu && isMainMenu)
        {
            // 主選單：不可見且不互動
            hudGroup.alpha = 0f;
            hudGroup.interactable = false;
            hudGroup.blocksRaycasts = false;
            currentTopKey = "mainmenu"; // 記錄當前在主選單
        }
        else
        {
            // 遊戲內：顯示，互動依外層狀態（若沒有任何 UI → 當作常態，HUD 開）
            hudGroup.alpha = 1f;
            hudGroup.interactable = defaultInteractable;
            hudGroup.blocksRaycasts = defaultBlocksRaycasts;

            // 依目前外層狀態做一次同步
            ApplyTopKeyToHUD();
        }
    }

    // 外層通知：某個前景被打開（我們只關心 system / mainmenu）
    private void OnOpenCanvas(string key)
    {
        currentTopKey = key;
        // 主選單場景交給場景規則處理，不覆蓋
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        ApplyTopKeyToHUD();
    }

    // 外層通知：前景關閉（System 全關）
    private void OnCloseActiveCanvas()
    {
        // 沒有任何外層 UI → 常態（HUD 開）
        currentTopKey = null;

        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        ApplyTopKeyToHUD();
    }

    // 把 currentTopKey 對應到 HUD 互動規則
    private void ApplyTopKeyToHUD()
    {
        // 在遊戲內（非主選單）：
        //  - key == "system"  → HUD 互動開
        //  - key == null      → 沒有任何 UI → HUD 互動開（常態）
        //  - 其他（理論上不會出現）→ 維持預設
        bool enable = (currentTopKey == "system" || currentTopKey == null);

        SetInteractionEnabled(enable);
    }

    /// <summary>
    /// 只改互動與射線，不動顯示；主選單場景下忽略（維持隱藏且不互動）
    /// </summary>
    public void SetInteractionEnabled(bool enabled)
    {
        if (hideInMainMenu && SceneManager.GetActiveScene().name == mainMenuSceneName)
            return;

        hudGroup.interactable = enabled;
        hudGroup.blocksRaycasts = enabled;
    }

    /// <summary>
    /// 可選：更新「非主選單」預設互動快照。
    /// </summary>
    public void UpdateDefaultInteractionSnapshot()
    {
        defaultInteractable = hudGroup.interactable;
        defaultBlocksRaycasts = hudGroup.blocksRaycasts;
    }
}
