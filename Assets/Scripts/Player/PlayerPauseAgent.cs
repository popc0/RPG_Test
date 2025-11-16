using System.Collections.Generic;
using UnityEngine;

public class PlayerPauseAgent : MonoBehaviour
{
    [Header("手動指定要暫停的腳本")]
    public List<MonoBehaviour> manualTargets = new List<MonoBehaviour>();

    [Header("自動掃描類名（會自動抓這些）")]
    public string[] autoIncludeTypeNames = {
        "PlayerMovement",
        "PlayerInteractor",
        "KeyboardInputSource"
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

    public void Pause()
    {
        pauseCount++;
        Debug.Log($"[PlayerPauseAgent] Pause, count = {pauseCount}");
        if (pauseCount > 1) return;

        snapshot.Clear();
        foreach (var mb in manualTargets)
        {
            if (mb == null) continue;
            snapshot.Add((mb, mb.enabled));
            mb.enabled = false;
            Debug.Log($"[PlayerPauseAgent] Disable {mb.GetType().Name}");
        }
    }

    public void Resume()
    {
        if (pauseCount == 0)
        {
            Debug.LogWarning("[PlayerPauseAgent] Resume 被叫但 pauseCount 已經是 0 了");
            return;
        }

        pauseCount--;
        Debug.Log($"[PlayerPauseAgent] Resume, count = {pauseCount}");

        if (pauseCount > 0) return;

        foreach (var s in snapshot)
        {
            if (s.mb != null)
            {
                s.mb.enabled = s.wasEnabled;
                Debug.Log($"[PlayerPauseAgent] Restore {s.mb.GetType().Name} → {s.wasEnabled}");
            }
        }
        snapshot.Clear();
    }

}
