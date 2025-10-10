using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public enum UIState
{
    None,
    MainMenu,
    IngameMenu,
    Options,
    HUD,
    Dialogue
}

/// <summary>
/// 負責全域 UI 狀態切換與面板啟用/停用。
/// 確保同時間只有一個主要 UI 狀態是開啟的。
/// </summary>
public class UIStateManager : MonoBehaviour
{
    [System.Serializable]
    public class PanelConfig
    {
        public UIState state;
        public GameObject root;           // 該狀態對應的根物件（Canvas 或 Panel）
        public Selectable defaultFocus;   // 預設聚焦按鈕
        public bool hideWhenInactive = true; // 關閉時是否隱藏
    }

    public static UIStateManager Instance { get; private set; }

    [Header("設定每個 UI 狀態的面板")]
    public List<PanelConfig> panels = new List<PanelConfig>();

    [Header("狀態標籤 (非必要)")]
    public TextMeshProUGUI stateLabelTMP;
    public Text stateLabelUGUI;

    public UnityEvent<UIState> OnStateChanged;

    public UIState CurrentState { get; private set; } = UIState.None;

    private Dictionary<UIState, PanelConfig> _panelMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var p in panels)
        {
            if (!_panelMap.ContainsKey(p.state))
                _panelMap.Add(p.state, p);
        }

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 切換 UI 狀態，只開啟目標面板。
    /// </summary>
    public void SwitchUI(UIState newState)
    {
        if (newState == CurrentState)
            return;

        // 關閉所有面板
        foreach (var p in _panelMap.Values)
        {
            if (p.root != null)
                SetPanelActive(p.root, false, p.hideWhenInactive);
        }

        // 開啟目標面板
        if (_panelMap.TryGetValue(newState, out var config) && config.root != null)
        {
            SetPanelActive(config.root, true, config.hideWhenInactive);
            if (config.defaultFocus != null)
                SelectObject(config.defaultFocus);
        }

        CurrentState = newState;
        UpdateStateLabel();

        OnStateChanged?.Invoke(CurrentState);
    }

    private void SetPanelActive(GameObject obj, bool active, bool hide)
    {
        if (obj == null) return;

        var group = obj.GetComponent<CanvasGroup>();
        if (group == null) group = obj.AddComponent<CanvasGroup>();

        if (active)
        {
            obj.SetActive(true);
            group.alpha = 1;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        else
        {
            group.interactable = false;
            group.blocksRaycasts = false;
            if (hide) group.alpha = 0;
        }
    }

    private void UpdateStateLabel()
    {
        if (stateLabelTMP != null)
            stateLabelTMP.text = $"State: {CurrentState}";
        if (stateLabelUGUI != null)
            stateLabelUGUI.text = $"State: {CurrentState}";
    }

    private void SelectObject(Selectable s)
    {
        if (s == null) return;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(s.gameObject);
    }
}
