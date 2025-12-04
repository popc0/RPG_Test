using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG;

public class PageStatus : MonoBehaviour
{
    [Header("基本資訊 UI")]
    public TextMeshProUGUI textLevel;
    public TextMeshProUGUI textExp;    // 顯示數值文字 "100 / 200"

    // [修改] 改用 Image 來控制進度條 (需設為 Filled 模式)
    public Image imageExpFill;

    public TextMeshProUGUI textPoints; // 顯示 "剩餘點數"

    [Header("六角能力圖")]
    public UIRadarChart radarChart; // [新增] 拖曳上面的腳本物件

    // [修改] 將原本的固定上限改為「最小刻度」
    [Tooltip("雷達圖刻度的最小值 (避免數值很低時圖形看起來是滿的)")]
    public float minRadarScale = 10f;

    [Header("屬性列 (請將 UI 拖曳到對應欄位)")]
    public StatusRow rowAttack;
    public StatusRow rowDefense;
    public StatusRow rowAgility;
    public StatusRow rowTechnique;
    public StatusRow rowMaxHP;
    public StatusRow rowMaxMP;

    [System.Serializable]
    public class StatusRow
    {
        public TextMeshProUGUI valueText; // 顯示屬性數值
        public Button plusButton;         // 加點按鈕
        [HideInInspector] public string statKey; // 內部用的 Key
    }

    private PlayerLevel _playerLevel;
    private MainPointComponent _mainPoint;

    void Awake()
    {
        // 設定對應屬性 Key (需對應 MainPointComponent 的屬性名)
        rowAttack.statKey = "Attack";
        rowDefense.statKey = "Defense";
        rowAgility.statKey = "Agility";
        rowTechnique.statKey = "Technique";
        rowMaxHP.statKey = "HPStat";
        rowMaxMP.statKey = "MPStat";

        BindButton(rowAttack);
        BindButton(rowDefense);
        BindButton(rowAgility);
        BindButton(rowTechnique);
        BindButton(rowMaxHP);
        BindButton(rowMaxMP);
    }

    void BindButton(StatusRow row)
    {
        if (row.plusButton)
            row.plusButton.onClick.AddListener(() => OnClickPlus(row.statKey));
    }

    void OnEnable()
    {
        FindPlayer();
        if (_playerLevel)
        {
            _playerLevel.OnExpChanged += OnExpChanged;
            _playerLevel.OnLevelUp += OnLevelUp;
            _playerLevel.OnStatPointsChanged += OnStatPointsChanged;
        }
        if (_mainPoint)
        {
            _mainPoint.OnStatChanged += OnStatChanged;
        }
        RefreshUI();
    }

    void OnDisable()
    {
        if (_playerLevel)
        {
            _playerLevel.OnExpChanged -= OnExpChanged;
            _playerLevel.OnLevelUp -= OnLevelUp;
            _playerLevel.OnStatPointsChanged -= OnStatPointsChanged;
        }
        if (_mainPoint)
        {
            _mainPoint.OnStatChanged -= OnStatChanged;
        }
    }

    void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p)
        {
            _playerLevel = p.GetComponent<PlayerLevel>();
            _mainPoint = p.GetComponent<MainPointComponent>();
        }
    }

    void OnExpChanged(float cur, float max) => RefreshUI();
    void OnLevelUp(int level) => RefreshUI();
    void OnStatPointsChanged() => RefreshUI();
    void OnStatChanged() => RefreshUI();

    void OnClickPlus(string key)
    {
        if (_playerLevel) _playerLevel.SpendStatPoint(key);
    }

    void RefreshUI()
    {
        if (!_playerLevel || !_mainPoint) return;

        // 1. 更新文字與經驗條 (保持不變)
        if (textLevel) textLevel.text = $"Lv.{_playerLevel.Level}";
        if (textPoints) textPoints.text = $"剩餘點數: {_playerLevel.UnspentStatPoints}";

        float nextExp = _playerLevel.ExpToNextLevel;
        float ratio = (nextExp > 0) ? _playerLevel.CurrentExp / nextExp : 0f;
        if (textExp) textExp.text = $"{_playerLevel.CurrentExp:F0} / {nextExp:F0}";
        if (imageExpFill) imageExpFill.fillAmount = ratio;

        // 2. 更新屬性數值文字 (保持不變)
        bool canSpend = _playerLevel.UnspentStatPoints > 0;
        UpdateRow(rowAttack, _mainPoint.Attack, canSpend);
        UpdateRow(rowDefense, _mainPoint.Defense, canSpend);
        UpdateRow(rowAgility, _mainPoint.Agility, canSpend);
        UpdateRow(rowTechnique, _mainPoint.Technique, canSpend);
        UpdateRow(rowMaxHP, _mainPoint.HPStat, canSpend);
        UpdateRow(rowMaxMP, _mainPoint.MPStat, canSpend);

        // 3. [修改] 更新雷達圖：自動計算最大值
        if (radarChart != null)
        {
            // [修改] 讀取時加上 MainPointComponent.BASE_VALUE (10)，讓圖表反映真實戰力
            // 或者直接用 _mainPoint.MP.HPStat (我們剛改好的屬性)

            var mp = _mainPoint.MP; // 這是包含基礎值 10 的總屬性結構

            float v0 = mp.HPStat;
            float v1 = mp.Attack;
            float v2 = mp.Defense;
            float v3 = mp.MPStat;
            float v4 = mp.Agility;
            float v5 = mp.Technique;

            // 步驟 A: 找出這 6 個屬性中「最大」的那一個
            float highestStat = Mathf.Max(v0, v1, v2, v3, v4, v5);

            // 步驟 B: 決定圖表的縮放比例 (Scale)
            // 取「設定的最小值」與「當前最高屬性」之間的較大者
            // 例子：如果 minRadarScale 是 100，你的最高屬性是 50，則 Scale 用 100（圖會畫一半滿）
            // 例子：如果你的最高屬性長到了 500，則 Scale 用 500（最高的那角會頂到邊邊）
            float currentScale = Mathf.Max(minRadarScale, highestStat);

            // 避免除以 0
            if (currentScale <= 0.001f) currentScale = 1f;

            // 步驟 C: 正規化並填入陣列
            float[] stats = new float[6];
            stats[0] = v0 / currentScale;
            stats[1] = v1 / currentScale;
            stats[2] = v2 / currentScale;
            stats[3] = v3 / currentScale;
            stats[4] = v4 / currentScale;
            stats[5] = v5 / currentScale;

            radarChart.SetValues(stats);
        }
    }

    void UpdateRow(StatusRow row, float val, bool canSpend)
    {
        if (row.valueText) row.valueText.text = val.ToString("F0");
        if (row.plusButton) row.plusButton.interactable = canSpend;
    }
}