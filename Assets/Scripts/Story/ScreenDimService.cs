using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenDimService : MonoBehaviour
{
    public Image overlay;            // ¥þ¿Ã¹õ Image
    public float fadeDuration = 0.25f;

    Coroutine co;

    public void SetDim(bool on)
    {
        if (overlay == null) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Fade(on));
    }

    IEnumerator Fade(bool on)
    {
        float start = overlay.color.a;
        float target = on ? 0.6f : 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration);
            float a = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t));
            var c = overlay.color; c.a = a; overlay.color = c;
            yield return null;
        }
        co = null;
    }
}
