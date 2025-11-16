using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// HUD 上專門管理技能顯示的腳本：
/// - 持有一組 SkillButton
/// - 提供 SetSkillCooldown / SetSkillIcon 給 Player / HUDStatsBinder 呼叫
/// - 提供「切換技能組」按鈕可以呼叫的 RequestNextSkillSet()
///   真正的切換邏輯交給 Player 實作
/// </summary>
[DisallowMultipleComponent]
public class HUDSkillStats : MonoBehaviour
{
    [Header("技能按鈕列表（拖進來）")]
    public List<SkillButton> skillButtons = new List<SkillButton>();

    [Header("技能組顯示（純視覺，可選）")]
    public TMP_Text skillSetLabel;   // 顯示「技能組 1 / 2 / 3…」之類
    public string[] skillSetNames;   // 每組的名字（可空）
    public int currentSetIndex = 0;

    [Header("事件：請 Player 切換技能組")]
    public UnityEvent onRequestNextSkillSet;
    // 之後你在 Player 或 HUDStatsBinder 那邊，把這個 event 接起來就好

    /// <summary>
    /// 設定某一個技能槽的冷卻
    /// 例如 slotIndex=0 代表第一顆技能
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
    /// 給 Player / HUDStatsBinder 叫的：更新目前在第幾組技能
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
    /// 給 UI 按鈕 OnClick() 用：
    /// 按下「切換技能組」時，只發一個 event，
    /// 真正切哪一組由 Player 來決定。
    /// </summary>
    public void RequestNextSkillSet()
    {
        onRequestNextSkillSet?.Invoke();
    }

    // ====== 內部工具 ======

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
