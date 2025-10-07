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
        if (pauseCount > 1) return; // 已暫停，不重複執行

        snapshot.Clear();
        foreach (var mb in manualTargets)
        {
            if (mb == null) continue;
            snapshot.Add((mb, mb.enabled));
            mb.enabled = false;
        }
    }

    public void Resume()
    {
        if (pauseCount == 0) return;
        pauseCount--;
        if (pauseCount > 0) return; // 還有其他暫停來源，先不恢復

        foreach (var s in snapshot)
        {
            if (s.mb != null) s.mb.enabled = s.wasEnabled;
        }
        snapshot.Clear();
    }
}
