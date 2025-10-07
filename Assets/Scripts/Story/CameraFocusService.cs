using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-9000)]
public class CameraFocusService : MonoBehaviour
{
    [Header("引用")]
    public Component virtualCamera;   // 若專案有 Cinemachine，拖一支 VCam 進來（可留空）
    public Transform mainCamera;      // 沒有 VCam 時使用；留空會自動抓 Camera.main

    [Header("參數")]
    public float moveDuration = 0.45f;

    object vcamFollowProp;
    Coroutine refreshCo;

    void Awake()
    {
        // ❌ 不要 DontDestroyOnLoad；跟著 SystemCanvas 一起跨場景
        if (virtualCamera != null)
        {
            var prop = virtualCamera.GetType().GetProperty("Follow");
            vcamFollowProp = prop; // 反射以避免硬依賴 Cinemachine
        }
        RefreshCamera(); // 初次抓主鏡頭
    }

    void OnEnable()
    {
        RefreshCamera();
        if (refreshCo == null) refreshCo = StartCoroutine(AutoRefreshCamera());
    }

    void OnDisable()
    {
        if (refreshCo != null) { StopCoroutine(refreshCo); refreshCo = null; }
    }

    IEnumerator AutoRefreshCamera()
    {
        while (true)
        {
            // 每秒檢查一次，場景切換時自動更新
            if (Camera.main != null && (mainCamera == null || mainCamera != Camera.main.transform))
                RefreshCamera();
            yield return new WaitForSeconds(1f);
        }
    }

    void RefreshCamera()
    {
        if (Camera.main != null)
            mainCamera = Camera.main.transform;
    }

    public void Focus(Transform target)
    {
        if (target == null) return;

        if (virtualCamera != null && vcamFollowProp != null)
        {
            (vcamFollowProp as System.Reflection.PropertyInfo)?.SetValue(virtualCamera, target, null);
        }
        else if (mainCamera != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveMainCam(target.position));
        }
    }

    IEnumerator MoveMainCam(Vector3 worldTarget)
    {
        Vector3 start = mainCamera.position;
        Vector3 goal = new Vector3(worldTarget.x, worldTarget.y, start.z);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, moveDuration);
            mainCamera.position = Vector3.Lerp(start, goal, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
    }

    public static bool IsVisibleByMainCamera(Transform t, Camera cam = null)
    {
        if (t == null) return false;
        cam = cam != null ? cam : Camera.main;
        if (cam == null) return true; // 沒相機就當作可見（避免卡住流程）
        Vector3 v = cam.WorldToViewportPoint(t.position);
        return v.z > 0 && v.x > 0 && v.x < 1 && v.y > 0 && v.y < 1;
    }
}
