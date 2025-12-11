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

    [Header("UI 提示")]
    public string destinationName;  // 顯示用目的地名稱

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

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();
    }

    // 玩家進入範圍時顯示提示
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || !interactable) return;
        // ★ 修改：不比對 Tag，而是檢查是否有玩家的互動組件
        // 這樣不管你的 Collider 是在 Root、Body 還是 Feet，只要它屬於玩家，就會被偵測到
        var interactor = other.GetComponentInParent<PlayerInteractor>();

        if (interactor == null) return; // 不是玩家(或玩家的手)，忽略

        string nameToShow = GetDisplayName();
        string tip = string.IsNullOrEmpty(nameToShow)
            ? "按 E 傳送"
            : $"前往 {nameToShow}，按 E 傳送";

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Show(tip);
    }

    // 玩家離開範圍時隱藏提示
    void OnTriggerExit2D(Collider2D other)
    {
        // 同樣改用 GetComponentInParent 檢查
        var interactor = other.GetComponentInParent<PlayerInteractor>();

        if (interactor == null) return;
        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();
    }

    string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(destinationName)) return destinationName;

        if (mode == TeleportMode.SameScene)
            return targetTransform != null ? targetTransform.name : "";

        if (!string.IsNullOrEmpty(targetSpawnId)) return targetSpawnId;
        if (!string.IsNullOrEmpty(targetSceneName)) return targetSceneName;
        return "";
    }
}
