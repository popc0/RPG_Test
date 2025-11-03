using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;

    [Header("模型/動畫物件")]
    public GameObject modelDownWalk;
    public GameObject modelUp;
    public GameObject modelDownIdle;

    [Header("翻轉根物件")]
    public Transform flipTargetDownWalk;
    public Transform flipTargetUp;
    public Transform flipTargetDownIdle;

    [Header("動畫名稱")]
    public string walkRU = "walk_ru";
    public string walkLU = "walk_lu";
    public string walkRD = "walk_rd";
    public string walkLD = "walk_ld";
    public string idleRU = "idle_ru";
    public string idleLU = "idle_lu";
    public string idleRD = "idle_rd";
    public string idleLD = "idle_ld";

    [Header("待機延遲 (秒)")]
    public float idleBuffer = 0.08f;

    [Header("輸入提供者（可放同物件上）")]
    public MonoBehaviour inputProviderBehaviour; // 需實作 IMoveInputProvider
    private IMoveInputProvider inputProvider;

    // ====== 內部 ======
    private Rigidbody2D rb;
    private Animator animDownWalk, animUp, animDownIdle, currentAnim;
    private Vector2 input;
    private Vector2 lastPos, moveDelta;
    private float idleTimer = 0f;
    private string currentState = null;
    private int lastHorizSign = +1; // -1=左, +1=右
    private int lastVertSign = -1;  // -1=下, +1=上

    private enum Facing { RU, LU, RD, LD }
    private Facing currentFacing = Facing.RD;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (modelDownWalk) animDownWalk = modelDownWalk.GetComponentInChildren<Animator>(true);
        if (modelUp) animUp = modelUp.GetComponentInChildren<Animator>(true);
        if (modelDownIdle) animDownIdle = modelDownIdle.GetComponentInChildren<Animator>(true);

        ActivateOnly(modelDownWalk);
        currentAnim = animDownWalk;

        if (!rb) lastPos = transform.position;
        else lastPos = rb.position;

        // 綁定輸入提供者
        if (inputProviderBehaviour is IMoveInputProvider p) inputProvider = p;
        else inputProvider = GetComponent<IMoveInputProvider>();
    }

    void Update()
    {
        // 從外部輸入提供者讀取（沒有就當作靜止）
        input = (inputProvider != null) ? inputProvider.ReadMove() : Vector2.zero;
        input = input.sqrMagnitude > 1e-6f ? Vector2.ClampMagnitude(input, 1f) : Vector2.zero;

        HandleAnimation();
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);
            moveDelta = rb.position - lastPos;
            lastPos = rb.position;
        }
        else
        {
            transform.position += (Vector3)(input * moveSpeed * Time.fixedDeltaTime);
            moveDelta = Vector2.zero;
        }
    }

    // ====== 動畫 ======
    void HandleAnimation()
    {
        Vector2 dir = moveDelta;

        if (dir == Vector2.zero)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleBuffer)
            {
                ShowForIdle(currentFacing);
                PlayIdle(currentFacing);
            }
            return;
        }
        else idleTimer = 0f;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            int sx = Mathf.Sign(dir.x) >= 0 ? +1 : -1;
            int sy = lastVertSign;
            lastHorizSign = sx;
            Facing f = ToFacing(sx, sy);
            ShowForWalk(f); PlayWalk(f);
        }
        else
        {
            int sy = Mathf.Sign(dir.y) >= 0 ? +1 : -1;
            int sx = lastHorizSign;
            lastVertSign = sy;
            Facing f = ToFacing(sx, sy);
            ShowForWalk(f); PlayWalk(f);
        }
    }

    Facing ToFacing(int sx, int sy)
    {
        if (sy >= 0) return (sx >= 0) ? Facing.RU : Facing.LU;
        else return (sx >= 0) ? Facing.RD : Facing.LD;
    }

    void ShowForWalk(Facing f)
    {
        currentFacing = f;
        if (f == Facing.RU || f == Facing.LU)
        {
            ActivateOnly(modelUp); currentAnim = animUp;
            ApplyFlip(flipTargetUp, f == Facing.LU);
        }
        else
        {
            ActivateOnly(modelDownWalk); currentAnim = animDownWalk;
            ApplyFlip(flipTargetDownWalk, f == Facing.LD);
        }
    }

    void ShowForIdle(Facing f)
    {
        currentFacing = f;
        if (f == Facing.RU || f == Facing.LU)
        {
            ActivateOnly(modelUp); currentAnim = animUp;
            ApplyFlip(flipTargetUp, f == Facing.LU);
        }
        else
        {
            ActivateOnly(modelDownIdle); currentAnim = animDownIdle;
            ApplyFlip(flipTargetDownIdle, f == Facing.LD);
        }
    }

    void PlayWalk(Facing f)
    {
        switch (f)
        {
            case Facing.RU: PlayOnce(walkRU); break;
            case Facing.LU: PlayOnce(walkLU); break;
            case Facing.RD: PlayOnce(walkRD); break;
            case Facing.LD: PlayOnce(walkLD); break;
        }
    }

    void PlayIdle(Facing f)
    {
        switch (f)
        {
            case Facing.RU: PlayOnce(idleRU); break;
            case Facing.LU: PlayOnce(idleLU); break;
            case Facing.RD: PlayOnce(idleRD); break;
            case Facing.LD: PlayOnce(idleLD); break;
        }
    }

    void PlayOnce(string stateName)
    {
        if (!currentAnim) return;
        if (currentState == stateName) return;
        currentAnim.Play(stateName);
        currentState = stateName;
    }

    void ApplyFlip(Transform target, bool flip)
    {
        if (!target) return;
        var s = target.localScale;
        float tx = flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        if (!Mathf.Approximately(s.x, tx))
        {
            s.x = tx;
            target.localScale = s;
        }
    }

    void ActivateOnly(GameObject go)
    {
        if (modelDownWalk) modelDownWalk.SetActive(go == modelDownWalk);
        if (modelUp) modelUp.SetActive(go == modelUp);
        if (modelDownIdle) modelDownIdle.SetActive(go == modelDownIdle);
    }
}

/// <summary>
/// 統一的「移動向量提供」介面，讓 PlayerMovement 只關心 Vector2。
/// </summary>
public interface IMoveInputProvider
{
    /// <returns>一般化到 -1..1 的移動向量（可為零向量）。</returns>
    Vector2 ReadMove();
}
