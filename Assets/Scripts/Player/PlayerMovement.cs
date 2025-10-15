using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;

    [Header("手把 / 類比搖桿（選填軸名，不填就略過）")]
    [Tooltip("手把 X 軸名稱（例如：JoyX / Joystick X / Horizontal_Stick 等）")]
    [SerializeField] private string joystickAxisX = "";   // 例如 "JoyX"
    [Tooltip("手把 Y 軸名稱（例如：JoyY / Joystick Y / Vertical_Stick 等）")]
    [SerializeField] private string joystickAxisY = "";   // 例如 "JoyY"
    [Tooltip("類比死區（避免微小抖動）")]
    [Range(0f, 1f)][SerializeField] private float analogDeadzone = 0.2f;
    [Tooltip("手把 Y 軸是否反向")]
    [SerializeField] private bool invertJoyY = false;

    [Header("模型")]
    [SerializeField] private GameObject modelDownWalk;   // ↓ 象限「走路」用
    [SerializeField] private GameObject modelUp;         // ↑ 象限（走路與待機）
    [SerializeField] private GameObject modelDownIdle;   // ↓ 象限「待機」用（避免參數汙染）

    [Header("翻轉目標(各模型外觀根)")]
    [SerializeField] private Transform flipTargetDownWalk;
    [SerializeField] private Transform flipTargetUp;
    [SerializeField] private Transform flipTargetDownIdle;

    [Header("動畫狀態名稱（各 Animator 需有同名狀態）")]
    [SerializeField] private string walkRU = "walk_ru";
    [SerializeField] private string walkLU = "walk_lu";   // 左側用水平翻轉
    [SerializeField] private string walkRD = "walk_rd";
    [SerializeField] private string walkLD = "walk_ld";   // 左側用水平翻轉
    [SerializeField] private string idleRU = "idle_ru";
    [SerializeField] private string idleLU = "idle_lu";
    [SerializeField] private string idleRD = "idle_rd";
    [SerializeField] private string idleLD = "idle_ld";

    [Header("待機延遲（秒）")]
    [SerializeField] private float idleBuffer = 0.08f;

    private Rigidbody2D rb;
    private Animator animDownWalk;
    private Animator animUp;
    private Animator animDownIdle;
    private Animator currentAnim;

    private Vector2 input;       // 最終用來移動/驅動畫面的輸入（鍵盤或手把）
    private Vector2 lastPos;
    private Vector2 moveDelta;

    // -1=左/下，+1=右/上（初始右下）
    private int lastHorizSign = +1;
    private int lastVertSign = -1;

    private enum Quad { RU, LU, RD, LD }
    private Quad currentQuad = Quad.RD;
    private string currentState = null;
    private float idleTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (modelDownWalk != null) animDownWalk = modelDownWalk.GetComponentInChildren<Animator>(true);
        if (modelUp != null) animUp = modelUp.GetComponentInChildren<Animator>(true);
        if (modelDownIdle != null) animDownIdle = modelDownIdle.GetComponentInChildren<Animator>(true);

        // 預設顯示「下走路」模型
        ActivateOnly(modelDownWalk);
        currentAnim = animDownWalk;

        // 預設翻轉根
        if (flipTargetDownWalk == null && animDownWalk != null) flipTargetDownWalk = animDownWalk.transform;
        if (flipTargetUp == null && animUp != null) flipTargetUp = animUp.transform;
        if (flipTargetDownIdle == null && animDownIdle != null) flipTargetDownIdle = animDownIdle.transform;

        lastPos = rb != null ? rb.position : (Vector2)transform.position;
    }

    void Update()
    {
        // 1) 鍵盤輸入（原本邏輯不動）
        Vector2 kb = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // 2) 手把類比輸入（若有設定軸名才讀）
        Vector2 joy = Vector2.zero;
        if (!string.IsNullOrEmpty(joystickAxisX))
            joy.x = SafeGetAxis(joystickAxisX);
        if (!string.IsNullOrEmpty(joystickAxisY))
            joy.y = SafeGetAxis(joystickAxisY);

        if (invertJoyY) joy.y = -joy.y;

        // 類比死區與正規化
        if (joy.magnitude < analogDeadzone) joy = Vector2.zero;
        else if (joy.magnitude > 1f) joy.Normalize();

        // 3) 合併策略：有手把就用手把；沒有再用鍵盤
        input = (joy != Vector2.zero) ? joy : kb;

        // 避免斜向過快
        if (input.magnitude > 1f) input.Normalize();

        HandleAnimation();
    }

    void FixedUpdate()
    {
        if (rb != null)
            rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);

        var pos = rb != null ? rb.position : (Vector2)transform.position;
        moveDelta = pos - lastPos;
        lastPos = pos;

        if (moveDelta.sqrMagnitude < 0.0001f)
            moveDelta = Vector2.zero;
    }

    void HandleAnimation()
    {
        Vector2 dir = moveDelta;

        // 停止：累積一點時間再切待機（避免當幀姿勢跳動）
        if (dir == Vector2.zero)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleBuffer)
            {
                ShowForIdle(currentQuad);
                PlayIdle(currentQuad);
            }
            return;
        }
        else
        {
            idleTimer = 0f;
        }

        // 主軸判定
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            int sx = Mathf.Sign(dir.x) >= 0 ? +1 : -1;
            int sy = lastVertSign;      // 沿用上一個「上/下」
            lastHorizSign = sx;

            Quad q = ToQuad(sx, sy);
            ShowForWalk(q);
            PlayWalk(q);
        }
        else
        {
            int sy = Mathf.Sign(dir.y) >= 0 ? +1 : -1;
            int sx = lastHorizSign;     // 沿用上一個「左/右」
            lastVertSign = sy;

            Quad q = ToQuad(sx, sy);
            ShowForWalk(q);
            PlayWalk(q);
        }
    }

    Quad ToQuad(int sx, int sy)
    {
        // sx: +1=右, -1=左 ; sy: +1=上, -1=下
        if (sy >= 0) return (sx >= 0) ? Quad.RU : Quad.LU;
        else return (sx >= 0) ? Quad.RD : Quad.LD;
    }

    // 行走時：上→modelUp；下→modelDownWalk
    void ShowForWalk(Quad q)
    {
        currentQuad = q;

        if (q == Quad.RU || q == Quad.LU)
        {
            ActivateOnly(modelUp);
            currentAnim = animUp;

            bool isLeft = (q == Quad.LU);
            ApplyFlip(flipTargetUp, isLeft);
        }
        else
        {
            ActivateOnly(modelDownWalk);
            currentAnim = animDownWalk;

            bool isLeft = (q == Quad.LD);
            ApplyFlip(flipTargetDownWalk, isLeft);
        }
    }

    // 待機時：上→modelUp；下→modelDownIdle（重點：與走路分開，避免參數互相影響）
    void ShowForIdle(Quad q)
    {
        currentQuad = q;

        if (q == Quad.RU || q == Quad.LU)
        {
            ActivateOnly(modelUp);
            currentAnim = animUp;

            bool isLeft = (q == Quad.LU);
            ApplyFlip(flipTargetUp, isLeft);
        }
        else
        {
            ActivateOnly(modelDownIdle);
            currentAnim = animDownIdle;

            bool isLeft = (q == Quad.LD);
            ApplyFlip(flipTargetDownIdle, isLeft);
        }
    }

    void PlayWalk(Quad q)
    {
        switch (q)
        {
            case Quad.RU: PlayOnce(walkRU); break;
            case Quad.LU: PlayOnce(walkLU); break;
            case Quad.RD: PlayOnce(walkRD); break;
            case Quad.LD: PlayOnce(walkLD); break;
        }
    }

    void PlayIdle(Quad q)
    {
        switch (q)
        {
            case Quad.RU: PlayOnce(idleRU); break;
            case Quad.LU: PlayOnce(idleLU); break;
            case Quad.RD: PlayOnce(idleRD); break;
            case Quad.LD: PlayOnce(idleLD); break;
        }
    }

    void PlayOnce(string stateName)
    {
        if (currentAnim == null) return;
        if (currentState == stateName) return;
        currentAnim.Play(stateName);
        currentState = stateName;
    }

    void ApplyFlip(Transform target, bool flip)
    {
        if (target == null) return;
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
        if (modelDownWalk != null) modelDownWalk.SetActive(go == modelDownWalk);
        if (modelUp != null) modelUp.SetActive(go == modelUp);
        if (modelDownIdle != null) modelDownIdle.SetActive(go == modelDownIdle);
    }

    // —— 輔助：安全讀取軸（未設置就當 0，不報錯）——
    float SafeGetAxis(string axisName)
    {
        try { return Input.GetAxis(axisName); }
        catch { return 0f; }
    }
}
