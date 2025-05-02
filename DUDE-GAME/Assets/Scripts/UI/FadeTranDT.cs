using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Importante para usar DoTween

public class FadeTranDT : MonoBehaviour
{
    [Header("Referencia a la imagen UI")]
    public Image uiImage;

    [Header("Duración del fade")]
    public float fadeDuration = 1f;

    void Start()
    {
        // Fade hacia transparencia (0 = invisible)
        FadeOut();

        // Si quieres hacer un fade in después de unos segundos:
        // Invoke("FadeIn", 3f);
    }

    public void FadeOut()
    {
        uiImage.DOFade(0f, fadeDuration); // Desaparece
    }

    public void FadeIn()
    {
        uiImage.DOFade(1f, fadeDuration); // Aparece
    }
}
