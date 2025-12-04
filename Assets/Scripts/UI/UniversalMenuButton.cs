using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UniversalMenuButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    // [修改] 新增 Play 類型
    public enum ActionType { Start, Continue, Options, Quit, DeleteSave, Play }

    [Header("行為類型")]
    public ActionType action;

    [Header("對應快捷鍵（例如 Return, Space, E）")]
    public KeyCode hotkey = KeyCode.Return;

    [Header("主選單控制器（可自動尋找）")]
    public MainMenuController controller;

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

        if (controller == null)
            controller = FindObjectOfType<MainMenuController>();

        btn.onClick.RemoveAllListeners();
        if (controller != null)
        {
            switch (action)
            {
                // [新增] Play 類型對應到 OnClickPlay
                case ActionType.Play:btn.onClick.AddListener(controller.OnClickPlay);break;
                case ActionType.Options: btn.onClick.AddListener(controller.OnClickOtherOptions); break;
                case ActionType.Quit: btn.onClick.AddListener(controller.OnClickQuit); break;
                case ActionType.DeleteSave: btn.onClick.AddListener(controller.OnClickDeleteSave); break;
            }
        }
    }

    void Update()
    {
        // 熱鍵觸發
        if (isFocused && Input.GetKeyDown(hotkey))
            btn.onClick.Invoke();
    }

    // UI 聚焦狀態
    public void OnSelect(BaseEventData eventData)
    {
        isFocused = true;
        transform.localScale = originalScale * highlightScale;
        if (img != null)
            img.color = highlightColor;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isFocused = false;
        transform.localScale = originalScale;
        if (img != null)
            img.color = originalColor;
    }
}