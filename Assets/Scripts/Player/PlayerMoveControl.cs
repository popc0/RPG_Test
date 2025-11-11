using UnityEngine;

/// <summary>
/// 專心處理「移動本體」：從 UnifiedInputSource 讀取向量，推動根物件 Player 的 Rigidbody2D/Transform。
/// 掛在 Player/Move_Core 上，targetRoot 指向根的 Player。
/// </summary>
public class PlayerMoveControl : MonoBehaviour
{
    [Header("輸入來源 (拖 Player/Input 上的 UnifiedInputSource)")]
    public UnifiedInputSource input;

    [Header("移動目標 (通常為根 Player)")]
    public Transform targetRoot; // 若留空，預設使用 transform.root

    [Header("速度設定")]
    public float moveSpeed = 4.5f;
    public float runMultiplier = 1.0f; // 保留未來擴充

    public Vector2 LastMove { get; private set; } // 上一幀的實際位移（世界座標）

    Rigidbody2D _rb; Transform _root;

    void Awake()
    {
        _root = targetRoot != null ? targetRoot : transform.root;
        _rb = _root.GetComponent<Rigidbody2D>();
        if (_rb == null) Debug.LogWarning("[PlayerMoveControl] 目標上沒有 Rigidbody2D，將改用 Transform 移動。");
    }

    void FixedUpdate()
    {
        Vector2 dir = (input != null) ? input.GetMoveVector() : Vector2.zero;
        float spd = moveSpeed * runMultiplier;

        if (_rb != null)
        {
            Vector2 next = _rb.position + dir * spd * Time.fixedDeltaTime;
            _rb.MovePosition(next);
            LastMove = next - _rb.position; // 注意：MovePosition 之後 _rb.position 仍是上一幀值，此行等同於 dir*...
            // 為了給動畫更準，直接用 dir*spd*dt：
            LastMove = dir * spd * Time.fixedDeltaTime;
        }
        else if (_root != null)
        {
            Vector3 before = _root.position;
            _root.position += (Vector3)(dir * spd * Time.fixedDeltaTime);
            LastMove = (Vector2)(_root.position - before);
        }
    }
}