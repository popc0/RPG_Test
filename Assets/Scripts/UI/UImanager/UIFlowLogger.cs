using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 輕量 UI flow 記錄器：只寫 Console，不顯示任何 UI。
/// - 追蹤：場景切換、導覽焦點變化、按鍵(Enter/R)、按鈕點擊、面板開關狀態快照
/// - 參考欄位可不填；能找到就記，找不到也不會報錯
/// </summary>
public class UIFlowLogger : MonoBehaviour
{
    [Header("可選：主要面板（不填也能跑）")]
    public IngameMenuSlide menuSlide;           // Panel_IngameMenu
    public IngameMenuSlide optionsWrapperSlide; // PageOptionsWrapper
    public IngameMenuSlide optionsSlide;        // Page_Options

    [Header("可選：頁根")]
    public GameObject pageMain;     // Page_Main
    public GameObject pageOptions;  // Page_Options

    [Header("可選：想觀察的按鈕（點擊會記 Log）")]
    public List<Button> watchButtons = new List<Button>();

    [Header("鍵位")]
    public KeyCode submitKey = KeyCode.Return; // Enter
    public KeyCode toggleIngameKey = KeyCode.R;
    public KeyCode snapshotKey = KeyCode.F10;  // 快照：印出當前關鍵狀態

    [Header("進階")]
    public bool verboseFocusLog = true;        // 是否每次焦點改變都印（多UI時輸出會很多）
    public float focusLogThrottleSec = 0.1f;   // 焦點變化訊息的節流

    private GameObject _lastSelected;
    private float _lastFocusLogTime;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 綁監聽：觀察的按鈕
        foreach (var b in watchButtons)
        {
            if (b == null) continue;
            b.onClick.AddListener(() => Debug.Log($"[UIFlow][Button] Click -> {GetPath(b.gameObject)}"));
        }

        AutoFindRefs(); // 能抓多少就抓多少
        Debug.Log("[UIFlow] Logger Awake.");
        PrintSnapshot("Awake");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        Debug.Log($"[UIFlow] SceneLoaded -> {s.name}");
        AutoFindRefs();
        PrintSnapshot("After SceneLoaded");
    }

    void Update()
    {
        // 鍵：Enter、R
        if (Input.GetKeyDown(submitKey))
            Debug.Log("[UIFlow][Key] Enter pressed.");

        if (Input.GetKeyDown(toggleIngameKey))
            Debug.Log("[UIFlow][Key] R pressed (toggle ingame menu).");

        // 一鍵快照
        if (Input.GetKeyDown(snapshotKey))
            PrintSnapshot("Manual Snapshot");

        // 追蹤焦點
        TrackFocus();
    }

    // ========= Helpers =========

    void TrackFocus()
    {
        if (EventSystem.current == null) return;

        var cur = EventSystem.current.currentSelectedGameObject;
        if (cur == _lastSelected) return;

        _lastSelected = cur;

        if (!verboseFocusLog)
            return;

        if (Time.unscaledTime - _lastFocusLogTime < focusLogThrottleSec)
            return;

        _lastFocusLogTime = Time.unscaledTime;

        var name = cur != null ? GetPath(cur) : "(null)";
        Debug.Log($"[UIFlow][Focus] -> {name}");
    }

    public void PrintSnapshot(string reason)
    {
        string scene = SceneManager.GetActiveScene().name;

        string ms = ToState(menuSlide);
        string ow = ToState(optionsWrapperSlide);
        string os = ToState(optionsSlide);

        string mainActive = pageMain ? (pageMain.activeInHierarchy ? "On" : "Off") : "NA";
        string optActive = pageOptions ? (pageOptions.activeInHierarchy ? "On" : "Off") : "NA";

        string focused = "(null)";
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            focused = GetPath(EventSystem.current.currentSelectedGameObject);

        Debug.Log(
            $"[UIFlow][Snapshot] {reason} | Scene={scene} | Focus={focused} | " +
            $"Menu={ms} | OptWrap={ow} | Opt={os} | PageMain={mainActive} | PageOptions={optActive}"
        );
    }

    string ToState(IngameMenuSlide s)
    {
        if (s == null) return "NA";
        return s.IsOpen ? "Open" : "Close";
    }

    public static string GetPath(GameObject go)
    {
        if (go == null) return "(null)";
        var names = new List<string>();
        var t = go.transform;
        while (t != null)
        {
            names.Add(t.name);
            t = t.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }

    void AutoFindRefs()
    {
        // 不影響效能的小嘗試：用常見命名自動抓
        if (menuSlide == null)
        {
            var m = GameObject.Find("Panel_IngameMenu");
            if (m) menuSlide = m.GetComponent<IngameMenuSlide>();
        }
        if (optionsWrapperSlide == null)
        {
            var w = GameObject.Find("PageOptionsWrapper");
            if (w) optionsWrapperSlide = w.GetComponent<IngameMenuSlide>();
        }
        if (optionsSlide == null)
        {
            var o = GameObject.Find("Page_Options");
            if (o) optionsSlide = o.GetComponent<IngameMenuSlide>();
        }
        if (pageMain == null)
        {
            var pm = GameObject.Find("Page_Main");
            if (pm) pageMain = pm;
        }
        if (pageOptions == null)
        {
            var po = GameObject.Find("Page_Options");
            if (po) pageOptions = po;
        }
    }

    // 供你在外部想手動插入關鍵點時呼叫
    public void Mark(string tag)
    {
        PrintSnapshot(tag);
    }
}
