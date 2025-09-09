using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TeleporterInteractable : InteractableBase
{
    [Header("資料（提示文字等）")]
    public TeleporterData data;

    [Header("同場景目標")]
    public Transform targetTransform;   // 場景內的空物件或地標
    public Vector2 offset;            // 微調位置，可為 (0,0)

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
        return interactable && targetTransform != null;
    }

    public override void Interact(GameObject interactor)
    {
        if (targetTransform == null) return;

        Vector2 dst = (Vector2)targetTransform.position + offset;
        interactor.transform.position = dst;

        var rb = interactor.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = Vector2.zero;
    }
}
