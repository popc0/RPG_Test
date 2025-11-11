using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class DynamicTouchJoystick : MonoBehaviour, IMoveInputProvider
{
    [Header("Canvas 與影響範圍")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;

    [Header("搖桿 UI")]
    [SerializeField] private RectTransform baseRect;
    [SerializeField] private RectTransform knobRect;
    [SerializeField] private float knobRange = 90f;
    [SerializeField, Range(0f, 1f)] private float deadZone = 0.06f;
    [SerializeField] private bool normalize = true;

    [Header("行為")]
    [Tooltip("只在 Editor 允許用滑鼠模擬觸控")]
    [SerializeField] private bool allowMouseInEditor = true;
    [Tooltip("綁定第幾根手指，0=第一根")]
    [SerializeField] private int useFingerIndex = 0;

    // 供 UnifiedInputSource 開關使用（避免停用 GameObject 仍殘留輸入）
    public void EnableJoystick(bool on)
    {
        enabled = on;
        if (!on) EndJoystick();
    }

    private bool active;
    private Vector2 startPosCanvas;
    private Vector2 output;            // [-1,1] Vec
    private int? currentTouchId = null;

    private PointerEventData ped;
    private readonly List<RaycastResult> rayResults = new();

    void Reset()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas) raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (!eventSystem) eventSystem = EventSystem.current;
    }

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!raycaster && canvas) raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (!eventSystem) eventSystem = EventSystem.current;
        SetVisible(false);
    }

    void OnEnable()
    {
        // 沒安裝 EnhancedTouch 也不會報錯
        EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
        TouchSimulation.Enable();
#endif
        output = Vector2.zero;
        currentTouchId = null;
        active = false;
        SetVisible(false);
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        TouchSimulation.Disable();
#endif
        EnhancedTouchSupport.Disable();
        EndJoystick();
    }

    void Update()
    {
        // 若少了必要組件就直接輸出零向量
        if (!canvas || (!raycaster && canvas) || eventSystem == null)
        {
            output = Vector2.zero;
            SetVisible(false);
            return;
        }

        var touches = Touch.activeTouches;

#if UNITY_EDITOR
        if (allowMouseInEditor && touches.Count == 0 && Mouse.current != null)
        {
            HandleMouseInEditor();
            return;
        }
#endif
        HandleTouches(touches);
    }

    void HandleTouches(ReadOnlyArray<Touch> touches)
    {
        if (touches.Count == 0) { EndJoystick(); return; }

        Touch? target = null;

        if (currentTouchId.HasValue)
        {
            int id = currentTouchId.Value;
            for (int i = 0; i < touches.Count; i++)
                if (touches[i].touchId == id) { target = touches[i]; break; }
        }
        else
        {
            target = touches.Count > useFingerIndex ? touches[useFingerIndex] : touches[0];
        }

        if (target == null) { EndJoystick(); return; }
        var t = target.Value;

        // Begin
        if (!active && t.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            if (IsOverBlockingUI(t.screenPosition)) return;
            BeginJoystick(t);
        }

        // Move/Stay
        if (active && (t.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                       t.phase == UnityEngine.InputSystem.TouchPhase.Stationary))
        {
            UpdateJoystick(t);
        }

        // End/Cancel
        if (active && (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                       t.phase == UnityEngine.InputSystem.TouchPhase.Canceled))
        {
            EndJoystick();
        }
    }

#if UNITY_EDITOR
    void HandleMouseInEditor()
    {
        var mouse = Mouse.current;
        Vector2 screen = mouse.position.ReadValue();
        bool down = mouse.leftButton.wasPressedThisFrame;
        bool held = mouse.leftButton.isPressed;
        bool up = mouse.leftButton.wasReleasedThisFrame;

        if (!active && down)
        {
            if (IsOverBlockingUI(screen)) return;
            ScreenToCanvas(screen, out startPosCanvas);
            SetVisible(true);
            SetBasePosition(startPosCanvas);
            SetKnobPosition(startPosCanvas);
            output = Vector2.zero;
            active = true;
            currentTouchId = -1; // 編輯器臨時 id
        }
        else if (active && held)
        {
            Vector2 canvasPos; ScreenToCanvas(screen, out canvasPos);
            UpdateKnobAndOutput(canvasPos - startPosCanvas);
        }
        else if (active && up) EndJoystick();
    }
#endif

    // ---------- Joystick life cycle ----------
    void BeginJoystick(Touch touch)
    {
        currentTouchId = touch.touchId;
        ScreenToCanvas(touch.screenPosition, out startPosCanvas);

        SetVisible(true);
        SetBasePosition(startPosCanvas);
        SetKnobPosition(startPosCanvas);
        output = Vector2.zero;
        active = true;
    }

    void UpdateJoystick(Touch touch)
    {
        Vector2 canvasPos; ScreenToCanvas(touch.screenPosition, out canvasPos);
        UpdateKnobAndOutput(canvasPos - startPosCanvas);
    }

    void EndJoystick()
    {
        currentTouchId = null;
        active = false;
        output = Vector2.zero;
        SetVisible(false);
    }

    // ---------- UI helpers ----------
    bool IsOverBlockingUI(Vector2 screenPos)
    {
        if (raycaster == null || eventSystem == null) return false;

        ped ??= new PointerEventData(eventSystem);
        ped.Reset();
        ped.position = screenPos;

        rayResults.Clear();
        raycaster.Raycast(ped, rayResults);

        for (int i = 0; i < rayResults.Count; i++)
        {
            var go = rayResults[i].gameObject;
            if (!go || !go.activeInHierarchy) continue;
            if (go.GetComponent<Selectable>() != null) return true;
            if (go.GetComponent<IJoystickBlocker>() != null) return true;
        }
        return false;
    }

    void ScreenToCanvas(Vector2 screen, out Vector2 canvasPos)
    {
        canvasPos = screen;
        if (!canvas) return;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return;

        var rt = canvas.transform as RectTransform;
        var cam = canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, cam, out canvasPos);
    }

    void SetBasePosition(Vector2 posCanvas)
    {
        if (!baseRect) return;
        if (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            baseRect.position = posCanvas;
        else
            baseRect.anchoredPosition = posCanvas;
    }

    void SetKnobPosition(Vector2 posCanvas)
    {
        if (!knobRect) return;
        if (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            knobRect.position = posCanvas;
        else
            knobRect.anchoredPosition = posCanvas;
    }

    void UpdateKnobAndOutput(Vector2 deltaCanvas)
    {
        float range = Mathf.Max(1f, knobRange);
        Vector2 clamped = Vector2.ClampMagnitude(deltaCanvas, range);

        if (knobRect)
        {
            if (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                knobRect.position = (Vector2)baseRect.position + clamped;
            else
                knobRect.anchoredPosition = baseRect.anchoredPosition + clamped;
        }

        Vector2 raw = clamped / range;
        float mag = raw.magnitude;

        if (mag < deadZone) { output = Vector2.zero; return; }
        output = normalize ? raw.normalized : raw;
    }

    void SetVisible(bool on)
    {
        if (baseRect) baseRect.gameObject.SetActive(on);
        if (knobRect) knobRect.gameObject.SetActive(on);
    }

    // ---------- IMoveInputProvider ----------
    public Vector2 ReadMove() => output;
}

public class JoystickBlocker : MonoBehaviour, IJoystickBlocker { }
public interface IJoystickBlocker { }
// 放在檔案最底部（不改其他東西）
public interface IMoveInputProvider
{
    Vector2 ReadMove();
}

