using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class Obj_Tweens : MonoBehaviour
{
    private Tween moveTween;
    private Tween rotateTween;
    private Tween scaleTween;
    private Tween floatTween;

    // --- POP ---
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
        KillScaleTween();
        transform.localScale = startScale;

        float popTime = duration * popRatio;
        float settleTime = duration - popTime;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(popScale, popTime).SetEase(popEase));
        seq.Append(transform.DOScale(endScale, settleTime).SetEase(settleEase));
        if (onComplete != null) seq.OnComplete(() => onComplete());

        scaleTween = seq;
    }

    // --- MOVE ---
    public void PlayMove(Vector3 startPos, Vector3 endPos, float duration, Ease ease, System.Action onComplete = null)
    {
        KillMoveTween();
        transform.position = startPos;

        moveTween = transform.DOMove(endPos, duration).SetEase(ease);
        if (onComplete != null) moveTween.OnComplete(() => onComplete());
    }

    // --- ROTATE ---
    public void PlayRotate(Vector3 startRot, Vector3 endRot, float duration, Ease ease, bool fullRotation = false, System.Action onComplete = null)
    {
        KillRotateTween();
        transform.eulerAngles = startRot;

        rotateTween = transform.DORotate(
            endRot,
            duration,
            fullRotation ? RotateMode.FastBeyond360 : RotateMode.Fast
        ).SetEase(ease);

        if (onComplete != null) rotateTween.OnComplete(() => onComplete());
    }

    // --- SCALE ---
    public void PlayScale(Vector3 startScale, Vector3 endScale, float duration, Ease ease, System.Action onComplete = null)
    {
        KillScaleTween();
        transform.localScale = startScale;

        scaleTween = transform.DOScale(endScale, duration).SetEase(ease);
        if (onComplete != null) scaleTween.OnComplete(() => onComplete());
    }

    // --- FLOAT ---
    public void PlayFloatEffect(float moveAmount = 0.5f, float moveDuration = 2f, float rotateAmount = 15f, float scaleAmount = 0.05f, float scaleDuration = 2f)
    {
        KillFloatTween();

        Vector3 originalPos = transform.position;
        Vector3 originalRot = transform.eulerAngles;
        Vector3 originalScale = transform.localScale;

        Sequence seq = DOTween.Sequence();

        Tween moveY = transform.DOMoveY(originalPos.y + moveAmount, moveDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        Tween rotateTween = transform.DORotate(originalRot + new Vector3(0f, rotateAmount, 0f), moveDuration * 1.2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        Tween scaleTween = transform.DOScale(originalScale * (1f + scaleAmount), scaleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        seq.Join(moveY);
        seq.Join(rotateTween);
        seq.Join(scaleTween);

        floatTween = seq;
    }

    // --- KILL HELPERS ---
    public void KillMoveTween() { if (moveTween != null && moveTween.IsActive()) moveTween.Kill(); }
    public void KillRotateTween() { if (rotateTween != null && rotateTween.IsActive()) rotateTween.Kill(); }
    public void KillScaleTween() { if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill(); }
    public void KillFloatTween() { if (floatTween != null && floatTween.IsActive()) floatTween.Kill(); }

    public void KillAllTweens()
    {
        KillMoveTween();
        KillRotateTween();
        KillScaleTween();
        KillFloatTween();
    }

    private void OnDisable() => KillAllTweens();
}
