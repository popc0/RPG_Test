using System.Collections.Generic;
using UnityEngine;

public class StoryPresenter : MonoBehaviour
{
    public StoryManager story;                    // 指向你的 StoryManager 物件
    public HighlightServiceEmission highlighter;  // 上面那個
    public ScreenDimService screenDim;            // 上面那個
    public CameraFocusService cameraFocus;        // 上面那個

    [Header("行為參數")]
    public bool dimOnNarration = true;
    public bool focusSpeakerIfOffscreen = true;

    void Reset()
    {
        story = FindObjectOfType<StoryManager>(true);
        highlighter = FindObjectOfType<HighlightServiceEmission>(true);
        screenDim = FindObjectOfType<ScreenDimService>(true);
        cameraFocus = FindObjectOfType<CameraFocusService>(true);
    }

    void OnEnable()
    {
        if (story != null)
        {
            story.OnLineStart += HandleLineStart;
            story.OnStoryEnd += HandleStoryEnd;
        }
    }

    void OnDisable()
    {
        if (story != null)
        {
            story.OnLineStart -= HandleLineStart;
            story.OnStoryEnd -= HandleStoryEnd;
        }
    }

    void HandleStoryEnd()
    {
        if (highlighter) highlighter.ClearAll();
        if (screenDim && dimOnNarration) screenDim.SetDim(false);
    }

    void HandleLineStart(DialogueData.Line line)
    {
        if (line == null) return;

        // 1) 旁白：整畫面變暗、清除高亮、不要移鏡頭
        bool isNarration = string.IsNullOrEmpty(line.displayName);
        if (isNarration)
        {
            if (highlighter) highlighter.ClearAll();
            if (screenDim && dimOnNarration) screenDim.SetDim(true);
            return;
        }

        // 非旁白：解除暗場
        if (screenDim && dimOnNarration) screenDim.SetDim(false);

        // 2) 找說話者（群體優先，其次 displayName）
        var targets = new List<StoryActor>();
        if (line.speakerIds != null && line.speakerIds.Count > 0)
            targets.AddRange(StoryActor.FindByIds(line.speakerIds));

        if (targets.Count == 0 && !string.IsNullOrEmpty(line.displayName))
            targets.AddRange(StoryActor.FindByDisplayName(line.displayName));

        // 找不到：直接略過（只顯示對話）
        if (targets.Count == 0) { if (highlighter) highlighter.ClearAll(); return; }

        // 3) 高亮全部（群體）
        if (highlighter)
        {
            highlighter.ClearAll();
            highlighter.SetHighlightedMany(targets, true);
        }

        // 4) 對焦：群體第一個（Leader > 第一個），不在畫面才移
        StoryActor leader = null;
        for (int i = 0; i < targets.Count; i++) if (targets[i].isGroupLeader) { leader = targets[i]; break; }
        if (leader == null) leader = targets[0];

        if (cameraFocus != null && focusSpeakerIfOffscreen)
        {
            var t = leader != null ? leader.FocusPoint : targets[0].FocusPoint;
            if (!CameraFocusService.IsVisibleByMainCamera(t))
                cameraFocus.Focus(t);
        }
    }
}
