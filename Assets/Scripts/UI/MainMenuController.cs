using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("主畫面預設聚焦")]
    [SerializeField] private Selectable defaultMainButton;

    [Header("進入的第一個遊戲場景與落點")]
    [SerializeField] private string firstSceneName = "Scene1";
    [SerializeField] private string firstSpawnId = "Start";
    [SerializeField] private GameObject playerPrefab;

    [Header("五個按鈕")]
    [SerializeField] private Button startbutton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button deleteSaveButton;

    private SystemCanvasController scc;
    private Coroutine refocusCo;

    void Awake()
    {
        TryFindSCC();

        Bind(startbutton, OnClickStart);
        Bind(continueButton, OnClickContinue);
        Bind(optionsButton, OnClickOtherOptions);
        Bind(quitButton, OnClickQuit);
        Bind(deleteSaveButton, OnClickDeleteSave);
    }

    void OnEnable()
    {
        // 外層前景 = MainMenu（外層互斥）
        UIEvents.RaiseOpenCanvas("mainmenu");

        // 每次啟用或回主選單場景時，重新聚焦
        RefocusMainButton();
    }

    void Start()
    {
        if (continueButton) continueButton.interactable = SaveSystem.Exists();
        RefocusMainButton();
    }

    // 聚焦副程式（延遲一幀以確保 EventSystem 與 UI 皆初始化完成）
    public void RefocusMainButton()
    {
        if (refocusCo != null) StopCoroutine(refocusCo);
        refocusCo = StartCoroutine(RefocusNextFrame());
    }

    IEnumerator RefocusNextFrame()
    {
        yield return null; // 等下一幀，避免過早設定焦點被忽略

        if (defaultMainButton == null) yield break;
        if (EventSystem.current == null) yield break;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(defaultMainButton.gameObject);

        refocusCo = null;
    }

    // 按鈕事件
    public void OnClickStart()
    {
        EnsurePlayer();
        TeleportRequest.hasPending = true;
        TeleportRequest.sceneName = firstSceneName;
        TeleportRequest.spawnId = firstSpawnId;

        Time.timeScale = 1f;
        SceneManager.LoadScene(firstSceneName, LoadSceneMode.Single);
    }

    public void OnClickContinue()
    {
        EnsurePlayer();

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
        // 切外層前景到 System，鎖住 MainMenu 互動
        UIEvents.RaiseOpenCanvas("system");

        TryFindSCC();
        if (scc == null) return;
        scc.OpenOptionsFromMainMenu(null);
    }

    public void OnClickQuit()
    {
        SaveManager.Instance?.SaveNow();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickDeleteSave()
    {
        if (!SaveSystem.Exists()) return;
        SaveSystem.Delete();
        if (continueButton) continueButton.interactable = false;
    }

    // 工具
    private void Bind(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (!btn || action == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private void TryFindSCC()
    {
        if (scc != null) return;
        scc = FindObjectOfType<SystemCanvasController>();
        if (scc == null)
            Debug.LogWarning("[MainMenuController] 找不到 SystemCanvasController，請確認 SystemCanvas 上有掛。");
    }

    private void EnsurePlayer()
    {
        if (GameObject.FindGameObjectWithTag("Player") != null) return;
        if (playerPrefab) Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
    }
}
