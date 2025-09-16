using UnityEngine;

public class HUDVisibilityController : MonoBehaviour
{
    public static HUDVisibilityController Instance { get; private set; }

    [Header("HUD 根物件（預設為此物件）")]
    [SerializeField] private GameObject hudRoot;

    [Header("（可選）用 CanvasGroup 控制")]
    [SerializeField] private CanvasGroup canvasGroup;

    void Awake()
    {
        Instance = this;
        if (hudRoot == null) hudRoot = gameObject;
        if (canvasGroup == null) canvasGroup = hudRoot.GetComponent<CanvasGroup>();

        // 保險：若 HUD Prefab 誤帶了 EventSystem，把多餘的清掉（全域只應有一個）
        var extraES = GetComponentInChildren<UnityEngine.EventSystems.EventSystem>();
        if (extraES != null) Destroy(extraES.gameObject);
    }

    public static void HideHUD()
    {
        var inst = Instance != null ? Instance : FindObjectOfType<HUDVisibilityController>();
        if (inst == null) return;
        inst.SetVisible(false);
    }

    public static void ShowHUD()
    {
        var inst = Instance != null ? Instance : FindObjectOfType<HUDVisibilityController>();
        if (inst == null) return;
        inst.SetVisible(true);
    }

    void SetVisible(bool on)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = on ? 1f : 0f;
            canvasGroup.interactable = on;
            canvasGroup.blocksRaycasts = on;
        }
        else if (hudRoot != null)
        {
            hudRoot.SetActive(on);
        }
    }
}
