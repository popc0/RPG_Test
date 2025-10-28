using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class PlayerMovement : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;
    [Tooltip("手把搖桿的圓形死區（額外保險，避免微抖）")]
    [Range(0f, 1f)] public float joystickDeadzone = 0.25f;

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

    // ====== 內部 ======
    private Rigidbody2D rb;
    private Animator animDownWalk, animUp, animDownIdle, currentAnim;
    private Vector2 input;
    private Vector2 lastPos, moveDelta;
    private float idleTimer = 0f;
    private string currentState = null;
    private int lastHorizSign = +1; // -1=左, +1=右
    private int lastVertSign = -1; // -1=下, +1=上

    private enum Facing { RU, LU, RD, LD }
    private Facing currentFacing = Facing.RD;

    private InputDevice boundPad = null; // Joystick 或 Gamepad

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

        TryBindPad();
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Joystick || device is Gamepad)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
                TryBindPad();
            else if (change == InputDeviceChange.Removed && device == boundPad)
                boundPad = null;
        }
    }

    void TryBindPad()
    {
        if (Joystick.current != null) { boundPad = Joystick.current; return; }
        if (Joystick.all.Count > 0) { boundPad = Joystick.all[0]; return; }
        if (Gamepad.current != null) { boundPad = Gamepad.current; return; }
        if (Gamepad.all.Count > 0) { boundPad = Gamepad.all[0]; return; }
        boundPad = null;
    }

    void Update()
    {
        // 1) 手把：Joystick（優先）或 Gamepad
        Vector2 stick = Vector2.zero;

        if (boundPad == null) TryBindPad();

        if (boundPad is Joystick js)
            stick = js.stick.ReadValue();
        else if (boundPad is Gamepad gp)
            stick = gp.leftStick.ReadValue();
        else
        {
            if (Joystick.current != null) stick = Joystick.current.stick.ReadValue();
            else if (Gamepad.current != null) stick = Gamepad.current.leftStick.ReadValue();
        }

        if (stick.magnitude < joystickDeadzone) stick = Vector2.zero;

        // 2) 鍵盤
        Vector2 k = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            int x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0)
                  - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
            int y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1 : 0)
                  - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);
            k = new Vector2(x, y);
            if (k != Vector2.zero) k.Normalize();
        }

        // 3) 手把優先，否則鍵盤
        input = (stick != Vector2.zero) ? stick : k;

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
