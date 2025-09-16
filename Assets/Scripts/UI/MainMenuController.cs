using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("進入的第一個遊戲場景")]
    public string firstSceneName = "Scene1";

    [Header("面板")]
    public GameObject optionsPanel;

    [Header("主選單的預設焦點")]
    public Selectable defaultMainButton; // 指到「開始遊戲」或你想要的那顆

    [Header("（可選）繼續遊戲按鈕")]
    public Button continueButton; // NEW: 有存檔才可按，沒有可留空

    void Start() // NEW: 進主選單時初始化
    {
        // 確保回到主選單時時間流速恢復
        Time.timeScale = 1f;

        // 根據是否有存檔，啟用/停用「繼續」按鈕（若未連線就略過）
        if (continueButton != null)
        {
            bool hasSave = SaveSystem.Exists();
            continueButton.interactable = hasSave;
        }
    }

    // ---- 主選單按鈕 ----
    public void OnClickStart()
    {
        SceneManager.LoadScene(firstSceneName);
    }

    public void OnClickContinue() // NEW: 繼續遊戲
    {
        // 有存檔 → 請 SaveManager 載入（含跨場景/定位）
        if (SaveSystem.Exists() && SaveManager.Instance != null)
        {
            Time.timeScale = 1f;
            SaveManager.Instance.LoadNow();
        }
        else
        {
            // 沒存檔就當新遊戲
            OnClickStart();
        }
    }

    public void OnClickOptions()
    {
        if (optionsPanel == null) return;
        optionsPanel.SetActive(true);
        // （需要的話在這裡設定 options 預設焦點）
    }

    public void OnClickQuit()
    {
        // NEW: 退出前先存檔一次（可保留最後位置/場景）
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveNow();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---- 返回起始介面（關閉所有子面板）----
    public void OnClickBackToMain()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);

        // 把選擇焦點設回主選單按鈕（手把/鍵盤操作更順）
        if (defaultMainButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(defaultMainButton.gameObject);
        }
    }
}
