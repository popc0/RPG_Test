using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG;

public class PageSkillEquip : MonoBehaviour
{
    [Header("核心引用 (會自動抓取)")]
    public SkillCaster skillCaster;

    // ============================================================
    // 1. 技能庫 (左側 / 兩個 ScrollView)
    // ============================================================
    [Header("一般技能庫 (Normal)")]
    public Transform normalLibContent;       // 普攻 ScrollView 的 Content
    public List<SkillData> normalSkills;     // 普攻技能清單 (Inspector 拖曳)

    [Header("大招技能庫 (Ultimate)")]
    public Transform ultimateLibContent;     // 大招 ScrollView 的 Content
    public List<SkillData> ultimateSkills;   // 大招技能清單 (Inspector 拖曳)

    [Header("共用資源")]
    public UISkillSlot librarySlotPrefab;    // 庫存格子 Prefab

    // ============================================================
    // 2. 裝備槽 (右側 / 4 個按鈕)
    // ============================================================
    [Header("裝備槽 - 普攻 (Normal)")]
    [Tooltip("Slot 0: 固定普攻")]
    public UISkillSlot slotFixedNormal;
    [Tooltip("Slot 1: 切換普攻")]
    public UISkillSlot slotSwitchNormal;

    [Header("裝備槽 - 大招 (Ultimate)")]
    [Tooltip("Slot 2: 固定大招")]
    public UISkillSlot slotFixedUlt;
    [Tooltip("Slot 3: 切換大招")]
    public UISkillSlot slotSwitchUlt;

    // ============================================================
    // [新增] 技能組切換功能
    // ============================================================
    [Header("技能組控制")]
    public Button btnSwitchGroup;          // SW 切換按鈕
    public TextMeshProUGUI groupNameText;  // 顯示目前是哪一組 (例如: "技能組 1")
    // [新增] 顯示總數的文字 (例如 "3" 或 "/ 3")
    public TextMeshProUGUI groupTotalCountText;

    // ============================================================
    // 內部狀態
    // ============================================================
    private SkillData _pendingSkill;
    private bool _isPendingSkillUltimate;

    private List<UISkillSlot> _allLibSlots = new List<UISkillSlot>();
    private int _editingGroupIndex = 0;

    // ============================================================
    // [新增] 技能組管理按鈕
    // ============================================================
    [Header("技能組管理")]
    public Button btnAddGroup;     // 新增 (+) 按鈕
    public Button btnRemoveGroup;  // 刪除 (-) 按鈕

    void Start()
    {
        // 1. 先生成靜態的技能庫
        GenerateLibraries();

        // 2. 綁定切換按鈕事件
        if (btnSwitchGroup)
        {
            btnSwitchGroup.onClick.AddListener(OnClickSwitchGroup);
        }
        // [新增] 綁定增刪按鈕
        if (btnAddGroup) btnAddGroup.onClick.AddListener(OnClickAddGroup);
        if (btnRemoveGroup) btnRemoveGroup.onClick.AddListener(OnClickRemoveGroup);

        // 3. 啟動自動搜尋協程
        StartCoroutine(AutoBindPlayerRoutine());
    }
    void Update()
    {
        // [新增] 自動同步邏輯
        // 如果 skillCaster 的索引變了（可能是因為按了鍵盤、體感，或是按鈕觸發），
        // UI 偵測到不同步，就立刻刷新自己。
        if (skillCaster != null && _editingGroupIndex != skillCaster.currentSkillGroupIndex)
        {
            _editingGroupIndex = skillCaster.currentSkillGroupIndex;
            RefreshEquippedView(); // 強制刷新畫面
        }
    }

