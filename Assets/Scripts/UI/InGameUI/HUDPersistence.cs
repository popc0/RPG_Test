using UnityEngine;

public class HUDPersistence : MonoBehaviour
{
    void Awake()
    {
        var sc = FindObjectOfType<SystemCanvas>();
        if (sc != null)
        {
            if (transform.parent != sc.transform)
                transform.SetParent(sc.transform, worldPositionStays: false);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
