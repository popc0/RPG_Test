using UnityEngine;

public class UIRootCanvasController : MonoBehaviour
{
    [Header("Managed canvases (CanvasGroup)")]
    [SerializeField] CanvasGroup canvasSystem;   // SystemCanvas
    [SerializeField] CanvasGroup canvasStory;    // Canvas_Story
    [SerializeField] CanvasGroup canvasTPHint;   // Canvas_TPHint（預設不互斥，可疊加）

    [Header("HUD behavior")]
    [SerializeField] CanvasGroup canvasHUD;                  // 可留空
    [SerializeField] bool disableHUDWhenOthersActive = false;

    [Header("Participation flags")]
    [SerializeField] bool includeTPHintInExclusion = false;  // 預設 false

    CanvasGroup currentActive;

    // 記錄 HUD 原始互動狀態
    bool hudOrigInteractable = true;
    bool hudOrigBlocksRaycasts = true;
    bool hudOrigCaptured = false;

    void Awake()
    {
        CaptureHUDOriginalState();
        // 預設外層都可見狀態由各自 Canvas 管；此控制器只負責互斥與互動權
    }

    void OnEnable()
    {
        UIEvents.OnOpenSystemCanvas += OpenSystem;
        UIEvents.OnOpenStoryCanvas += OpenStory;
        UIEvents.OnOpenTPHintCanvas += OpenTPHint;
        UIEvents.OnCloseActiveCanvas += CloseActive;
    }

    void OnDisable()
    {
        UIEvents.OnOpenSystemCanvas -= OpenSystem;
        UIEvents.OnOpenStoryCanvas -= OpenStory;
        UIEvents.OnOpenTPHintCanvas -= OpenTPHint;
        UIEvents.OnCloseActiveCanvas -= CloseActive;
    }

    void OpenSystem() => SetExclusive(canvasSystem);
    void OpenStory() => SetExclusive(canvasStory);

    void OpenTPHint()
    {
        if (canvasTPHint == null) return;

        if (includeTPHintInExclusion)
        {
            SetExclusive(canvasTPHint);
        }
        else
        {
            // 疊加提示：只開自身互動與可見，不影響 currentActive
            SetCGVisible(canvasTPHint, true);
            // 不改 HUD（提示通常不該封鎖 HUD）
        }
    }

    void CloseActive()
    {
        if (currentActive != null)
        {
            SetCGVisible(currentActive, false);
            currentActive = null;
        }
        RestoreHUD();
    }

    void SetExclusive(CanvasGroup target)
    {
        if (target == null) return;

        // 關閉其他互斥 Canvas（只用 CanvasGroup）
        if (canvasSystem && canvasSystem != target) SetCGVisible(canvasSystem, false);
        if (canvasStory && canvasStory != target) SetCGVisible(canvasStory, false);

        // TPHint 可選擇是否也互斥
        if (includeTPHintInExclusion && canvasTPHint && canvasTPHint != target)
            SetCGVisible(canvasTPHint, false);

        // 開啟目標
        SetCGVisible(target, true);
        currentActive = target;

        // HUD 只關互動不關顯示，之後 CloseActive 會復原
        ApplyHUDAccess(target);
    }

    // ===== 顯示/互動工具（只用 CanvasGroup，不關物件） =====

    static bool IsVisible(CanvasGroup cg)
    {
        if (!cg) return false;
        return cg.alpha > 0.001f && (cg.interactable || cg.blocksRaycasts);
    }

    static void SetCGVisible(CanvasGroup cg, bool visible)
    {
        if (!cg) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    void ApplyHUDAccess(CanvasGroup top)
    {
        if (!canvasHUD) return;

        CaptureHUDOriginalState();

        bool hudAccess = true;
        if (disableHUDWhenOthersActive && top != null && top != canvasHUD)
            hudAccess = false;

        // 只調互動，不動顯示
        canvasHUD.interactable = hudAccess;
        canvasHUD.blocksRaycasts = hudAccess;
    }

    void RestoreHUD()
    {
        if (!canvasHUD || !hudOrigCaptured) return;

        canvasHUD.interactable = hudOrigInteractable;
        canvasHUD.blocksRaycasts = hudOrigBlocksRaycasts;
    }

    void CaptureHUDOriginalState()
    {
        if (canvasHUD == null || hudOrigCaptured) return;
        hudOrigInteractable = canvasHUD.interactable;
        hudOrigBlocksRaycasts = canvasHUD.blocksRaycasts;
        hudOrigCaptured = true;
    }
}
