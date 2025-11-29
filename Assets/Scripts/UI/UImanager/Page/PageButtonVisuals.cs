using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PageButtonVisuals : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("UI 視覺元件")]
    public Image backgroundImage; // 按鈕的背景圖 (用於改變顏色/Alpha)
    public TMPro.TMP_Text labelText; // 按鈕上的文字 (用於改變顏色)

    [Header("顏色設定")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow; // 選中時的加深或高亮顏色
    //  新增：被導覽時的顏色 (滑鼠懸停或鍵盤/手把焦點)
    public Color highlightedColor = Color.cyan;
    public float fadeDuration = 0.1f; // 顏色過渡時間

    private bool isCurrentlySelected = false; //  這裡宣告了 'isCurrentlySelected'
    // 內部追蹤：按鈕是否處於導覽/懸停狀態
    private bool isHighlightedBySystem = false;

    private Button button;

    void Awake()
    {
        //  獲取 Button 組件
        button = GetComponent<Button>();
        // 確保 backgroundImage 和 labelText 被設定，如果沒有則嘗試尋找
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (labelText == null)
            labelText = GetComponentInChildren<TMPro.TMP_Text>();

        // 確保初始狀態正確
        SetVisuals(isCurrentlySelected, true);

        //  確保 Button 處於 None 模式，並使用程式碼控制顏色，
        // 否則 Button 內建的顏色機制會覆蓋 SetVisuals 的效果。
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            //  或者：您可以選擇使用 ColorTint 讓 Button 處理懸停，但會使程式碼複雜化。
        }
    }

    /// <summary>
    /// 由 PageMain 呼叫，設定按鈕是否為當前選中狀態。
    /// </summary>
// 🚨 這是關鍵方法：當 PageMain 告訴我被選中時
    public void SetSelected(bool isSelected)
    {
        if (isCurrentlySelected == isSelected) return;

        isCurrentlySelected = isSelected;
        // 當選中狀態改變時，立即刷新視覺
        SetVisuals(isSelected, true);
    }

    // ----------------------------------------------------
    // 🚨 新增：懸停/導覽事件處理 (最高優先級邏輯)
    // ----------------------------------------------------

    // 鍵盤/手把導覽選中
    public void OnSelect(BaseEventData eventData) => HandleHighlightEnter();
    // 鼠標懸停
    public void OnPointerEnter(PointerEventData eventData) => HandleHighlightEnter();

    // 鍵盤/手把導覽取消選中
    public void OnDeselect(BaseEventData eventData) => HandleHighlightExit();
    // 鼠標移開
    public void OnPointerExit(PointerEventData eventData) => HandleHighlightExit();
    void HandleHighlightEnter()
    {
        // 如果已經在 highlight 狀態則跳出
        if (isHighlightedBySystem) return;

        isHighlightedBySystem = true;

        // 🚨 邏輯：即使已經是選中狀態，也要切換到 Highlighted Color
        // 這樣就能覆蓋 Selected Color (最高優先級)

        Color targetColor = isHighlightedBySystem ? highlightedColor : normalColor;
        backgroundImage.CrossFadeColor(targetColor, fadeDuration, true, true);
    }

    void HandleHighlightExit()
    {
        // 如果沒有被 Highlight，則跳出
        if (!isHighlightedBySystem) return;

        isHighlightedBySystem = false;

        // 🚨 邏輯：恢復到正確的狀態
        if (isCurrentlySelected)
        {
            // 如果當前頁面是選中的，則恢復到 Selected Color
            SetVisuals(true, false);
        }
        else
        {
            // 如果不是選中的，則恢復到 Normal Color
            SetVisuals(false, false);
        }
    }

    // ----------------------------------------------------
    // 修正 SetVisuals 邏輯
    // ----------------------------------------------------

    void SetVisuals(bool isSelected, bool immediate)
    {
        //  修正：如果按鈕當前處於被導覽狀態，SetVisuals 不做任何事！
        // 優先級交給 HandleHighlightEnter/Exit 處理
        if (isHighlightedBySystem) return;

        Color targetColor = isSelected ? selectedColor : normalColor;

        if (backgroundImage != null)
        {
            backgroundImage.CrossFadeColor(targetColor, immediate ? 0f : fadeDuration, true, true);
        }

        //  提示：您可能還想在這裡改變文字顏色或大小
        if (labelText != null)
        {
            //labelText.color = isSelected ? selectedColor : normalColor;
        }
    }
}
