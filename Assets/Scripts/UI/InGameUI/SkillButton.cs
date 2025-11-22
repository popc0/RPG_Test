using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class SkillButton : MonoBehaviour
{
    [Header("UI 元件")]
    public Button button;
    public Image iconImage;
    public Image cooldownMask;
    public TMP_Text cooldownText;

    [Header("第二層：靜態冷卻遮罩")]
    public Image cooldownStaticMask;

    [Header("旋轉外框")]
    [Tooltip("會根據狀態旋轉的外框 RectTransform")]
    public RectTransform spinningFrame;

    [Tooltip("可施放時的旋轉速度 (度/秒)")]
    public float readySpinSpeed = 30f;

    [Tooltip("冷卻中的旋轉速度 (度/秒)")]
    public float cooldownSpinSpeed = 10f;

    [Tooltip("施放中的旋轉速度 (度/秒)")]
    public float castingSpinSpeed = 180f;

    [Header("設定")]
    public int slotIndex = 0;
    public bool hideTextWhenReady = true;

    // 狀態
    float currentCooldown;
    float maxCooldown;
    bool isCasting;

    public bool IsOnCooldown => currentCooldown > 0f;
    public bool IsCasting => isCasting;

    void Awake()
    {
        if (!button)
            button = GetComponent<Button>();

        if (cooldownMask != null && cooldownMask.type != Image.Type.Filled)
            cooldownMask.type = Image.Type.Filled;

        UpdateVisuals();
    }

    void Update()
    {
        UpdateFrameRotation();
    }

    /// <summary>
    /// 根據目前狀態旋轉外框
    /// </summary>
    void UpdateFrameRotation()
    {
        if (spinningFrame == null) return;

        float speed;

        if (isCasting)
        {
            // 施放中：快速轉
            speed = castingSpinSpeed;
        }
        else if (currentCooldown > 0f)
        {
            // CD 中：慢速轉
            speed = cooldownSpinSpeed;
        }
        else
        {
            // 可施放：正常轉
            speed = readySpinSpeed;
        }

        // 用 unscaledDeltaTime 確保暫停時也能轉（如果你希望暫停時停轉就改成 deltaTime）
        spinningFrame.Rotate(0f, 0f, -speed * Time.unscaledDeltaTime);
    }

    /// <summary>
    /// 由外部呼叫：更新冷卻狀態
    /// </summary>
    public void SetCooldown(float current, float max)
    {
        maxCooldown = Mathf.Max(0.0001f, max);
        currentCooldown = Mathf.Clamp(current, 0f, maxCooldown);
        UpdateVisuals();
    }

    /// <summary>
    /// 由外部呼叫：設定是否正在施放中
    /// </summary>
    public void SetCasting(bool casting)
    {
        isCasting = casting;
    }

    void UpdateVisuals()
    {
        // 動態冷卻遮罩（圈圈）
        if (cooldownMask)
        {
            float ratio = (maxCooldown > 0f) ? currentCooldown / maxCooldown : 0f;
            cooldownMask.fillAmount = ratio;
            cooldownMask.enabled = currentCooldown > 0f;
        }

        // 靜態冷卻遮罩
        if (cooldownStaticMask)
        {
            cooldownStaticMask.enabled = currentCooldown > 0f;
        }

        // 文字
        if (cooldownText)
        {
            if (currentCooldown > 0f)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = string.Format("{0:F1}s", currentCooldown);
            }
            else
            {
                cooldownText.gameObject.SetActive(!hideTextWhenReady);
                if (!hideTextWhenReady)
                    cooldownText.text = "";
            }
        }

        // 可不可以按
        if (button)
            button.interactable = currentCooldown <= 0f && !isCasting;
    }

    public void SetIcon(Sprite sprite)
    {
        if (!iconImage) return;
        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    public int GetSlotIndex() => slotIndex;
}