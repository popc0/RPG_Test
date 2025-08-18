using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 moveInput;
    private string lastIdleState = "idle_down";

    [Header("翻轉目標（建議設定為角色外觀，例如 Q0 prefab）")]
    [SerializeField] private Transform flipTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        // 如果沒指定 flipTarget，預設使用 Animator 物件
        if (flipTarget == null && animator != null)
            flipTarget = animator.transform;
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
                ApplyFlip(false);
            }
            else
            {
                animator.Play("walk_right"); // 共用右邊動畫
                lastIdleState = "idle_right"; // idle_left → idle_right
                ApplyFlip(true); // 水平翻轉
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

    // true = 左翻，false = 正常
    void ApplyFlip(bool flip)
    {
        if (flipTarget == null) return;

        Vector3 scale = flipTarget.localScale;
        scale.x = flip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        flipTarget.localScale = scale;
    }
}
