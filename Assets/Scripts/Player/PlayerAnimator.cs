using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;

    private string currentState = "";

    void Update()
    {
        Vector2 velocity = rb.velocity;

        // 判斷是否有在移動
        if (velocity.magnitude < 0.01f)
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
        string dir = GetDirection(rb.velocity);
        ChangeAnimation("idle_" + dir);
    }

    void SetWalk(Vector2 velocity)
    {
        string dir = GetDirection(velocity);
        ChangeAnimation("walk_" + dir);
    }

    // 根據速度向量決定方向字串
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

    // 避免重複播放同一動畫
    void ChangeAnimation(string newState)
    {
        if (currentState == newState) return;
        animator.Play(newState);
        currentState = newState;
    }
}
