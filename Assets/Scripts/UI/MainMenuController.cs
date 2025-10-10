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
        if (startButton) { startButton.onClick.RemoveAllListeners(); startButton.onClick.AddListener(OnClickStart); }
        if (continueButton) { continueButton.onClick.RemoveAllListeners(); continueButton.onClick.AddListener(OnClickContinue); }
        if (optionsButton) { optionsButton.onClick.RemoveAllListeners(); optionsButton.onClick.AddListener(OnClickOtherOptions); }
        if (quitButton) { quitButton.onClick.RemoveAllListeners(); quitButton.onClick.AddListener(OnClickQuit); }
        if (deleteSaveButton) { deleteSaveButton.onClick.RemoveAllListeners(); deleteSaveButton.onClick.AddListener(OnClickDeleteSave); }
    }

    void Start()
    {
        // 進入主選單時：切一次 MainMenu 狀態
        UIStateManager.Instance?.SwitchUI(UIState.MainMenu);

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
        // 關掉主頁面
        if (pageMain != null)
            pageMain.SetActive(false);

        // 開啟 Options Root
        if (pageOptionsRoot != null)
            pageOptionsRoot.SetActive(true);

        // 打開 Wrapper + Slide
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

        // 切 UI 狀態
        UIStateManager.Instance?.SwitchUI(UIState.Options);
    }

    public void OnClickOptionsBack()
    {
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

        // 切回主選單狀態
        UIStateManager.Instance?.SwitchUI(UIState.MainMenu);
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
        if (pageOptionsSlide != null)
            pageOptionsSlide.Open();
    }

    void CloseWrapperAfterPage()
    {
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
        if (s == null || EventSystem.current == null) return;
        if (!s.gameObject.activeInHierarchy) return;

        EventSystem.current.SetSelectedGameObject(null);
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
}
