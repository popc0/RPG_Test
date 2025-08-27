using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("第一個要進入的遊戲場景")]
    public string firstSceneName = "Scene1";  // ← 改成你第一張地圖的名字

    public void OnClickStart()
    {
        SceneManager.LoadScene(firstSceneName);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 在編輯器裡結束播放
#else
        Application.Quit(); // 打包後退出程式
#endif
    }
}
