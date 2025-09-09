using UnityEngine;
using TMPro;

public class PlayerInteractor : MonoBehaviour
{
    [Header("搜尋")]
    public float radius = 1.2f;
    public LayerMask interactableMask;
    public Transform origin; // 如未指定，預設用自身 transform

    [Header("輸入來源")]
    public MonoBehaviour inputSourceBehaviour; // 指向一個實作 IInputSource 的元件
    IInputSource inputSource;

    [Header("提示 UI（可選）")]
    public TMP_Text promptText;

    IInteractable current;

    void Awake()
    {
        inputSource = inputSourceBehaviour as IInputSource;
        if (origin == null) origin = transform;
    }

    void Update()
    {
        FindBest();
        UpdatePrompt();

        if (current != null && current.CanInteract() && inputSource != null && inputSource.InteractPressedThisFrame())
        {
            current.Interact(gameObject);
        }
    }

    void FindBest()
    {
        current = null;
        var hits = Physics2D.OverlapCircleAll(origin.position, radius, interactableMask);
        float best = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h) continue;
            var it = h.GetComponentInParent<IInteractable>();
            if (it == null || !it.CanInteract()) continue;

            float d = (h.transform.position - origin.position).sqrMagnitude;
            if (d < best) { best = d; current = it; }
        }
    }

    void UpdatePrompt()
    {
        if (promptText == null) return;
        if (current != null && current.CanInteract()) promptText.text = current.GetPrompt();
        else promptText.text = string.Empty;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var p = origin != null ? origin.position : transform.position;
        Gizmos.DrawWireSphere(p, radius);
    }
}
