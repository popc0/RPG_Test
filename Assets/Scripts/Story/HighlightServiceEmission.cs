using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-9000)] // 在 SystemCanvas 之後但仍很早
public class HighlightServiceEmission : MonoBehaviour
{
    public static HighlightServiceEmission Instance { get; private set; }

    [Tooltip("目標亮度倍數（Emission 或 Tint）")]
    public float highlightIntensity = 2.0f;
    [Tooltip("淡入/淡出秒數")]
    public float fadeDuration = 0.25f;

    readonly Dictionary<Renderer, Coroutine> running = new();
    MaterialPropertyBlock mpb;  // ✅ Awake 初始化

    static readonly int ID_Emiss = Shader.PropertyToID("_EmissionColor");
    static readonly int ID_Base = Shader.PropertyToID("_BaseColor");
    static readonly int ID_Color = Shader.PropertyToID("_Color");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // ❌ 不要 DontDestroyOnLoad；由 SystemCanvas（父物件）負責跨場景
        mpb = new MaterialPropertyBlock(); // ✅ 正確的初始化時機
    }

    public void ClearAll()
    {
        foreach (var r in new List<Renderer>(running.Keys))
            SetRendererHighlight(r, false, fadeDuration);
    }

    public void SetHighlighted(StoryActor actor, bool on)
    {
        if (actor == null || actor.renderers == null) return;
        foreach (var r in actor.renderers) SetRendererHighlight(r, on, fadeDuration);
    }

    public void SetHighlightedMany(IEnumerable<StoryActor> actors, bool on)
    {
        if (actors == null) return;
        foreach (var a in actors) SetHighlighted(a, on);
    }

    void SetRendererHighlight(Renderer r, bool on, float dur)
    {
        if (r == null) return;
        if (running.TryGetValue(r, out var co) && co != null) StopCoroutine(co);
        running[r] = StartCoroutine(FadeRenderer(r, on, dur));
    }

    IEnumerator FadeRenderer(Renderer r, bool on, float dur)
    {
        r.GetPropertyBlock(mpb);

        bool hasEmiss = r.sharedMaterial != null && r.sharedMaterial.HasProperty(ID_Emiss);

        // 取原始底色（退路：沒有 Emission 時用加亮）
        Color baseOrig = Color.white;
        if (r.sharedMaterial != null)
        {
            if (r.sharedMaterial.HasProperty(ID_Base)) baseOrig = r.sharedMaterial.GetColor(ID_Base);
            else if (r.sharedMaterial.HasProperty(ID_Color)) baseOrig = r.sharedMaterial.GetColor(ID_Color);
        }

        Color fromE = hasEmiss ? mpb.GetColor(ID_Emiss) : Color.black;
        Color toE = on ? Color.white * highlightIntensity : Color.black;
        Color fromC = baseOrig;
        Color toC = on ? baseOrig * highlightIntensity : baseOrig;

        float t = 0f;
        dur = Mathf.Max(0.01f, dur);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = Mathf.SmoothStep(0, 1, t);

            if (hasEmiss)
            {
                mpb.SetColor(ID_Emiss, Color.Lerp(fromE, toE, k));
            }
            else
            {
                var v = Color.Lerp(fromC, toC, k);
                if (r.sharedMaterial.HasProperty(ID_Base)) mpb.SetColor(ID_Base, v);
                else mpb.SetColor(ID_Color, v);
            }
            r.SetPropertyBlock(mpb);
            yield return null;
        }

        running[r] = null;
    }
}
