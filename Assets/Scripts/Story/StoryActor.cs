using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class StoryActor : MonoBehaviour
{
    [Header("識別")]
    public string actorId;              // 可對應 DialogueData.Line.speakerIds
    public string displayName;          // 可對應 Line.displayName
    public string groupId;              // 群體 ID（怪物群等），單體可留空
    public bool isGroupLeader = false;  // 同一群的第一個

    [Header("對焦點（可留空用自身）")]
    public Transform focusTarget;

    [Header("要被高亮的渲染器（自動抓）")]
    public Renderer[] renderers;

    static readonly List<StoryActor> s_all = new();
    public static IReadOnlyList<StoryActor> All => s_all;

    void Reset() { AutoCollectRenderers(); }
    void OnValidate() { if (renderers == null || renderers.Length == 0) AutoCollectRenderers(); }
    void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }
    void OnDisable() { s_all.Remove(this); }

    void AutoCollectRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        if (focusTarget == null) focusTarget = transform;
    }

    public static List<StoryActor> FindByIds(IList<string> ids)
    {
        var list = new List<StoryActor>();
        if (ids == null) return list;
        for (int i = 0; i < s_all.Count; i++)
            if (!string.IsNullOrEmpty(s_all[i].actorId) && ids.Contains(s_all[i].actorId))
                list.Add(s_all[i]);
        return list;
    }

    public static List<StoryActor> FindByDisplayName(string name)
    {
        var list = new List<StoryActor>();
        if (string.IsNullOrEmpty(name)) return list;
        for (int i = 0; i < s_all.Count; i++)
            if (s_all[i].displayName == name)
                list.Add(s_all[i]);
        return list;
    }

    public static List<StoryActor> FindByGroup(string gid)
    {
        var list = new List<StoryActor>();
        if (string.IsNullOrEmpty(gid)) return list;
        for (int i = 0; i < s_all.Count; i++)
            if (s_all[i].groupId == gid)
                list.Add(s_all[i]);
        return list;
    }

    public Transform FocusPoint => focusTarget != null ? focusTarget : transform;
}
