using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(500)] // 在大多數角色位移後再執行
public class SortingOrderController : MonoBehaviour
{
    public enum Mode { Manual, YSort }

    [Header("模式")]
    public Mode mode = Mode.YSort;

    [Header("共用設定")]
    [Tooltip("要一起被設定排序的 Renderer；若留空會自動抓取所有子階層 Renderer。")]
    public List<Renderer> targetRenderers = new List<Renderer>();

    [Tooltip("指定 Sorting Layer（留空不改）。")]
    public string sortingLayerName = "";

    [Header("Manual 模式")]
    [Tooltip("手動指定 sortingOrder。")]
    public int manualOrder = 0;

    [Header("YSort 模式")]
    [Tooltip("腳底座標（通常設為角色腳下的小空物件）。若留空就用本物件 Transform。")]
    public Transform feet;

    [Tooltip("把世界座標 y 乘上這個倍率再取整數，預設 100。")]
    public int yToOrderMultiplier = 100;

    [Tooltip("最後加上的微調偏移，越大越靠上層。")]
    public int offset = 0;

    [Tooltip("是否把 order 設為負的（常見作法：order = -y * M + offset）。")]
    public bool invert = true;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Count == 0)
        {
            // 自動抓取所有子階層 Renderer（SpriteRenderer / MeshRenderer 都會被抓到）
            targetRenderers = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
        }

        // 避免把粒子或其他不需要的也抓進來時可視情況過濾：
        // targetRenderers.RemoveAll(r => r is ParticleSystemRenderer);

        ApplySortingLayer();
        ApplyOrder();
    }

    private void LateUpdate()
    {
        ApplyOrder();
    }

    private void ApplySortingLayer()
    {
        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            int layerId = SortingLayer.NameToID(sortingLayerName);
            foreach (var r in targetRenderers)
            {
                if (r == null) continue;
                r.sortingLayerID = layerId;
            }
        }
    }

    private void ApplyOrder()
    {
        int order = manualOrder;

        if (mode == Mode.YSort)
        {
            var t = feet != null ? feet : transform;
            float y = t.position.y;
            int baseOrder = Mathf.RoundToInt(y * yToOrderMultiplier);
            order = (invert ? -baseOrder : baseOrder) + offset;
        }

        foreach (var r in targetRenderers)
        {
            if (r == null) continue;
            r.sortingOrder = order;
        }
    }

    // 讓你在程式或事件中即時切換
    public void SetManualOrder(int order)
    {
        mode = Mode.Manual;
        manualOrder = order;
        ApplyOrder();
    }

    public void SetYSortOffset(int newOffset)
    {
        offset = newOffset;
        if (mode == Mode.YSort) ApplyOrder();
    }

    public void SetMode(Mode m)
    {
        mode = m;
        ApplyOrder();
    }
}
