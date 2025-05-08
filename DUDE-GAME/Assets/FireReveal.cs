using UnityEngine;
using DG.Tweening;

public class BlinkingAnimation : MonoBehaviour
{
    public Material blinkingMaterial;
    public float blinkSpeed = 2f;
    public float maxAlpha = 1f;
    public float minAlpha = 0.1f;
    public float movementSpeed = 0.5f;

    private bool isBlinking = false;
    private float time;

    private Vector3 originalScale;
    private Vector3 originalPosition;

    void Start()
    {
        if (blinkingMaterial == null)
        {
            Debug.LogError("Blinking Material is not assigned.");
            return;
        }

        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) // BOTÓN V
        {
            AnimateAndStartBlinking(); // Subir + escalar + parpadeo
        }

        if (Input.GetKeyDown(KeyCode.B)) // BOTÓN B
        {
            AnimateAndReturn(); // Subir + escalar + volver
        }

        if (isBlinking)
        {
            time += Time.deltaTime;
            blinkingMaterial.SetFloat("_BlinkSpeed", blinkSpeed);
            blinkingMaterial.SetFloat("_MaxAlpha", maxAlpha);
            blinkingMaterial.SetFloat("_MinAlpha", minAlpha);
            blinkingMaterial.SetFloat("_Time", time);
        }
    }

    // BOTÓN V
    public void AnimateAndStartBlinking()
    {
        isBlinking = true;

        Vector3 targetScale = originalScale * 1.5f;
        Vector3 targetPosition = originalPosition + new Vector3(0, 1f, 0);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(targetScale, movementSpeed).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOLocalMove(targetPosition, movementSpeed).SetEase(Ease.OutQuad));
    }

    // BOTÓN B
    public void AnimateAndReturn()
    {
        isBlinking = false;
        blinkingMaterial.SetFloat("_MinAlpha", 1f);

        Vector3 targetScale = originalScale * 1.5f;
        Vector3 targetPosition = originalPosition + new Vector3(0, 1f, 0);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(targetScale, movementSpeed).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOLocalMove(targetPosition, movementSpeed).SetEase(Ease.OutQuad));

        sequence.Append(transform.DOScale(originalScale, movementSpeed).SetEase(Ease.InQuad));
        sequence.Join(transform.DOLocalMove(originalPosition, movementSpeed).SetEase(Ease.InQuad));
    }
}
