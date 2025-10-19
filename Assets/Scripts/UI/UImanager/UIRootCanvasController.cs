using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIRootCanvasController : MonoBehaviour
{
    [Header("外層 key")]
    [SerializeField] string keyMain = "mainmenu";
    [SerializeField] string keySystem = "system";

    private enum TopIndex { None, MainMenu, System }
    private TopIndex current = TopIndex.None;

    private readonly Dictionary<string, CanvasGroup> map = new();
    private string pendingOpenKey = null;

    void OnEnable()
    {
        UIEvents.OnRegisterCanvas += OnRegister;
        UIEvents.OnUnregisterCanvas += OnUnregister;
        UIEvents.OnOpenCanvas += OnOpenCanvas;
        UIEvents.OnCloseActiveCanvas += OnCloseActive;
    }

    void OnDisable()
    {
        UIEvents.OnRegisterCanvas -= OnRegister;
        UIEvents.OnUnregisterCanvas -= OnUnregister;
        UIEvents.OnOpenCanvas -= OnOpenCanvas;
        UIEvents.OnCloseActiveCanvas -= OnCloseActive;
    }

    void Start()
    {
        StartCoroutine(DelayedRegisterAnchors());
    }

    IEnumerator DelayedRegisterAnchors()
    {
        yield return null;
        foreach (var anchor in FindObjectsOfType<UICanvasAnchor>(true))
        {
            if (string.IsNullOrEmpty(anchor.key)) continue;
            var cg = anchor.GetComponent<CanvasGroup>();
            if (cg && !map.ContainsKey(anchor.key))
            {
                map[anchor.key] = cg;
                Debug.Log($"[UIRoot] Registered key={anchor.key}");
            }
        }

        if (!string.IsNullOrEmpty(pendingOpenKey))
            TryApplyTop(pendingOpenKey);
        else
            SetTopIndex(TopIndex.None);
    }

    void OnRegister(string key, CanvasGroup cg)
    {
        map[key] = cg;
        Debug.Log($"[UIRoot] OnRegister key={key}");
        if (!string.IsNullOrEmpty(pendingOpenKey))
            TryApplyTop(pendingOpenKey);
    }

    void OnUnregister(string key)
    {
        map.Remove(key);
        Debug.Log($"[UIRoot] OnUnregister key={key}");
    }

    void OnOpenCanvas(string key)
    {
        Debug.Log($"[UIRoot] OnOpenCanvas received. key={key}");
        pendingOpenKey = key;
        TryApplyTop(key);
    }

    void OnCloseActive()
    {
        Debug.Log("[UIRoot] OnCloseActive received. (No auto-switch this time)");
        pendingOpenKey = null;
        SetTopIndex(TopIndex.None);
    }

    void TryApplyTop(string key)
    {
        bool ready =
            (key == keyMain && Get(keyMain) != null) ||
            (key == keySystem && Get(keySystem) != null);

        Debug.Log($"[UIRoot] TryApplyTop key={key} ready={ready}");
        if (!ready)
        {
            Debug.Log($"[UIRoot] Skip apply, map count={map.Count}");
            return;
        }

        if (key == keyMain)
        {
            SetTopIndex(TopIndex.MainMenu);
        }
        else if (key == keySystem)
        {
            SetTopIndex(TopIndex.System);
        }
        else
        {
            Debug.Log($"[UIRoot] Unknown key={key}");
        }
    }

    void SetTopIndex(TopIndex idx)
    {
        current = idx;
        var main = Get(keyMain);
        var system = Get(keySystem);

        SetInteract(main, current == TopIndex.MainMenu);
        SetInteract(system, current == TopIndex.System);

        Debug.Log($"[UIRoot] >>> Switched top key={current} <<<");
        Debug.Log($"[UIRoot] SetTopIndex current={current}, " +
                  $"main(i={Flag(main)}), system(i={Flag(system)}), " +
                  $"mapCount={map.Count}");
    }

    CanvasGroup Get(string key)
        => (!string.IsNullOrEmpty(key) && map.TryGetValue(key, out var cg)) ? cg : null;

    static void SetInteract(CanvasGroup cg, bool interactive)
    {
        if (!cg) return;
        cg.interactable = interactive;
        cg.blocksRaycasts = interactive;
        // 不動 alpha，保留顯示邏輯給其他控制器
        Debug.Log($"[UIRoot] SetInteract {cg.name} -> {interactive}");
    }

    static string Flag(CanvasGroup cg)
    {
        if (!cg) return "null";
        return cg.interactable ? "1" : "0";
    }
}
