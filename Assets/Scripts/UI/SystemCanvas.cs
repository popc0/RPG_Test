using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class SystemCanvas : MonoBehaviour
{
    public static SystemCanvas Instance;

    [Header("EventSystem")]
    [SerializeField] private EventSystem eventSystem;

    [Header("System 子 Canvas 排序設定")]
    [SerializeField] private bool forceChildCanvasSorting = true;
    [SerializeField] private string systemSortingLayerName = "SystemUI";
    [SerializeField] private int minSortingOrder = 600;

    [Header("例外設定")]
    [SerializeField] private bool excludeHUDCanvases = true;

    void Awake()
    {
        if (eventSystem == null)
            eventSystem = GetComponentInChildren<EventSystem>(true);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        EnsureSingleEventSystem(this);
        ApplySortingToChildCanvases();
    }

    void OnEnable()
    {
        EnsureSingleEventSystem(this);
        ApplySortingToChildCanvases();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            Instance = null;
        }
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (Instance != null)
        {
            EnsureSingleEventSystem(Instance);
            Instance.ApplySortingToChildCanvases();
        }
    }

    void OnTransformChildrenChanged()
    {
        ApplySortingToChildCanvases();
    }

    void ApplySortingToChildCanvases()
    {
        if (!forceChildCanvasSorting) return;

        var canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c == null) continue;

            if (excludeHUDCanvases && c.GetComponentInParent<HUDManager>(true) != null)
                continue;

            c.overrideSorting = true;

            if (!string.IsNullOrEmpty(systemSortingLayerName))
                c.sortingLayerName = systemSortingLayerName;

            if (c.sortingOrder < minSortingOrder)
                c.sortingOrder = minSortingOrder;
        }
    }

    static void EnsureSingleEventSystem(SystemCanvas preferThis)
    {
        var allES = Object.FindObjectsOfType<EventSystem>(true);

        if (preferThis.eventSystem == null)
            preferThis.eventSystem = preferThis.GetComponentInChildren<EventSystem>(true);

        var keep = (preferThis.eventSystem != null)
            ? preferThis.eventSystem.gameObject
            : preferThis.gameObject;

        foreach (var es in allES)
        {
            var go = es.gameObject;
            bool isKeep = go == keep;

            es.enabled = isKeep;
            if (!isKeep && go.activeSelf) go.SetActive(false);
        }
    }

    [ContextMenu("Refresh Child Canvas Sorting")]
    public void RefreshSortingNow()
    {
        ApplySortingToChildCanvases();
    }
}
