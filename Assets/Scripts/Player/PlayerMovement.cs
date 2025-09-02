using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;

    [Header("模型參考")]
    [SerializeField] private GameObject modelLR;      // 左右/向下用的 Live2D（翻轉共用）
    [SerializeField] private Transform  flipTargetLR; // 建議指到 modelLR 的可見根
    [SerializeField] private GameObject modelUp;      // 專門往上用的 Live2D（不翻轉）

    [Header("動畫狀態名稱（兩個模型的 Animator 都要有相同命名）")]
    [SerializeField] private string walkRight = "walk_right";
    [SerializeField] private string walkDown  = "walk_down";
    [SerializeField] private string walkUp    = "walk_up";
    [SerializeField] private string idleRight = "idle_right";
    [SerializeField] private string idleDown  = "idle_down";
    [SerializeField] private string idleUp    = "idle_up";

    private Rigidbody2D rb;
    private Animator animLR;
    private Animator animUp;

    private Vector2 moveInput;
    private string lastIdleState = "idle_down";
    private Animator currentAnim = null; // 目前正在使用的 Animator（會在模型切換時更新）

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (modelLR != null) animLR = modelLR.GetComponentInChildren<Animator>(true);
        if (modelUp != null) animUp = modelUp.GetComponentInChildren<Animator>(true);

        // 預設顯示 LR，隱藏 Up
        SetActiveModel(useUp: false);

        // flipTargetLR 未指定時，嘗試用 LR 的 Animator Transform
        if (flipTargetLR == null && animLR != null)
            flipTargetLR = animLR.transform;
    }

    void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
        HandleAnimation();
    }

    void FixedUpdate()
    {
        if (rb != null)
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void HandleAnimation()
    {
        if (moveInput == Vector2.zero)
        {
            // 停下時播最後一次移動方向對應的待機，使用當前啟用的 Animator
            if (currentAnim != null)
                currentAnim.Play(lastIdleState);
            return;
        }

        // 左右優先
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            // 使用 LR 模型
            SetActiveModel(useUp: false);

            if (moveInput.x > 0f)
            {
                ApplyFlipLR(false);
                currentAnim.Play(walkRight);
                lastIdleState = idleRight;
            }
            else
            {
                ApplyFlipLR(true);          // 左向用右向動畫翻轉
                currentAnim.Play(walkRight);
                lastIdleState = idleRight;  // 待機也用右向（翻轉）
            }
        }
        else
        {
            if (moveInput.y > 0f)
            {
                // 使用 Up 模型（不水平翻轉）
                SetActiveModel(useUp: true);
                currentAnim.Play(walkUp);
                lastIdleState = idleUp;
            }
            else
            {
                // 使用 LR 模型
                SetActiveModel(useUp: false);
                ApplyFlipLR(false); // 向下時不需要翻轉
                currentAnim.Play(walkDown);
                lastIdleState = idleDown;
            }
        }
    }

    // 切換啟用哪個模型，同時更新 currentAnim
    void SetActiveModel(bool useUp)
    {
        if (modelLR != null) modelLR.SetActive(!useUp);
        if (modelUp != null) modelUp.SetActive(useUp);

        if (useUp)
        {
            currentAnim = animUp;
            // 確保 LR 翻轉對象不被誤用
            if (flipTargetLR != null)
            {
                var s = flipTargetLR.localScale;
                s.x = Mathf.Abs(s.x);
                flipTargetLR.localScale = s;
            }
        }
        else
        {
            currentAnim = animLR;
        }
    }

    // 專門改 LR 模型的水平翻轉（Up 模型不翻）
    void ApplyFlipLR(bool flip)
    {
        if (flipTargetLR == null) return;
        var s = flipTargetLR.localScale;
        float targetX = flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        if (!Mathf.Approximately(s.x, targetX))
        {
            s.x = targetX;
            flipTargetLR.localScale = s;
        }
    }
}
