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

    [ContextMenu("分層所有 Tile")]
    public void SplitTilemapByY()
    {
        if (sourceTilemap == null || sourceGrid == null)
        {
            Debug.LogError("請指定來源 Tilemap 和 Grid！");
            return;
        }

        BoundsInt bounds = sourceTilemap.cellBounds;
        TileBase[] allTiles = sourceTilemap.GetTilesBlock(bounds);

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileBase tile = sourceTilemap.GetTile(pos);
                if (tile == null) continue;

                // 尋找或建立對應 Y 值的 Tilemap
                string tilemapName = $"Tilemap_Object_Y{y}";
                Transform found = targetParent.Find(tilemapName);
                Tilemap targetTilemap = null;

                if (found == null)
                {
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

                    TilemapRenderer rend = newTM.GetComponent<TilemapRenderer>();
                    rend.sortingLayerName = "Default";
                    rend.sortingOrder = -y * 100; // 依 Y 值設排序

                    targetTilemap = newTM.GetComponent<Tilemap>();
                }
                else
                {
                    targetTilemap = found.GetComponent<Tilemap>();
                }

                targetTilemap.SetTile(pos, tile);
            }
        }

        Debug.Log("Tilemap 分層完成！");
    }
}
#endif
