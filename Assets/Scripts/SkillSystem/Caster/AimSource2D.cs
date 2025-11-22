using UnityEngine;

public class AimSource2D : MonoBehaviour
{
    [Header("輸入來源 (拖 Player/Input 上的 UnifiedInputSource)")]
    public UnifiedInputSource input;

    [Header("參考點（不設則用自身）")]
    public Transform origin;

    [Range(0f, 1f)] public float keyDeadZone = 0f;
    public bool normalizeOutput = true;

    Camera _cam;

    /// <summary>最終的瞄準方向（單位向量）</summary>
    public Vector2 AimDir { get; private set; } = Vector2.right;

    Vector2 _lastDir = Vector2.right;

    void Reset()
    {
        if (!origin) origin = transform;
        _cam = Camera.main;
    }

    void Awake()
    {
        if (!origin) origin = transform;
        _cam = Camera.main;
    }

    void Update()
    {
        Vector2 dir = (input != null) ? input.GetMoveVector() : Vector2.zero;

        if (dir.sqrMagnitude >= 1e-6f)
        {
            _lastDir = normalizeOutput ? dir.normalized : dir;
            AimDir = _lastDir;
        }
        else
        {
            AimDir = _lastDir; // 維持最後方向
        }
    }
}
