using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 掛在 Page_Options：按 ESC 關閉 Options（自動回 caller）
/// - 主選單或遊戲內皆可用，不需知道來源；關閉後由 Page_Options 的 Close 機制回上一頁
/// </summary>
public class OptionsEscClose : MonoBehaviour
{
    [Header("熱鍵")]
    [SerializeField] private KeyCode escKey = KeyCode.Escape;

    [Header("（可選）僅在此物件啟用時響應 ESC")]
    [SerializeField] private GameObject targetPageOptions; // 指 Page_Options 根物件

    private SystemCanvasController scc;

    void Awake() => TryFindSCC();

    void Update()
    {
        if (!Input.GetKeyDown(escKey)) return;

        if (targetPageOptions != null && !targetPageOptions.activeInHierarchy)
            return;

        TryFindSCC();
        if (scc != null) scc.CloseOptions();
    }

    private void TryFindSCC()
    {
        if (scc != null) return;
        scc = FindObjectOfType<SystemCanvasController>();
        if (scc == null)
            Debug.LogWarning("[OptionsEscClose] 找不到 SystemCanvasController。請確認它掛在 SystemCanvas。");
    }
}
