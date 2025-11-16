using UnityEngine;

public class AimSource2D : MonoBehaviour
{
    [Header("輸入來源 (拖 Player/Input 上的 UnifiedInputSource)")]
    public UnifiedInputSource input;

    [Header("參考點（不設則用自身）")]
    public Transform origin;

    [Header("靈敏度")]
    [Tooltip("滑鼠相對 origin 的最小有效距離")]
    public float mouseDeadRadius = 0.05f;
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
        //if (dir.sqrMagnitude < mouseDeadRadius * mouseDeadRadius)
        //dir = allowKeyboardAim ? ReadKeyboardDir() : Vector2.zero;

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
    /*
    Vector2 ReadMouseDir()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return Vector2.zero;

        var mouse = Mouse.current;
        if (mouse == null) return Vector2.zero;

        Vector3 o = origin ? origin.position : transform.position;
        Vector2 screen = mouse.position.ReadValue();
        Vector3 world = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
        world.z = 0f;
        return (Vector2)(world - o);
    }

    Vector2 ReadKeyboardDir()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        int x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0)
              - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
        int y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1 : 0)
              - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);

        if (invertY) y = -y;

        Vector2 v = new Vector2(x, y);
        if (v.magnitude <= keyDeadZone) return Vector2.zero;
        return normalizeOutput && v != Vector2.zero ? v.normalized : v;
    }
      */
}
