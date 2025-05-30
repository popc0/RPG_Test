using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 moveInput;
    private string lastIdleState = "idle_down";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>(); // Animator 在 Body 上
    }

    void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // 正規化避免斜方向過快
        if (moveInput.magnitude > 1)
            moveInput.Normalize();

        HandleAnimation();
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void HandleAnimation()
    {
        if (moveInput == Vector2.zero)
        {
            animator.Play(lastIdleState);
            return;
        }

        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            // 左右優先
            if (moveInput.x > 0)
            {
                animator.Play("walk_right");
                lastIdleState = "idle_right";
            }
            else
            {
                animator.Play("walk_left");
                lastIdleState = "idle_left";
            }
        }
        else
        {
            // 上下
            if (moveInput.y > 0)
            {
                animator.Play("walk_up");
                lastIdleState = "idle_up";
            }
            else
            {
                animator.Play("walk_down");
                lastIdleState = "idle_down";
            }
        }
    }
}
