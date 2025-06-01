using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[InitializeOnLoad]
public static class TilemapOrderSetter
{
    static TilemapOrderSetter()
    {
        EditorApplication.hierarchyChanged += SetTilemapOrders;
    }

    private static void SetTilemapOrders()
    {
        var tilemaps = GameObject.FindObjectsOfType<TilemapRenderer>();

        foreach (var renderer in tilemaps)
        {
            // 如果已經有設定過就略過
            if (renderer.sortingOrder != 0) continue;

            float y = renderer.transform.position.y;
            renderer.sortingOrder = Mathf.RoundToInt(-y * 100);
        }
    }
}
