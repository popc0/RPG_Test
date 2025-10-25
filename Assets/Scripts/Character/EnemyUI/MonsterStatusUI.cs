using UnityEngine;
using UnityEngine.UI;

public class MonsterStatusUI : MonoBehaviour
{
    [Header("來源（怪物本體或父物件）")]
    public PlayerStats stats;

    [Header("條件式自動綁定")]
    public bool autoBind = true;

    [Header("UI 參考（兩條直線）")]
    public Image hpBar;   // 橫向直條（從右消失）
    public Image mpBar;   // 橫向直條（從右消失）

    [Header("顯示選項")]
    public bool autoConfigureBar = true;
    public bool smoothLerp = true;
    public float lerpSpeed = 12f;

    float targetHpFill, targetMpFill;
    Canvas canvas;

    void Awake()
    {
        // 自動綁定 PlayerStats
        if (autoBind && !stats)
            stats = GetComponentInParent<PlayerStats>();

        // 綁事件
        if (stats != null)
            stats.OnStatsChanged += OnStatsChanged;

        // 自動設置條
        if (autoConfigureBar)
        {
            ConfigureBar(hpBar, fromRight: true);
            ConfigureBar(mpBar, fromRight: true);
        }

        // 自動尋找主攝影機
        canvas = GetComponent<Canvas>();
        if (canvas && canvas.renderMode == RenderMode.WorldSpace)
        {
            if (Camera.main != null)
                canvas.worldCamera = Camera.main;
        }

        ForceRefresh();
        ApplyImmediate();
    }

    void OnEnable()
    {
        // 避免重載場景後 camera 為 null
        if (canvas && canvas.worldCamera == null && Camera.main != null)
            canvas.worldCamera = Camera.main;
    }

    void OnDestroy()
    {
        if (stats != null)
            stats.OnStatsChanged -= OnStatsChanged;
    }

    void Update()
    {
        if (!smoothLerp) return;
        if (hpBar) hpBar.fillAmount = Mathf.MoveTowards(hpBar.fillAmount, targetHpFill, Time.deltaTime * lerpSpeed);
        if (mpBar) mpBar.fillAmount = Mathf.MoveTowards(mpBar.fillAmount, targetMpFill, Time.deltaTime * lerpSpeed);
    }

    void OnStatsChanged(float curHP, float maxHP, float curMP, float maxMP)
    {
        UpdateTargets(curHP, maxHP, curMP, maxMP);
    }

    public void ForceRefresh()
    {
        if (!stats) return;
        UpdateTargets(stats.CurrentHP, stats.MaxHP, stats.CurrentMP, stats.MaxMP);
    }

    public void ApplyImmediate()
    {
        if (hpBar) hpBar.fillAmount = targetHpFill;
        if (mpBar) mpBar.fillAmount = targetMpFill;
    }

    void UpdateTargets(float curHP, float maxHP, float curMP, float maxMP)
    {
        targetHpFill = SafeRatio(curHP, maxHP);
        targetMpFill = SafeRatio(curMP, maxMP);
    }

    float SafeRatio(float v, float max) => (max <= 0f) ? 0f : Mathf.Clamp01(v / max);

    void ConfigureBar(Image img, bool fromRight)
    {
        if (!img) return;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = fromRight ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
    }
}
