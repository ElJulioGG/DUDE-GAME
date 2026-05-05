using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DrunkFlickerOverlay : MonoBehaviour
{
    private Image img;
    private bool enabledFlicker = false;
    private float maxAlpha = 0.08f;
    private float freq = 9f;

    void Awake()
    {
        img = GetComponent<Image>();
        var c = img.color; c.a = 0f; img.color = c;
    }

    public void SetEnabled(bool on, float maxAlpha = 0.08f, float frequency = 9f)
    {
        enabledFlicker = on;
        this.maxAlpha = Mathf.Max(0f, maxAlpha);
        this.freq = Mathf.Max(0.1f, frequency);
        if (!on) FadeTo(0f, 0.15f);
    }

    void Update()
    {
        if (!enabledFlicker) return;

        // Perlin para parpadeo orgánico (0..1), normalizado
        float n = Mathf.PerlinNoise(Time.time * freq, 0.37f);
        float a = Mathf.Clamp01((n - 0.4f) * (1f / 0.6f)) * maxAlpha; // recorte bajo para micro destellos

        var c = img.color; c.a = Mathf.Lerp(c.a, a, 0.2f);
        img.color = c;
    }

    private void FadeTo(float alpha, float time)
    {
        // mini tween sin DOTween
        StopAllCoroutines();
        StartCoroutine(FadeCR(alpha, time));
    }

    private System.Collections.IEnumerator FadeCR(float alpha, float time)
    {
        float t = 0f;
        Color start = img.color;
        Color end = start; end.a = alpha;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, time);
            img.color = Color.Lerp(start, end, t);
            yield return null;
        }
        img.color = end;
    }
}
