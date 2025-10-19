using UnityEngine;
using UnityEngine.SceneManagement;

public class SystemLayerMux : MonoBehaviour
{
    [Header("主選單場景名稱")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("同層 CanvasGroups")]
    [SerializeField] private CanvasGroup canvasHUD;       // Canvas_HUD
    [SerializeField] private CanvasGroup canvasSystem;    // SystemCanvas (根)
    [SerializeField] private CanvasGroup canvasStory;     // Canvas_Story
    [SerializeField] private CanvasGroup canvasTPHint;    // Canvas_TPHint

    [Header("自動尋找名稱(可選)")]
    [SerializeField] private string nameHUD = "Canvas_HUD";
    [SerializeField] private string nameSystem = "SystemCanvas";
    [SerializeField] private string nameStory = "Canvas_Story";
    [SerializeField] private string nameTPHint = "Canvas_TPHint";

    private enum LayerKey { None, HUD, System, Story, TPHint }
    private LayerKey current = LayerKey.None;
    private bool isMainMenuScene;
    private bool lastAllClosed = false;

    void Awake()
    {
        AutoBindIfMissing();
        ApplySceneRule(SceneManager.GetActiveScene());
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        Debug.Log("[SystemLayerMux] Awake. Bound: HUD=" + Has(canvasHUD) + " System=" + Has(canvasSystem) + " Story=" + Has(canvasStory) + " TPHint=" + Has(canvasTPHint));
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void Update()
    {
        bool any = AnyLayerInteractive();
        if (!any && !lastAllClosed)
        {
            Debug.Log("[SystemLayerMux] All layers closed this frame. isMainMenu=" + isMainMenuScene);
            OnAllLayersClosed();
        }
        lastAllClosed = !any;
    }

    // 對外入口：開啟指定同層
    public void OpenHUD() { Open(LayerKey.HUD); }
    public void OpenSystemCanvas() { Open(LayerKey.System); }
    public void OpenStory() { Open(LayerKey.Story); }
    public void OpenTPHint() { Open(LayerKey.TPHint); }

    public void CloseActive()
    {
        Debug.Log("[SystemLayerMux] CloseActive requested. current=" + current);
        switch (current)
        {
            case LayerKey.HUD: SetInteract(canvasHUD, false); break;
            case LayerKey.System: SetInteract(canvasSystem, false); break;
            case LayerKey.Story: SetInteract(canvasStory, false); break;
            case LayerKey.TPHint: SetInteract(canvasTPHint, false); break;
        }
        current = LayerKey.None;
        DebugDump();
        // 關閉後由 Update 做 all-closed 決策
    }

    public void NotifyChildClosed()
    {
        Debug.Log("[SystemLayerMux] NotifyChildClosed received.");
    }

    private void Open(LayerKey key)
    {
        Debug.Log("[SystemLayerMux] Open request. key=" + key + " isMainMenu=" + isMainMenuScene);

        // 外層標記 system 在前景
        UIEvents.RaiseOpenCanvas("system");

        // 互斥：只開當前，其餘關
        SetInteract(canvasHUD, key == LayerKey.HUD);
        SetInteract(canvasSystem, key == LayerKey.System);
        SetInteract(canvasStory, key == LayerKey.Story);
        SetInteract(canvasTPHint, key == LayerKey.TPHint);

        current = key;
        DebugDump();
    }

    private void OnAllLayersClosed()
    {
        if (isMainMenuScene)
        {
            Debug.Log("[SystemLayerMux] All closed in MainMenu. RaiseCloseActiveCanvas()");
            current = LayerKey.None;
            UIEvents.RaiseCloseActiveCanvas();
            return;
        }

        // 非主選單：idle 開 HUD
        if (canvasHUD != null && !IsInteractive(canvasHUD))
        {
            Debug.Log("[SystemLayerMux] All closed in game scene. Open HUD as idle.");
            Open(LayerKey.HUD);
        }
    }

    private void ApplySceneRule(Scene s)
    {
        isMainMenuScene = (s.name == mainMenuSceneName);
        Debug.Log("[SystemLayerMux] ApplySceneRule. Scene=" + s.name + " isMainMenu=" + isMainMenuScene);

        // 不強制關，交給流程；但場景切換後延遲再次檢查 idle
        DebugDump();
        StartCoroutine(CoEnsureIdleAfterSceneChange());
    }

    private System.Collections.IEnumerator CoEnsureIdleAfterSceneChange()
    {
        // 等 2 幀讓各 Canvas 在各自的 Start/Awake 完成初始化
        yield return null;
        yield return null;

        bool any = AnyLayerInteractive();
        Debug.Log("[SystemLayerMux] Post-scene idle check. any=" + any + " isMainMenu=" + isMainMenuScene);
        if (!any) OnAllLayersClosed();
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        Debug.Log("[SystemLayerMux] Scene changed: " + oldScene.name + " -> " + newScene.name);
        ApplySceneRule(newScene);
    }

    // 工具
    private void SetInteract(CanvasGroup cg, bool on)
    {
        if (!cg) return;
        cg.interactable = on;
        cg.blocksRaycasts = on;
        // 不動 alpha
    }

    // 只要任一同層「啟用中、可見、可互動」就視為有東西開著
    private bool AnyLayerInteractive()
    {
        if (IsInteractive(canvasHUD)) return true;
        if (IsInteractive(canvasStory)) return true;
        if (IsInteractive(canvasTPHint)) return true;

        if (canvasSystem)
        {
            if (IsInteractive(canvasSystem)) return true;
            if (HasActiveChildCanvasGroup(canvasSystem.transform)) return true;
        }
        return false;
    }

    private bool IsInteractive(CanvasGroup cg)
    {
        if (!cg) return false;
        if (!cg.gameObject.activeInHierarchy) return false;
        if (cg.alpha <= 0.001f) return false;
        return cg.interactable;
    }

    private bool HasActiveChildCanvasGroup(Transform root)
    {
        var groups = root.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            var g = groups[i];
            if (g == null || g == canvasSystem) continue;
            if (!g.gameObject.activeInHierarchy) continue;
            if (g.alpha <= 0.001f) continue;
            if (g.interactable) return true;
        }
        return false;
    }

