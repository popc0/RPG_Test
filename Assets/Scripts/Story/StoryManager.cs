using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Rendering; // for SortingGroup

public class StoryManager : MonoBehaviour
{
    [Header("UI 連線（Panel_Dialogue 底下）")]
    public CanvasGroup panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;
    public Button btnNext;
    public Button btnSkip;
    public Toggle toggleAuto;
    public Transform choicesPanel;
    public Button choiceButtonPrefab;

    [Header("打字機")]
    public float defaultCharsPerSec = 30f;
    public KeyCode keyNext = KeyCode.Space;

    [Header("（選用）從 Inspector 觸發的預設資料")]
    public DialogueData initialData;
    public string initialStartNodeId = "start";

    [Header("聚焦設定")]
    public Selectable initialFocus;

    [Header("說話者置頂（可選）")]
    public bool raiseSpeaker = true;
    [Tooltip("置頂時使用的排序值（需高於場上其他物件）")]
    public int speakerTopOrder = 5000;

    [System.Serializable]
    public class ActorBinding
    {
        public string displayName;                  // 與 Line.displayName 對應
        public SortingGroup sortingGroup;          // 優先使用 SortingGroup
        public SpriteRenderer spriteRenderer;      // 沒有 SG 時用單一 SR
        [HideInInspector] public int originalOrder;
        [HideInInspector] public bool hasOriginal;
    }
    [Tooltip("把場上會說話的角色對應到顯示名與其 Renderer/SortingGroup")]
    public List<ActorBinding> actorBindings = new List<ActorBinding>();

    // 狀態
    public bool IsPlaying { get; private set; }
    public bool IsTyping { get; private set; }
    public bool IsAuto { get; private set; }

    // 內部資料
    private DialogueData data;
    private DialogueData.Line current;
    private Dictionary<string, DialogueData.Line> map = new Dictionary<string, DialogueData.Line>();
    private Coroutine typingCo;
    private float charsPerSec;
    private ActorBinding raisedNow; // 目前被置頂的演員

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

