using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class HUDManagerB : MonoBehaviour
{
    public static HUDManagerB Instance { get; private set; }

    [Header("HUD 設定")]
    [Tooltip("指到你的 HUD Prefab（例如 Canvas_HUD）")]
    public GameObject hudPrefab;

    [Tooltip("進入主選單場景時是否自動隱藏 HUD")]
    public bool hideInMainMenu = true;

    [Tooltip("主選單場景名稱（需與 Build Settings 一致）")]
    public string mainMenuSceneName = "MainMenu";

    private GameObject hudInstance;             // 產生後的 HUD
    private CanvasGroup hudCanvasGroupCache;    // 若 HUD 上有 CanvasGroup

    void Awake()
    {
        // 單例
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 場上若已有一份 HUD（例如你先手動放了），就沿用；否則用 Prefab 產生
        if (hudInstance == null)
        {
            var existing = FindExistingHUDRoot();
            if (existing != null)
            {
                hudInstance = existing;
            }
            else if (hudPrefab != null)
            {
                hudInstance = Instantiate(hudPrefab);
                hudInstance.name = hudPrefab.name; // 乾淨命名
            }
        }

        if (hudInstance != null)
        {
            DontDestroyOnLoad(hudInstance);
            hudCanvasGroupCache = hudInstance.GetComponent<CanvasGroup>();
            StripExtraEventSystem(hudInstance);
        }

        // 監聽場景切換（主選單自動隱藏）
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // 進第一個場景時同步一次可見狀態
        var now = SceneManager.GetActiveScene().name;
        if (hideInMainMenu && now == mainMenuSceneName) SetVisible(false);
        else SetVisible(true);
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (!hideInMainMenu) return;

        if (newScene.name == mainMenuSceneName) SetVisible(false);
        else SetVisible(true);
    }

    // —— 公開靜態 API ——
    public static void ShowHUD()
    {
        if (Instance == null) return;
        Instance.SetVisible(true);
    }

    public static void HideHUD()
    {
        if (Instance == null) return;
        Instance.SetVisible(false);
    }

    // —— 內部可見性控制 —— 
    void SetVisible(bool on)
    {
        // 若場上有 HUDVisibilityController，優先使用（更細膩）
        var hvc = HUDVisibilityController.Instance ?? FindObjectOfType<HUDVisibilityController>();
        if (hvc != null)
        {
            if (on) HUDVisibilityController.ShowHUD();
            else HUDVisibilityController.HideHUD();
            return;
        }

        // 沒有 HVC 就以 CanvasGroup/SetActive 控制
        if (hudCanvasGroupCache != null)
        {
            hudCanvasGroupCache.alpha = on ? 1f : 0f;
            hudCanvasGroupCache.interactable = on;
            hudCanvasGroupCache.blocksRaycasts = on;
        }
        else if (hudInstance != null)
        {
            hudInstance.SetActive(on);
        }
    }

    // 盡量沿用現場已經存在的 HUD（避免重複）
    GameObject FindExistingHUDRoot()
    {
        // 你可以依照命名習慣調整這裡的尋找邏輯
        var hvc = FindObjectOfType<HUDVisibilityController>();
        if (hvc != null) return hvc.gameObject;

        // 次選：找一個看起來像 HUD 的 Canvas（避免抓到主選單）
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            if (!canvas.isRootCanvas) continue;
            var name = canvas.name.ToLower();
            if (name.Contains("hud") || name.Contains("ingame") || name.Contains("in-game"))
                return canvas.gameObject;
        }
        return null;
    }

    // 清掉 HUD Prefab 內誤帶的 EventSystem（全域只允許一份）
    void StripExtraEventSystem(GameObject root)
    {
        var es = root.GetComponentInChildren<EventSystem>(true);
        if (es != null)
        {
            // 只有當這份不是全域 EventSystem 時才刪
            if (EventSystem.current != es)
                Destroy(es.gameObject);
        }
    }
}
