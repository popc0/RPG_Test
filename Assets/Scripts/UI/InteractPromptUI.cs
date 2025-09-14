using UnityEngine;
using TMPro;

public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI Instance { get; private set; }

    [SerializeField] private TMP_Text label;   // 指到你的 Label（TMP_Text）
    [SerializeField] private CanvasGroup group; // 指到 UI_InteractPrompt 的 CanvasGroup

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (group != null)
        {
            group.alpha = 0f;          // 預設隱藏
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        if (label != null) label.text = "";
    }

    public void Show(string msg)
    {
        if (label != null) label.text = msg;
        if (group != null) group.alpha = 1f;
    }

    public void Hide()
    {
        if (group != null) group.alpha = 0f;
        if (label != null) label.text = "";
    }
}
