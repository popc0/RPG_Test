using UnityEngine;

public class SpawnSystem : MonoBehaviour
{
    public string playerTag = "Player";

    void Start()
    {
        if (!TeleportRequest.hasPending) return;

        // 找目標落點
        var all = FindObjectsOfType<SpawnPoint>(true);
        Transform target = null;
        foreach (var s in all)
        {
            if (s.spawnId == TeleportRequest.spawnId)
            {
                target = s.transform; break;
            }
        }

        // 把玩家放到落點
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null && target != null)
        {
            player.transform.position = target.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb) rb.velocity = Vector2.zero;
        }
        else
        {
            Debug.LogWarning($"SpawnSystem: 找不到 Player 或 spawnId={TeleportRequest.spawnId}。");
        }

        TeleportRequest.Clear();
    }
}
