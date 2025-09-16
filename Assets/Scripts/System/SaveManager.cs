using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Player 尋找設定")]
    public string playerTag = "Player";

    [Header("主選單場景名（僅供他處參考，不在此自動存檔）")]
    public string mainMenuSceneName = "MainMenu";

    // 讀檔後等待進場定位用
    private bool hasPendingSpawn;
    private string pendingScene;
    private Vector2 pendingPos;

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
        var data = new SaveData(SceneManager.GetActiveScene().name, p.x, p.y, vol);
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

        if (current == data.sceneName)
        {
            PlacePlayerAt(pendingPos);
            hasPendingSpawn = false;
        }
        else
        {
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
