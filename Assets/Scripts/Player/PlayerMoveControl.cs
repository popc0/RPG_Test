using RPG;
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

    [Header("方向死區（只看方向，不看力度）")]
    [Range(0f, 1f)]
    public float moveDeadzone = 0.15f;   // 小於這個長度就當成完全不動

    public Vector2 LastMove { get; private set; } // 上一幀的實際位移（世界座標）

    Rigidbody2D _rb;
    Transform _root;


    // [新增] 引用 StatusManager、主屬性(Start時自動抓取)
    [Header("Status")]
    [SerializeField] private StatusManager statusManager;
    [SerializeField] private MainPointComponent _mainPoint;

    void Awake()
    {
        _root = targetRoot != null ? targetRoot : transform.root;
        _rb = _root.GetComponent<Rigidbody2D>();
        if (_rb == null)
            Debug.LogWarning("[PlayerMoveControl] 目標上沒有 Rigidbody2D，將改用 Transform 移動。");
        if (!statusManager) statusManager = GetComponent<StatusManager>();
        _mainPoint = GetComponentInParent<MainPointComponent>();
    }

    void FixedUpdate()
    {
        // ★ 修改：加入 CanMove 檢查
        // 如果狀態管理器說不能動，就停止移動
        if (statusManager != null && !statusManager.CanMove)
        {
            if (_rb) _rb.velocity = Vector2.zero; // 確保完全停下
            return;
        }
        // 1. 從 UnifiedInputSource 讀值（鍵盤 / 手把 / DynamicTouchJoystick 都會經過這裡）
        Vector2 dir = (input != null) ? input.GetMoveVector() : Vector2.zero;

        if (dir.sqrMagnitude < moveDeadzone * moveDeadzone)
        {
            dir = Vector2.zero;
        }else{
            // ★ 核心邏輯修改：決定是否要 "吃掉力度" (強制全速)

            bool allowAnalog = (statusManager != null && statusManager.IsAnalogMove);

            if (!allowAnalog)
            {
                // 【模式 A：數位移動 (預設)】
                // 我們希望忽略推桿力度，強制皆為 "該方向的最大透視速度"

                // 1. 取得純方向 (正規化)
                Vector2 normalizedVisualDir = dir.normalized;

                // 2. 問 PerspectiveUtils：這個方向的最大長度應該是多少？
                // (例如往右是 1.0，往上是 0.577)
                float maxVisualMagnitude = PerspectiveUtils.GetVisualScaleFactor(normalizedVisualDir);

                // 3. 強制設定為最大長度
                dir = normalizedVisualDir * maxVisualMagnitude;
            }
            // else { 
            //    【模式 B：類比移動 (被動技能)】
            //    直接保留 input 給的 dir，因為它已經是 (原始力度 * 透視倍率) 了
            //    輕推就會走得慢，但依然符合透視比例
            // }
        }

        float agility = (_mainPoint != null) ? _mainPoint.MP.Agility : 0f;

        float finalSpeed = Balance.MoveSpeed(moveSpeed, agility);

        // 跑步加成 (如果有按跑步鍵的話，再乘上去)
        finalSpeed *= runMultiplier;

        // 套用速度
        if (_rb != null)
        {
            Vector2 next = _rb.position + dir * finalSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(next);
            LastMove = dir * finalSpeed * Time.fixedDeltaTime;
        }
        else if (_root != null)
        {
            Vector3 before = _root.position;
            _root.position += (Vector3)(dir * finalSpeed * Time.fixedDeltaTime);
            LastMove = (Vector2)(_root.position - before);
        }
    }
}
