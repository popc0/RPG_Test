using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    [Header("輸入選項")]
    public bool useDynamicTouchJoystick = false;
    public MonoBehaviour dynamicTouchJoystickBehaviour; // 指到場景上的 DynamicTouchJoystick
#if ENABLE_INPUT_SYSTEM
    public InputActionReference moveAction; // 指到你設定好的 InputAction (Vector2)
#endif

    // ====== 內部 ======
    private IMoveInputProvider inputProvider;
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

        // 1) 明確指定的 inputProviderBehaviour（最高優先）
        if (inputProviderBehaviour is IMoveInputProvider p)
        {
            inputProvider = p;
        }
        else
        {
            // 2) 自動依勾選建立提供者
            if (useDynamicTouchJoystick && dynamicTouchJoystickBehaviour != null)
            {
                inputProvider = new DynJoyProvider(dynamicTouchJoystickBehaviour);
            }
#if ENABLE_INPUT_SYSTEM
            else if (!useDynamicTouchJoystick && moveAction != null)
            {
                inputProvider = new InputActionProvider(moveAction);
            }
#endif
            else
            {
                // 3) 嘗試抓同物件上任何 IMoveInputProvider
                inputProvider = GetComponent<IMoveInputProvider>();
            }
        }
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (inputProvider is InputActionProvider ap) ap.Enable();
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (inputProvider is InputActionProvider ap) ap.Disable();
#endif
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

    // ====== 內部輸入提供者（避免新增檔案，直接放這隻） ======

#if ENABLE_INPUT_SYSTEM
    /// <summary>使用 Input System 的 InputActionReference（Vector2）。</summary>
    private class InputActionProvider : IMoveInputProvider
    {
        private readonly InputAction _action;
        public InputActionProvider(InputActionReference reference)
        {
            _action = reference != null ? reference.action : null;
        }
        public void Enable() { _action?.Enable(); }
        public void Disable() { _action?.Disable(); }
        public Vector2 ReadMove() => _action != null ? _action.ReadValue<Vector2>() : Vector2.zero;
    }
#endif

    /// <summary>用反射包一層 DynamicTouchJoystick，避免 API 名稱差異造成編譯錯。</summary>
    private class DynJoyProvider : IMoveInputProvider
    {
        private readonly object _joystick;
        private readonly System.Func<Vector2> _getter;

        public DynJoyProvider(MonoBehaviour joystickBehaviour)
        {
            _joystick = joystickBehaviour;
            _getter = BuildGetter(joystickBehaviour);
        }

        public Vector2 ReadMove() => _getter != null ? _getter() : Vector2.zero;

        private static System.Func<Vector2> BuildGetter(MonoBehaviour b)
        {
            if (b == null) return null;
            var t = b.GetType();

            // 常見 Vector2 屬性
            string[] props = { "Value", "Direction", "Input", "Delta", "Axis", "Current", "Vector" };
            foreach (var name in props)
            {
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (p != null && p.PropertyType == typeof(Vector2) && p.CanRead)
                    return () => (Vector2)p.GetValue(b, null);
            }

            // 常見 Vector2 方法
            string[] methods = { "Read", "GetValue", "GetDirection", "GetVector", "GetAxis" };
            foreach (var mname in methods)
            {
                var m = t.GetMethod(mname, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, System.Type.EmptyTypes, null);
                if (m != null && m.ReturnType == typeof(Vector2))
                    return () => (Vector2)m.Invoke(b, null);
            }

            // 找不到就回零，避免噴錯
            Debug.LogWarning($"[PlayerMovement] 無法在 {t.Name} 上找到可讀取 Vector2 的屬性/方法，請確認 DynamicTouchJoystick 暴露的 API。");
            return null;
        }
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
