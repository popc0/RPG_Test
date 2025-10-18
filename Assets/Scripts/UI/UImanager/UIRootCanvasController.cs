using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIRootCanvasController : MonoBehaviour
{
    [Header("外層 key（只管這兩個）")]
    [SerializeField] string keyMain = "mainmenu";
    [SerializeField] string keySystem = "system";

    enum TopIndex { None, MainMenu, System }
    TopIndex current = TopIndex.None;

    readonly Dictionary<string, CanvasGroup> map = new();
    string pendingOpenKey = null;

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
        // 等一幀收集 Anchor，避免時序競賽
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
                map[anchor.key] = cg;
        }
        if (!string.IsNullOrEmpty(pendingOpenKey))
            TryApplyTop(pendingOpenKey);
    }

    // —— 註冊 / 反註冊 ——
    void OnRegister(string key, CanvasGroup cg)
    {
        map[key] = cg;
        if (!string.IsNullOrEmpty(pendingOpenKey))
            TryApplyTop(pendingOpenKey);
    }

    void OnUnregister(string key) { map.Remove(key); }

    // —— 外層切前景（只接受 mainmenu / system）——
    void OnOpenCanvas(string key)
    {
        pendingOpenKey = key;
        TryApplyTop(key);
    }

    // System 全關 → 回 MainMenu（若沒有就回 None）
    void OnCloseActive()
    {
        pendingOpenKey = null;
        if (Get(keyMain) != null) SetTopIndex(TopIndex.MainMenu);
        else SetTopIndex(TopIndex.None);
    }

    void TryApplyTop(string key)
    {
        bool ready =
            (key == keyMain && Get(keyMain) != null) ||
            (key == keySystem && Get(keySystem) != null);

        if (!ready) return;

        if (key == keyMain) SetTopIndex(TopIndex.MainMenu);
        if (key == keySystem) SetTopIndex(TopIndex.System);
    }

    // —— 只改互動，不動顯示（alpha/SetActive 都不改）——
    void SetTopIndex(TopIndex idx)
    {
        current = idx;

        var main = Get(keyMain);
        var system = Get(keySystem);

        bool mainInteract = (current == TopIndex.MainMenu);
        bool systemInteract = (current == TopIndex.System);

        SetInteract(main, mainInteract);
        SetInteract(system, systemInteract);
        // HUD 交由 HUDManager 依事件自行處理（不在此更動）
    }

    // —— 工具 —— 
    CanvasGroup Get(string key)
        => (!string.IsNullOrEmpty(key) && map.TryGetValue(key, out var cg)) ? cg : null;

    static void SetInteract(CanvasGroup cg, bool interactive)
    {
        if (!cg) return;
        cg.interactable = interactive;
        cg.blocksRaycasts = interactive;
    }
}
