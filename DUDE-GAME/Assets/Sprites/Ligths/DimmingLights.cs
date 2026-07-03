using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Flash de explosion: al aparecer, la Light2D de este objeto arranca en
// startIntensity y cae a 0. OutExpo = caida brusca al inicio con cola suave,
// como el destello real de una explosion.
[RequireComponent(typeof(Light2D))]
public class DimmingLights : MonoBehaviour
{
    [SerializeField] private float startIntensity = 1f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private Ease ease = Ease.OutExpo;

    private Light2D _light;
    private Tween _tween;

    void Start()
    {
        _light = GetComponent<Light2D>();
        _light.intensity = startIntensity;

        _tween = DOTween.To(() => _light.intensity, x => _light.intensity = x, 0f, duration)
                        .SetEase(ease);
    }

    void OnDestroy()
    {
        // Si la explosion se destruye antes de terminar (p. ej. limpieza de ronda),
        // el tween no debe quedar vivo apuntando a una luz destruida.
        _tween?.Kill();
    }
}
