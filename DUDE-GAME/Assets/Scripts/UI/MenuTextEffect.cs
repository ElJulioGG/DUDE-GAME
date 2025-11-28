using UnityEngine;
using DG.Tweening;

public class MenuTextEffect : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float scaleMultiplier = 1.2f;
    [SerializeField] private float totalDuration = 0.2f;
    [SerializeField] private Ease scaleEaseOut = Ease.OutBack;
    [SerializeField] private Ease scaleEaseIn = Ease.InBack;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationAmount = 10f;
    [SerializeField] private Ease rotationEase = Ease.OutBack;

    private Tween activeTween;

    private Vector3 baseScale;
    private Vector3 baseRotation;

    void Awake()
    {
        baseScale = transform.localScale;
        baseRotation = transform.localEulerAngles;
    }

    public void PlayElasticEffect()
    {
        // Kill previous tween
        if (activeTween != null && activeTween.IsActive())
            activeTween.Kill();

        // Reset transform BEFORE starting new animation
        transform.localScale = baseScale;
        transform.localEulerAngles = baseRotation;

        Vector3 popScale = baseScale * scaleMultiplier;
        Vector3 rotRight = baseRotation + new Vector3(0, 0, -rotationAmount);
        Vector3 rotLeft = baseRotation + new Vector3(0, 0, rotationAmount);

        float half = totalDuration * 0.5f;
        float quarter = totalDuration * 0.25f;

        Sequence seq = DOTween.Sequence();

        // SCALE
        seq.Join(transform.DOScale(popScale, half).SetEase(scaleEaseOut));
        seq.Join(transform.DOScale(baseScale, half).SetEase(scaleEaseIn).SetDelay(half));

        // ROTATION
        seq.Join(transform.DOLocalRotate(rotRight, quarter).SetEase(rotationEase));
        seq.Join(transform.DOLocalRotate(baseRotation, quarter).SetEase(rotationEase).SetDelay(quarter));
        seq.Join(transform.DOLocalRotate(rotLeft, quarter).SetEase(rotationEase).SetDelay(half));
        seq.Join(transform.DOLocalRotate(baseRotation, quarter).SetEase(rotationEase).SetDelay(half + quarter));

        activeTween = seq;
    }
}
