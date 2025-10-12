using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class UIOrchestrator : MonoBehaviour
{
    public static UIOrchestrator I { get; private set; }

    public enum PanelType { Page, Overlay, Modal }

    [System.Serializable]
    public class PanelRef
    {
        public string id;
        public CanvasGroup group;
        public GameObject defaultFocus;

        [Header("Behavior")]
        public PanelType type = PanelType.Page;
        public bool manageVisibility = true;     // 是否由管理器控制 alpha
        public bool restoreOnDeactivate = true;  // 離堆疊時是否還原初始狀態

        [HideInInspector] public float initAlpha;
        [HideInInspector] public bool initInteractable;
        [HideInInspector] public bool initBlocks;
    }

    [Header("註冊的頁面/面板（跨 Canvas 可）")]
    public List<PanelRef> panels = new();

    // 當前活躍堆疊（最末為頂層）
    private readonly List<PanelRef> _stack = new();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        // 若要跨場景保留，開啟下一行
        // DontDestroyOnLoad(gameObject);

        foreach (var p in panels)
        {
            if (p.group == null) continue;
            p.initAlpha = p.group.alpha;
            p.initInteractable = p.group.interactable;
            p.initBlocks = p.group.blocksRaycasts;
        }
    }

    private PanelRef Find(string id) => panels.FirstOrDefault(p => p.id == id);

    // === 公開 API ===
    public void ShowExclusive(string id)
    {
        var target = Find(id);
        if (target == null || target.group == null) { Debug.LogWarning($"UIOrchestrator: id {id} not found"); return; }
        _stack.Clear();
        _stack.Add(target);
        ApplyState();
        Focus(target);
    }

    public void Push(string id)
    {
        var target = Find(id);
        if (target == null || target.group == null) { Debug.LogWarning($"UIOrchestrator: id {id} not found"); return; }
        if (_stack.Count > 0 && _stack[^1] == target) return; // 已在頂端
        _stack.Add(target);
        ApplyState();
        Focus(target);
    }

    public void Pop()
    {
        if (_stack.Count <= 1) return; // 到根頁就不再後退（依需求可改）
        _stack.RemoveAt(_stack.Count - 1);
        ApplyState();
        Focus(_stack[^1]);
    }

    public void ClearAll()
    {
        _stack.Clear();
        ApplyState();
        EventSystem.current?.SetSelectedGameObject(null);
    }

    // === 套用狀態 ===
    private void ApplyState()
    {
        PanelRef top = _stack.Count > 0 ? _stack[^1] : null;
        PanelRef belowTop = _stack.Count > 1 ? _stack[^2] : null;

        foreach (var p in panels)
        {
            if (p.group == null) continue;

            bool inStack = _stack.Contains(p);
            bool isTop = (p == top);

            if (!inStack)
            {
                if (p.restoreOnDeactivate)
                {
                    if (p.manageVisibility) p.group.alpha = p.initAlpha;
                    p.group.interactable = p.initInteractable;
                    p.group.blocksRaycasts = p.initBlocks;
                }
                continue;
            }

            switch (p.type)
            {
                case PanelType.Page:
                    if (p.manageVisibility) p.group.alpha = 1f;
                    p.group.interactable = isTop; // 只有頂層 Page 可互動
                    p.group.blocksRaycasts = isTop; // 只有頂層 Page 擋射線
                    break;

                case PanelType.Overlay:
                    if (p.manageVisibility) p.group.alpha = 1f;
                    p.group.interactable = isTop;   // 也可改成永遠 false
                    p.group.blocksRaycasts = false;   // 讓下層可點
                    break;

                case PanelType.Modal:
                    if (p.manageVisibility) p.group.alpha = 1f;
                    if (isTop)
                    {
                        p.group.interactable = true;
                        p.group.blocksRaycasts = true;
                    }
                    else
                    {
                        p.group.interactable = false;
                        p.group.blocksRaycasts = true; // 次頂層當遮罩底板
                    }
                    break;
            }
        }
    }

    private void Focus(PanelRef p)
    {
        if (p?.defaultFocus != null) EventSystem.current?.SetSelectedGameObject(p.defaultFocus);
        else EventSystem.current?.SetSelectedGameObject(null);
    }
}
