using UnityEngine;
using UnityEngine.SceneManagement;

public enum TeleportMode { SameScene, OtherScene }

[RequireComponent(typeof(Collider2D))]
public class TeleporterInteractable : InteractableBase
{
    [Header("顯示文字（可接 ScriptableObject 也可直接填）")]
    public TeleporterData data;           // 可為空；只拿 promptText 用

    [Header("模式")]
    public TeleportMode mode = TeleportMode.SameScene;

    [Header("同場景")]
    public Transform targetTransform;
    public Vector2 offset;

    [Header("跨場景")]
    public string targetSceneName;  // 必須加入 Build Settings
    public string targetSpawnId;    // 目標場景裡 SpawnPoint 的 ID

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
        prompt = "按 E 傳送";
    }

    void Awake()
    {
        if (data != null && !string.IsNullOrEmpty(data.promptText))
            prompt = data.promptText;
    }

    public override bool CanInteract()
    {
        if (!interactable) return false;
        if (mode == TeleportMode.SameScene) return targetTransform != null;
        return !string.IsNullOrEmpty(targetSceneName) && !string.IsNullOrEmpty(targetSpawnId);
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract()) return;

        if (mode == TeleportMode.SameScene)
        {
            Vector2 dst = (Vector2)targetTransform.position + offset;
            interactor.transform.position = dst;
            var rb = interactor.GetComponent<Rigidbody2D>();
            if (rb) rb.velocity = Vector2.zero;
        }
        else
        {
            // 記錄需求，切場景
            TeleportRequest.hasPending = true;
            TeleportRequest.sceneName = targetSceneName;
            TeleportRequest.spawnId = targetSpawnId;
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
