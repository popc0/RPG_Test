using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("進入的第一個遊戲場景")]
    public string firstSceneName = "Scene1";

    [Header("第一個落點 SpawnId（要和場景內 SpawnPoint.spawnId 對上）")]
    public string firstSpawnId = "Start";

    [Header("玩家預置（主選單先生成，讓 HUD 能綁定）")]
    public GameObject playerPrefab;

    [Header("（可選）繼續遊戲按鈕")]
    public Button continueButton;

    [Header("主畫面預設焦點")]
    public Selectable defaultMainButton;

    [Header("其他選項（兩段式：先上，再左）")]
    [Tooltip("水平滑動的 Page_Options 本體")]
    public IngameMenuSlide pageOptionsSlide;           // Page_Options（Hidden=(800,0), Shown=(0,0)）
    [Tooltip("只在主選單使用：垂直滑動的 Wrapper（Page_Options 的父）")]
    public IngameMenuSlide pageOptionsWrapperSlide;    // Wrapper（Hidden=(0,-800), Shown=(0,0)）
    [Tooltip("Page_Options 的根物件（用於 SetActive 控制）")]
    public GameObject pageOptionsRoot;                 // Page_Options 的 GameObject

    [Header("按鍵")]
    public KeyCode escKey = KeyCode.Escape; // 關 Options 用（主選單）

    void Start()
    {
        Time.timeScale = 1f;

        if (continueButton != null)
            continueButton.interactable = SaveSystem.Exists();

        // 初始化：確保 Page_Options 關閉（主選單需要兩段皆關）
        if (pageOptionsRoot != null) pageOptionsRoot.SetActive(true); // Slide 需要啟用才有動畫
        if (pageOptionsSlide != null) pageOptionsSlide.Close();
        if (pageOptionsWrapperSlide != null) pageOptionsWrapperSlide.Close();

        SafeSetFocus(defaultMainButton);
    }

    void Update()
    {
        if (Input.GetKeyDown(escKey) && IsOptionsOpen())
        {
            OnClickOptionsBack();
        }
    }

    // ====== 主功能 ======

    public void OnClickStart()
    {
        // 1) 主選單就先確保場上有 Player，讓 Canvas_HUD 能立即綁定
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null && playerPrefab != null)
        {
            player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            // PlayerLifetime 會自動 DontDestroyOnLoad & 去重（請掛在 Player.prefab 上）
        }

        // 2) 指定起始場景與落點（交給你現有的機制）
        TeleportRequest.hasPending = true;
        TeleportRequest.sceneName = firstSceneName;
        TeleportRequest.spawnId = firstSpawnId; // SpawnSystem 會對應場景內的 SpawnPoint.spawnId

        // 3) 切場景，SpawnSystem 在新場景的 Start() 會把 Player 放到目標點
        Time.timeScale = 1f;
        SceneManager.LoadScene(firstSceneName);
    }

    public void OnClickContinue()
    {
        // ★ 修復點：「繼續」也先確保有 Player，避免 SaveManager.PlacePlayerAt 找不到玩家
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null && playerPrefab != null)
        {
            player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            // PlayerLifetime 建議掛在 Prefab 上：保留最新或保留第一顆，依你策略
        }

        if (SaveSystem.Exists() && SaveManager.Instance != null)
        {
            Time.timeScale = 1f;
            SaveManager.Instance.LoadNow(); // SaveManager 會自行切到存檔場景並定位/還原 HP/MP
        }
        else
        {
            OnClickStart();
        }
    }

    // 「其他選項」：先上，再左（僅主選單用）
    public void OnClickOtherOptions()
    {
        if (pageOptionsRoot != null) pageOptionsRoot.SetActive(true);

        if (pageOptionsWrapperSlide != null)
        {
            // 先上
            pageOptionsWrapperSlide.Opened.RemoveListener(OpenOptionsHorizontal);
            pageOptionsWrapperSlide.Opened.AddListener(OpenOptionsHorizontal);
            pageOptionsWrapperSlide.Open();
        }
        else
        {
            // 沒有 Wrapper 就直接左
            OpenOptionsHorizontal();
        }
    }

    // Page_Options 內的返回：先左，再下（若有 Wrapper）
    public void OnClickOptionsBack()
    {
        // 先關左
        if (pageOptionsSlide != null)
        {
            pageOptionsSlide.Closed.RemoveListener(CloseWrapperAfterPage);
            pageOptionsSlide.Closed.AddListener(CloseWrapperAfterPage);
            pageOptionsSlide.Close();
        }
        else
        {
            CloseWrapperAfterPage();
        }
    }

    // ====== 內部串接 ======

    void OpenOptionsHorizontal()
    {
        if (pageOptionsSlide != null) pageOptionsSlide.Open();
    }

    void CloseWrapperAfterPage()
    {
        if (pageOptionsWrapperSlide != null)
            pageOptionsWrapperSlide.Close();

        // 回主畫面聚焦
        SafeSetFocus(defaultMainButton);
    }

    bool IsOptionsOpen()
    {
        if (pageOptionsSlide != null && pageOptionsSlide.IsOpen) return true;
        if (pageOptionsWrapperSlide != null && pageOptionsWrapperSlide.IsOpen) return true;
        return false;
    }

    void SafeSetFocus(Selectable s)
    {
        if (s == null || EventSystem.current == null) return;
        if (!s.gameObject.activeInHierarchy) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    public void OnClickQuit()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveNow(); // 主選單下 SaveManager 會自動略過不存。

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
