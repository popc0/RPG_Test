using UnityEngine;
using UnityEngine.EventSystems;

public class SystemCanvasController : MonoBehaviour
{
    // —— 群組層（只用來鎖互動）——
    [Header("群組 CanvasGroup")]
    [SerializeField] private CanvasGroup groupIngameMenu; // 遊戲內主選單群組
    [SerializeField] private CanvasGroup groupOptions;    // 共用設定群組

    // —— 頁面層（各自管理動畫/焦點）——
    [Header("頁面控制腳本")]
    [SerializeField] private PageMain pageMain;           // Page_Main：下滑入/下滑出
    [SerializeField] private PageOptions pageOptions;     // Page_Options：右滑入/右滑出

    private enum UiBlock { None, IngameMenu, Options }
    private UiBlock current = UiBlock.None;

    // —— 公開 API：給外部呼叫 —— //
    // 遊戲內：按 R 開關主選單
    public void ToggleIngameMenu()
    {
        if (current == UiBlock.IngameMenu)
            CloseIngameMenu();
        else
            OpenIngameMenu();
    }

    // 開啟遊戲內主選單
    public void OpenIngameMenu()
    {
        // 若設定頁開著，先關
        if (current == UiBlock.Options)
            CloseOptions();

        LockAllExcept(UiBlock.IngameMenu);
        pageMain.Open();
    }

    // 關閉遊戲內主選單
    public void CloseIngameMenu()
    {
        pageMain.Close();
        UnlockAll(); // 回到無互動群組
    }

    // 從「主選單」開啟設定
    public void OpenOptionsFromMainMenu(GameObject callerPage)
    {
        LockAllExcept(UiBlock.Options);
        pageOptions.Open(callerPage); // caller=主選單的頁物件
    }

    // 從「遊戲內主選單 Page_Main」開啟設定
    public void OpenOptionsFromIngame(GameObject callerPage)
    {
        LockAllExcept(UiBlock.Options);
        pageOptions.Open(callerPage); // caller=Ingame 的 Page_Main
    }

    // 關閉設定（會自動回上一頁）
    public void CloseOptions()
    {
        ClearFocus();
        pageOptions.Close();

        // 嘗試判斷上一頁是不是遊戲內主選單（最常見情境）
        // 由於 PageOptions 內已回到 caller，這裡只需把互斥鎖回去
        if (pageMain != null && pageMain.gameObject.activeInHierarchy)
            LockAllExcept(UiBlock.IngameMenu);
        else
            UnlockAll(); // 可能是主選單回來，交給主選單自己接管
    }

    // —— 內部工具 —— //
    private void LockAllExcept(UiBlock which)
    {
        SetGroupInteract(groupIngameMenu, which == UiBlock.IngameMenu);
        SetGroupInteract(groupOptions, which == UiBlock.Options);
        current = which;
    }

    private void UnlockAll()
    {
        SetGroupInteract(groupIngameMenu, false);
        SetGroupInteract(groupOptions, false);
        current = UiBlock.None;
    }

    private void SetGroupInteract(CanvasGroup cg, bool on)
    {
        if (cg == null) return;
        cg.interactable = on;
        cg.blocksRaycasts = on;
        // alpha 是否要一起管理由你決定；通常群組層 alpha 保持 1 即可
    }

    private void ClearFocus()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
