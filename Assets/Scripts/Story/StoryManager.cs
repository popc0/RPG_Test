using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.InputSystem; // ★ 新增

public class StoryManager : MonoBehaviour
{
    [Header("UI 元件")]
    public CanvasGroup panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;
    public Button btnNext;
    public Button btnSkip;
    public Toggle toggleAuto;
    public Transform choicesPanel;
    public Button choiceButtonPrefab;

    [Header("打字機設定")]
    public float defaultCharsPerSec = 30f;

    [Header("輸入控制")]
    public Key keyNext = Key.Space;

    [Header("預設對話資料（可空）")]
    public DialogueData initialData;
    public string initialStartNodeId = "start";

    [Header("說話者排序設定")]
    public bool raiseSpeaker = true;
    public int speakerTopOrder = 5000;

    [System.Serializable]
    public class ActorBinding
    {
        public string displayName;
        public SortingGroup sortingGroup;
        public SpriteRenderer spriteRenderer;
        [HideInInspector] public int originalOrder;
        [HideInInspector] public bool hasOriginal;
    }
    public List<ActorBinding> actorBindings = new List<ActorBinding>();

    // 狀態
    public bool IsPlaying { get; private set; }
    public bool IsTyping { get; private set; }
    public bool IsAuto { get; private set; }

    // 事件
    public event Action OnStoryStart;
    public event Action<DialogueData.Line> OnLineStart;
    public event Action OnStoryEnd;

    // 內部資料
    private DialogueData data;
    private DialogueData.Line current;
    private Dictionary<string, DialogueData.Line> map = new Dictionary<string, DialogueData.Line>();
    private Coroutine typingCo;
    private float charsPerSec;
    private ActorBinding raisedNow;