    private void AutoBindIfMissing()
    {
        if (!canvasHUD) canvasHUD = FindByName<CanvasGroup>(nameHUD);
        if (!canvasSystem) canvasSystem = FindByName<CanvasGroup>(nameSystem);
        if (!canvasStory) canvasStory = FindByName<CanvasGroup>(nameStory);
        if (!canvasTPHint) canvasTPHint = FindByName<CanvasGroup>(nameTPHint);
    }

    private T FindByName<T>(string childName) where T : Component
    {
        if (string.IsNullOrEmpty(childName)) return null;
        var t = transform.Find(childName);
        if (!t) return null;
        return t.GetComponent<T>();
    }

    private static bool Has(Object o) => o != null;

    private void DebugDump()
    {
        string dump =
            "[SystemLayerMux] State" +
            " current=" + current +
            " HUD(i=" + Flag(canvasHUD) + ")" +
            " SystemRoot(i=" + Flag(canvasSystem) + ", childAny=" + HasActiveChildCanvasGroupSafe() + ")" +
            " Story(i=" + Flag(canvasStory) + ")" +
            " TPHint(i=" + Flag(canvasTPHint) + ")";
        Debug.Log(dump);
    }

    private string Flag(CanvasGroup cg)
    {
        if (!cg) return "null";
        var active = cg && cg.gameObject.activeInHierarchy;
        var visible = cg && cg.alpha > 0.001f;
        return (active && visible && cg.interactable) ? "1" : "0";
    }

    private string HasActiveChildCanvasGroupSafe()
    {
        if (!canvasSystem) return "null";
        return HasActiveChildCanvasGroup(canvasSystem.transform) ? "1" : "0";
    }
}
