using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    public GameObject saveManagerPrefab; // 拖一個空物件上有 SaveManager 的 Prefab

    private void Awake()
    {
        if (SaveManager.Instance == null && saveManagerPrefab != null)
        {
            var go = Instantiate(saveManagerPrefab);
            go.name = "SaveManager";
        }
        DontDestroyOnLoad(gameObject);
    }
}
