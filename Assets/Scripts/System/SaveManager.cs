using RPG;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // ★ 改用新輸入系統
using System.Linq; // 引用 Linq 方便查詢
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{

    public static SaveData CurrentData { get; private set; } = new SaveData();
    public static SaveManager Instance { get; private set; }

    [Header("技能資料庫 (必須包含遊戲內所有技能)")]
    public List<SkillData> allSkillsLibrary;

    // ============================================================
    // ★ [新增] 新遊戲預設配置 (New Game Defaults)
    // ============================================================
    [Header("新遊戲預設配置")]
    [Tooltip("預設的固定普攻 (Slot 0)")]
    public SkillData defaultFixedNormal;

    [Tooltip("預設的固定大招 (Slot 2)")]
    public SkillData defaultFixedUltimate;

    [Tooltip("預設的技能組清單")]
    public List<DefaultSkillGroupConfig> defaultSkillGroups;

    // 定義一個簡單的結構，讓你可以在 Inspector 設定每一組的預設值
    [System.Serializable]
    public struct DefaultSkillGroupConfig
    {
        public string groupName;
        public SkillData normalSkill;   // Slot 1
        public SkillData ultimateSkill; // Slot 3
    }
    // ============================================================

    [Header("Player 尋找設定")]
    public string playerTag = "Player";

    [Header("主選單場景名（僅供他處參考，不在此自動存檔）")]
    public string mainMenuSceneName = "MainMenuScene";

    // 讀檔後等待進場定位 / 還原數值用
    private bool hasPendingSpawn;
    private string pendingScene;
    private Vector2 pendingPos;

    // 讀檔後暫存的數值（跨場景時在 OnSceneLoaded 才能套）
    private SaveData _loadedData;
    // [新增] 補上這行定義
    private static readonly SaveData DEFAULT_SAVE_DATA = new SaveData();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        // ★ 新輸入系統：F5 存檔、F9 讀檔
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.f5Key.wasPressedThisFrame) SaveNow();
            if (kb.f9Key.wasPressedThisFrame) LoadNow();
        }
    }

    // —— 自動存檔點（退出 / 進入背景）——
    void OnApplicationQuit() { SaveNow(); }
    void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

    // —— 對外 API —— 
    public void SaveNow()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        var p = player.transform.position;
        float vol = Mathf.Clamp01(AudioListener.volume);

        var stats = player.GetComponent<PlayerStats>();
        var levelAgent = player.GetComponent<PlayerLevel>(); // [新增] 獲取 PlayerLevel
        var mainPoint = player.GetComponent<MainPointComponent>(); // [新增] 抓取主屬性組件
        var caster = player.GetComponentInChildren<SkillCaster>(true); // [新增] 抓 SkillCaster
        // [修改] 改用 GetComponentInChildren(true)
        var pm = player.GetComponentInChildren<PassiveManager>(true); // ★ 這裡修改
        var data = new SaveData(SceneManager.GetActiveScene().name, p.x, p.y, vol);
        // 將靜態資料中的頁面索引拷貝到要儲存的新資料中
        data.pageMainLastPageIndex = CurrentData.pageMainLastPageIndex;
        if (stats != null)
        {
            data.playerHP = stats.CurrentHP;
            data.playerMP = stats.CurrentMP;
            data.playerMaxHP = stats.MaxHP;
            data.playerMaxMP = stats.MaxMP;
        }
        if (levelAgent != null) // [新增] 儲存等級系統資料
        {
            data.playerLevel = levelAgent.Level;
            data.playerCurrentExp = levelAgent.CurrentExp;
            data.playerUnspentPoints = levelAgent.UnspentStatPoints;
        }
        // [存檔] 儲存屬性點數
        if (mainPoint != null)
        {
            data.statAttack = mainPoint.Attack;
            data.statDefense = mainPoint.Defense;
            data.statAgility = mainPoint.Agility;
            data.statTechnique = mainPoint.Technique;
            data.statHPStat = mainPoint.HPStat;
            data.statMPStat = mainPoint.MPStat;
        }

        // [新增] 儲存技能組
        if (caster != null)
        {
            // 1. 存固定技能 ID (注意 null 檢查)
            data.fixedNormalSkillID = caster.fixedNormalSkill ? caster.fixedNormalSkill.skillID : "";
            data.fixedUltimateSkillID = caster.fixedUltimateSkill ? caster.fixedUltimateSkill.skillID : "";

            data.currentSkillGroupIndex = caster.currentSkillGroupIndex;

            // 2. 存技能組清單 (存 ID)
            data.skillGroups = new List<SaveData.SavedSkillGroup>();
            foreach (var group in caster.skillGroups)
            {
                var saveGroup = new SaveData.SavedSkillGroup
                {
                    groupName = group.groupName,
                    normalSkillID = group.switchableNormal ? group.switchableNormal.skillID : "",
                    ultimateSkillID = group.switchableUltimate ? group.switchableUltimate.skillID : ""
                };
                data.skillGroups.Add(saveGroup);
            }
        }
        // =================被動技能組=========================
        if (pm != null)
        {
            data.currentPassiveGroupIndex = pm.currentGroupIndex;

            // ★ 新增：儲存生效索引
            data.appliedPassiveGroupIndex = pm.appliedGroupIndex;

            data.passiveGroups = new List<SaveData.SavedPassiveGroup>();

            foreach (var group in pm.passiveGroups)
            {
                var saveGroup = new SaveData.SavedPassiveGroup
                {
                    groupName = group.groupName,
                    skillIDs = new List<string>()
                };

                // 把這一組的所有技能轉成 ID 存起來
                foreach (var skill in group.slots)
                {
                    saveGroup.skillIDs.Add(skill != null ? skill.skillID : "");
                }
                data.passiveGroups.Add(saveGroup);
            }
        }
        // ==========================================

        SaveSystem.Save(data);
    }

    public void LoadNow()
    {
        if (!SaveSystem.TryLoad(out var data))
        {
            Debug.LogWarning("[SaveManager] No save file to load.");
            return;
        }
        // 載入成功時，更新靜態資料 (讓 PageMain 可以存取)
        CurrentData = data;
        AudioListener.volume = (data.masterVolume > 0f) ? data.masterVolume : 1f;

        var current = SceneManager.GetActiveScene().name;
        pendingPos = new Vector2(data.playerX, data.playerY);

        _loadedData = data;

        if (current == data.sceneName)
        {
            PlacePlayerAt(pendingPos);
            ApplyLoadedStatsIfPossible();
            hasPendingSpawn = false;
            _loadedData = null;
        }
        else
        {
            hasPendingSpawn = true;
            pendingScene = data.sceneName;
            Time.timeScale = 1f;
            SceneManager.LoadScene(data.sceneName);
        }
    }

    public void DeleteSave()
    {
        SaveSystem.Delete();
        Debug.Log("[SaveManager] Save deleted.");
    }

    // —— 內部 —— 
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (hasPendingSpawn && scene.name == pendingScene)
        {
            PlacePlayerAt(pendingPos);
            hasPendingSpawn = false;
            pendingScene = null;
        }

        if (_loadedData != null)
        {
            ApplyLoadedStatsIfPossible();
            _loadedData = null;
        }
    }

    void ApplyLoadedStatsIfPossible()
    {
        var dataToApply = _loadedData ?? DEFAULT_SAVE_DATA; // 確保有資料

        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;
        var mainPoint = player.GetComponent<MainPointComponent>(); // [新增]
        var stats = player.GetComponent<PlayerStats>();
        var levelAgent = player.GetComponent<PlayerLevel>(); // [新增] 獲取 PlayerLevel

        // 1. 先載入屬性點 (因為這會影響 MaxHP/MaxMP)
        if (mainPoint != null)
        {
            // 使用我們剛寫好的方法，將存檔中的數值填回去
            mainPoint.LoadStats(
                dataToApply.statAttack,
                dataToApply.statDefense,
                dataToApply.statAgility,
                dataToApply.statTechnique,
                dataToApply.statHPStat,
                dataToApply.statMPStat
            );
        }

        // 2. 再載入等級 (雖然等級通常不影響屬性，但保持順序良好)
        if (levelAgent != null)
        {
            levelAgent.SetData(
                dataToApply.playerLevel,
                dataToApply.playerCurrentExp,
                dataToApply.playerUnspentPoints
            );
        }

        // 3. 最後載入當前 HP/MP (這時 MaxHP 已經因為第 1 步更新正確了)
        if (stats != null)
        {
            // ... (原本的 HP/MP 載入邏輯保持不變) ...
            if (dataToApply.playerMaxHP > 0f) stats.MaxHP = dataToApply.playerMaxHP;
            if (dataToApply.playerMaxMP > 0f) stats.MaxMP = dataToApply.playerMaxMP;

            stats.SetStats(
                Mathf.Clamp(dataToApply.playerHP, 0f, stats.MaxHP),
                Mathf.Clamp(dataToApply.playerMP, 0f, stats.MaxMP)
            );
        }
        // [新增] 載入技能組
        var caster = player.GetComponentInChildren<SkillCaster>(true);
        if (caster != null)
        {
            // 1. 還原固定技能 (用 FindSkillByID)
            caster.fixedNormalSkill = FindSkillByID(dataToApply.fixedNormalSkillID);
            caster.fixedUltimateSkill = FindSkillByID(dataToApply.fixedUltimateSkillID);

            // 2. 還原技能組
            if (dataToApply.skillGroups != null && dataToApply.skillGroups.Count > 0)
            {
                caster.skillGroups.Clear();

                foreach (var savedGroup in dataToApply.skillGroups)
                {
                    var newGroup = new SkillGroup
                    {
                        groupName = savedGroup.groupName,
                        switchableNormal = FindSkillByID(savedGroup.normalSkillID),
                        switchableUltimate = FindSkillByID(savedGroup.ultimateSkillID)
                    };
                    caster.skillGroups.Add(newGroup);
                }
            }
            caster.SetSkillGroupIndex(dataToApply.currentSkillGroupIndex);
        }

        // =================被動技能組=========================
        // [修改] 改用 GetComponentInChildren(true)
        var pm = player.GetComponentInChildren<PassiveManager>(true);
        if (pm != null)
        {
            // 1. 還原群組資料
            if (dataToApply.passiveGroups != null && dataToApply.passiveGroups.Count > 0)
            {
                pm.passiveGroups.Clear();
                foreach (var savedGroup in dataToApply.passiveGroups)
                {
                    var newGroup = new PassiveManager.PassiveSkillGroup
                    {
                        groupName = savedGroup.groupName,
                        slots = new List<SkillData>()
                    };

                    // 還原技能
                    foreach (var id in savedGroup.skillIDs)
                    {
                        newGroup.slots.Add(FindSkillByID(id));
                    }
                    pm.passiveGroups.Add(newGroup);
                }
            }

            // 2. 還原 UI 編輯索引
            pm.currentGroupIndex = dataToApply.currentPassiveGroupIndex;

            // 3. ★ 還原並套用生效索引
            // 注意：這裡呼叫 ApplyGroup 會自動觸發效果套用，所以不需要再手動呼叫 ApplyPassives
            // 如果存檔中的 appliedIndex 是有效的，就套用它；否則設為 -1 (無生效)

            // 檢查欄位是否存在 (舊存檔可能沒有這個欄位，預設是 0 或 -1 需要小心)
            // 這裡假設 int 預設是 0，為了兼容舊存檔，我們可以加個檢查
            // 但因為我們加了 public int appliedPassiveGroupIndex = -1; 在 SaveData 初始值
            // 所以如果是新存檔應該沒問題。

            int indexToApply = dataToApply.appliedPassiveGroupIndex;

            // 強制套用 (forceRefresh = true)，確保效果在載入後立即執行
            if (indexToApply >= 0 && indexToApply < pm.passiveGroups.Count)
            {
                pm.ApplyGroup(indexToApply, true);
            }
            else
            {
                pm.appliedGroupIndex = -1; // 確保狀態正確
            }
        }
        // ==========================================
    }

    // [修改] 搜尋方法改為比對 ID
    SkillData FindSkillByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        // 使用 Linq 搜尋 (需引用 System.Linq)
        // 確保您的 allSkillsLibrary 已經在 Inspector 裡填滿了所有技能
        return allSkillsLibrary.FirstOrDefault(s => s.skillID == id);
    }
    void PlacePlayerAt(Vector2 pos)
    {
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning("[SaveManager] Load: Player not found in scene.");
            return;
        }

        player.transform.position = pos;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = Vector2.zero;
    }

    // [修改] 這是您提到的「初始化的檔案」
    public void PrepareNewGame()
    {
        // 1. 建立一個全新的 SaveData
        var initData = new SaveData();

        // 2. 填入初始數值 (1等, 0經驗, 30點, 滿血滿魔)
        initData.playerLevel = 1;
        initData.playerCurrentExp = 0;
        initData.playerUnspentPoints = 30;
        initData.playerHP = 99999f; // 強制補滿
        initData.playerMP = 99999f;
        initData.playerMaxHP = 0;
        initData.playerMaxMP = 0;
        initData.pageMainLastPageIndex = 0; // 重置 UI 頁籤位置

        // ============================================================
        // ★ [新增] 將 Inspector 設定的預設技能寫入存檔
        // ============================================================

        // A. 寫入固定技能 ID
        initData.fixedNormalSkillID = defaultFixedNormal != null ? defaultFixedNormal.skillID : "";
        initData.fixedUltimateSkillID = defaultFixedUltimate != null ? defaultFixedUltimate.skillID : "";

        // B. 寫入技能組
        initData.skillGroups = new List<SaveData.SavedSkillGroup>();

        if (defaultSkillGroups != null && defaultSkillGroups.Count > 0)
        {
            // 如果你有在 Inspector 設定預設組，就用你設定的
            foreach (var config in defaultSkillGroups)
            {
                var savedGroup = new SaveData.SavedSkillGroup
                {
                    // 如果沒填名字就給預設值
                    groupName = string.IsNullOrEmpty(config.groupName) ? $"技能組 {initData.skillGroups.Count + 1}" : config.groupName,
                    // 轉成 ID 存起來
                    normalSkillID = config.normalSkill != null ? config.normalSkill.skillID : "",
                    ultimateSkillID = config.ultimateSkill != null ? config.ultimateSkill.skillID : ""
                };
                initData.skillGroups.Add(savedGroup);
            }
        }
        else
        {
            // 如果你沒設定任何預設組，我們至少給一組空的，避免報錯
            // 或者：留空讓系統去讀 Player Prefab 上的設定 (依你的需求)
            // 這裡示範：自動給一組空的 "預設組"
            initData.skillGroups.Add(new SaveData.SavedSkillGroup
            {
                groupName = "預設組",
                normalSkillID = "",
                ultimateSkillID = ""
            });
        }

        initData.currentSkillGroupIndex = 0;

        // ============================================================
        //預設被動技能組
        // ============================================================
        initData.passiveGroups = new List<SaveData.SavedPassiveGroup>();
        // 預設給一組空的
        var defaultPassiveGroup = new SaveData.SavedPassiveGroup
        {
            groupName = "預設被動組",
            skillIDs = new List<string>() // 裡面看你要不要塞預設技能 ID
        };
        // 補滿空字串 (對應 maxSlots，假設是 5)
        for (int i = 0; i < 5; i++) defaultPassiveGroup.skillIDs.Add("");

        initData.passiveGroups.Add(defaultPassiveGroup);
        initData.currentPassiveGroupIndex = 0;

        // 新遊戲預設沒有生效的被動組 (或者您可以設為 0)
        initData.appliedPassiveGroupIndex = -1;
        // ============================================================

        // 3. 關鍵步驟：把它存入 _loadedData
        _loadedData = initData;

        Debug.Log($"[SaveManager] New game prepared. (Groups: {initData.skillGroups.Count})");
    }
}
