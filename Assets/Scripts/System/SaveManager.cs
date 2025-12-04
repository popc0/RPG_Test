using RPG;
using UnityEngine;
using UnityEngine.InputSystem; // ★ 改用新輸入系統
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{

    public static SaveData CurrentData { get; private set; } = new SaveData();
    public static SaveManager Instance { get; private set; }

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
    // [新增] 這是您提到的「初始化的檔案」
    // 當玩家點擊 Start Game 時呼叫這個方法
    public void PrepareNewGame()
    {
        // 1. 建立一個全新的 SaveData (這就是初始化的檔案)
        var initData = new SaveData();

        // 2. 填入初始數值 (確保玩家從 1 等、0 經驗開始)
        // 注意：SceneName 這裡可以不填，因為 MainMenuController 會負責切換場景
        initData.playerLevel = 1;
        initData.playerCurrentExp = 0;
        initData.playerUnspentPoints = 30;

        // [修改] 將初始血魔設為很大的數字 (例如 99999)
        // 這樣在 ApplyLoadedStatsIfPossible 裡的 Mathf.Clamp 
        // 就會自動把它變成 MaxHP 和 MaxMP (即滿血滿魔)
        initData.playerHP = 99999f;
        initData.playerMP = 99999f;
        initData.playerMaxHP = 0; // 0 代表使用角色預設的最大血量
        initData.playerMaxMP = 0;

        // 3. 關鍵步驟：把它存入 _loadedData
        // 這樣等到場景載入完成 (OnSceneLoaded) 時，
        // 系統就會以為這是剛讀出來的存檔，並把這些初始值強制套用到場上的玩家身上
        _loadedData = initData;

        Debug.Log("[SaveManager] New game data prepared.");
    }
}
