using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SystemLayerMux : MonoBehaviour
{
    [Header("Main menu scene name")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Sibling CanvasGroups (under System)")]
    [SerializeField] private CanvasGroup canvasHUD;       // Canvas_HUD
    [SerializeField] private CanvasGroup canvasSystem;    // SystemCanvas (root)
    [SerializeField] private CanvasGroup canvasStory;     // Canvas_Story
    [SerializeField] private CanvasGroup canvasTPHint;    // Canvas_TPHint

    [Header("Optional auto-find by names (children of this object)")]
    [SerializeField] private string nameHUD = "Canvas_HUD";
    [SerializeField] private string nameSystem = "SystemCanvas";
    [SerializeField] private string nameStory = "Canvas_Story";
    [SerializeField] private string nameTPHint = "Canvas_TPHint";

    [Header("Scene switch grace frames")]
    [SerializeField] private int sceneSwitchGraceFrames = 2;

    private enum LayerKey { None, HUD, System, Story, TPHint }
    private LayerKey current = LayerKey.None;

    private bool isMainMenuScene;
    private bool lastAllClosed = false;
    private int graceFramesLeft = 0;

    void Awake()
    {
        AutoBindIfMissing();
        ApplySceneRule(SceneManager.GetActiveScene());
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void Update()
    {
        if (graceFramesLeft > 0)
        {
            graceFramesLeft--;
            return;
        }

        bool any = AnyLayerInteractive();
        if (!any && !lastAllClosed)
        {
            OnAllLayersClosed();
        }
        lastAllClosed = !any;
    }

    // --- Public API ----------------------------------------------------------

    public void OpenHUD() { Open(LayerKey.HUD); }
    public void OpenSystem() { Open(LayerKey.System); }
    public void OpenStory() { Open(LayerKey.Story); }
    public void OpenTPHint() { Open(LayerKey.TPHint); }

    // Called by SystemCanvasController when its pages are all closed (None)
    public void NotifyChildClosed() { /* decision deferred to Update() */ }

    // --- Internal logic ------------------------------------------------------

    private void Open(LayerKey key)
    {
        // mutual exclusion (interaction only)
        SetInteract(canvasHUD, key == LayerKey.HUD);
        SetInteract(canvasSystem, key == LayerKey.System);
        SetInteract(canvasStory, key == LayerKey.Story);
        SetInteract(canvasTPHint, key == LayerKey.TPHint);
        current = key;

        // keep outer layer on "system" (deferred to ensure anchors are registered)
        StartCoroutine(CoRaiseOuterSystem());
    }

    private void OnAllLayersClosed()
    {
        if (isMainMenuScene)
        {
            UIEvents.RaiseCloseActiveCanvas();   // outer goes back to mainmenu
            current = LayerKey.None;
            return;
        }

        // gameplay scene idle -> show HUD
        if (!IsInteractive(canvasHUD))
        {
            Open(LayerKey.HUD);
        }
    }

    private void ApplySceneRule(Scene s)
    {
        isMainMenuScene = (s.name == mainMenuSceneName);
        graceFramesLeft = sceneSwitchGraceFrames;

        if (!isMainMenuScene)
        {
            // gameplay baseline: HUD interactive, others off
            SetInteract(canvasHUD, true);
            SetInteract(canvasSystem, false);
            SetInteract(canvasStory, false);
            SetInteract(canvasTPHint, false);
            current = LayerKey.HUD;

            StartCoroutine(CoRaiseOuterSystem());
        }

        StartCoroutine(CoPostSceneIdleCheck());
    }

    private IEnumerator CoRaiseOuterSystem()
    {
        // wait a couple frames to ensure UIRoot registered all anchors
        yield return null;
        yield return null;
        UIEvents.RaiseOpenCanvas("system");
    }

    private IEnumerator CoPostSceneIdleCheck()
    {
        yield return null;
        yield return null;
        if (!AnyLayerInteractive()) OnAllLayersClosed();
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        ApplySceneRule(newScene);
    }

    // --- Helpers -------------------------------------------------------------

    private void SetInteract(CanvasGroup cg, bool on)
    {
        if (!cg) return;
        cg.interactable = on;
        cg.blocksRaycasts = on;
        // do not touch alpha (visibility managed elsewhere)
    }

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
            if (!g || g == canvasSystem) continue;
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
}
