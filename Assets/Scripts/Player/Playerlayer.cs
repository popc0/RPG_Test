using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class IsometricSorting : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    void LateUpdate()
    {
        // 負數會讓越低的 Y 在越上層
        spriteRenderer.sortingOrder = -(int)(transform.position.y * 100);
    }
}
