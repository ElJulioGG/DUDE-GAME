using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using DG.Tweening;

public class GrayscaleToColor : MonoBehaviour
{
    [Header("Material con el shader Grayscale")]
    public Material grayscaleMaterial;

    [Header("Duración de la animación en segundos")]
    public float transitionDuration = 2f;


    private float currentAmount = 0f;


    public CameraShake camera;
    void Start()
    {
        if (grayscaleMaterial == null)
        {
            Debug.LogError("No se asignó el material al script.");
            return;
        }
        grayscaleMaterial.SetFloat("_LerpAmount", 0f);

    }
    void Update()
    {
        // Comprobar si la tecla "A" se presiona
        if (Input.GetKeyDown(KeyCode.A))
        {
            OnAnimate();
            StartCoroutine(camera.Shake(0.2f, 0.1f));
        }
    }

    public void OnAnimate()
    {
        // Comenzar en escala de grises


        // Iniciar animación hacia color
        DOTween.To(() => currentAmount, x =>
        {
            currentAmount = x;
            grayscaleMaterial.SetFloat("_LerpAmount", x);
        },
        1f, transitionDuration)
        .SetEase(Ease.InOutSine);
    }
}
