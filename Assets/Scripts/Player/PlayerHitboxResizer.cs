using UnityEngine;
using System.Collections.Generic;
using RPG;

public class PlayerHitboxResizer : MonoBehaviour
{
    [Header("核心引用")]
    public MainPointComponent mainPoint;

    [Header("要縮放的容器 (父物件)")]
    [Tooltip("請把 Body 或 Feet 的『父物件』拖進來。\n腳本會修改這些父物件的 Scale，讓底下的 CircleCollider 自動變成橢圓且隨敏捷縮小。")]
    public List<Transform> scalingRoots = new List<Transform>();

    void Awake()
    {
        if (!mainPoint) mainPoint = GetComponentInParent<MainPointComponent>();
    }

    void OnEnable()
    {
        if (mainPoint) mainPoint.OnStatChanged += RefreshSize;
        RefreshSize();
    }

    void OnDisable()
    {
        if (mainPoint) mainPoint.OnStatChanged -= RefreshSize;
    }

    public void RefreshSize()
    {
        if (!mainPoint) return;

        // 1. 取得敏捷係數 (例如 敏捷 0 -> 1.0, 敏捷 100 -> 0.8)
        float agi = mainPoint.MP.Agility;
        float agilityScale = Balance.PlayerHitboxScale(agi);

        // 2. 取得全域透視比例 (例如 X=1, Y=0.577)
        Vector3 globalPerspective = PerspectiveUtils.GlobalScale;

        // 3. 計算最終 Scale
        // X = 1.0 * 0.8 = 0.8 (變瘦)
        // Y = 0.577 * 0.8 = 0.46 (變扁 + 變瘦)
        Vector3 finalScale = new Vector3(
            globalPerspective.x * agilityScale,
            globalPerspective.y * agilityScale,
            1f
        );

        // 4. 套用到所有指定的父物件
        foreach (var t in scalingRoots)
        {
            if (t != null)
                t.localScale = finalScale;
        }
    }
}