using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UIRadarChart : MaskableGraphic
{
    [Header("數值設定 (0 ~ 1)")]
    // 順序建議：0:Top, 1:Top-Right, 2:Bottom-Right, 3:Bottom, 4:Bottom-Left, 5:Top-Left
    public float[] values = new float[6] { 1, 1, 1, 1, 1, 1 };

    [Header("外觀設定")]
    public float thickness = 0f; // 實心模式設為 0
    public bool fill = true;     // 是否填滿顏色

    [Header("旋轉角度 (90度表示頂點朝上)")]
    public float startAngleOffset = 90f;

    // 用於快取計算
    private int segments = 6;

    /// <summary>
    /// 供外部呼叫：設定 6 個屬性的正規化數值 (0.0 ~ 1.0)
    /// </summary>
    public void SetValues(float[] newValues)
    {
        if (newValues.Length != 6) return;

        // 複製並限制在 0~1 之間
        for (int i = 0; i < 6; i++)
        {
            values[i] = Mathf.Clamp01(newValues[i]);
        }

        // 通知 UI 系統重新繪製網格
        SetVerticesDirty();
    }

    // 這是 Unity UI 繪圖的核心方法
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        // 半徑取寬高中較小的一半
        float radius = Mathf.Min(width, height) / 2f;

        Vector2 center = Vector2.zero;

        // 1. 如果是實心，先加中心點 (Index 0)
        if (fill)
        {
            vh.AddVert(center, color, Vector2.zero);
        }

        // 2. 計算 6 個頂點的位置
        float angleStep = 360f / segments;

        // 我們先算出所有頂點位置存起來
        Vector2[] vertices = new Vector2[segments];

        for (int i = 0; i < segments; i++)
        {
            // 數值 * 半徑 = 該角的距離
            float r = radius * values[i];

            // 角度 (順時針排列)
            float rad = (startAngleOffset - (i * angleStep)) * Mathf.Deg2Rad;

            vertices[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
        }

        // 3. 繪製三角形
        if (fill)
        {
            // 實心模式：畫 6 個三角形，從中心連到邊緣
            // 頂點已經在上面迴圈算好了，現在加入 VertexHelper
            for (int i = 0; i < segments; i++)
            {
                vh.AddVert(vertices[i], color, Vector2.zero);
            }

            // 連接三角形 (Center=0, Current=i+1, Next=(i+1)%6 + 1)
            for (int i = 0; i < segments; i++)
            {
                int currentVertIdx = i + 1;
                int nextVertIdx = ((i + 1) % segments) + 1;
                vh.AddTriangle(0, currentVertIdx, nextVertIdx);
            }
        }
        else
        {
            // 線框模式 (如果不想填滿，只想畫外框線) - 簡單實作：畫粗線較複雜，這裡暫略
            // 建議直接疊兩層：底層半透明填滿，上層用 LineRenderer 或另一張圖做框
        }
    }
}