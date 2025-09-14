using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IngameMenuRouter : MonoBehaviour
{
    [Header("Pages")]
    public GameObject pageMain;
    public GameObject pageOptions;

    [Header("Focus (Optional)")]
    public Selectable focusOnMain;    // e.g. 「繼續」按鈕
    public Selectable focusOnOptions; // e.g. 「返回」或第一個 Slider

    [Header("ESC 行為")]
    public KeyCode escKey = KeyCode.Escape;
    public IngameMenuSlide slide;     // 指向 Panel_IngameMenu 上的 IngameMenuSlide

    void Awake()
    {
        ShowMain();
    }

    void Update()
    {
        if (Input.GetKeyDown(escKey))
        {
            if (pageOptions.activeSelf)
                ShowMain();       // 先從選項退回主頁
            else
                slide?.Toggle();  // 再切整個 in-game 面板
        }
    }

    public void OnClickOptions()
    {
        ShowOptions();
    }

    public void OnClickBackFromOptions()
    {
        ShowMain();
    }

    void ShowMain()
    {
        pageMain.SetActive(true);
        pageOptions.SetActive(false);
        SetFocus(focusOnMain);
    }

    void ShowOptions()
    {
        pageMain.SetActive(false);
        pageOptions.SetActive(true);
        SetFocus(focusOnOptions);
    }

    void SetFocus(Selectable s)
    {
        if (s == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}
