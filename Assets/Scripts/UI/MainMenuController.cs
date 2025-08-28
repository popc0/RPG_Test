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

    // ---- 主選單按鈕 ----
    public void OnClickStart()
    {
        SceneManager.LoadScene(firstSceneName);
    }

    public void OnClickOptions()
    {
        if (optionsPanel == null) return;
        optionsPanel.SetActive(true);
        // 也可以在這裡設定選項面板內預設焦點
    }

    public void OnClickQuit()
    {
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
        if (defaultMainButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(defaultMainButton.gameObject);
        }
    }
}
