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

    [Header("MainMenu 根 CanvasGroup（可選）")]
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;

    private SystemCanvasController scc;
    private Coroutine refocusCo;

    // 新增：用來避免 Start/Continue 期間被 OnCloseActive 拉回 mainmenu
    private bool isLeavingMainMenu = false;

    void Awake()
    {
        TryFindSCC();

        if (!mainMenuCanvasGroup) mainMenuCanvasGroup = GetComponentInParent<CanvasGroup>();

        Bind(startbutton, OnClickStart);
        Bind(continueButton, OnClickContinue);
        Bind(optionsButton, OnClickOtherOptions);
        Bind(quitButton, OnClickQuit);
        Bind(deleteSaveButton, OnClickDeleteSave);
    }

    void OnEnable()
    {
        // 先訂閱事件
        UIEvents.OnCloseActiveCanvas += OnOuterClosed;

        // 下一幀再要求切到 mainmenu，確保 UIRootCanvasController 已經啟動並訂閱完畢
        StartCoroutine(CoOpenMainMenuNextFrame());

        // 聚焦按鈕一樣做，但放在 coroutine 後面也無妨
        RefocusMainButton();
    }

    IEnumerator CoOpenMainMenuNextFrame()
    {
        // 等 1 frame，讓所有 UIRoot / Anchor 都 OnEnable + Start 完
        yield return null;

        UIEvents.RaiseOpenCanvas("mainmenu");
    }

    void OnDisable()
    {
        UIEvents.OnCloseActiveCanvas -= OnOuterClosed;
    }

    void Start()
    {
        if (continueButton) continueButton.interactable = SaveSystem.Exists();
        RefocusMainButton();
        Debug.Log("[MainMenu] Start. Continue interactable=" + (continueButton ? continueButton.interactable : false));
    }

    // 收到外層關閉（例如 Options 關閉回主選單）
    void OnOuterClosed()
    {
        // 若正在離開主選單啟動遊戲，就忽略這次回傳，避免切回 mainmenu
        if (isLeavingMainMenu)
        {
            Debug.Log("[MainMenu] OnOuterClosed ignored because leaving main menu.");
            return;
        }

        Debug.Log("[MainMenu] OnOuterClosed received. Restore interaction and refocus.");

        if (mainMenuCanvasGroup)
        {
            mainMenuCanvasGroup.interactable = true;
            mainMenuCanvasGroup.blocksRaycasts = true;
        }

        // 不再主動廣播回 mainmenu，維持由觸發端決定外層狀態
        RefocusMainButton();
    }

    // 聚焦副程式
    public void RefocusMainButton()
    {
        if (refocusCo != null) StopCoroutine(refocusCo);
        refocusCo = StartCoroutine(RefocusNextFrame());
    }

    IEnumerator RefocusNextFrame()
    {
        yield return null;

        if (defaultMainButton == null) yield break;
        if (EventSystem.current == null) yield break;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(defaultMainButton.gameObject);

        Debug.Log("[MainMenu] Refocused to " + defaultMainButton.name);
        refocusCo = null;
    }

    // 按鈕事件
    public void OnClickStart()
    {
        Debug.Log("[MainMenu] Start clicked.");
        isLeavingMainMenu = true;                          // 進入離開流程
        UIEvents.RaiseOpenCanvas("system");                // 外層直接切到 system

        EnsurePlayer();
        TeleportRequest.hasPending = true;
        TeleportRequest.sceneName = firstSceneName;
        TeleportRequest.spawnId = firstSpawnId;

        Time.timeScale = 1f;
        SceneManager.LoadScene(firstSceneName, LoadSceneMode.Single);
    }

    public void OnClickContinue()
    {
        Debug.Log("[MainMenu] Continue clicked.");
        isLeavingMainMenu = true;                          // 進入離開流程
        UIEvents.RaiseOpenCanvas("system");                // 外層直接切到 system

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
        Debug.Log("[MainMenu] Options clicked. Open system then open options page.");
        // 留在主選單，不設 isLeavingMainMenu，這樣 Options 關閉後仍會回主選單
        UIEvents.RaiseOpenCanvas("system");

        TryFindSCC();
        if (scc == null) return;
        scc.OpenOptionsFromMainMenu(null);
    }

    public void OnClickQuit()
    {
        Debug.Log("[MainMenu] Quit clicked.");
        SaveManager.Instance?.SaveNow();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickDeleteSave()
    {
        Debug.Log("[MainMenu] DeleteSave clicked.");
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
            Debug.LogWarning("[MainMenuController] SystemCanvasController not found on SystemCanvas.");
    }

    private void EnsurePlayer()
    {
        if (GameObject.FindGameObjectWithTag("Player") != null) return;
        if (playerPrefab) Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
    }
}
