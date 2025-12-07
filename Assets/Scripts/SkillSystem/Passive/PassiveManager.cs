using UnityEngine;
using System.Collections.Generic;
using RPG;

public class PassiveManager : MonoBehaviour
{
    [System.Serializable]
    public class PassiveSkillGroup
    {
        public string groupName = "被動組";
        public List<SkillData> slots = new List<SkillData>();
    }

    [Header("被動技能組設定")]
    public List<PassiveSkillGroup> passiveGroups = new List<PassiveSkillGroup>();

    [Tooltip("UI 當前顯示/編輯的群組索引")]
    public int currentGroupIndex = 0;

    [Tooltip("當前實際生效(執行中)的群組索引，-1 表示無")]
    public int appliedGroupIndex = -1;

    [Tooltip("每一組的最大格數")]
    public int maxSlots = 5;

    // 用來緩存當前已生效的技能，方便在切換時精確移除舊效果
    private List<SkillData> _activeSkillsCache = new List<SkillData>();

    // 取得當前編輯中的群組 (UI用)
    public PassiveSkillGroup CurrentEditingGroup
    {
        get
        {
            if (passiveGroups == null || passiveGroups.Count == 0) return null;
            int idx = Mathf.Clamp(currentGroupIndex, 0, passiveGroups.Count - 1);
            return passiveGroups[idx];
        }
    }

    void Awake()
    {
        if (passiveGroups.Count == 0) AddNewGroup();
        ValidateGroupSlots();
    }

    void Start()
    {
        // 遊戲開始時，如果存檔有紀錄生效的組別，就執行套用
        if (appliedGroupIndex >= 0 && appliedGroupIndex < passiveGroups.Count)
        {
            // 強制刷新一次以確保效果啟動
            ApplyGroup(appliedGroupIndex, true);
        }
    }

    // ============================================================
    //  核心功能：套用與切換
    // ============================================================

    /// <summary>
    /// UI 呼叫：將「當前編輯」的群組設為「生效群組」
    /// </summary>
    public void ApplyCurrentEditingGroup()
    {
        ApplyGroup(currentGroupIndex);
    }

    /// <summary>
    /// 執行切換邏輯：移除舊組效果 -> 設定新組 -> 套用新組效果
    /// </summary>
    public void ApplyGroup(int index, bool forceRefresh = false)
    {
        if (index < 0 || index >= passiveGroups.Count) return;

        // 如果目標組已經是生效組，且不強制刷新，則不動作 (避免重複觸發)
        if (!forceRefresh && index == appliedGroupIndex)
        {
            Debug.Log($"[PassiveManager] 第 {index + 1} 組已經是生效狀態。");
            return;
        }

        // 1. 切掉之前的被動技能 (移除舊效果)
        UnapplyCurrentEffects();

        // 2. 更新生效索引
        appliedGroupIndex = index;

        // 3. 換新的被動技能執行 (套用新效果)
        var group = passiveGroups[appliedGroupIndex];
        foreach (var skill in group.slots)
        {
            if (skill != null)
            {
                ApplySkillEffect(skill);
                _activeSkillsCache.Add(skill); // 加入緩存以便未來移除
            }
        }

        Debug.Log($"[PassiveManager] 已切換並套用被動組：{group.groupName}");
    }

    // 移除當前所有生效的效果
    private void UnapplyCurrentEffects()
    {
        foreach (var skill in _activeSkillsCache)
        {
            if (skill != null)
            {
                RemoveSkillEffect(skill);
            }
        }
        _activeSkillsCache.Clear();
    }

    // ============================================================
    //  狀態效果執行 (Action Status)
    //  未來這裡會連接 StatusManager
    // ============================================================

    private void ApplySkillEffect(SkillData skill)
    {
        // 被動技能的常駐效果放在 ActingStatusEffects
        if (skill.UseActingStatus && skill.ActingStatusEffects != null)
        {
            foreach (var effect in skill.ActingStatusEffects)
            {
                if (effect == null) continue;

                // ★ 待辦： StatusManager.Instance.Apply(effect, this.gameObject);
                Debug.Log($"[PassiveManager] 啟用被動效果: {effect.name} (來源: {skill.SkillName})");
            }
        }
    }

    private void RemoveSkillEffect(SkillData skill)
    {
        if (skill.UseActingStatus && skill.ActingStatusEffects != null)
        {
            foreach (var effect in skill.ActingStatusEffects)
            {
                if (effect == null) continue;

                // ★ 待辦： StatusManager.Instance.Remove(effect, this.gameObject);
                Debug.Log($"[PassiveManager] 移除被動效果: {effect.name} (來源: {skill.SkillName})");
            }
        }
    }

    // ============================================================
    //  編輯與管理 (UI 編輯用)
    // ============================================================

    // 切換編輯視角 (不影響實際生效的組，除非按套用)
    public void SwitchToNextGroup()
    {
        if (passiveGroups.Count <= 1) return;
        currentGroupIndex = (currentGroupIndex + 1) % passiveGroups.Count;
    }

    public void AddNewGroup()
    {
        var newGroup = new PassiveSkillGroup();
        for (int i = 0; i < maxSlots; i++) newGroup.slots.Add(null);

        int insertIndex = (passiveGroups.Count == 0) ? 0 : currentGroupIndex + 1;
        if (insertIndex < passiveGroups.Count) passiveGroups.Insert(insertIndex, newGroup);
        else passiveGroups.Add(newGroup);

        RefreshGroupNames();
        currentGroupIndex = insertIndex; // 切換編輯視角到新組
    }

    public void RemoveCurrentGroup()
    {
        if (passiveGroups.Count <= 1) return;

        // 如果刪除的是當前生效的組，先移除效果
        if (currentGroupIndex == appliedGroupIndex)
        {
            UnapplyCurrentEffects();
            appliedGroupIndex = -1; // 暫時無生效
        }
        else if (currentGroupIndex < appliedGroupIndex)
        {
            appliedGroupIndex--; // 修正索引偏移
        }

        passiveGroups.RemoveAt(currentGroupIndex);
        RefreshGroupNames();

        if (currentGroupIndex >= passiveGroups.Count) currentGroupIndex = passiveGroups.Count - 1;
    }

    // 裝備技能到「當前編輯組」
    public bool EquipToCurrent(SkillData skill, int slotIndex)
    {
        var group = CurrentEditingGroup;
        if (group == null) return false;
        if (slotIndex < 0 || slotIndex >= group.slots.Count) return false;

        // 防重複邏輯
        if (skill != null)
        {
            int existingIndex = group.slots.IndexOf(skill);
            if (existingIndex != -1 && existingIndex != slotIndex)
                group.slots[existingIndex] = null;
        }

        group.slots[slotIndex] = skill;

        // ★ 自動刷新：如果正在編輯的這組「剛好是生效組」，則即時刷新效果
        if (currentGroupIndex == appliedGroupIndex)
        {
            ApplyGroup(appliedGroupIndex, true);
        }

        return true;
    }

    public void UnequipFromCurrent(int slotIndex)
    {
        EquipToCurrent(null, slotIndex);
    }

    private void ValidateGroupSlots()
    {
        foreach (var group in passiveGroups)
        {
            while (group.slots.Count < maxSlots) group.slots.Add(null);
        }
    }

    private void RefreshGroupNames()
    {
        for (int i = 0; i < passiveGroups.Count; i++)
            passiveGroups[i].groupName = $"被動組 {i + 1}";
    }
}