using UnityEngine;

// Va en el BlackHoleVortex (el particle system del viento), como hijo del
// black hole. Liga la POTENCIA del vortice al crecimiento del agujero, igual
// que BlackHoleVisual liga el shader:
//
//   - Los valores del PREFAB (los que tuneaste) = maxima expansion (t = 1).
//   - Al estar pequeno (t = 0), cada parametro baja a la fraccion configurada.
//   - 't' sale de la escala que TU animas con DOTween en rangeTransform, asi
//     hereda tu Ease.OutExpo / Ease.InExpo: sube al expandirse, baja al implosionar.
//
// NOTA: asume valores CONSTANTES en emision, radio y velocidades (como los crea
// CreateVortexParticlePrefab); si los cambias a curvas, este script los pisa.
[RequireComponent(typeof(ParticleSystem))]
public class BlackHoleVortexPower : MonoBehaviour
{
    [Header("Sincronizacion con tu DOTween (igual que BlackHoleVisual)")]
    [Tooltip("El transform cuya localScale animas (el rangeTransform de BlackHoleEntity)")]
    [SerializeField] private Transform scaleSource;
    [SerializeField] private float minScale = 1f;   // escala al spawnear
    [SerializeField] private float maxScale = 8f;   // DEBE coincidir con BlackHoleVisual/rangeMaxScale

    [Header("Fraccion del valor del prefab cuando esta PEQUENO (t = 0)")]
    [Range(0f, 1f)] [SerializeField] private float emissionAtMin = 0.1f;  // casi no absorbe viento
    [Range(0f, 1f)] [SerializeField] private float radiusAtMin   = 0.25f; // radio de captura chico
    [Range(0f, 1f)] [SerializeField] private float speedAtMin    = 0.5f;  // succion/giro debiles

    private ParticleSystem _ps;
    private bool _released;

    // Llamado por BlackHoleEntity cuando el agujero EMPIEZA a cerrarse (y antes
    // de cualquier Destroy): se suelta del agujero para sobrevivir a su Destroy,
    // deja de emitir, y las particulas vivas se siguen succionando hasta morir.
    // ParticleSelfDestruct limpia el objeto suelto al final.
    public void ReleaseAndFade()
    {
        if (_released) return;
        _released = true;

        transform.SetParent(null, true);
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        gameObject.AddComponent<ParticleSelfDestruct>();
        // OJO: LateUpdate sigue corriendo mientras el agujero implosiona, asi
        // el giro/succion de las particulas restantes se debilita con el (t baja).
    }

    // Valores del prefab = punto de MAXIMA expansion.
    private float _maxEmission, _maxRadius, _maxOrbital, _maxRadial;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();

        // Auto-conexion: si no asignaste Scale Source, busca el BlackHoleEntity
        // padre y usa SU rangeTransform (el que anima DOTween) y SU escala maxima.
        // OJO: transform.parent NO sirve de fallback aqui — el root del agujero
        // no escala nunca, por eso el vortice "no crecia".
        if (scaleSource == null)
        {
            var entity = GetComponentInParent<BlackHoleEntity>();
            if (entity != null)
            {
                scaleSource = entity.RangeTransform;
                maxScale = entity.RangeMaxScale;
            }
            else
            {
                Debug.LogWarning("[BlackHoleVortexPower] Sin Scale Source y sin BlackHoleEntity padre: el vortice no va a escalar.", this);
            }
        }

        _maxEmission = _ps.emission.rateOverTime.constant;
        _maxRadius   = _ps.shape.radius;
        _maxOrbital  = _ps.velocityOverLifetime.orbitalZ.constant;
        _maxRadial   = _ps.velocityOverLifetime.radial.constant;
    }

    void LateUpdate()
    {
        if (scaleSource == null) return;
        float t = Mathf.InverseLerp(minScale, maxScale, scaleSource.localScale.x);

        var emission = _ps.emission;
        emission.rateOverTime = _maxEmission * Mathf.Lerp(emissionAtMin, 1f, t);

        var shape = _ps.shape;
        shape.radius = _maxRadius * Mathf.Lerp(radiusAtMin, 1f, t);

        var vel = _ps.velocityOverLifetime;
        float speedK = Mathf.Lerp(speedAtMin, 1f, t);
        vel.orbitalZ = _maxOrbital * speedK;
        vel.radial   = _maxRadial * speedK;
    }
}
