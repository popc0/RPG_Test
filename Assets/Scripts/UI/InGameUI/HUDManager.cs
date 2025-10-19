using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class HUDManager : MonoBehaviour
{
    [Header("主選單場景名稱")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private CanvasGroup hudGroup;
    private float initialAlpha = 1f;

    // 外層前景： "system" / "mainmenu" / null(無外層UI)
    private string currentTopKey = null;

    void Awake()
    {
        hudGroup = GetComponent<CanvasGroup>();
        if (!hudGroup) hudGroup = gameObject.AddComponent<CanvasGroup>();

        initialAlpha = hudGroup.alpha;

        // 先依當前場景套一次規則
        ApplyByScene(SceneManager.GetActiveScene());

        // 只聽場景切換與外層事件（HUD 互動不由 UIRoot直接控制）
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        UIEvents.OnOpenCanvas += OnOpenCanvas;
        UIEvents.OnCloseActiveCanvas += OnCloseActiveCanvas;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        UIEvents.OnOpenCanvas -= OnOpenCanvas;
        UIEvents.OnCloseActiveCanvas -= OnCloseActiveCanvas;
    }

    // 場景切換
    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        ApplyByScene(newScene);
    }

    private void ApplyByScene(Scene s)
    {
        bool isMainMenu = s.name == mainMenuSceneName;

        if (isMainMenu)
        {
            // 特殊狀態：主選單 → 隱藏 + 關互動（唯一會動能見度）
            hudGroup.alpha = 0f;
            hudGroup.interactable = false;
            hudGroup.blocksRaycasts = false;

            // 在主選單時外層狀態視為 mainmenu，且忽略後續外層互動切換
            currentTopKey = "mainmenu";
        }
        else
        {
            // 離開主選單：恢復顯示，不動互動；外層狀態設為「無外層UI」
            hudGroup.alpha = initialAlpha;
            currentTopKey = null;        // 常態
            ApplyInteractionByTopKey();  // 依常態開啟互動
        }
    }

    // 外層通知：某個前景被打開（只關心 system/mainmenu）
    private void OnOpenCanvas(string key)
    {
        // 主選單由場景規則接管，不覆蓋
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        currentTopKey = key;
        ApplyInteractionByTopKey();
    }

    // 外層通知：前景關閉（通常是 System 全關 → 回常態）
    private void OnCloseActiveCanvas()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        currentTopKey = null; // 無外層UI → 常態
        ApplyInteractionByTopKey();
    }

    // 非主選單下的互動規則（只改互動，不動能見度）
    private void ApplyInteractionByTopKey()
    {
        // 非主選單：
        // - key == null（None） → HUD 互動開（常態）
        // - key == "system"     → HUD 互動關（避免擋到系統 UI）
        // - key == "mainmenu"   → 理論上不會出現在非主選單；若出現，視為關
        bool enable = (currentTopKey == null);
        hudGroup.interactable = enable;
        hudGroup.blocksRaycasts = enable;
    }
}
