using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class HUDManager : MonoBehaviour
{
    [Header("主選單場景名稱")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private CanvasGroup hudGroup;
    private float initialAlpha = 1f;

    void Awake()
    {
        hudGroup = GetComponent<CanvasGroup>();
        if (!hudGroup) hudGroup = gameObject.AddComponent<CanvasGroup>();

        initialAlpha = hudGroup.alpha;
    }

    void OnEnable()
    {
        // 一啟用就訂閱事件
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // ✅ 每次啟用都用「目前 active scene」套一次規則
        ApplyByScene(SceneManager.GetActiveScene());
    }

    void OnDisable()
    {
        // 關閉時就退訂，避免場景切換時還在收事件
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        ApplyByScene(newScene);
    }

    private void ApplyByScene(Scene s)
    {
        bool isMainMenu = s.name == mainMenuSceneName;

        if (isMainMenu)
        {
            // 主選單：隱藏且關互動
            hudGroup.alpha = 0f;
            hudGroup.interactable = false;
            hudGroup.blocksRaycasts = false;
        }
        else
        {
            // 離開主選單：恢復顯示；互動狀態交由別的系統去控
            hudGroup.alpha = initialAlpha;
            // 不動 interactable / blocksRaycasts
        }
    }
}
