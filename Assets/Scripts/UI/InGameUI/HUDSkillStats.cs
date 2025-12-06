using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
//  [新增] 必須引用 SkillData 所在的命名空間
using RPG;

[DisallowMultipleComponent]
public class HUDSkillStats : MonoBehaviour
{
    [Header("技能按鈕列表（拖進來）")]
    public List<SkillButton> skillButtons = new List<SkillButton>();

    [Header("技能組顯示（純視覺，可選）")]
    public TMP_Text skillSetLabel;
    public string[] skillSetNames;
    public int currentSetIndex = 0;

    [Header("事件：請 Player 切換技能組")]
    public UnityEvent onRequestNextSkillSet;

    // 用於儲存當前技能組的 SkillData 列表，方便後續查詢（例如：懸停提示）
    private List<SkillData> _currentSkillDatas = new List<SkillData>();

    /// <summary>
    /// 設定某一個技能槽的冷卻
    /// </summary>
    public void SetSkillCooldown(int slotIndex, float current, float max)
    {
        var btn = FindSkillButton(slotIndex);
        if (btn != null)
            btn.SetCooldown(current, max);
    }

    /// <summary>
    /// 設定某一個技能的圖示
    /// </summary>
    public void SetSkillIcon(int slotIndex, Sprite sprite)
    {
        var btn = FindSkillButton(slotIndex);
        if (btn != null)
            btn.SetIcon(sprite);
    }
    /// <summary>
    /// 接收 SkillCaster 傳來的當前技能組數據
    /// </summary>
    public void SetSkillSetData(int index, List<SkillData> skillsList)
    {
        // 1. 更新索引
        SetSkillSetIndex(index);

        // 2. 儲存數據清單
        _currentSkillDatas = skillsList;

        if (skillsList == null) return;

        // 3. [核心修改] 改用 foreach 遍歷按鈕，並依據按鈕的 slotIndex 去清單裡找資料
        foreach (var btn in skillButtons)
        {
            if (btn == null) continue;

            int slot = btn.slotIndex; // 讀取按鈕上設定的 Slot (0, 1, 2, 3)

            // 檢查這個 Slot 是否在資料範圍內 (0~3)
            if (slot >= 0 && slot < skillsList.Count)
            {
                SkillData data = skillsList[slot];

                // 如果該槽位有裝技能 (data != null) 則顯示 Icon，否則傳 null (隱藏)
                Sprite icon = (data != null) ? data.Icon : null;

                btn.SetIcon(icon);
            }
            else
            {
                // 如果按鈕設定的 Slot 超出範圍 (例如設了 4)，就清空
                btn.SetIcon(null);
            }
        }
    }

    /// <summary>
    /// ★ NEW：設定某一個技能是否正在施放中
    /// </summary>
    public void SetSkillCasting(int slotIndex, bool isCasting)
    {
        var btn = FindSkillButton(slotIndex);
        if (btn != null)
            btn.SetCasting(isCasting);
    }

    /// <summary>
    /// 更新目前在第幾組技能
    /// </summary>
    public void SetSkillSetIndex(int index)
    {
        currentSetIndex = index;
        if (skillSetLabel != null)
        {
            string name;
            if (skillSetNames != null && index >= 0 && index < skillSetNames.Length)
                name = skillSetNames[index];
            else
                name = $"Set {index + 1}";
            skillSetLabel.text = name;
        }
    }

    /// <summary>
    /// 給 UI 按鈕 OnClick() 用
    /// </summary>
    public void RequestNextSkillSet()
    {
        onRequestNextSkillSet?.Invoke();
    }

    SkillButton FindSkillButton(int slotIndex)
    {
        for (int i = 0; i < skillButtons.Count; ++i)
        {
            var btn = skillButtons[i];
            if (btn != null && btn.slotIndex == slotIndex)
                return btn;
        }
        return null;
    }
}