    // 自動搜尋協程：接到為止！
    IEnumerator AutoBindPlayerRoutine()
    {
        while (skillCaster == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                skillCaster = player.GetComponentInChildren<SkillCaster>(true);
            }

            if (skillCaster != null)
            {
                // 成功連結！立即刷新畫面
                _editingGroupIndex = skillCaster.currentSkillGroupIndex;
                RefreshEquippedView();
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    void OnEnable()
    {
        if (skillCaster)
        {
            _editingGroupIndex = skillCaster.currentSkillGroupIndex;
            RefreshEquippedView();
        }

        _pendingSkill = null;
        RefreshLibraryVisuals();
    }

    // --- [新增] 切換群組按鈕事件 ---
    void OnClickSwitchGroup()
    {
        if (!skillCaster) return;

        // 1. 呼叫 Caster 切換到下一組
        skillCaster.SwitchToNextSkillGroup();

        // 2. 同步本地索引
        _editingGroupIndex = skillCaster.currentSkillGroupIndex;

        // 3. 刷新介面 (顯示新群組的技能)
        RefreshEquippedView();
    }

    // [新增] 點擊新增按鈕
    void OnClickAddGroup()
    {
        if (!skillCaster) return;

        // 1. 呼叫 Caster 建立新組
        skillCaster.AddNewSkillGroup();

        // 2. 更新本地索引 (因為 Caster 會自動切換到新組)
        _editingGroupIndex = skillCaster.currentSkillGroupIndex;

        // 3. 刷新介面
        RefreshEquippedView();

        Debug.Log("已新增技能組");
    }

    // [新增] 點擊刪除按鈕
    void OnClickRemoveGroup()
    {
        if (!skillCaster) return;

        // 1. 呼叫 Caster 刪除當前組
        skillCaster.RemoveCurrentSkillGroup();

        // 2. 更新本地索引 (可能因刪除而改變)
        _editingGroupIndex = skillCaster.currentSkillGroupIndex;

        // 3. 刷新介面
        RefreshEquippedView();

        Debug.Log("已刪除技能組");
    }

    // --- 1. 生成技能庫 ---
    void GenerateLibraries()
    {
        foreach (Transform child in normalLibContent) Destroy(child.gameObject);
        foreach (Transform child in ultimateLibContent) Destroy(child.gameObject);
        _allLibSlots.Clear();

        // 處理 Normal 技能庫
        foreach (var skill in normalSkills)
        {
            // ... (Instantiate slot, Setup) ...
            var slot = Instantiate(librarySlotPrefab, normalLibContent);
            slot.Setup(skill);

            // ★ 修改點 1：切換選取邏輯 (Normal)
            slot.OnClick = (s) =>
            {
                if (_pendingSkill == s.data) // 重複點擊 -> 取消選取
                {
                    _pendingSkill = null;
                    _isPendingSkillUltimate = false;
                }
                else
                {
                    _pendingSkill = s.data;
                    _isPendingSkillUltimate = false;
                }
                RefreshLibraryVisuals();
            };
            _allLibSlots.Add(slot);
        }
        // 處理 Ultimate 技能庫
        foreach (var skill in ultimateSkills)
        {
            // ... (Instantiate slot, Setup) ...
            var slot = Instantiate(librarySlotPrefab, ultimateLibContent);
            slot.Setup(skill);

            // ★ 修改點 2：切換選取邏輯 (Ultimate)
            slot.OnClick = (s) =>
            {
                if (_pendingSkill == s.data) // 重複點擊 -> 取消選取
                {
                    _pendingSkill = null;
                    _isPendingSkillUltimate = false;
                }
                else
                {
                    _pendingSkill = s.data;
                    _isPendingSkillUltimate = true;
                }
                RefreshLibraryVisuals();
            };
            _allLibSlots.Add(slot);
        }

        Canvas.ForceUpdateCanvases();
        if (normalLibContent.GetComponent<ContentSizeFitter>())
            LayoutRebuilder.ForceRebuildLayoutImmediate(normalLibContent.GetComponent<RectTransform>());
        if (ultimateLibContent.GetComponent<ContentSizeFitter>())
            LayoutRebuilder.ForceRebuildLayoutImmediate(ultimateLibContent.GetComponent<RectTransform>());
    }

    void CreateLibSlot(SkillData skill, Transform parent, bool isUltimate)
    {
        if (!skill) return;
        var slot = Instantiate(librarySlotPrefab, parent);
        slot.Setup(skill);
        slot.OnClick = (s) =>
        {
            _pendingSkill = s.data;
            _isPendingSkillUltimate = isUltimate;
            RefreshLibraryVisuals();
        };
        _allLibSlots.Add(slot);
    }

    void RefreshLibraryVisuals()
    {
        foreach (var slot in _allLibSlots) slot.SetSelected(slot.data == _pendingSkill);
    }

    // --- 2. 刷新裝備槽顯示 ---
    void RefreshEquippedView()
    {
        if (!skillCaster || skillCaster.skillGroups.Count == 0) return;

        var group = skillCaster.skillGroups[_editingGroupIndex];

        // 更新標題
        if (groupNameText) groupNameText.text = group.groupName;

        // [新增] 更新總數顯示
        if (groupTotalCountText)
        {
            // 顯示 "1 / 3" 這種格式
            groupTotalCountText.text = $"{_editingGroupIndex + 1} / {skillCaster.skillGroups.Count}";
        }
        // 填入 4 個槽位
        slotFixedNormal.Setup(skillCaster.fixedNormalSkill);
        slotFixedNormal.OnClick = (s) => OnEquipSlotClicked(0);

        slotSwitchNormal.Setup(group.switchableNormal);
        slotSwitchNormal.OnClick = (s) => OnEquipSlotClicked(1);

        slotFixedUlt.Setup(skillCaster.fixedUltimateSkill);
        slotFixedUlt.OnClick = (s) => OnEquipSlotClicked(2);

        slotSwitchUlt.Setup(group.switchableUltimate);
        slotSwitchUlt.OnClick = (s) => OnEquipSlotClicked(3);

        // 重置選中狀態
        slotFixedNormal.SetSelected(false);
        slotSwitchNormal.SetSelected(false);
        slotFixedUlt.SetSelected(false);
        slotSwitchUlt.SetSelected(false);
    }

    // --- 3. 裝備邏輯 ---
    void OnEquipSlotClicked(int slotIndex)
    {
        if (!skillCaster) return;

        if (_pendingSkill != null)
        {
            // 防呆：普攻(0,1) vs 大招(2,3)
            bool isTargetUltimateSlot = (slotIndex == 2 || slotIndex == 3);
            if (_isPendingSkillUltimate != isTargetUltimateSlot)
            {
                Debug.LogWarning("類型不符：普攻不能裝入大招槽（反之亦然）！");
                // 這裡可以加一個 UI 提示，例如 ShowMessage("類型不符")
                return;
            }
        }

        // ============================================================
        // ★ 修改處：呼叫 SkillCaster 的新方法進行驗證與裝備
        // ============================================================

        // 呼叫驗證邏輯
        string errorMsg = skillCaster.TryEquipSkill(slotIndex, _pendingSkill, _editingGroupIndex);

        if (!string.IsNullOrEmpty(errorMsg))
        {
            // 如果有錯誤訊息 (例如 "跟 Slot 0 重複")，這裡可以跳出 UI 通知
            Debug.LogWarning(errorMsg);

            // 假設你有一個全局的提示 UI，可以在這裡呼叫，例如：
            // InteractPromptUI.Instance.Show(errorMsg); 
            // 或是專門的 Toast UI

            // 失敗了，直接返回，不更新介面
            return;
        }

        // ============================================================
        // 裝備成功後的後續處理
        // ============================================================

        RefreshEquippedView();

        if (_editingGroupIndex == skillCaster.currentSkillGroupIndex)
        {
            skillCaster.SetSkillGroupIndex(_editingGroupIndex);
        }

        Debug.Log($"已將槽位 {slotIndex} 更新為: {(_pendingSkill ? _pendingSkill.SkillName : "空")}");
    }
}