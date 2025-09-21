using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        // 不在這裡自動因切到 MainMenu 而存檔
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        // 測試熱鍵（可保留）
        if (Input.GetKeyDown(KeyCode.F5)) SaveNow();
        if (Input.GetKeyDown(KeyCode.F9)) LoadNow();
    }

    // —— 自動存檔點（退出 / 進入背景）——
    void OnApplicationQuit()
    {
        SaveNow(); // 退出前存檔（在遊玩場景才會成功）
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow(); // 切到背景時存（主選單會自動略過）
    }

    // —— 對外 API —— 
    public void SaveNow()
    {
        // 在主選單就不做（避免找不到 Player）
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            return;

        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            // 例如切場景過程中略過即可
            return;
        }

        var p = player.transform.position;
        float vol = Mathf.Clamp01(AudioListener.volume);

        // 取玩家數值
        var stats = player.GetComponent<PlayerStats>();
        var data = new SaveData(SceneManager.GetActiveScene().name, p.x, p.y, vol);
        if (stats != null)
        {
            data.playerHP = stats.CurrentHP;
            data.playerMP = stats.CurrentMP;
            data.playerMaxHP = stats.MaxHP;
            data.playerMaxMP = stats.MaxMP;
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

        // 先還原音量（不論是否切場景）
        AudioListener.volume = (data.masterVolume > 0f) ? data.masterVolume : 1f;

        var current = SceneManager.GetActiveScene().name;
        pendingPos = new Vector2(data.playerX, data.playerY);

        // 暫存數值（同場景會立刻用，跨場景到 OnSceneLoaded 用）
        _loadedData = data;

        if (current == data.sceneName)
        {
            // 同場景：直接定位 + 套數值
            PlacePlayerAt(pendingPos);
            ApplyLoadedStatsIfPossible();
            hasPendingSpawn = false;
            _loadedData = null;
        }
        else
        {
            // 跨場景：等載入完成再定位 + 套數值
            hasPendingSpawn = true;
            pendingScene = data.sceneName;
            Time.timeScale = 1f; // 切場景前確保不是暫停
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

        // 若剛載入過存檔，這裡把數值套回 Player
        if (_loadedData != null)
        {
            ApplyLoadedStatsIfPossible();
            _loadedData = null;
        }
    }

    void ApplyLoadedStatsIfPossible()
    {
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        var stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            // 還原 Max 再寫 Current
            if (_loadedData.playerMaxHP > 0f) stats.MaxHP = _loadedData.playerMaxHP;
            if (_loadedData.playerMaxMP > 0f) stats.MaxMP = _loadedData.playerMaxMP;

            stats.SetStats(
                Mathf.Clamp(_loadedData.playerHP, 0f, stats.MaxHP),
                Mathf.Clamp(_loadedData.playerMP, 0f, stats.MaxMP)
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
}
