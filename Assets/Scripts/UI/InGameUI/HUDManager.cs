using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDManager : MonoBehaviour
{
    [Header("要控制可見性的根物件, 可填 Canvas 或整個 HUD 根節點")]
    public GameObject hudRoot;

    [Header("可選, 若有 CanvasGroup 會用 alpha 切換")]
    public CanvasGroup hudGroup;

    [Header("在主選單隱藏 HUD")]
    public bool hideInMainMenu = true;

    [Header("可選, 如果主選單不在 build index 0, 填入主選單場景名稱")]
    public string mainMenuSceneName = "";

    void Awake()
    {
        if (hudRoot == null)
            hudRoot = gameObject;

        // 啟動時先判斷一次
        var now = SceneManager.GetActiveScene();
        SetVisible(!IsMainMenuScene(now));

        if (hideInMainMenu)
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        if (hideInMainMenu)
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        SetVisible(!IsMainMenuScene(newScene));
    }

    bool IsMainMenuScene(Scene s)
    {
        // 建議把主選單放在 build index 0
        if (s.buildIndex == 0) return true;

        if (!string.IsNullOrEmpty(mainMenuSceneName) && s.name == mainMenuSceneName)
            return true;

        return false;
    }

    public void SetVisible(bool visible)
    {
        if (hudGroup != null)
        {
            hudGroup.alpha = visible ? 1f : 0f;
            hudGroup.interactable = visible;
            hudGroup.blocksRaycasts = visible;
        }
        else if (hudRoot != null)
        {
            hudRoot.SetActive(visible);
        }
    }
}
