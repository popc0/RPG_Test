using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 單一技能按鈕的顯示控制：
/// - 冷卻轉圈圈
/// - 冷卻秒數文字
/// - 是否可按
/// 實際施放邏輯交給外部（Player / 其他腳本）
/// </summary>
[DisallowMultipleComponent]
public class SkillButton : MonoBehaviour
{
    [Header("UI 元件")]
    public Button button;            // 技能按鈕本體
    public Image iconImage;          // 技能圖示（可不填）
    public Image cooldownMask;       // 冷卻遮罩（通常設成 Filled + Radial360）
    public TMP_Text cooldownText;    // 冷卻秒數文字（可不填）

    [Header("設定")]
    [Tooltip("這顆按鈕代表第幾個技能槽（例如 0,1,2...）")]
    public int slotIndex = 0;

    [Tooltip("冷卻結束時是否隱藏文字")]
    public bool hideTextWhenReady = true;

    float currentCooldown;
    float maxCooldown;

    public bool IsOnCooldown => currentCooldown > 0f;

    void Awake()
    {
        if (!button)
            button = GetComponent<Button>();

        // 讓遮罩預設是 Filled，但不強迫你一定用 Radial
        if (cooldownMask != null && cooldownMask.type != Image.Type.Filled)
            cooldownMask.type = Image.Type.Filled;

        UpdateVisuals();
    }

    /// <summary>
    /// 由外部（Player / HUDStatsBinder）呼叫，更新冷卻狀態
    /// current = 剩餘秒數, max = 總冷卻秒數
    /// </summary>
    public void SetCooldown(float current, float max)
    {
        maxCooldown = Mathf.Max(0.0001f, max);             // 避免除以 0
        currentCooldown = Mathf.Clamp(current, 0f, maxCooldown);
        UpdateVisuals();
    }

    /// <summary>
    /// 更新 UI 顯示：遮罩、文字、是否可按
    /// </summary>
    void UpdateVisuals()
    {
        // 遮罩
        if (cooldownMask)
        {
            float ratio = (maxCooldown > 0f) ? currentCooldown / maxCooldown : 0f;
            cooldownMask.fillAmount = ratio;
            cooldownMask.enabled = currentCooldown > 0f;   // 冷卻結束就隱藏遮罩
        }

        // 文字
        if (cooldownText)
        {
            if (currentCooldown > 0f)
            {
                cooldownText.gameObject.SetActive(true);
                // 顯示「剩餘秒數（向上取整）」例如 3.2s -> 4
                cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
            }
            else
            {
                cooldownText.gameObject.SetActive(!hideTextWhenReady);
                if (!hideTextWhenReady)
                    cooldownText.text = "";
            }
        }

        // 按鈕能不能按
        if (button)
        {
            button.interactable = currentCooldown <= 0f;
        }
    }

    /// <summary>
    /// 設定技能圖示（可選）
    /// </summary>
    public void SetIcon(Sprite sprite)
    {
        if (!iconImage)
            return;

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    /// <summary>
    /// 回傳自己代表的技能槽 index（外部有需要可以用）
    /// </summary>
    public int GetSlotIndex() => slotIndex;
}
