using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPauseAgent : MonoBehaviour
{
    [Header("手動指定要暫停的腳本")]
    public List<MonoBehaviour> manualTargets = new List<MonoBehaviour>();

    [Header("輸入暫停：手動指定要停止的 Actions")]
    [Tooltip("將所有要在暫停時禁用的 InputActionReference 拖曳到此清單")]
    public List<InputActionReference> actionsToPause = new List<InputActionReference>();

    [Header("自動掃描類名（會自動抓這些）")]
    public string[] autoIncludeTypeNames = {
    };

    private readonly List<(MonoBehaviour mb, bool wasEnabled)> snapshot = new();
    private int pauseCount = 0;

    void Awake()
    {
        if (manualTargets.Count == 0)
        {
            var all = GetComponents<MonoBehaviour>();
            foreach (var mb in all)
            {
                if (mb == null) continue;
                foreach (var typeName in autoIncludeTypeNames)
                {
                    if (mb.GetType().Name == typeName)
                    {
                        manualTargets.Add(mb);
                        break;
                    }
                }
            }
        }
    }

    // PlayerPauseAgent.cs (修改 Pause / Resume)

    // PlayerPauseAgent.cs

    public void Pause()
    {
        pauseCount++;
        Debug.Log($"[PlayerPauseAgent] Pause, count = {pauseCount}");
        if (pauseCount > 1) return;

        snapshot.Clear();

        // 1. 禁用所有指定的 MonoBehaviour (現有邏輯)
        foreach (var mb in manualTargets)
        {
            if (mb == null) continue;

            // 處理輸入組件的特殊禁用邏輯（如果需要，像 UnifiedInputSource 那樣）
            if (mb is UnifiedInputSource uis)
            {
                // 確保我們呼叫了這個方法，來禁用所有 Action
                uis.DisableAllInputActions();
                uis.ResetAllInputStates();
            }

            snapshot.Add((mb, mb.enabled));
            mb.enabled = false;
            Debug.Log($"[PlayerPauseAgent] Disable MonoBehaviour {mb.GetType().Name}");
        }

        // 2.  新增：禁用所有指定 Action
        foreach (var actionRef in actionsToPause)
        {
            if (actionRef?.action != null && actionRef.action.enabled)
            {
                actionRef.action.Disable();
                // Debug.Log($"[PlayerPauseAgent] Disable Action {actionRef.action.name}");
            }
        }
    }

    public void Resume()
    {
        // ... 忽略檢查 ...
        if (pauseCount == 0)
        {
            Debug.LogWarning("[PlayerPauseAgent] Resume 被叫但 pauseCount 已經是 0 了");
            return;
        }
        pauseCount--;
        Debug.Log($"[PlayerPauseAgent] Resume, count = {pauseCount}");

        if (pauseCount > 0) return;

        // 1. 恢復所有指定的 MonoBehaviour (現有邏輯)
        foreach (var s in snapshot)
        {
            if (s.mb != null)
            {
                s.mb.enabled = s.wasEnabled;
                // 處理輸入組件的特殊啟用邏輯（如果需要）
                Debug.Log($"[PlayerPauseAgent] Restore {s.mb.GetType().Name} → {s.wasEnabled}");
            }
        }
        snapshot.Clear();

        // 2.  新增：恢復所有指定的 Action
        foreach (var actionRef in actionsToPause)
        {
            // 只有當 Action 原本應該啟用 (即 .action.enabled 為 false) 且我們沒有其他邏輯控制時，才啟用。
            // 最簡單的做法是直接啟用，因為我們在 Pause 時把它們禁用了。
            if (actionRef?.action != null && !actionRef.action.enabled)
            {
                actionRef.action.Enable();
                // Debug.Log($"[PlayerPauseAgent] Enable Action {actionRef.action.name}");
            }
        }
    }

}
