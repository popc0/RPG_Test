using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // ★ 新輸入系統

[RequireComponent(typeof(Button))]
public class ButtonKey : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("對應快捷鍵（例如 Enter, Space, E）")]
    public Key hotkey = Key.Enter; // ★ 原本 KeyCode → 新系統 Key

    [Header("聚焦視覺效果")]
    public float highlightScale = 1.1f;
    public Color highlightColor = Color.yellow;

    private Vector3 originalScale;
    private Color originalColor;
    private Button btn;
    private Image img;
    private bool isFocused;

    void Awake()
    {
        btn = GetComponent<Button>();
        img = GetComponent<Image>();

        originalScale = transform.localScale;
        if (img != null)
            originalColor = img.color;
    }

    void Update()
    {
        // ★ 新系統：以 Keyboard.current 讀取
        var kb = Keyboard.current;
        // 修改處：在 kb[hotkey] 之前加入 && hotkey != Key.None
        if (isFocused && kb != null && hotkey != Key.None && kb[hotkey].wasPressedThisFrame)
        {
            btn.onClick.Invoke();
        }
    }

    // 聚焦時（鍵盤或控制器導覽）
    public void OnSelect(BaseEventData eventData)
    {
        isFocused = true;
        transform.localScale = originalScale * highlightScale;
        if (img != null)
            img.color = highlightColor;
    }

    // 失焦時
    public void OnDeselect(BaseEventData eventData)
    {
        isFocused = false;
        transform.localScale = originalScale;
        if (img != null)
            img.color = originalColor;
    }
}
