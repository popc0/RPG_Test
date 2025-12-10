using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private UnifiedInputSource inputSource;

    [Header("互動設定 (分離架構)")]
    [Tooltip("真正要進行互動的主體 (例如 Player Root)。\n當觸發傳送時，移動的是這個物件。")]
    [SerializeField] private GameObject interactionAvatar;

    [Tooltip("用來計算「最近距離」的參考點。\n如果不填，預設使用本物件(Sensor)的位置。\n你可以調整本物件在 Prefab 中的 Local Position 來達成偵測偏移。")]
    [SerializeField] private Transform distanceReference;

    [Header("選擇邏輯")]
    [SerializeField] private bool pickNearestInOverlap = true;

    private readonly List<IInteractable> candidates = new();
    private IInteractable currentTarget;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        // 1. 自動抓取 Input
        if (inputSource == null)
            inputSource = GetComponentInParent<UnifiedInputSource>();

        // 2. 設定互動主體 (預設抓根物件)
        if (interactionAvatar == null)
            interactionAvatar = transform.root.gameObject;

        // 3. 設定距離參考點 (預設抓自己/Sensor)
        // 這樣你就可以把 Sensor 往前移，讓判定以 Sensor 為準，而不是以腳底(Root)為準
        if (distanceReference == null)
            distanceReference = transform;
    }

    void Update()
    {
        currentTarget = SelectTarget();

        if (currentTarget != null &&
            inputSource != null &&
            inputSource.InteractPressedThisFrame())
        {
            // ★ 關鍵：互動時，把「主體 (Root)」傳給對方
            // 對方 (如傳送點) 會移動傳進去的這個物件
            currentTarget.Interact(interactionAvatar);
        }
    }

    private IInteractable SelectTarget()
    {
        IInteractable best = null;
        float bestSqr = float.PositiveInfinity;

        // 取得測量基準點 (通常是 Sensor 自己的位置)
        Vector3 originPos = distanceReference.position;

        for (int i = candidates.Count - 1; i >= 0; --i)
        {
            var c = candidates[i];
            if (c == null) { candidates.RemoveAt(i); continue; }

            if (!c.CanInteract()) continue;

            if (!pickNearestInOverlap) return c;

            var mb = c as MonoBehaviour;
            if (mb == null) continue;

            // ★ 關鍵：計算距離時，使用 distanceReference (Sensor) 而不是 Root
            float sqr = (mb.transform.position - originPos).sqrMagnitude;
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