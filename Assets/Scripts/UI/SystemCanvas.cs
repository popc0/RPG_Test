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
    [Tooltip("只設定排序，不會動到可見性/CanvasGroup/SetActive。")]
    [SerializeField] private bool forceChildCanvasSorting = true;

    [Tooltip("System 專用 Sorting Layer（留空不改）。")]
    [SerializeField] private string systemSortingLayerName = "SystemUI";

    [Tooltip("將子 Canvas 的排序至少拉到這個下限；高於下限的保留原值。")]
    [SerializeField] private int minSortingOrder = 600;

    [Header("例外設定")]
    [Tooltip("略過掛在 HUDManager 之下的 Canvas，讓 HUDManager 自行管理顯示/隱藏與任何排序調整。")]
    [SerializeField] private bool excludeHUDCanvases = true;

    private void Awake()
    {
        if (eventSystem == null)
            eventSystem = GetComponentInChildren<EventSystem>(true);

        if (Instance != null && Instance != this)
        {
            EnsureSingleEventSystem(this);
            Destroy(Instance.gameObject); // 保留最新
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSingleEventSystem(this);
        }

        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        ApplySortingToChildCanvases();
    }

    private void OnEnable()
    {
        EnsureSingleEventSystem(this);
        ApplySortingToChildCanvases();
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
        if (Instance != null)
        {
            EnsureSingleEventSystem(Instance);
            Instance.ApplySortingToChildCanvases();
        }
    }

    private void OnTransformChildrenChanged()
    {
        ApplySortingToChildCanvases();
    }

    // —— 只調整排序，不影響可見性 —— //
    private void ApplySortingToChildCanvases()
    {
        if (!forceChildCanvasSorting) return;

        var canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c == null) continue;

            // 【關鍵】略過 HUD：由 HUDManager 完全掌控（可見性、排序都不干涉）
            if (excludeHUDCanvases && c.GetComponentInParent<HUDManager>(true) != null)
                continue;

            c.overrideSorting = true;

            if (!string.IsNullOrEmpty(systemSortingLayerName))
                c.sortingLayerName = systemSortingLayerName;

            if (c.sortingOrder < minSortingOrder)
                c.sortingOrder = minSortingOrder;
        }
    }

    private static void EnsureSingleEventSystem(SystemCanvas preferThis)
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
