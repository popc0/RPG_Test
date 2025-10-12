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

    [Header("主畫面預設聚焦")]
    public Selectable defaultMainButton;

    [Header("其他選項（兩段式：先上，再左）")]
    public IngameMenuSlide pageOptionsSlide;         // 水平滑動的 Page_Options 本體
    public IngameMenuSlide pageOptionsWrapperSlide;  // 垂直滑動的 Wrapper（Page_Options 的父）
    public GameObject pageOptionsRoot;               // Page_Options 的根物件
    public GameObject pageMain;                      // 主畫面 Panel_Main

    [Header("五個按鈕直接拖進來即可")]
    public Button startButton;
    public Button continueButton;
    public Button optionsButton;
    public Button quitButton;
    public Button deleteSaveButton;

    void Awake()
    {
        // 回主選單場景時，可能是「重載主場景」或「從遊戲場景回來」：於是先 Rebind。
        TryRebindOptionsRefs();

        if (startButton) { startButton.onClick.RemoveAllListeners(); startButton.onClick.AddListener(OnClickStart); }
        if (continueButton) { continueButton.onClick.RemoveAllListeners(); continueButton.onClick.AddListener(OnClickContinue); }
        if (optionsButton) { optionsButton.onClick.RemoveAllListeners(); optionsButton.onClick.AddListener(OnClickOtherOptions); }
        if (quitButton) { quitButton.onClick.RemoveAllListeners(); quitButton.onClick.AddListener(OnClickQuit); }
        if (deleteSaveButton) { deleteSaveButton.onClick.RemoveAllListeners(); deleteSaveButton.onClick.AddListener(OnClickDeleteSave); }
    }

    void Start()
    {
        TryRebindOptionsRefs(); // 再保險一次

        Time.timeScale = 1f;

        if (continueButton != null)
            continueButton.interactable = SaveSystem.Exists();

        // 初始化：確保 Page_Options 關閉
        if (pageOptionsRoot != null) pageOptionsRoot.SetActive(true);
        if (pageOptionsSlide != null) pageOptionsSlide.Close();
        if (pageOptionsWrapperSlide != null) pageOptionsWrapperSlide.Close();

        FocusNowAndNext(defaultMainButton);
    }

    public void OnClickStart()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null && playerPrefab != null)
            player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        TeleportRequest.hasPending = true;
        TeleportRequest.sceneName = firstSceneName;
        TeleportRequest.spawnId = firstSpawnId;

        Time.timeScale = 1f;
        SceneManager.LoadScene(firstSceneName);
    }

    public void OnClickContinue()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null && playerPrefab != null)
            player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        if (SaveSystem.Exists() && SaveManager.Instance != null)
        {
            Time.timeScale = 1f;
            SaveManager.Instance.LoadNow();
        }
        else
        {
            OnClickStart();
        }
    }

    public void OnClickOtherOptions()
    {
        TryRebindOptionsRefs(); // ← 打開前，重新抓一次

        // 關掉主畫面
        if (pageMain != null)
            pageMain.SetActive(false);

        // 開啟 Options Root
        if (pageOptionsRoot != null)
            pageOptionsRoot.SetActive(true);

        // 打開 Wrapper + Slide（上下→再左右）
        if (pageOptionsWrapperSlide != null)
        {
            pageOptionsWrapperSlide.Opened.RemoveListener(OpenOptionsHorizontal);
            pageOptionsWrapperSlide.Opened.AddListener(OpenOptionsHorizontal);
            pageOptionsWrapperSlide.Open();
        }
        else
        {
            OpenOptionsHorizontal();
        }
    }

    public void OnClickOptionsBack()
    {
        TryRebindOptionsRefs();

        // 關閉水平滑動
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

        FocusNowAndNext(defaultMainButton);
    }

    public void OnClickQuit()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveNow();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickDeleteSave()
    {
        if (SaveSystem.Exists())
        {
            SaveSystem.Delete();
            if (continueButton) continueButton.interactable = false;
        }
    }

    void OpenOptionsHorizontal()
    {
        TryRebindOptionsRefs();
        if (pageOptionsSlide != null)
            pageOptionsSlide.Open();
    }

    void CloseWrapperAfterPage()
    {
        TryRebindOptionsRefs();

        // 關閉 Wrapper
        if (pageOptionsWrapperSlide != null)
            pageOptionsWrapperSlide.Close();

        // 重新打開主畫面
        if (pageMain != null)
            pageMain.SetActive(true);

        FocusNowAndNext(defaultMainButton);
    }

    // —— 聚焦工具（先即時、下一幀再補一次）——
    private Coroutine _focusCo;
    private void FocusNowAndNext(Selectable s)
    {
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        if (s == null) return;

        if (!s.gameObject.activeInHierarchy) return;
        EventSystem.current.SetSelectedGameObject(s.gameObject);

        if (_focusCo != null) StopCoroutine(_focusCo);
        _focusCo = StartCoroutine(CoFocusNextFrame(s));
    }
    private System.Collections.IEnumerator CoFocusNextFrame(Selectable s)
    {
        yield return null;
        if (EventSystem.current == null || s == null) yield break;
        if (!s.gameObject.activeInHierarchy) yield break;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    // —— 重新抓引用（回到主選單時若舊引用失效，用名稱補綁）——
    private void TryRebindOptionsRefs()
    {
        if (pageOptionsRoot == null)
        {
            var go = GameObject.Find("Page_Options");
            if (go != null) pageOptionsRoot = go;
        }
        if (pageOptionsSlide == null)
        {
            var go = GameObject.Find("Page_Options");
            if (go != null) pageOptionsSlide = go.GetComponent<IngameMenuSlide>();
        }
        if (pageOptionsWrapperSlide == null)
        {
            var go = GameObject.Find("PageOptionsWrapper");
            if (go != null) pageOptionsWrapperSlide = go.GetComponent<IngameMenuSlide>();
        }
        if (pageMain == null)
        {
            var go = GameObject.Find("MainMenuCanvas/Page_Main");
            if (go == null) go = GameObject.Find("Page_Main"); // 備援
            if (go != null) pageMain = go;
        }
    }
}
