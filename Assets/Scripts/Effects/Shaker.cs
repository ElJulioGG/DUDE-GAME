using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class Shaker : MonoBehaviour
{
    [Header("Default Shake Settings")]
    public float duration = 0.3f;
    public float strength = 11f;
    public int vibrato = 200;
    public float randomness = 90f;
    public bool fadeOut = true;
    public bool useScaleInstead = false;

    private Vector3 originalScale;
    private Vector2 originalUIPosition;
    private Vector3 originalWorldPosition;

    private RectTransform rectTransform;
    private bool isUI = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        isUI = rectTransform != null;

        originalScale = transform.localScale;

        if (isUI)
            originalUIPosition = rectTransform.anchoredPosition;
        else
            originalWorldPosition = transform.localPosition;
    }

   
    public void Shake()
    {
        Shake(duration, strength, vibrato, randomness, fadeOut, useScaleInstead);
    }

    
    public void Shake(float duration, float strength, int vibrato = 10, float randomness = 90f, bool fadeOut = true, bool useScale = false)
{
    if (useScale)
    {
        transform.DOKill(true);
        originalScale = transform.localScale;
        transform.DOShakeScale(duration, strength * 0.01f, vibrato, randomness, fadeOut)
                 .SetUpdate(true);
    }
    else if (isUI)
    {
        rectTransform.DOKill(true);
        Vector2 currentPos = rectTransform.anchoredPosition;
        rectTransform.DOShakeAnchorPos(duration, strength, vibrato, randomness, fadeOut)
                     .SetUpdate(true)
                     .OnComplete(() => rectTransform.anchoredPosition = currentPos);
    }
    else
    {
        transform.DOKill(true);
        Vector3 currentPos = transform.localPosition;
        transform.DOShakePosition(duration, strength, vibrato, randomness, false, fadeOut)
                 .SetUpdate(true)
                 .OnComplete(() => transform.localPosition = currentPos);
    }
}


     public void SetParameters(float dur, float str, int vib, float rand, bool fade, bool scale)
    {
        duration = dur;
        strength = str;
        vibrato = vib;
        randomness = rand;
        fadeOut = fade;
        useScaleInstead = scale;
    }
}
