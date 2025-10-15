using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主選單控制器（極薄）
/// - 「其他選項」→ SystemCanvasController.OpenOptionsFromMainMenu(pageMainRoot)
/// - 返回主頁面：聚焦回 defaultMainButton
/// 其他（開始/繼續/離開）沿用你現有邏輯即可。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("主選單 Page_Main 根物件（作為 Options 的 caller）")]
    [SerializeField] private GameObject pageMainRoot; // MainMenuCanvas 下的 Page_Main

    [Header("主畫面預設聚焦")]
    [SerializeField] private Selectable defaultMainButton; // 例如 Continue/Start

    [Header("進入的第一個遊戲場景 & 落點（依你現有流程）")]
    [SerializeField] private string firstSceneName = "Scene1";
    [SerializeField] private string firstSpawnId = "Start";
    [SerializeField] private GameObject playerPrefab;

    [Header("五個按鈕（直接拖）")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button deleteSaveButton;

    private SystemCanvasController scc;

    void Awake()
    {
        TryFindSCC();

        Bind(startButton, OnClickStart);
        Bind(continueButton, OnClickContinue);
        Bind(optionsButton, OnClickOtherOptions);
        Bind(quitButton, OnClickQuit);
        Bind(deleteSaveButton, OnClickDeleteSave);
    }

    void Start()
    {
        // 可選：設定 Continue 可用狀態
        if (continueButton != null) continueButton.interactable = SaveSystem.Exists();

        // 進主選單聚焦預設
        if (defaultMainButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(defaultMainButton.gameObject);
    }

    public void OnClickStart()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null && playerPrefab != null)
            Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

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
            Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

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
        TryFindSCC();
        if (scc == null || pageMainRoot == null) return;

        // 主選單開 Options：把主選單 Page_Main 當 caller
        scc.OpenOptionsFromMainMenu(pageMainRoot);
    }

    public void OnClickOptionsBack_ToMain()
    {
        // 若你在主選單頁內有「返回主頁面」的按鈕，聚焦回預設
        if (defaultMainButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(defaultMainButton.gameObject);
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

    // —— 小工具 —— //
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
            Debug.LogWarning("[MainMenuController] 找不到 SystemCanvasController。請確認 SystemCanvas 上有掛它。");
    }
}
