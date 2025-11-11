using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input (UnifiedInputSource)")]
    [SerializeField] private UnifiedInputSource inputSource; // 若沒指定會自動往上找一次

    [Header("Selection")]
    [SerializeField] private bool pickNearestInOverlap = true; // 同時多個時挑最近
    [SerializeField] private Transform playerRoot;             // 以這個點算距離，預設用最上層 Player

    private readonly List<IInteractable> candidates = new();
    private IInteractable currentTarget;

    void Reset()
    {
        // 確保是 Trigger：Player 走進互動區才會被收集
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        if (playerRoot == null) playerRoot = transform.root;
        if (inputSource == null)
        {
            // 只自動找同樹上的一次，避免抓到場上其他來源
            inputSource = GetComponentInParent<UnifiedInputSource>();
        }
    }

    void Update()
    {
        // 每幀選出有效目標
        currentTarget = SelectTarget();

        // 互動鍵按下：轉呼叫到目標
        if (currentTarget != null &&
            inputSource != null &&
            inputSource.InteractPressedThisFrame())
        {
            currentTarget.Interact(playerRoot != null ? playerRoot.gameObject : gameObject);
        }
    }

    private IInteractable SelectTarget()
    {
        IInteractable best = null;
        float bestSqr = float.PositiveInfinity;

        // 清掉已被刪除的引用
        for (int i = candidates.Count - 1; i >= 0; --i)
        {
            var c = candidates[i];
            if (c == null) { candidates.RemoveAt(i); continue; }

            if (!c.CanInteract()) continue;

            if (!pickNearestInOverlap)
                return c; // 第一個有效就用

            var mb = c as MonoBehaviour;
            if (mb == null) continue;

            Vector2 p = (playerRoot ? playerRoot.position : transform.position);
            float sqr = (mb.transform.position - (Vector3)p).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = c;
                bestSqr = sqr;
            }
        }

        return best;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null && !candidates.Contains(interactable))
            candidates.Add(interactable);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            candidates.Remove(interactable);
            if (currentTarget == interactable) currentTarget = null;
        }
    }
}
