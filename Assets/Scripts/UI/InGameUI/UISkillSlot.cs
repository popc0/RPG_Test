using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RPG;

[RequireComponent(typeof(Button))]
public class UISkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("UI 參照")]
    public Image background;
    public Image iconImage;

    [Header("顏色設定")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color highlightColor = Color.cyan;
    public Color selectedColor = Color.yellow;
    public float fadeDuration = 0.1f;

    [Header("捲動設定")]
    public bool autoScroll = true;
    public float scrollPadding = 40f; // 加大一點緩衝區

    [Header("資料")]
    public SkillData data;
    public System.Action<UISkillSlot> OnClick;

    private bool _isSelected = false;
    private bool _isHighlighted = false;
    private Button _btn;

    // 這裡改用 SerializeField 方便我們在編輯器觀察有沒有抓到
    [Header("Debug 資訊")]
    [SerializeField] private ScrollRect _parentScrollRect;

    void Awake()
    {
        _btn = GetComponent<Button>();
        if (_btn)
        {
            _btn.transition = Selectable.Transition.None;
            _btn.onClick.AddListener(() => OnClick?.Invoke(this));
        }

        if (!background) background = GetComponent<Image>();

        // 嘗試抓取 ScrollRect
        _parentScrollRect = GetComponentInParent<ScrollRect>();
        if (_parentScrollRect == null)
        {
            // 如果 Awake 抓不到，可能因為生成順序，留給 Start 再試一次
        }

        UpdateVisuals(true);
    }

    void Start()
    {
        // 再次嘗試抓取 (確保父子關係已建立)
        if (_parentScrollRect == null)
        {
            _parentScrollRect = GetComponentInParent<ScrollRect>();
            if (_parentScrollRect == null)
            {
                Debug.LogWarning($"[UISkillSlot] {name} 找不到父層的 ScrollRect！自動捲動將失效。請檢查 Hierarchy 結構。");
            }
        }
    }

    public void Setup(SkillData skillData)
    {
        data = skillData;
        if (data != null && data.Icon != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.color = Color.clear;
        }
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        UpdateVisuals();
    }

    // --- 導覽事件 ---
    public void OnSelect(BaseEventData eventData)
    {
        _isHighlighted = true;
        UpdateVisuals();

        // 觸發自動捲動
        if (autoScroll) ScrollToVisible();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isHighlighted = false;
        UpdateVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData) { _isHighlighted = true; UpdateVisuals(); }
    public void OnPointerExit(PointerEventData eventData) { _isHighlighted = false; UpdateVisuals(); }

    void UpdateVisuals(bool immediate = false)
    {
        if (!background) return;
        Color targetColor = normalColor;
        if (_isHighlighted) targetColor = highlightColor;
        else if (_isSelected) targetColor = selectedColor;
        background.CrossFadeColor(targetColor, immediate ? 0f : fadeDuration, true, true);
    }

    // --- [修正版] 自動捲動邏輯 ---
    void ScrollToVisible()
    {
        if (!_parentScrollRect) return;

        // 1. 停止 ScrollRect 目前的滑動慣性，避免打架
        _parentScrollRect.StopMovement();

        RectTransform viewport = _parentScrollRect.viewport;
        RectTransform content = _parentScrollRect.content;
        RectTransform itemRect = GetComponent<RectTransform>();

        // 如果 ScrollRect 沒有設定 viewport，通常視為 ScrollRect 本身
        if (viewport == null) viewport = _parentScrollRect.GetComponent<RectTransform>();

        // 2. 取得 Item 相對於 Viewport 的位置
        Vector3 itemLocalPos = viewport.InverseTransformPoint(itemRect.position);

        // 計算 Item 的上下邊界 (相對於 Viewport 中心/原點)
        float itemTop = itemLocalPos.y + itemRect.rect.height * (1f - itemRect.pivot.y);
        float itemBottom = itemLocalPos.y - itemRect.rect.height * itemRect.pivot.y;

        // 取得 Viewport 的上下邊界
        float viewportTop = viewport.rect.yMax;
        float viewportBottom = viewport.rect.yMin;

        // 3. 計算位移量
        float moveAmount = 0f;

        // 檢查上面
        if (itemTop > viewportTop - scrollPadding)
        {
            // 距離 = Item頂 - (視窗頂 - 邊距)
            float distance = itemTop - (viewportTop - scrollPadding);
            moveAmount = -distance; // 往下移
        }
        // 檢查下面
        else if (itemBottom < viewportBottom + scrollPadding)
        {
            // 距離 = (視窗底 + 邊距) - Item底
            float distance = (viewportBottom + scrollPadding) - itemBottom;
            moveAmount = distance; // 往上移
        }

        // 4. 應用位移
        if (moveAmount != 0f)
        {
            Vector2 pos = content.anchoredPosition;
            pos.y += moveAmount;
            content.anchoredPosition = pos;

            // Debug.Log($"[AutoScroll] {name} 觸發捲動: {moveAmount:F1}");
        }
    }
}