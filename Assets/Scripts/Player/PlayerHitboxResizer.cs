using UnityEngine;
using System.Collections.Generic;
using RPG; // 引用 Balance 和 MainPointComponent

public class PlayerHitboxResizer : MonoBehaviour
{
    [Header("核心引用")]
    public MainPointComponent mainPoint;

    [Header("要縮放的碰撞器 (請把 Body/Feet 的 Collider 拖進來)")]
    // 支援 Circle, Box, Capsule
    public List<Collider2D> targetColliders = new List<Collider2D>();

    // 內部用來記錄「原始大小」，避免重複縮放導致越來越小
    private class OriginalSizeData
    {
        public Collider2D col;
        public float radius; // For Circle
        public Vector2 size; // For Box / Capsule
    }
    private List<OriginalSizeData> _originals = new List<OriginalSizeData>();

    void Awake()
    {
        if (!mainPoint) mainPoint = GetComponentInParent<MainPointComponent>();

        // 如果沒有手動拉，自動抓所有子物件的 Collider
        if (targetColliders.Count == 0)
        {
            targetColliders.AddRange(GetComponentsInChildren<Collider2D>());
        }

        // 記錄原始尺寸
        foreach (var col in targetColliders)
        {
            var data = new OriginalSizeData { col = col };
            if (col is CircleCollider2D c) data.radius = c.radius;
            else if (col is BoxCollider2D b) data.size = b.size;
            else if (col is CapsuleCollider2D cap) data.size = cap.size;
            _originals.Add(data);
        }
    }

    void OnEnable()
    {
        if (mainPoint) mainPoint.OnStatChanged += RefreshSize;
        RefreshSize(); // 初始執行一次
    }

    void OnDisable()
    {
        if (mainPoint) mainPoint.OnStatChanged -= RefreshSize;
    }

    public void RefreshSize()
    {
        if (!mainPoint) return;

        // 1. 取得敏捷並計算縮放比例
        float agi = mainPoint.MP.Agility; // 注意要用含基礎值的總敏捷
        float scale = Balance.PlayerHitboxScale(agi);

        // 2. 套用到所有記錄的 Collider
        foreach (var data in _originals)
        {
            if (data.col == null) continue;

            if (data.col is CircleCollider2D c)
            {
                c.radius = data.radius * scale;
            }
            else if (data.col is BoxCollider2D b)
            {
                b.size = data.size * scale;
            }
            else if (data.col is CapsuleCollider2D cap)
            {
                cap.size = data.size * scale;
            }
        }

        // Debug.Log($"[Hitbox] 敏捷 {agi} -> 縮放比例 {scale:F2}");
    }
}