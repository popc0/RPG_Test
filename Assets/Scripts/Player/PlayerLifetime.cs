using UnityEngine;

/// <summary>
/// 保留「最新」玩家：若已有玩家在場上，複製狀態到新玩家 → 刪掉舊的 → 讓新玩家 DontDestroyOnLoad。
/// </summary>
[DefaultExecutionOrder(-500)]
public class PlayerLifetime : MonoBehaviour
{
    private static PlayerLifetime _current;

    void Awake()
    {
        if (_current == null)
        {
            _current = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (_current == this)
        {
            DontDestroyOnLoad(gameObject);
            return;
        }

        // —— 場上已有舊玩家，又來了一個新玩家 —— 
        var oldGO = _current.gameObject;
        var newGO = this.gameObject;

        // 1) 搬移狀態（位置 / 速度 / HPMP）
        newGO.transform.position = oldGO.transform.position;

        var oldRb = oldGO.GetComponent<Rigidbody2D>();
        var newRb = newGO.GetComponent<Rigidbody2D>();
        if (oldRb && newRb) newRb.velocity = oldRb.velocity;

        var oldStats = oldGO.GetComponent<PlayerStats>();
        var newStats = newGO.GetComponent<PlayerStats>();
        if (oldStats && newStats)
        {
            newStats.MaxHP = oldStats.MaxHP;
            newStats.MaxMP = oldStats.MaxMP;
            newStats.SetStats(oldStats.CurrentHP, oldStats.CurrentMP);
        }

        // 2) 以新玩家為主
        _current = this;
        DontDestroyOnLoad(newGO);

        // 3) 刪舊
        Destroy(oldGO);
    }
}
