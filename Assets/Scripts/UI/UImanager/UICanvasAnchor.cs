using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UICanvasAnchor : MonoBehaviour
{
    [Tooltip("外層 key（mainmenu 或 system）")]
    public string key = "mainmenu";

    CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (string.IsNullOrEmpty(key))
            Debug.LogWarning("[UICanvasAnchor] key 未設定");
    }

    void OnEnable() { if (!string.IsNullOrEmpty(key)) UIEvents.RegisterCanvas(key, cg); }
    void OnDisable() { if (!string.IsNullOrEmpty(key)) UIEvents.UnregisterCanvas(key); }
}
