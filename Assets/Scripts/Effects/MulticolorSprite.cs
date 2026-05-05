using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class RainbowSprite : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private float cycleDuration = 5f; // Time for one full rainbow cycle
    [SerializeField] private bool infiniteDuration = true;
    [SerializeField] private Ease easeType = Ease.Linear;
    [SerializeField] private bool playOnStart = true;

    private SpriteRenderer spriteRenderer;
    private Sequence colorSequence;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (playOnStart) StartRainbowEffect();
    }

    public void StartRainbowEffect()
    {
        StopRainbowEffect();

        colorSequence = DOTween.Sequence();

        // Create smooth color transitions through the rainbow spectrum
        colorSequence.Append(spriteRenderer.DOColor(GetRainbowColor(0f), 0f)); // Start color

        // Add key points in the color spectrum
        for (float t = 0; t < 1f; t += 0.1667f) // 6 segments for rainbow colors
        {
            colorSequence.Append(
                spriteRenderer.DOColor(GetRainbowColor(t), cycleDuration / 6f)
                    .SetEase(easeType)
            );
        }

        // Complete the loop
        colorSequence.Append(spriteRenderer.DOColor(GetRainbowColor(0f), cycleDuration / 6f));

        // Set looping
        colorSequence.SetLoops(infiniteDuration ? -1 : 1);
    }

    public void StopRainbowEffect()
    {
        if (colorSequence != null)
        {
            colorSequence.Kill();
            colorSequence = null;
        }
    }

    private Color GetRainbowColor(float t)
    {
        // Smooth rainbow color spectrum using HSV
        return Color.HSVToRGB(t, 1f, 1f);
    }

    void OnDestroy()
    {
        StopRainbowEffect();
    }

    void OnDisable()
    {
        StopRainbowEffect();
    }
}