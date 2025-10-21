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

        // 依當前場景先套一次規則
        ApplyByScene(SceneManager.GetActiveScene());

        // 只聽場景切換；不再訂閱 UIEvents（互動交給 SystemLayerMux）
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
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
            // 主選單：隱藏且關互動（唯一會動能見度的地方）
            hudGroup.alpha = 0f;
            hudGroup.interactable = false;
            hudGroup.blocksRaycasts = false;
        }
        else
        {
            // 離開主選單：恢復顯示；互動狀態交由 SystemLayerMux 控制
            hudGroup.alpha = initialAlpha;
            // 不動 interactable / blocksRaycasts
        }
    }
}
