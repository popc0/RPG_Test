using UnityEngine;

[DefaultExecutionOrder(400)] // 在角色移動之後再更新
public class YSort_Object : MonoBehaviour
{
    [Tooltip("要排序的 Renderer。如果留空會自動抓取本物件的 Renderer。")]
    public Renderer targetRenderer;

    [Tooltip("用哪個點作為排序依據，通常設為物件底部的空物件。")]
    public Transform pivot;

    [Tooltip("數值越大代表精度越高，建議 100。")]
    public int yToOrderMultiplier = 100;

    [Tooltip("額外偏移，用來手動調整優先權。")]
    public int offset = 0;

    [Tooltip("是否取負，預設 true：越下方 order 越大。")]
    public bool invert = true;

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
    }

    void LateUpdate()
    {
        if (targetRenderer == null) return;

        float y = (pivot != null ? pivot.position.y : transform.position.y);
        int baseOrder = Mathf.RoundToInt(y * yToOrderMultiplier);
        int order = (invert ? -baseOrder : baseOrder) + offset;

        targetRenderer.sortingOrder = order;
    }
}
