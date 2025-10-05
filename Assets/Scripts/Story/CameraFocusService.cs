using System.Collections;
using UnityEngine;

public class CameraFocusService : MonoBehaviour
{
    [Header("引用")]
    public Component virtualCamera;   // 若有 Cinemachine，就拖這個
    public Transform mainCamera;      // 若沒有，就留空讓系統自動找

    [Header("參數")]
    public float moveDuration = 0.45f;

    object vcamFollowProp;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);  // ✅ 保留跨場景

        if (virtualCamera != null)
        {
            var prop = virtualCamera.GetType().GetProperty("Follow");
            vcamFollowProp = prop;
        }

        RefreshCamera(); // 初始抓一次
    }

    void OnEnable()
    {
        // 啟用時重新抓相機並持續監控
        RefreshCamera();
        StartCoroutine(AutoRefreshCamera());
    }

    IEnumerator AutoRefreshCamera()
    {
        while (true)
        {
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
        if (cam == null) return true; // 沒相機就當作可見
        Vector3 v = cam.WorldToViewportPoint(t.position);
        return v.z > 0 && v.x > 0 && v.x < 1 && v.y > 0 && v.y < 1;
    }
}
