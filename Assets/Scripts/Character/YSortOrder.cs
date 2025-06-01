using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortOrder : MonoBehaviour
{
    [SerializeField] private int offset = 0;
    [SerializeField] private int multiplier = 100;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        sr.sortingOrder = -Mathf.RoundToInt(transform.position.y * multiplier) + offset;
    }
}
