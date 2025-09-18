using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)] // 極早執行，先於 EventSystem.OnEnable()
public class SystemCanvas : MonoBehaviour
{
    public static SystemCanvas Instance;

    [Tooltip("可留空，會自動尋找子階層的 EventSystem")]
    [SerializeField] private EventSystem eventSystem;

    private void Awake()
    {
        if (eventSystem == null)
            eventSystem = GetComponentInChildren<EventSystem>(true);

        if (Instance != null && Instance != this)
        {
            // 你要「保留最新」，所以先把舊的 EventSystem 關掉，避免同幀雙啟用
            EnsureSingleEventSystem(preferThis: this);

            // 幹掉舊的 SystemCanvas
            Destroy(Instance.gameObject);

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 只有自己時也保險清一次
            EnsureSingleEventSystem(preferThis: this);
        }

        // 進入新場景時再保險清理一次（處理場景載入同名預置的情況）
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnEnable()
    {
        // 若因為啟用順序導致其它 EventSystem 被激活，這裡再保險清一次
        EnsureSingleEventSystem(preferThis: this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            Instance = null;
        }
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        // 場景切換後再次確保只有一個 EventSystem 啟用
        if (Instance != null)
            EnsureSingleEventSystem(preferThis: Instance);
    }

    private static void EnsureSingleEventSystem(SystemCanvas preferThis)
    {
        var allES = Object.FindObjectsOfType<EventSystem>(true);

        // 若 preferThis 沒有 eventSystem，嘗試抓一次
        if (preferThis.eventSystem == null)
            preferThis.eventSystem = preferThis.GetComponentInChildren<EventSystem>(true);

        var keep = (preferThis.eventSystem != null) ? preferThis.eventSystem.gameObject : preferThis.gameObject;

        foreach (var es in allES)
        {
            var go = es.gameObject;
            bool isKeep = go == keep;

            // 只有「保留的那個」保持啟用；其它立即關閉，避免同幀雙啟用報錯
            es.enabled = isKeep;
            if (!isKeep && go.activeSelf) go.SetActive(false);
        }
    }
}
