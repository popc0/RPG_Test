using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveOnSceneChange : MonoBehaviour
{
    void Awake()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveNow();
    }
}
