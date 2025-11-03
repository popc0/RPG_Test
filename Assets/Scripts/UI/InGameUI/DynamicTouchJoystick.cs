using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
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
    [SerializeField] private bool allowMouseInEditor = true;
    [SerializeField] private int useFingerIndex = 0;

    private bool active;
    private Vector2 startPosCanvas;
    private Vector2 output;

    // ★ 修正：用 touchId 追蹤手指（null 表示目前未綁定）
    private int? currentTouchId = null;

    private PointerEventData ped;
    private readonly List<RaycastResult> rayResults = new List<RaycastResult>();

    void Reset()
    {
        canvas = GetComponentInParent<Canvas>();
        raycaster = canvas ? canvas.GetComponent<GraphicRaycaster>() : null;
        eventSystem = EventSystem.current;
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
        EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
        TouchSimulation.Enable();
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        TouchSimulation.Disable();
#endif
        currentTouchId = null;
        active = false;
        output = Vector2.zero;
        SetVisible(false);
    }

    void Update()
    {
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
        // 沒觸控 → 結束搖桿
        if (touches.Count == 0)
        {
            EndJoystick();
            return;
        }

        // 嘗試找到我們正在追蹤的那根手指
        Touch? targetTouch = null;

        if (currentTouchId.HasValue)
        {
            // ★ 修正：用 touchId（int）比對
            int id = currentTouchId.Value;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].touchId == id)
                {
                    targetTouch = touches[i];
                    break;
                }
            }
        }
        else
        {
            // 尚未綁定 → 取指定 index 的手指（若不存在就取第一根）
            targetTouch = touches.Count > useFingerIndex ? touches[useFingerIndex] : touches[0];
        }

        if (targetTouch == null)
        {
            EndJoystick();
            return;
        }

        var touch = targetTouch.Value;

        // Begin：若在可點擊 UI 上就讓 UI 吃
        if (!active && touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            if (IsOverBlockingUI(touch.screenPosition)) return;
            BeginJoystick(touch);
        }

        // Move/Stationary：更新向量
        if (active && (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                       touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary))
        {
            UpdateJoystick(touch);
        }

        // End/Canceled：關閉
        if (active && (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                       touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled))
        {
            EndJoystick();
        }
    }

#if UNITY_EDITOR
    void HandleMouseInEditor()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 screen = mouse.position.ReadValue();
        bool down = mouse.leftButton.wasPressedThisFrame;
        bool held = mouse.leftButton.isPressed;
        bool up = mouse.leftButton.wasReleasedThisFrame;

        if (!active && down)
        {
            if (IsOverBlockingUI(screen)) return;
            Vector2 canvasPos; ScreenToCanvas(screen, out canvasPos);
            startPosCanvas = canvasPos;
            SetVisible(true);
            SetBasePosition(canvasPos);
            SetKnobPosition(canvasPos);
            output = Vector2.zero;
            active = true;
            currentTouchId = -1; // 編輯器滑鼠模式的臨時 id
        }
        else if (active && held)
        {
            Vector2 canvasPos; ScreenToCanvas(screen, out canvasPos);
            Vector2 delta = canvasPos - startPosCanvas;
            UpdateKnobAndOutput(delta);
        }
        else if (active && up)
        {
            EndJoystick();
        }
    }
#endif

    // ---------- Joystick life cycle ----------
    void BeginJoystick(Touch touch)
    {
        // ★ 修正：記住 touchId（int），不再用 TouchControl
        currentTouchId = touch.touchId;

        Vector2 canvasPos; ScreenToCanvas(touch.screenPosition, out canvasPos);
        startPosCanvas = canvasPos;

        SetVisible(true);
        SetBasePosition(canvasPos);
        SetKnobPosition(canvasPos);
        output = Vector2.zero;
        active = true;
    }

    void UpdateJoystick(Touch touch)
    {
        Vector2 canvasPos; ScreenToCanvas(touch.screenPosition, out canvasPos);
        Vector2 delta = canvasPos - startPosCanvas;
        UpdateKnobAndOutput(delta);
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

        if (ped == null) ped = new PointerEventData(eventSystem);
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

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // 直接使用螢幕座標（UI 元素用 position）
            return;
        }
        else
        {
            var rt = canvas.transform as RectTransform;
            Camera cam = canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, cam, out canvasPos);
        }
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
