// Assets/Scripts/Tilemap/TilemapLayerSplitter.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

#if UNITY_EDITOR
public class TilemapLayerSplitter : MonoBehaviour
{
    [Header("來源 Tilemap（用來畫圖）")]
    public Grid sourceGrid;
    public Tilemap sourceTilemap;

    [Header("目標父物件（所有分層會掛在這裡）")]
    public Transform targetParent;

    [Header("Tilemap Prefab 或模板（可選）")]
    public GameObject tilemapTemplate;

    [ContextMenu("分層所有 Tile（依 Y 分橫列）")]
    public void SplitTilemapByY()
    {
        if (sourceTilemap == null || sourceGrid == null || targetParent == null)
        {
            Debug.LogError("請指定來源 Tilemap、Grid 和目標父物件！");
            return;
        }

        BoundsInt bounds = sourceTilemap.cellBounds;

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            string tilemapName = $"Tilemap_Object_Y{y}";
            Transform existing = targetParent.Find(tilemapName);
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            GameObject newTM;
            if (tilemapTemplate != null)
            {
                newTM = (GameObject)PrefabUtility.InstantiatePrefab(tilemapTemplate);
            }
            else
            {
                newTM = new GameObject(tilemapName);
                newTM.AddComponent<Tilemap>();
                newTM.AddComponent<TilemapRenderer>();
            }

            newTM.name = tilemapName;
            newTM.transform.SetParent(targetParent);
            newTM.transform.localPosition = Vector3.zero;

            Tilemap targetTilemap = newTM.GetComponent<Tilemap>();
            TilemapRenderer rend = newTM.GetComponent<TilemapRenderer>();
            rend.sortingLayerName = sourceTilemap.GetComponent<TilemapRenderer>().sortingLayerName;
            rend.sortingOrder = -y * 100;

            // 自動加入 TilemapCollider2D
            if (newTM.GetComponent<TilemapCollider2D>() == null)
            {
                newTM.AddComponent<TilemapCollider2D>();
            }

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileBase tile = sourceTilemap.GetTile(pos);
                if (tile != null)
                {
                    targetTilemap.SetTile(pos, tile);
                }
            }
        }

        Debug.Log("依橫列分層完成（覆寫舊層，含碰撞）！");
    }
}
#endif
