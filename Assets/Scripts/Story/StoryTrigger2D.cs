using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StoryTrigger2D : InteractableBase
{
    [Header("劇情資料")]
    public DialogueData dialogue;
    public string startNodeId = "start";

    [Header("觸發設定")]
    public bool autoStartWhenEnter = false;
    public bool oneShot = false;
    private bool used = false;

    private StoryManager story;
    private PlayerPauseAgent pauseAgent;
    private GameObject player;
    private bool playerInside = false;

    void Awake()
    {
        story = FindObjectOfType<StoryManager>(true);
    }

    void Start()
    {
        // 確保 UI 初始化完畢後再檢查玩家是否在範圍中
        StartCoroutine(WaitForUIAndCheckInside());
    }

    IEnumerator WaitForUIAndCheckInside()
    {
        // 等待 InteractPromptUI 實例出現（最多 3 秒防止無限等待）
        float timer = 0f;
        while (InteractPromptUI.Instance == null && timer < 3f)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 嘗試取得玩家
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            yield break;

        // 玩家一開始就在觸發區時，手動模擬 OnTriggerEnter2D
        var col = GetComponent<Collider2D>();
        if (col != null && player.TryGetComponent(out Collider2D playerCol))
        {
            if (col.bounds.Intersects(playerCol.bounds))
            {
                OnTriggerEnter2D(playerCol);
            }
        }
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
        if (string.IsNullOrEmpty(prompt)) prompt = "按 E 對話";
    }

    public override bool CanInteract()
    {
        if (!interactable) return false;
        if (used && oneShot) return false;
        return dialogue != null && story != null;
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract()) return;

        HidePrompt();

        // 自動找目前場上的玩家（跨場景安全）
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found;
        }

        if (player != null)
            pauseAgent = player.GetComponent<PlayerPauseAgent>();

        if (pauseAgent != null)
            pauseAgent.Pause();

        story.StartStory(dialogue, startNodeId);

        StartCoroutine(WaitStoryEnd());

        if (oneShot)
            used = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || !interactable) return;
        if (!IsPlayer(other)) return;

        playerInside = true;

        if (autoStartWhenEnter)
        {
            Interact(other.gameObject);
            return;
        }

        ShowPrompt(prompt);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInside = false;

        HidePrompt();
    }

    IEnumerator WaitStoryEnd()
    {
        while (story != null && story.IsPlaying)
            yield return null;

        if (pauseAgent != null)
            pauseAgent.Resume();

        if (playerInside && !autoStartWhenEnter)
            ShowPrompt(prompt);
    }

    bool IsPlayer(Collider2D col)
    {
        if (col == null) return false;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        return col.gameObject == player;
    }

    // -----------------------
    // 安全呼叫 UI 顯示/隱藏
    // -----------------------

    void ShowPrompt(string msg)
    {
        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Show(msg);
    }

    void HidePrompt()
    {
        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();
    }
}
