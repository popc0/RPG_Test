using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IngameMenuRouterB : MonoBehaviour
{
    [Header("Slides")]
    public IngameMenuSlide menuSlide;    // Panel_IngameMenu
    public IngameMenuSlide optionsSlide; // Page_Options

    [Header("Pages")]
    public GameObject pageMain;          // Page_Main
    public GameObject pageOptions;       // Page_Options

    [Header("Focus (Optional)")]
    public Selectable focusMain;         // Btn_Continue
    public Selectable focusOptions;      // Btn_Back

    [Header("ESC Key")]
    public KeyCode escKey = KeyCode.Escape;

    [Header("Main Menu Scene")]
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        pageMain.SetActive(true);
        pageOptions.SetActive(true);
        optionsSlide.Close();
        SetFocus(focusMain);
    }

    void Update()
    {
        if (Input.GetKeyDown(escKey))
        {
            if (optionsSlide != null && optionsSlide.IsOpen)
            {
                CloseOptions();
                return;
            }
            if (menuSlide != null) menuSlide.Toggle();
        }
    }

    // 主頁
    public void OnClick_Continue() { menuSlide?.Close(); }
    public void OnClick_Options() { OpenOptions(); }
    public void OnClick_BackToMain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // 選項頁
    public void OnClick_OptionsBack() { CloseOptions(); }

    // 內部
    void OpenOptions()
    {
        pageMain.SetActive(true);
        optionsSlide.Open();
        SetFocus(focusOptions);
    }
    void CloseOptions()
    {
        optionsSlide.Close();
        SetFocus(focusMain);
    }
    void SetFocus(Selectable s)
    {
        if (s == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}
