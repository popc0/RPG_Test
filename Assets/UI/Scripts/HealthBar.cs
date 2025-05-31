using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class HealthBar : MonoBehaviour
{
    public Image healthFill;
    public Image manaFill;
    public CanvasGroup canvasGroup;

    public bool alwaysVisible = false;
    private Coroutine hideRoutine;

    private void Start()
    {
        if (!alwaysVisible) HideImmediate();
    }

    public void SetHealth(float value) // value: 0 ~ 1
    {
        healthFill.fillAmount = value;
    }

    public void SetMana(float value) // value: 0 ~ 1
    {
        if (manaFill != null)
            manaFill.fillAmount = value;
    }

    public void Show(float duration = 3f)
    {
        if (alwaysVisible) return;
        canvasGroup.alpha = 1;

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterSeconds(duration));
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideImmediate();
    }

    public void HideImmediate()
    {
        if (!alwaysVisible)
            canvasGroup.alpha = 0;
    }
}
