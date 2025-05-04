using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Importante para usar DoTween

public class FadeTranDT : MonoBehaviour
{
    [Header("Referencia a la imagen UI")]
    public Image uiImage;

    [Header("Duración del fade")]
    public float fadeDuration = 1f;

    [Header("GameObject a desactivar")]
    public GameObject image;

    void Start()
    {
        // Fade hacia transparencia (0 = invisible)
        FadeOut();
    }

    public void FadeOut()
    {
        uiImage.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            image.SetActive(false);
        });
    }

    public void FadeIn()
    {
        image.SetActive(true); // Asegúrate de activarlo antes del fade in
        uiImage.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            image.SetActive(false); // También puedes cambiar esto si no quieres desactivarlo tras el fade in
        });
    }
}
