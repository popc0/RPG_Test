using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIRootCanvasController : MonoBehaviour
{
    [Header("Outer layer keys")]
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
        // 等待場景內 Anchor 完成註冊
        yield return null;

        foreach (var anchor in FindObjectsOfType<UICanvasAnchor>(true))
        {
            if (string.IsNullOrEmpty(anchor.key)) continue;
            var cg = anchor.GetComponent<CanvasGroup>();
            if (cg != null && !map.ContainsKey(anchor.key))
                map[anchor.key] = cg;
        }

        if (!string.IsNullOrEmpty(pendingOpenKey))
        {
            TryApplyTop(pendingOpenKey);
        }
        else if (Get(keyMain) != null)
        {
            // ✅ 如果沒有指定要開誰，但場景裡有 mainmenu，就當作預設打開 MainMenu
            SetTopIndex(TopIndex.MainMenu);
        }
        else
        {
            SetTopIndex(TopIndex.None);
        }
    }

    void OnRegister(string key, CanvasGroup cg)
    {
        if (string.IsNullOrEmpty(key) || cg == null) return;
        map[key] = cg;
        if (!string.IsNullOrEmpty(pendingOpenKey))
            TryApplyTop(pendingOpenKey);
    }

    void OnUnregister(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        map.Remove(key);
    }

    void OnOpenCanvas(string key)
    {
        pendingOpenKey = key;
        TryApplyTop(key);
    }

    // 只在「主選單情境」會被呼叫（由 Mux 決策），外層回到 MainMenu
    void OnCloseActive()
    {
        pendingOpenKey = null;
        if (Get(keyMain) != null)
            SetTopIndex(TopIndex.MainMenu);
        else
            SetTopIndex(TopIndex.None);
    }

    void TryApplyTop(string key)
    {
        bool ready =
            (key == keyMain && Get(keyMain) != null) ||
            (key == keySystem && Get(keySystem) != null);

        if (!ready) return;

        if (key == keyMain) SetTopIndex(TopIndex.MainMenu);
        else if (key == keySystem) SetTopIndex(TopIndex.System);
    }

    void SetTopIndex(TopIndex idx)
    {
        current = idx;

        var main = Get(keyMain);
        var system = Get(keySystem);

        SetInteract(main, current == TopIndex.MainMenu);
        SetInteract(system, current == TopIndex.System);
    }

    CanvasGroup Get(string key)
        => (!string.IsNullOrEmpty(key) && map.TryGetValue(key, out var cg)) ? cg : null;

    static void SetInteract(CanvasGroup cg, bool interactive)
    {
        if (!cg) return;
        cg.interactable = interactive;
        cg.blocksRaycasts = interactive;
        // 不動 alpha（顯示交給各自控制器）
    }
}
