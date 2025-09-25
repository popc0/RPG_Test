using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 劇情互動點（單一腳本版）
/// - 進出範圍顯示提示（與 Teleporter 互動風格一致）
/// - 玩家互動器呼叫 Interact() 後啟動對話
/// - 對話期間可暫停指定行為（如 PlayerMovement / Interactor / Input）
/// - 可選：進入範圍自動觸發
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StoryTrigger2D : InteractableBase
{
    [Header("Dialogue")]
    public DialogueData dialogue;
    public string startNodeId = "start";

    [Header("觸發行為")]
    [Tooltip("玩家進入範圍就自動開始，不用按鍵/互動器")]
    public bool autoStartWhenEnter = false;

    [Header("一次性")]
    public bool oneShot = false;
    private bool used = false;

    [Header("玩家（可留空用 tag=Player 取得）")]
    public GameObject player;

    private bool playerInside = false;
    private StoryManager story;

    // 只暫停這些腳本（依型別名稱）
    static readonly string[] k_ScriptsToPause = {
        "PlayerMovement",
        "PlayerInteractor",
        "KeyboardInputSource"
    };
    private readonly List<MonoBehaviour> pausedScripts = new();

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        if (string.IsNullOrEmpty(prompt))
            prompt = "按 E 對話";
    }

    void Awake()
    {
        story = FindObjectOfType<StoryManager>(true);
    }

    public override bool CanInteract()
    {
        if (!interactable) return false;
        if (used && oneShot) return false;
        return (dialogue != null && story != null);
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract()) return;

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();

        // 暫停玩家必要腳本
        var p = player != null ? player : GameObject.FindGameObjectWithTag("Player");
        PausePlayerScripts(p, true);

        story.StartStory(dialogue, startNodeId);
        StartCoroutine(WaitStoryEndAndResume());

        if (oneShot) used = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || !interactable) return;
        if (!IsPlayer(other)) return;

        playerInside = true;

        if (autoStartWhenEnter)
        {
            Interact(player != null ? player : other.gameObject);
            return;
        }

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Show(prompt);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInside = false;

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();
    }

    IEnumerator WaitStoryEndAndResume()
    {
        while (story != null && story.IsPlaying)
            yield return null;

        var p = player != null ? player : GameObject.FindGameObjectWithTag("Player");
        PausePlayerScripts(p, false);

        if (playerInside && !autoStartWhenEnter && InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Show(prompt);
    }

    void PausePlayerScripts(GameObject target, bool pause)
    {
        if (target == null) return;

        if (pause)
        {
            pausedScripts.Clear();
            foreach (var mb in target.GetComponents<MonoBehaviour>())
            {
                if (mb == null || !mb.enabled) continue;

                var typeName = mb.GetType().Name;
                for (int i = 0; i < k_ScriptsToPause.Length; i++)
                {
                    if (typeName == k_ScriptsToPause[i])
                    {
                        mb.enabled = false;
                        pausedScripts.Add(mb);
                        break;
                    }
                }
            }
        }
        else
        {
            foreach (var mb in pausedScripts)
            {
                if (mb != null) mb.enabled = true;
            }
            pausedScripts.Clear();
        }
    }

    bool IsPlayer(Collider2D col)
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        return (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == player)
               || col.gameObject == player;
    }
}
