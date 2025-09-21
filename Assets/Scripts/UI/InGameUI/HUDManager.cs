using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class HUDManager : MonoBehaviour
{
    [Header("玩家數值來源")]
    public PlayerStats playerStats;

    [Header("半圓影像")]
    public Image halfHP;   // 左半圓（紅）
    public Image halfMP;   // 右半圓（藍）
    public Image portrait;

    [Header("數值文字")]
    public TextMeshProUGUI textHP;
    public TextMeshProUGUI textMP;

    [Header("按鈕")]
    public Button btnBag;
    public Button btnMap;

    [Header("主選單可見性")]
    public bool hideInMainMenu = true;
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (btnBag) btnBag.onClick.AddListener(() => Debug.Log("Open Bag (TODO)"));
        if (btnMap) btnMap.onClick.AddListener(() => Debug.Log("Open Map (TODO)"));

        // 初始化半圓：HP +90° 順時針、MP -90° 逆時針
        SetupHalfRadial360(halfHP, Image.Origin360.Left, true, +90f);
        SetupHalfRadial360(halfMP, Image.Origin360.Right, false, -90f);

        RefreshAll();

        if (hideInMainMenu)
        {
            var now = SceneManager.GetActiveScene().name;
            SetVisible(now != mainMenuSceneName);
            SceneManager.activeSceneChanged += OnSceneChangedVisible;
        }

        // 場景切換時自動重新綁定 PlayerStats
        SceneManager.activeSceneChanged += OnSceneChangedRebind;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChangedVisible;
        SceneManager.activeSceneChanged -= OnSceneChangedRebind;
        if (playerStats != null)
            playerStats.OnStatsChanged -= RefreshAll;
    }

    void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= RefreshAll;
            playerStats.OnStatsChanged += RefreshAll;
        }
        RefreshAll();
    }

    void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnStatsChanged -= RefreshAll;
    }

    public void RefreshAll()
    {
        if (playerStats == null) return;

        float hp = Mathf.Max(0f, playerStats.CurrentHP);
        float mp = Mathf.Max(0f, playerStats.CurrentMP);
        float maxHp = Mathf.Max(1f, playerStats.MaxHP);
        float maxMp = Mathf.Max(1f, playerStats.MaxMP);

        if (halfHP) halfHP.fillAmount = (hp / maxHp) * 0.5f;
        if (halfMP) halfMP.fillAmount = (mp / maxMp) * 0.5f;

        if (textHP) textHP.text = $"{(int)hp}/{(int)maxHp}";
        if (textMP) textMP.text = $"{(int)mp}/{(int)maxMp}";
    }

    void SetVisible(bool on)
    {
        var hvc = FindObjectOfType<HUDVisibilityController>();
        if (hvc != null)
        {
            if (on) HUDVisibilityController.ShowHUD();
            else HUDVisibilityController.HideHUD();
            return;
        }

        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = on ? 1f : 0f;
            cg.interactable = on;
            cg.blocksRaycasts = on;
        }
        else
        {
            gameObject.SetActive(on);
        }
    }

    // 設定半圓（Radial360 + 旋轉角度）
    void SetupHalfRadial360(Image img, Image.Origin360 origin, bool clockwise, float rotateZ)
    {
        if (img == null) return;

        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)origin;
        img.fillClockwise = clockwise;
        img.fillAmount = 0.5f;

        var rt = img.rectTransform;
        var euler = rt.localEulerAngles;
        euler.z = rotateZ;
        rt.localEulerAngles = euler;
    }

    // —— 場景切換：可見性 —— 
    void OnSceneChangedVisible(Scene oldScene, Scene newScene)
    {
        if (!hideInMainMenu) return;
        SetVisible(newScene.name != mainMenuSceneName);
    }

    // —— 場景切換：重新綁定 PlayerStats —— 
    void OnSceneChangedRebind(Scene oldScene, Scene newScene)
    {
        RebindPlayerStats();
    }

    void RebindPlayerStats()
    {
        if (playerStats != null)
            playerStats.OnStatsChanged -= RefreshAll;

        var p = GameObject.FindGameObjectWithTag("Player");
        var stats = (p != null) ? p.GetComponent<PlayerStats>() : FindObjectOfType<PlayerStats>();
        playerStats = stats;

        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= RefreshAll;
            playerStats.OnStatsChanged += RefreshAll;
            RefreshAll();
        }
        else
        {
            // 沒抓到就清空顯示（避免殘值）
            if (halfHP) halfHP.fillAmount = 0f;
            if (halfMP) halfMP.fillAmount = 0f;
            if (textHP) textHP.text = "--/--";
            if (textMP) textMP.text = "--/--";
        }
    }
}
