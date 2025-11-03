using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DamageHUDFeed : MonoBehaviour
{
    [Header("HUD 元件")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    [Header("圖示（可選）")]
    public Image iconImage;
    public Sprite defaultSprite;

    [Header("顯示/隱藏控制")]
    [Tooltip("要一起顯示/隱藏的容器；不指定時就用本物件。")]
    public GameObject contentRoot;

    [Tooltip("有受傷事件時自動顯示 HUD。")]
    public bool autoShowOnDamage = true;

    [Tooltip("顯示多久後自動隱藏（0 表示不自動隱藏）。")]
    public float displayDuration = 2f;

    [Header("淡出效果（可選）")]
    [Tooltip("勾選後，隱藏時以 CanvasGroup 做淡出。")]
    public bool useFadeOut = false;

    [Tooltip("淡出所需時間（秒）。")]
    public float fadeOutDuration = 0.25f;

    [Tooltip("使用不受 Time.timeScale 影響的時間。")]
    public bool useUnscaledTime = true;

    // ───────────── 內部 ─────────────
    float hideTimer;
    CanvasGroup cg;
    Coroutine fadeCo;

    void Reset()
    {
        contentRoot = null; // 預設用本物件
    }

    void Awake()
    {
        if (!contentRoot) contentRoot = gameObject;

        if (useFadeOut)
        {
            cg = contentRoot.GetComponent<CanvasGroup>();
            if (!cg) cg = contentRoot.AddComponent<CanvasGroup>();
        }

        // 初始隱藏
        SetVisible(false, immediate: true);
    }

    void OnEnable()
    {
        RPG.EffectApplier.OnAnyDamaged += HandleAnyDamaged;
    }

    void OnDisable()
    {
        RPG.EffectApplier.OnAnyDamaged -= HandleAnyDamaged;
    }

    void Update()
    {
        if (displayDuration <= 0f) return; // 永久顯示，不自動隱藏

        if (hideTimer > 0f)
        {
            hideTimer -= (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            if (hideTimer <= 0f)
            {
                // 到時關閉
                if (useFadeOut && fadeOutDuration > 0f)
                {
                    // 淡出到不可見再 Deactivate
                    if (fadeCo != null) StopCoroutine(fadeCo);
                    fadeCo = StartCoroutine(CoFadeOutAndHide());
                }
                else
                {
                    SetVisible(false, immediate: true);
                }
            }
        }
    }

    void HandleAnyDamaged(string displayName, float curHP, float maxHP, float curMP, float maxMP)
    {
        if (nameText) nameText.text = displayName;
        if (hpText) hpText.text = $"{Mathf.CeilToInt(curHP)}/{Mathf.CeilToInt(maxHP)}";
        if (mpText) mpText.text = $"{Mathf.CeilToInt(curMP)}/{Mathf.CeilToInt(maxMP)}";

        if (iconImage != null)
        {
            if (defaultSprite) iconImage.sprite = defaultSprite;
            iconImage.enabled = true;
        }

        if (autoShowOnDamage)
        {
            ShowNow();
        }
    }

    // ───────────── 顯示/隱藏 ─────────────
    public void ShowNow(float? customDuration = null)
    {
        if (fadeCo != null) { StopCoroutine(fadeCo); fadeCo = null; }
        SetVisible(true, immediate: true);

        // 重設計時
        hideTimer = (customDuration.HasValue ? customDuration.Value : displayDuration);
    }

    public void HideNow()
    {
        if (fadeCo != null) { StopCoroutine(fadeCo); fadeCo = null; }
        if (useFadeOut && fadeOutDuration > 0f)
            fadeCo = StartCoroutine(CoFadeOutAndHide());
        else
            SetVisible(false, immediate: true);
    }

    void SetVisible(bool visible, bool immediate)
    {
        if (!contentRoot) return;

        if (useFadeOut && cg != null)
        {
            // 需要先 Active 才看得到淡入/淡出
            if (visible)
            {
                if (!contentRoot.activeSelf) contentRoot.SetActive(true);
                cg.alpha = immediate ? 1f : cg.alpha;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else
            {
                cg.alpha = immediate ? 0f : cg.alpha;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                if (immediate && contentRoot.activeSelf) contentRoot.SetActive(false);
            }
        }
        else
        {
            // 直接顯示/隱藏容器
            contentRoot.SetActive(visible);
        }
    }

    System.Collections.IEnumerator CoFadeOutAndHide()
    {
        if (!cg) yield break;

        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeOutDuration);
        float start = cg.alpha;
        float end = 0f;

        while (t < dur)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            float k = Mathf.Clamp01(t / dur);
            cg.alpha = Mathf.Lerp(start, end, k);
            yield return null;
        }

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (contentRoot.activeSelf) contentRoot.SetActive(false);
        fadeCo = null;
    }
}
