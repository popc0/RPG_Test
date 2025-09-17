using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("進入的第一個遊戲場景")]
    public string firstSceneName = "Scene1";

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

        // 初始化：確保 Page_Options 處於關閉狀態（主選單需要兩段皆關）
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
        SceneManager.LoadScene(firstSceneName);
    }

    public void OnClickContinue()
    {
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
            SaveManager.Instance.SaveNow();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
