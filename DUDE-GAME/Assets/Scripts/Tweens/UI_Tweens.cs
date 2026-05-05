using UnityEngine;
using DG.Tweening;

public class UI_Tweens : MonoBehaviour
{
    private Tween currentTween;

    public void PlayPop(
        Vector3 startScale,
        Vector3 popScale,
        Vector3 endScale,
        float duration,
        float popRatio,
        Ease popEase,
        Ease settleEase,
        System.Action onComplete = null)
    {
        KillCurrentTween();
        transform.localScale = startScale;

        float popTime = duration * popRatio;
        float settleTime = duration - popTime;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(popScale, popTime).SetEase(popEase));
        seq.Append(transform.DOScale(endScale, settleTime).SetEase(settleEase));
        if (onComplete != null) seq.OnComplete(() => onComplete());

        currentTween = seq;
    }

    public void PlayMove(
        Vector3 startPos,
        Vector3 endPos,
        float duration,
        Ease ease,
        System.Action onComplete = null)
    {
        KillCurrentTween();
        transform.localPosition = startPos;

        currentTween = transform.DOLocalMove(endPos, duration).SetEase(ease);
        if (onComplete != null) currentTween.OnComplete(() => onComplete());
    }

    public void PlayFade(
        float startAlpha,
        float endAlpha,
        float duration,
        Ease ease,
        System.Action onComplete = null)
    {
        KillCurrentTween();
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = startAlpha;

        currentTween = cg.DOFade(endAlpha, duration).SetEase(ease);
        if (onComplete != null) currentTween.OnComplete(() => onComplete());
    }

    public void PlayRotate(
        Vector3 startRot,
        Vector3 endRot,
        float duration,
        Ease ease,
        System.Action onComplete = null)
    {
        KillCurrentTween();
        transform.localEulerAngles = startRot;

        currentTween = transform.DOLocalRotate(endRot, duration).SetEase(ease);
        if (onComplete != null) currentTween.OnComplete(() => onComplete());
    }

    public void KillCurrentTween()
    {
        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();
    }

    public void PlayFloatEffect(
     float moveAmount = 10f,
     float moveDuration = 2f,
     float rotateAmount = 5f,
     float scaleAmount = 0.05f,
     float scaleDuration = 2f)
    {
        KillCurrentTween();

        Vector3 originalPos = transform.localPosition;
        Vector3 originalRot = transform.localEulerAngles;
        Vector3 originalScale = transform.localScale;

        // Use separate tweens and store them in a parent sequence
        Sequence floatSequence = DOTween.Sequence();

        // X movement (left and right)
        Tween moveXTween = transform.DOLocalMoveX(originalPos.x + moveAmount, moveDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Y movement (up and down)
        Tween moveYTween = transform.DOLocalMoveY(originalPos.y + moveAmount, moveDuration * 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Rotation (tilting)
        Tween rotateTween = transform.DOLocalRotate(originalRot + new Vector3(0f, 0f, rotateAmount), moveDuration * 1.2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Scale
        Tween scaleTween = transform.DOScale(originalScale * (1f + scaleAmount), scaleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Combine all into a single sequence to keep track of them
        floatSequence.Join(moveXTween);
        floatSequence.Join(moveYTween);
        floatSequence.Join(rotateTween);
        floatSequence.Join(scaleTween);

        currentTween = floatSequence;
    }
    public void PlayFloatEffectSimple(
     float moveAmount = 10f,
     float moveDuration = 2f,
     float moveAmountVariance = 3f,
     float durationVariance = 0.5f)
    {
        KillCurrentTween();

        Vector3 originalPos = transform.localPosition;

        // Apply slight randomness to amount and duration
        float xAmount = moveAmount + Random.Range(-moveAmountVariance, moveAmountVariance);
        float yAmount = moveAmount + Random.Range(-moveAmountVariance, moveAmountVariance);

        float xDuration = moveDuration + Random.Range(-durationVariance, durationVariance);
        float yDuration = moveDuration * 0.8f + Random.Range(-durationVariance, durationVariance);

        Sequence floatSequence = DOTween.Sequence();

        Tween moveXTween = transform.DOLocalMoveX(originalPos.x + xAmount, xDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        Tween moveYTween = transform.DOLocalMoveY(originalPos.y + yAmount, yDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        floatSequence.Join(moveXTween);
        floatSequence.Join(moveYTween);

        currentTween = floatSequence;
    }
    public void PlayScale(
    Vector3 startScale,
    Vector3 endScale,
    float duration,
    Ease ease,
    System.Action onComplete = null)
    {
        KillCurrentTween();
        transform.localScale = startScale;

        currentTween = transform.DOScale(endScale, duration).SetEase(ease);
        if (onComplete != null) currentTween.OnComplete(() => onComplete());
    }





}