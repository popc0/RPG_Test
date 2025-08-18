using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("必要元件")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;

    [Header("翻轉目標（預設用 Animator 物件）")]
    [Tooltip("建議指定到角色的視覺子物件（例如 Body / Q0），避免翻轉到含 Rigidbody2D 的根物件。")]
    [SerializeField] private Transform flipTarget;

    private string currentState = "";
    private const float idleThreshold = 0.01f;

    void Awake()
    {
        // 若未指定翻轉目標，預設用 Animator 所在的 Transform
        if (flipTarget == null && animator != null)
            flipTarget = animator.transform;
    }

    void Update()
    {
        Vector2 velocity = rb != null ? rb.velocity : Vector2.zero;

        if (velocity.magnitude < idleThreshold)
        {
            SetIdle();
        }
        else
        {
            SetWalk(velocity);
        }
    }

    void SetIdle()
    {
        // 依速度判斷朝向（停下時沿用 rb 的最後方向判定）
        string dir = GetDirection(rb != null ? rb.velocity : Vector2.zero);

        // 左向改用右向動畫名稱，並水平翻轉
        string animDir = MapDirForAnim(dir); // left -> right，其餘維持
        ApplyFlip(dir);

        ChangeAnimation("idle_" + animDir);
    }

    void SetWalk(Vector2 velocity)
    {
        string dir = GetDirection(velocity);

        // 左向改用右向動畫名稱，並水平翻轉
        string animDir = MapDirForAnim(dir);
        ApplyFlip(dir);

        ChangeAnimation("walk_" + animDir);
    }

    // 根據速度向量決定邏輯方向字串（left/right/up/down）
    string GetDirection(Vector2 velocity)
    {
        if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
        {
            return velocity.x > 0 ? "right" : "left";
        }
        else
        {
            return velocity.y > 0 ? "up" : "down";
        }
    }

    // 將左向的動畫名稱映射成「右向」，其餘維持原向
    string MapDirForAnim(string dir)
    {
        return dir == "left" ? "right" : dir;
    }

    // 依方向進行水平翻轉：left -> x = -1，其餘 -> x = 1
    void ApplyFlip(string dir)
    {
        if (flipTarget == null) return;

        Vector3 s = flipTarget.localScale;
        float targetX = (dir == "left") ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);

        // 僅在需要時才變更，避免每幀寫入
        if (!Mathf.Approximately(s.x, targetX))
        {
            s.x = targetX;
            flipTarget.localScale = s;
        }
    }

    // 避免重複播放同一動畫
    void ChangeAnimation(string newState)
    {
        if (currentState == newState) return;
        animator.Play(newState);
        currentState = newState;
    }
}