        if (Input.GetKeyDown(keyNext) && btnNext != null && btnNext.gameObject.activeInHierarchy)
            OnClickNext();
    }

    // ====== API ======

    public void StartStoryFromInspector()
    {
        if (initialData == null)
        {
            Debug.LogWarning("[Story] initialData 未指定。");
            return;
        }
        StartStory(initialData, initialStartNodeId);
    }

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
            if (string.IsNullOrEmpty(l.nodeId)) continue;
            if (!map.ContainsKey(l.nodeId)) map.Add(l.nodeId, l);
        }

        SetPanelVisible(true);

        IsPlaying = true;
        IsAuto = toggleAuto != null && toggleAuto.isOn;
        charsPerSec = (data.typewriterCharsPerSec > 0f) ? data.typewriterCharsPerSec : defaultCharsPerSec;

        if (!map.TryGetValue(startNodeId, out current))
        {
            Debug.LogWarning($"[Story] 找不到起始節點: {startNodeId}");
            EndStory();
            return;
        }

        PlayCurrent();

        var target = initialFocus != null ? initialFocus : (btnNext != null ? btnNext as Selectable : null);
        DeferFocus(target);
    }

    public void EndStory()
    {
        IsPlaying = false;
        IsTyping = false;

        if (typingCo != null) StopCoroutine(typingCo);
        ClearChoices();
        ClearSelected();

        // 還原置頂
        RestoreRaised();

        SetPanelVisible(false);
    }

    // ====== 流程 ======

    private void PlayCurrent()
    {
        if (nameText != null) nameText.text = current.displayName ?? "";
        if (contentText != null) contentText.text = "";

        // 說話者置頂
        if (raiseSpeaker) RaiseSpeaker(current.displayName);

        ClearChoices();

        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(Typewriter(current));
    }

    private IEnumerator Typewriter(DialogueData.Line line)
    {
        IsTyping = true;

        string text = line.content ?? "";
        float cps = (data != null && data.typewriterCharsPerSec > 0f) ? data.typewriterCharsPerSec : defaultCharsPerSec;

        if (cps <= 0f)
        {
            if (contentText != null) contentText.text = text;
            IsTyping = false;
        }
        else
        {
            float tPerChar = 1f / cps;
            for (int i = 0; i < text.Length; i++)
            {
                if (contentText != null) contentText.text = text.Substring(0, i + 1);
                yield return new WaitForSeconds(tPerChar);
            }
            IsTyping = false;
        }

        if (line.choices != null && line.choices.Count > 0)
        {
            ShowChoices(line.choices);
            if (btnNext != null) btnNext.gameObject.SetActive(false);
        }
        else
        {
            if (btnNext != null) btnNext.gameObject.SetActive(true);
            Focus(btnNext);

            if (IsAuto)
            {
                float wait = Mathf.Max(0f, line.autoDelay);
                yield return new WaitForSeconds(wait);
                OnClickNext();
            }
        }
    }

    private void ShowChoices(List<DialogueData.Choice> choices)
    {
        ClearChoices();
        if (choicesPanel == null || choiceButtonPrefab == null) return;

        Button first = null;

        foreach (var c in choices)
        {
            var btn = Instantiate(choiceButtonPrefab, choicesPanel);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = c.text;

            string target = c.gotoNodeId;
            btn.onClick.AddListener(() => OnClickChoice(target));
            btn.gameObject.SetActive(true);

            if (first == null) first = btn;
        }

        if (first != null) DeferFocus(first);
    }

    private void ClearChoices()
    {
        if (choicesPanel == null) return;
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
            Debug.LogWarning($"[Story] 選項跳轉失敗，找不到節點: {targetNodeId}");
            EndStory();
            return;
        }

        PlayCurrent();
        if (btnNext != null) DeferFocus(btnNext);
    }

    public void OnClickNext()
    {
        if (!IsPlaying) return;

        if (IsTyping)
        {
            IsTyping = false;
            if (typingCo != null) StopCoroutine(typingCo);
            if (contentText != null && current != null) contentText.text = current.content;

            if (current.choices != null && current.choices.Count > 0)
            {
                ShowChoices(current.choices);
                if (btnNext != null) btnNext.gameObject.SetActive(false);
            }
            else
            {
                Focus(btnNext);
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
            Debug.LogWarning($"[Story] 找不到節點: {nextId}");
            EndStory();
            return;
        }

        PlayCurrent();
        if (btnNext != null) DeferFocus(btnNext);
    }

    public void OnClickSkip() => EndStory();
    public void OnToggleAuto(bool on) => IsAuto = on;

    private void SetPanelVisible(bool on)
    {
        if (panel == null) return;
        panel.alpha = on ? 1f : 0f;
        panel.interactable = on;
        panel.blocksRaycasts = on;

        if (btnNext != null) btnNext.gameObject.SetActive(false);
        ClearChoices();
    }

    public void JumpTo(string nodeId)
    {
        if (!IsPlaying || data == null) return;
        if (map.TryGetValue(nodeId, out var line))
        {
            current = line;
            PlayCurrent();
            if (btnNext != null) DeferFocus(btnNext);
        }
    }

    void OnDisable()
    {
        if (typingCo != null) StopCoroutine(typingCo);
        IsPlaying = false;
        IsTyping = false;

        RestoreRaised();
        ClearSelected();
    }

    // ====== 聚焦工具 ======

    void Focus(Selectable s)
    {
        if (s == null) return;
        var es = EventSystem.current;
        if (es == null) return;

        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(s.gameObject);
    }

    void DeferFocus(Selectable s)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(DeferFocusCo(s));
    }
    IEnumerator DeferFocusCo(Selectable s) { yield return null; Focus(s); }
    void ClearSelected() { var es = EventSystem.current; if (es != null) es.SetSelectedGameObject(null); }

    // ====== 說話者置頂 ======

    void RaiseSpeaker(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) { RestoreRaised(); return; }

        // 找對應演員
        ActorBinding target = null;
        for (int i = 0; i < actorBindings.Count; i++)
        {
            if (actorBindings[i] != null && actorBindings[i].displayName == displayName)
            {
                target = actorBindings[i];
                break;
            }
        }

        if (target == null) { RestoreRaised(); return; }
        if (raisedNow == target) return;

        // 先還原舊的
        RestoreRaised();

        // 記錄並置頂
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
}
