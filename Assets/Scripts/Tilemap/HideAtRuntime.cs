using UnityEngine;
using UnityEngine.Tilemaps;

public class HideAtRuntime : MonoBehaviour
{
    void Start()
    {
        var renderer = GetComponent<TilemapRenderer>();
        if (renderer != null)
            renderer.enabled = false;
    }
}
