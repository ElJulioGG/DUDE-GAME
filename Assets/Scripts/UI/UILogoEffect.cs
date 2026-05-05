using UnityEngine;
using DG.Tweening;

public class UILogoEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float scaleAmount = 0.5f;
    [SerializeField] private float rotationAmount = 30f;
    [SerializeField] private float cycleDuration = 2f;
    [SerializeField] private Ease easeTypeRot = Ease.InOutSine;
    [SerializeField] private Ease easeTypeScale = Ease.InOutSine;

    private Vector3 originalScale;
    private Vector3 originalRot;
    private int direction = 1; // 1 = right, -1 = left
    private Sequence cycleSequence;

    void Start()
    {
        originalScale = transform.localScale;
        originalRot = transform.localEulerAngles;
        PlayCycle();
    }

    void OnDisable()
    {
        if (cycleSequence != null && cycleSequence.IsActive())
            cycleSequence.Kill();
    }

    void PlayCycle()
    {
        if (cycleSequence != null && cycleSequence.IsActive())
            cycleSequence.Kill();

        cycleSequence = DOTween.Sequence();

        Vector3 targetScale = originalScale * (1f + scaleAmount);
        Vector3 targetRot = originalRot + new Vector3(0f, 0f, rotationAmount * direction);

        float half = cycleDuration / 2f;

        // Scale up + rotate
        cycleSequence.Append(
            transform.DOScale(targetScale, half).SetEase(easeTypeRot)
        );
        cycleSequence.Join(
            transform.DOLocalRotate(targetRot, half).SetEase(easeTypeRot)
        );

        // Scale down + rotate back
        cycleSequence.Append(
            transform.DOScale(originalScale, half).SetEase(easeTypeScale)
        );
        cycleSequence.Join(
            transform.DOLocalRotate(originalRot, half).SetEase(easeTypeScale)
        );

        // When cycle finishes, flip rotation direction and repeat
        cycleSequence.OnComplete(() =>
        {
            direction *= -1;
            PlayCycle();
        });
    }
}
