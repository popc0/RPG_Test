using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;

    [Header("手把（舊 Input Manager 軸）")]
    [Tooltip("手把 X 軸名稱（在 Input Manager → Axes 建立），建議: JoyX")]
    [SerializeField] private string joystickAxisX = "JoyX";
    [Tooltip("手把 Y 軸名稱（在 Input Manager → Axes 建立），建議: JoyY")]
    [SerializeField] private string joystickAxisY = "JoyY";
    [Tooltip("手把 Y 軸是否反向（你的 ESP32 已做往上=正值，通常不用反）")]
    [SerializeField] private bool invertJoyY = false;
    [Tooltip("圓形死區（避免微抖），0.20~0.30 較穩")]
    [Range(0f, 1f)][SerializeField] private float analogDeadzone = 0.25f;

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
    private Animator animDownWalk, animUp, animDownIdle, currentAnim;

    // 最終用來移動/驅動畫面的輸入（鍵盤或手把）
    private Vector2 input;
    private Vector2 lastPos, moveDelta;

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
        // 1) 鍵盤輸入（保留原本邏輯）
        Vector2 kb = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // 2) 手把類比輸入（舊 Input Manager 軸）
        Vector2 joy = ReadJoystickAxes(joystickAxisX, joystickAxisY, invertJoyY);
        joy = ApplyCircularDeadzone(joy, analogDeadzone);   // 圓形死區 + 正規化

        // 3) 合併策略：有手把就用手把；沒有再用鍵盤
        input = (joy != Vector2.zero) ? joy : kb;

        // 避免斜向過快
        if (input.magnitude > 1f) input.Normalize();
        //Debug.Log(new Vector2(Input.GetAxisRaw("JoyX"), Input.GetAxisRaw("JoyY")));
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

    // ====== 動畫邏輯（原樣保留） ======
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

    // ====== 輔助：讀軸 + 圓形死區處理 ======
    static Vector2 ReadJoystickAxes(string ax, string ay, bool invertY)
    {
        float x = 0f, y = 0f;
        if (!string.IsNullOrEmpty(ax))
        {
            try { x = Input.GetAxisRaw(ax); } catch { }
        }
        if (!string.IsNullOrEmpty(ay))
        {
            try { y = Input.GetAxisRaw(ay); } catch { }
        }
        if (invertY) y = -y;
        return new Vector2(x, y);
    }

    static Vector2 ApplyCircularDeadzone(Vector2 v, float dead)
    {
        float m = v.magnitude;
        if (m <= dead) return Vector2.zero;

        // 把 [dead,1] 線性映到 [0,1]，並做平滑
        float t = Mathf.InverseLerp(dead, 1f, m);
        t = Mathf.SmoothStep(0f, 1f, t);
        return (v / m) * t;
    }
}
