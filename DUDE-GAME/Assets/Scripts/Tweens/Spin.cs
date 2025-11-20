using UnityEngine;
using DG.Tweening;

public class SpinWithDOTween : MonoBehaviour
{
    [Header("Spin Settings")]
    public Vector3 rotationAxis = new Vector3(0, 1, 0); // Default: Y-axis
    public float rotationAmount = 360f; // Degrees per tween
    public float duration = 2f; // Time in seconds per full rotation
    public bool isLocal = true; // Local or world space
    public LoopType loopType = LoopType.Restart; // Restart or Yoyo

    [Header("Control")]
    public bool autoStart = true;
    private Tween spinTween;

    void Start()
    {
        if (autoStart)
            StartSpin();
    }

    public void StartSpin()
    {
        StopSpin(); // In case it's already spinning

        if (isLocal)
        {
            spinTween = transform.DOLocalRotate(rotationAxis * rotationAmount, duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, loopType);
        }
        else
        {
            spinTween = transform.DORotate(rotationAxis * rotationAmount, duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, loopType);
        }
    }

    public void StopSpin()
    {
        if (spinTween != null && spinTween.IsActive())
            spinTween.Kill();
    }

    void OnDestroy()
    {
        StopSpin();
    }
}
