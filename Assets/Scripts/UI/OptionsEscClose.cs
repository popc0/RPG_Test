using UnityEngine;

public class OptionsEscClose : MonoBehaviour
{
    public MainMenuController menu;
    void Update()
    {
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            menu.OnClickBackToMain();
    }
}
