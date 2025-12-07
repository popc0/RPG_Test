using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG;

public class PagePassive : MonoBehaviour
{
    [Header("核心引用")]
    public PassiveManager passiveManager;

    [Header("技能庫 (左側)")]
    public Transform libraryContent;
    public List<SkillData> passiveLibrary;
    public UISkillSlot librarySlotPrefab;

    [Header("裝備槽 (右側)")]
    public Transform equipContent;
    public UISkillSlot equipSlotPrefab;

    [Header("群組控制")]
    public Button btnSwitchGroup;
    public Button btnAddGroup;
    public Button btnRemoveGroup;

    [Header("套用控制 (新增)")]
    public Button btnApply;                // ★ 套用按鈕

    [Header("資訊顯示")]
    public TextMeshProUGUI groupNameText;  // 顯示 "被動組 1" (編輯中)
    public TextMeshProUGUI groupCountText; // 顯示 "1 / 3"
    public TextMeshProUGUI appliedGroupText; // ★ 顯示 "目前生效: 被動組 1"

    // 內部狀態
    private SkillData _pendingSkill;
    private List<UISkillSlot> _libSlots = new List<UISkillSlot>();
    private List<UISkillSlot> _equipSlots = new List<UISkillSlot>();

    private int _lastEditingIndex = -1;
    private int _lastAppliedIndex = -1;
    private int _lastGroupCount = -1;

    void Start()
    {
        GenerateLibrary();

        if (btnSwitchGroup) btnSwitchGroup.onClick.AddListener(OnClickSwitch);
        if (btnAddGroup) btnAddGroup.onClick.AddListener(OnClickAdd);
        if (btnRemoveGroup) btnRemoveGroup.onClick.AddListener(OnClickRemove);

        // ★ 綁定套用按鈕
        if (btnApply) btnApply.onClick.AddListener(OnClickApply);

        StartCoroutine(AutoBindPlayerRoutine());
    }

    void OnEnable()
    {
        _pendingSkill = null;
        RefreshLibraryVisuals();
        _lastEditingIndex = -1; // 強制刷新
        CheckAndRefresh();
    }

    void Update()
    {
        CheckAndRefresh();
    }

    void CheckAndRefresh()
    {
        if (!passiveManager) return;

        // 偵測：編輯索引變更 OR 生效索引變更 OR 群組數量變更
        if (passiveManager.currentGroupIndex != _lastEditingIndex ||
            passiveManager.appliedGroupIndex != _lastAppliedIndex ||
            passiveManager.passiveGroups.Count != _lastGroupCount)
        {
            _lastEditingIndex = passiveManager.currentGroupIndex;
            _lastAppliedIndex = passiveManager.appliedGroupIndex; // 記錄生效索引
            _lastGroupCount = passiveManager.passiveGroups.Count;

            RefreshEquippedView();
        }
    }

    IEnumerator AutoBindPlayerRoutine()
    {
        while (passiveManager == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) passiveManager = p.GetComponentInChildren<PassiveManager>(true);

            if (passiveManager)
            {
                GenerateEquipSlots();
                RefreshEquippedView();
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // --- 按鈕事件 ---
    void OnClickSwitch() { if (passiveManager) passiveManager.SwitchToNextGroup(); }
    void OnClickAdd() { if (passiveManager) passiveManager.AddNewGroup(); }
    void OnClickRemove() { if (passiveManager) passiveManager.RemoveCurrentGroup(); }

    // ★ 套用當前組
    void OnClickApply()
    {
        if (passiveManager)
        {
            passiveManager.ApplyCurrentEditingGroup();
            RefreshEquippedView(); // 立即刷新 UI 文字
        }
    }

    // --- UI 邏輯 ---

    void GenerateEquipSlots()
    {
        if (!passiveManager) return;
        foreach (Transform child in equipContent) Destroy(child.gameObject);
        _equipSlots.Clear();

        for (int i = 0; i < passiveManager.maxSlots; i++)
        {
            var slot = Instantiate(equipSlotPrefab, equipContent);
            slot.Setup(null);
            int index = i;
            slot.OnClick = (s) => OnEquipSlotClicked(index);
            _equipSlots.Add(slot);
        }
    }

    void RefreshEquippedView()
    {
        if (!passiveManager) return;

        var editingGroup = passiveManager.CurrentEditingGroup;

        // 1. 更新編輯中的群組資訊
        if (editingGroup != null)
        {
            if (groupNameText) groupNameText.text = editingGroup.groupName;
            if (groupCountText) groupCountText.text = $"{passiveManager.currentGroupIndex + 1} / {passiveManager.passiveGroups.Count}";
        }

        // 2. 更新「目前生效」的文字
        if (appliedGroupText)
        {
            int activeIdx = passiveManager.appliedGroupIndex;
            if (activeIdx >= 0 && activeIdx < passiveManager.passiveGroups.Count)
            {
                string activeName = passiveManager.passiveGroups[activeIdx].groupName;
                appliedGroupText.text = $"目前生效: {activeName}";
                appliedGroupText.color = Color.green; // 可選：生效時顯示綠色
            }
            else
            {
                appliedGroupText.text = "目前生效: 無";
                appliedGroupText.color = Color.gray;
            }
        }

        // 3. 控制 Apply 按鈕狀態 (可選：如果正在看的就是生效組，可以讓按鈕變灰，或是保持可按當作刷新)
        if (btnApply)
        {
            // 這裡我保持開啟，讓玩家隨時可以點擊確認
            btnApply.interactable = true;
        }

        // 4. 填入格子
        if (_equipSlots.Count != passiveManager.maxSlots) GenerateEquipSlots();

        for (int i = 0; i < _equipSlots.Count; i++)
        {
            SkillData skill = null;
            if (editingGroup != null && i < editingGroup.slots.Count)
            {
                skill = editingGroup.slots[i];
            }
            _equipSlots[i].Setup(skill);
            _equipSlots[i].SetSelected(false);
        }
    }

    void OnEquipSlotClicked(int index)
    {
        if (!passiveManager) return;

        if (_pendingSkill != null)
            passiveManager.EquipToCurrent(_pendingSkill, index);
        else
            passiveManager.UnequipFromCurrent(index);

        RefreshEquippedView();
    }

    // ... (左側技能庫邏輯保持不變) ...
    void GenerateLibrary()
    {
        foreach (Transform child in libraryContent) Destroy(child.gameObject);
        _libSlots.Clear();
        foreach (var skill in passiveLibrary)
        {
            if (!skill) continue;
            var slot = Instantiate(librarySlotPrefab, libraryContent);
            slot.Setup(skill);
            slot.OnClick = (s) =>
            {
                if (_pendingSkill == s.data) _pendingSkill = null;
                else _pendingSkill = s.data;
                RefreshLibraryVisuals();
            };
            _libSlots.Add(slot);
        }
    }
    void RefreshLibraryVisuals() { foreach (var s in _libSlots) s.SetSelected(s.data == _pendingSkill); }
}