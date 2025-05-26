using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Importante para usar DoTween

public class FadeTranDT : MonoBehaviour
{
    [Header("Referencia a la imagen UI")]
    public Image uiImage;

    [Header("Duraci�n del fade")]
    public float fadeDuration = 1f;

    [Header("GameObject a desactivar")]
    public GameObject image;

    public GameObject volumen;
    public GameObject music;
    public GameObject particles;
    void Start()
    {
        // Fade hacia transparencia (0 = invisible)
        FadeOut();
        volumen.SetActive(false);
        music.SetActive(false);
        particles.SetActive(false);
    }

    public void FadeOut()
    {
        uiImage.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            image.SetActive(false);
        });
        volumen.SetActive(true);
        music.SetActive(true);
        particles.SetActive(true);

    }

    public void FadeIn()
    {
        image.SetActive(true); // Aseg�rate de activarlo antes del fade in
        uiImage.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            image.SetActive(false); // Tambi�n puedes cambiar esto si no quieres desactivarlo tras el fade in
        });
    }
}
