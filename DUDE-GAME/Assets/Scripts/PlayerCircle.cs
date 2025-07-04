using UnityEngine;
using DG.Tweening;
using System.Collections;

public class PlayerCircle : MonoBehaviour
{
    [Header("Scale Animation")]
    public Vector3 startScale = new Vector3(0.5f, 0.5f, 0.5f);
    public float scaleDuration = 1f;
    public Ease scaleEase = Ease.OutBack;

    [Header("Rotation Animation")]
    public Vector3 rotationAmount = new Vector3(0f, 0f, 360f); // e.g., rotate around Z
    public float rotationDuration = 2f;
    public Ease rotationEase = Ease.InOutSine;

    [Header("Options")]
    public bool playOnStart = true;
    public bool loopRotation = false;

    private Vector3 originalScale;

    [SerializeField] private float timeToDisable = 1f;

    void OnEnable()
    {
        SoundFXManager.instance.PlaySoundByName("playerCircle",transform,0.6f,1f);
        originalScale = transform.localScale;

        if (playOnStart)
        {
            AnimateScale();
            AnimateRotation();
            StartCoroutine(DisableAfterTime());
        }
    }
    private IEnumerator DisableAfterTime()
    {
        yield return new WaitForSeconds(timeToDisable);
        gameObject.SetActive(false);
    }
    
    public void AnimateScale()
    {
        transform.localScale = startScale;
        transform.DOScale(originalScale, scaleDuration).SetEase(scaleEase);
    }

    public void AnimateRotation()
    {
        var rotationTween = transform.DORotate(transform.eulerAngles + rotationAmount, rotationDuration, RotateMode.FastBeyond360)
                                     .SetEase(rotationEase);

        if (loopRotation)
        {
            rotationTween.SetLoops(-1, LoopType.Restart);
        }
    }
}
