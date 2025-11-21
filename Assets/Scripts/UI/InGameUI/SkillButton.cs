using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class SkillButton : MonoBehaviour
{
    [Header("UI 元件")]
    public Button button;
    public Image iconImage;

    public Image cooldownMask;        // 原本的「會轉圈」的遮罩
    public TMP_Text cooldownText;

    [Header("第二層：靜態冷卻遮罩 (不會轉、不會漸變)")]
    public Image cooldownStaticMask;   // ★ NEW：新增不動的第二層遮罩

    [Header("設定")]
    public int slotIndex = 0;
    public bool hideTextWhenReady = true;

    float currentCooldown;
    float maxCooldown;

    public bool IsOnCooldown => currentCooldown > 0f;

    void Awake()
    {
        if (!button)
            button = GetComponent<Button>();

        // 讓第一層是 filled
        if (cooldownMask != null && cooldownMask.type != Image.Type.Filled)
            cooldownMask.type = Image.Type.Filled;

        UpdateVisuals();
    }

    // 由外部呼叫更新冷卻
    public void SetCooldown(float current, float max)
    {
        maxCooldown = Mathf.Max(0.0001f, max);
        currentCooldown = Mathf.Clamp(current, 0f, maxCooldown);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        // ===========================
        // 一層：動態冷卻遮罩（圈圈）
        // ===========================
        if (cooldownMask)
        {
            float ratio = (maxCooldown > 0f) ? currentCooldown / maxCooldown : 0f;
            cooldownMask.fillAmount = ratio;
            cooldownMask.enabled = currentCooldown > 0f;
        }

        // ===========================
        // 二層：靜態冷卻遮罩（只要冷卻就顯示）
        // ===========================
        if (cooldownStaticMask) // ★ NEW
        {
            cooldownStaticMask.enabled = currentCooldown > 0f;
            // 不需要改 fillAmount 或 rotation
        }

        // ===========================
        // 文字
        // ===========================
        if (cooldownText)
        {
            if (currentCooldown > 0f)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
            }
            else
            {
                cooldownText.gameObject.SetActive(!hideTextWhenReady);
                if (!hideTextWhenReady)
                    cooldownText.text = "";
            }
        }

        // ===========================
        // 可不可以按
        // ===========================
        if (button)
            button.interactable = currentCooldown <= 0f;
    }

    public void SetIcon(Sprite sprite)
    {
        if (!iconImage)
            return;

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    public int GetSlotIndex() => slotIndex;
}
