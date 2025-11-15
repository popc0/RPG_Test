using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 動態觸控搖桿：
/// - 按下時才顯示
/// - 搖桿底座會移到第一次按下的位置
/// - Value 為 -1~1 的向量，給 UnifiedInputSource 使用
/// </summary>
public class DynamicTouchJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("UI 參照")]
    public RectTransform background; // 搖桿底座
    public RectTransform handle;     // 搖桿頭

    [Header("設定")]
    [Tooltip("搖桿可以偏移的最大半徑（單位：背景父物件的座標）")]
    public float maxRadius = 80f;

    [Tooltip("這個裝置是否啟用觸控搖桿（可以用來做平台切換）")]
    public bool enabledOnThisPlatform = true;

    /// <summary>給外面讀的搖桿輸出，-1..1</summary>
    public Vector2 Value { get; private set; }

    Canvas _canvas;
    Camera _uiCamera;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null &&
            (_canvas.renderMode == RenderMode.ScreenSpaceCamera ||
             _canvas.renderMode == RenderMode.WorldSpace))
        {
            _uiCamera = _canvas.worldCamera;
        }
        else
        {
            _uiCamera = null; // ScreenSpaceOverlay
        }

        SetVisible(false);
        Value = Vector2.zero;
    }

    void SetVisible(bool visible)
    {
        if (background != null) background.gameObject.SetActive(visible);
        if (handle != null) handle.gameObject.SetActive(visible);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enabledOnThisPlatform)
            return;

        if (background == null || handle == null)
            return;

        // 把底座移到點擊位置（以父 Rect 為座標系）
        RectTransform parent = background.parent as RectTransform;
        if (parent != null)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                eventData.position,
                _uiCamera,
                out localPos
            );
            background.anchoredPosition = localPos;
        }

        // 顯示搖桿
        SetVisible(true);

        // 一開始 knob 先在中心
        handle.anchoredPosition = Vector2.zero;
        Value = Vector2.zero;

        // 如果按下時手指不是剛好在中心，立刻更新一次
        UpdateJoystick(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null)
            return;

        UpdateJoystick(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 放開：值歸零，搖桿隱藏
        Value = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
        SetVisible(false);
    }

    /// <summary>
    /// 依據螢幕座標更新 knob 位置與 Value
    /// </summary>
    void UpdateJoystick(Vector2 screenPos)
    {
        RectTransform parent = background.parent as RectTransform;
        if (parent == null)
            return;

        // 先把手指的位置轉成「父物件 local 座標」
        Vector2 localInParent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            screenPos,
            _uiCamera,
            out localInParent
        );

        // 再用「手指位置 - 底座中心位置」當作位移量
        Vector2 center = background.anchoredPosition;
        Vector2 delta = localInParent - center;

        // 限制在圓形範圍內
        Vector2 clamped = Vector2.ClampMagnitude(delta, maxRadius);
        handle.anchoredPosition = clamped;

        // 正規化成 -1..1
        Vector2 v = clamped / Mathf.Max(maxRadius, 0.0001f);

        // 太小當成 0，避免角色微抖
        if (v.sqrMagnitude < 0.001f)
            v = Vector2.zero;

        Value = v;
    }
}
