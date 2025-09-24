using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 劇情 UI 控制器：顯示姓名、台詞、下一句、跳過、自動、選項。
/// 掛在 Canvas_Story 上；使用 CanvasGroup 控制顯示/互動。
/// </summary>
public class StoryManager : MonoBehaviour
{
    [Header("UI 連線（Panel_Dialogue 底下）")]
    public CanvasGroup panel;              // Panel_Dialogue 的 CanvasGroup
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;
    public Button btnNext;
    public Button btnSkip;
    public Toggle toggleAuto;
    public Transform choicesPanel;         // 垂直群組的父物件
    public Button choiceButtonPrefab;      // 簡單的 Button + TMP

    [Header("打字機")]
    public float defaultCharsPerSec = 30f; // 當資料沒填或 <=0 時使用
    public KeyCode keyNext = KeyCode.Space;

    [Header("（選用）從 Inspector 觸發的預設資料")]
    public DialogueData initialData;
    public string initialStartNodeId = "start";

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
        {
            OnClickNext();
        }
    }

    // === 對外 API ===

    /// <summary>
    /// 方便在 Button.OnClick 直接綁定（不傳參數）。
    /// 先在 Inspector 指好 initialData / initialStartNodeId。
    /// </summary>
    public void StartStoryFromInspector()
    {
        if (initialData == null)
        {
            Debug.LogWarning("[Story] initialData 未指定。");
            return;
        }
        StartStory(initialData, initialStartNodeId);
    }

    /// <summary>
    /// 開始播放一段劇情；startNodeId 預設 "start"
    /// </summary>
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

        // 顯示 UI
        SetPanelVisible(true);

        IsPlaying = true;
        IsAuto = toggleAuto != null && toggleAuto.isOn;

        // 決定打字速度
        charsPerSec = (data.typewriterCharsPerSec > 0f) ? data.typewriterCharsPerSec : defaultCharsPerSec;

        // 找起點
        if (!map.TryGetValue(startNodeId, out current))
        {
            Debug.LogWarning($"[Story] 找不到起始節點: {startNodeId}");
            EndStory();
            return;
        }

        // 播第一句
        PlayCurrent();
    }

    /// <summary> 結束劇情並關閉 UI。 </summary>
    public void EndStory()
    {
        IsPlaying = false;
        IsTyping = false;

        if (typingCo != null) StopCoroutine(typingCo);
        ClearChoices();

        SetPanelVisible(false);
    }

    // === 主流程 ===

    private void PlayCurrent()
    {
        // 顯示姓名與清空文字
        if (nameText != null) nameText.text = current.displayName ?? "";
        if (contentText != null) contentText.text = "";

        // 清空舊選項
        ClearChoices();

        // 打字或直接顯示
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

        // 打完字：有選項就生成，否則顯示下一句按鈕 / 自動播放
        if (line.choices != null && line.choices.Count > 0)
        {
            ShowChoices(line.choices);
            if (btnNext != null) btnNext.gameObject.SetActive(false);
        }
        else
        {
            if (btnNext != null) btnNext.gameObject.SetActive(true);

            if (IsAuto)
            {
                float wait = Mathf.Max(0f, line.autoDelay);
                yield return new WaitForSeconds(wait);
                OnClickNext();
            }
        }
    }

    // === 選項 ===

    private void ShowChoices(List<DialogueData.Choice> choices)
    {
        ClearChoices();
        if (choicesPanel == null || choiceButtonPrefab == null) return;

        foreach (var c in choices)
        {
            var btn = Instantiate(choiceButtonPrefab, choicesPanel);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = c.text;

            string target = c.gotoNodeId;
            btn.onClick.AddListener(() => OnClickChoice(target));

            btn.gameObject.SetActive(true);
        }
    }

    private void ClearChoices()
    {
        if (choicesPanel == null) return;

        for (int i = choicesPanel.childCount - 1; i >= 0; i--)
        {
            Destroy(choicesPanel.GetChild(i).gameObject);
        }
    }

    // === 選項點擊 ===
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
    }

    // === 操作 ===

    public void OnClickNext()
    {
        if (!IsPlaying) return;

        // 若還在打字 → 先補滿
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
            return;
        }

        // 跳下一句或結束
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
    }

    public void OnClickSkip()
    {
        EndStory();
    }

    public void OnToggleAuto(bool on)
    {
        IsAuto = on;
    }

    // === 顯示/互動控制 ===

    private void SetPanelVisible(bool on)
    {
        if (panel == null) return;

        panel.alpha = on ? 1f : 0f;
        panel.interactable = on;
        panel.blocksRaycasts = on;

        if (btnNext != null) btnNext.gameObject.SetActive(false); // 進場先關，等打完再決定
        ClearChoices();
    }

    // 方便從外部直接跳某節點（可選）
    public void JumpTo(string nodeId)
    {
        if (!IsPlaying || data == null) return;
        if (map.TryGetValue(nodeId, out var line))
        {
            current = line;
            PlayCurrent();
        }
    }

    private void OnDisable()
    {
        if (typingCo != null) StopCoroutine(typingCo);
        IsPlaying = false;
        IsTyping = false;
    }
}
