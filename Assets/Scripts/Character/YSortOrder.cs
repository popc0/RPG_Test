using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortOrder : MonoBehaviour
{
    [SerializeField] private int sortingOffset = 0;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        sr.sortingOrder = -(int)(transform.position.y * 100) + sortingOffset;
    }
}
