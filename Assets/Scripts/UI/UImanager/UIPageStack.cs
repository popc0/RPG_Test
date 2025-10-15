using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 超小型 UI 頁面堆疊：誰開下一頁，誰就被 Push；關閉時 Pop 並回到上一頁。
/// 建議掛在 System 或 SystemCanvas 上（場景常駐）。
/// </summary>
public class UIPageStack : MonoBehaviour
{
    public static UIPageStack Instance { get; private set; }

    private readonly Stack<GameObject> _stack = new Stack<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 可選：DontDestroyOnLoad(gameObject);
    }

    /// <summary>把呼叫者頁面丟進堆疊。</summary>
    public void Push(GameObject page)
    {
        if (page == null) return;
        _stack.Push(page);
    }

    /// <summary>彈出並回上一頁（顯示、解鎖互動、嘗試設定焦點）。</summary>
    public GameObject PopAndReturn(params string[] preferFocusNames)
    {
        if (_stack.Count == 0) return null;

        var prev = _stack.Pop();
        if (prev == null) return null;

        // 重新顯示上一頁
        prev.SetActive(true);
        var cg = prev.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // 嘗試聚焦上一頁的預設按鈕
        Selectable focus = null;
        var t = prev.transform;
        foreach (var name in preferFocusNames)
        {
            var tr = t.Find(name);
            if (tr != null)
            {
                focus = tr.GetComponent<Selectable>();
                if (focus != null) break;
            }
        }
        // 後備方案：找第一個可用的 Selectable
        if (focus == null) focus = prev.GetComponentInChildren<Selectable>();

        if (focus != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(focus.gameObject);

        return prev;
    }

    public void Clear() => _stack.Clear();

    public GameObject Peek() => _stack.Count > 0 ? _stack.Peek() : null;
}
