
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonKey : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("對應快捷鍵（例如 Return, Space, E）")]
    public KeyCode hotkey = KeyCode.Return;

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
        // 熱鍵觸發（可用在任何 UI 按鈕）
        if (isFocused && Input.GetKeyDown(hotkey))
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
