using UnityEngine;

public class SystemCanvas : MonoBehaviour
{
    private void Awake()
    {
        // 如果已經有一個 SystemCanvas，就刪掉新生的這個
        if (FindObjectsOfType<SystemCanvas>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
}