    void Awake()
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }

        if (btnNext != null) btnNext.onClick.AddListener(OnClickNext);
        if (btnSkip != null) btnSkip.onClick.AddListener(OnClickSkip);
        if (toggleAuto != null) toggleAuto.onValueChanged.AddListener(OnToggleAuto);

        ClearChoices();
    }

    void Update()
    {
        if (!IsPlaying) return;

        var kb = Keyboard.current;
        if (kb != null && kb[keyNext].wasPressedThisFrame)
        {
            OnPressNextKey();
        }
    }

    // -------------------------------------------------------------
    // 對話啟動 / 結束
    // -------------------------------------------------------------
    public void StartStory(DialogueData dialogue, string startNodeId = "start")
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Count == 0)
        {
            Debug.LogWarning("[Story] DialogueData 為空。");
            return;
        }

        data = dialogue;
        map.Clear();
        foreach (var l in data.lines)
        {
            if (!string.IsNullOrEmpty(l.nodeId) && !map.ContainsKey(l.nodeId))
                map.Add(l.nodeId, l);
        }

        var mux = FindObjectOfType<SystemLayerMux>();
        if (mux != null) mux.OpenStory();

        SetPanelVisible(true);

        IsPlaying = true;
        IsAuto = toggleAuto != null && toggleAuto.isOn;
        charsPerSec = (data.typewriterCharsPerSec > 0f) ? data.typewriterCharsPerSec : defaultCharsPerSec;

        if (!map.TryGetValue(startNodeId, out current))
        {
            Debug.LogWarning("[Story] 找不到節點: " + startNodeId);
            EndStory();
            return;
        }

        OnStoryStart?.Invoke();
        PlayCurrent();
    }

    public void EndStory()
    {
        IsPlaying = false;
        IsTyping = false;
        if (typingCo != null) StopCoroutine(typingCo);
        ClearChoices();
        RestoreRaised();
        SetPanelVisible(false);
        OnStoryEnd?.Invoke();

        var mux = FindObjectOfType<SystemLayerMux>();
        if (mux != null) mux.NotifyChildClosed();
    }

    // -------------------------------------------------------------
    // 主流程
    // -------------------------------------------------------------
    private void PlayCurrent()
    {
        if (nameText != null) nameText.text = current.displayName ?? "";
        if (contentText != null) contentText.text = "";

        OnLineStart?.Invoke(current);

        if (raiseSpeaker) RaiseSpeaker(current.displayName);
        ClearChoices();

        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(Typewriter(current));
    }

    private IEnumerator Typewriter(DialogueData.Line line)
    {
        IsTyping = true;

        string text = line.content ?? "";
        float cps = (data != null && data.typewriterCharsPerSec > 0f)
            ? data.typewriterCharsPerSec : defaultCharsPerSec;

        if (cps <= 0f)
        {
            if (contentText != null) contentText.text = text;
            IsTyping = false;
        }
        else
        {
            float tPerChar = 1f / cps;
            float timer = 0f;
            int index = 0;
            while (index < text.Length)
            {
                timer += Time.unscaledDeltaTime;
                while (timer >= tPerChar && index < text.Length)
                {
                    timer -= tPerChar;
                    index++;
                    if (contentText != null)
                        contentText.text = text.Substring(0, index);
                }
                yield return null;
            }
            IsTyping = false;
        }

        if (line.choices != null && line.choices.Count > 0)
        {
            // 有選項：維持原本邏輯 → 顯示選項並聚焦到第一個
            ShowChoices(line.choices);
            if (btnNext != null) btnNext.gameObject.SetActive(false);
        }
        else
        {
            // 無選項：顯示 Next 並自動聚焦 Next
            if (btnNext != null)
            {
                btnNext.gameObject.SetActive(true);
                StartCoroutine(FocusNextButtonNextFrame()); // ★ 新增：下一幀聚焦到 Next
            }

            if (IsAuto)
            {
                float wait = Mathf.Max(0f, line.autoDelay);
                float t = 0f;
                while (t < wait)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                OnClickNext();
            }
        }
    }

    // -------------------------------------------------------------
    // 空白鍵輸入
    // -------------------------------------------------------------
    void OnPressNextKey()
    {
        bool showingChoices = (choicesPanel != null && choicesPanel.childCount > 0);
        if (showingChoices) return;
        OnClickNext();
    }

    public void OnClickNext()
    {
        if (!IsPlaying) return;

        if (IsTyping)
        {
            IsTyping = false;
            if (typingCo != null) StopCoroutine(typingCo);
            if (contentText != null && current != null)
                contentText.text = current.content;

            if (current.choices != null && current.choices.Count > 0)
            {
                ShowChoices(current.choices);
                if (btnNext != null) btnNext.gameObject.SetActive(false);
            }
            else
            {
                if (btnNext != null) btnNext.gameObject.SetActive(true);
                //（此分支是「點 Next 直接跳過打字機」的情況，不主動改焦點，
                // 讓現有焦點保持在 Next，以免來回閃爍）
            }
            return;
        }

        string nextId = (current != null) ? current.nextNodeId : "";
        if (string.IsNullOrEmpty(nextId))
        {
            EndStory();
            return;
        }

        if (!map.TryGetValue(nextId, out current))
        {
            Debug.LogWarning("[Story] 找不到節點: " + nextId);
            EndStory();
            return;
        }

        PlayCurrent();
    }

    // -------------------------------------------------------------
    // 選項
    // -------------------------------------------------------------
    private void ShowChoices(List<DialogueData.Choice> choices)
    {
        ClearChoices();
        if (choicesPanel == null || choiceButtonPrefab == null) return;

        foreach (var c in choices)
        {
            var btn = Instantiate(choiceButtonPrefab, choicesPanel);

            // 確保可被 EventSystem 導覽
            var nav = btn.navigation;
            if (nav.mode == Navigation.Mode.None)
            {
                nav.mode = Navigation.Mode.Automatic;
                btn.navigation = nav;
            }

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = c.text;

            string target = c.gotoNodeId;
            btn.onClick.AddListener(() => OnClickChoice(target));
            btn.gameObject.SetActive(true);
        }

        FocusFirstChoiceNextFrame();
    }

    private void ClearChoices()
    {
        if (choicesPanel == null) return;

        // 清除現有選中，避免殘留到不存在的按鈕
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        for (int i = choicesPanel.childCount - 1; i >= 0; i--)
            Destroy(choicesPanel.GetChild(i).gameObject);
    }

    private void OnClickChoice(string targetNodeId)
    {
        if (!IsPlaying) return;
        if (string.IsNullOrEmpty(targetNodeId))
        {
            EndStory();
            return;
        }
        if (!map.TryGetValue(targetNodeId, out current))
        {
            Debug.LogWarning("[Story] 找不到節點: " + targetNodeId);
            EndStory();
            return;
        }
        PlayCurrent();
    }

    // -------------------------------------------------------------
    // 其他
    // -------------------------------------------------------------
    public void OnClickSkip() => EndStory();
    public void OnToggleAuto(bool on) => IsAuto = on;

    private void SetPanelVisible(bool on)
    {
        if (panel == null) return;
        panel.alpha = on ? 1f : 0f;
        panel.interactable = on;
        panel.blocksRaycasts = on;

        if (btnNext != null) btnNext.gameObject.SetActive(false);

        // 清掉焦點，避免殘留到不可見的物件
        if (!on && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        ClearChoices();
    }

    void RaiseSpeaker(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) { RestoreRaised(); return; }
        ActorBinding target = null;
        foreach (var ab in actorBindings)
        {
            if (ab.displayName == displayName)
            {
                target = ab;
                break;
            }
        }
        if (target == null) { RestoreRaised(); return; }
        if (raisedNow == target) return;
        RestoreRaised();

        if (target.sortingGroup != null)
        {
            if (!target.hasOriginal)
            {
                target.originalOrder = target.sortingGroup.sortingOrder;
                target.hasOriginal = true;
            }
            target.sortingGroup.sortingOrder = speakerTopOrder;
            raisedNow = target;
        }
        else if (target.spriteRenderer != null)
        {
            if (!target.hasOriginal)
            {
                target.originalOrder = target.spriteRenderer.sortingOrder;
                target.hasOriginal = true;
            }
            target.spriteRenderer.sortingOrder = speakerTopOrder;
            raisedNow = target;
        }
    }

    void RestoreRaised()
    {
        if (raisedNow == null) return;

        if (raisedNow.sortingGroup != null && raisedNow.hasOriginal)
            raisedNow.sortingGroup.sortingOrder = raisedNow.originalOrder;
        else if (raisedNow.spriteRenderer != null && raisedNow.hasOriginal)
            raisedNow.spriteRenderer.sortingOrder = raisedNow.originalOrder;

        raisedNow = null;
    }

    // -------------------------------------------------------------
    // 聚焦輔助
    // -------------------------------------------------------------
    void FocusFirstChoiceNextFrame()
    {
        StartCoroutine(CoFocusFirstChoice());
    }

    IEnumerator CoFocusFirstChoice()
    {
        // 等一幀讓 Layout 與 GraphicRaycaster 準備完成
        yield return null;

        if (choicesPanel != null && choicesPanel.childCount > 0)
        {
            var first = choicesPanel.GetChild(0).gameObject;
            var es = EventSystem.current;
            if (es != null)
            {
                es.SetSelectedGameObject(null);
                es.SetSelectedGameObject(first);

                var sel = first.GetComponent<Selectable>();
                if (sel != null) sel.Select();
            }
        }
    }

    // ★ 新增：每句普通對話（無選項）結束後，下一幀聚焦 Next 按鈕
    IEnumerator FocusNextButtonNextFrame()
    {
        yield return null; // 等一幀，確保 Layout / GraphicRaycaster 更新完成
        if (btnNext != null && btnNext.gameObject.activeInHierarchy && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btnNext.gameObject);

            var sel = btnNext.GetComponent<Selectable>();
            if (sel != null) sel.Select();
        }
    }
}
