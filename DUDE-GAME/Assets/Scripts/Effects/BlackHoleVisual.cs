using UnityEngine;

// Va en el sprite del shader (tu "BlackHoleShader").
// Interpola las 8 propiedades del material desde su valor en el INSPECTOR
// (look del agujero pequeno, t=0) hasta los valores finales de abajo
// (look en maxima expansion, t=1), segun cuanto se ha expandido tu rangeTransform.
//
// No necesita su propio tween: 't' sale de la escala que TU animas con DOTween,
// asi que hereda tu Ease.OutExpo / Ease.InExpo y la transicion se ve fluida
// tanto al crecer como al encoger. Usa MaterialPropertyBlock (no instancia material).
[RequireComponent(typeof(Renderer))]
public class BlackHoleVisual : MonoBehaviour
{
    [Header("Sincronizacion con tu DOTween")]
    [Tooltip("El transform cuya localScale animas (el de DOScale en BlackHoleEntity)")]
    [SerializeField] private Transform scaleSource;
    [SerializeField] private float minScale = 1f;   // escala al spawnear (Vector3.one)
    [SerializeField] private float maxScale = 8f;   // = rangeMaxScale (DEBE coincidir con BlackHoleEntity.rangeMaxScale)

    [Header("Valores en MAXIMA expansion (preview de Claude)")]
    // Fuerza = fraccion del radio que se desplaza la lente (0.35 = 35% del radio). No subir mucho de 0.4.
    [SerializeField] private float endStrengthInner = 0.45f;
    [SerializeField] private float endSwirlInner    = -0.5f;
    [SerializeField] private float endStrengthOuter = 0.28f;
    [SerializeField] private float endSwirlOuter    = 2.0f;
    [SerializeField] private float endInnerRadius   = 0.58f;
    [SerializeField] private float endBlendWidth    = 0.40f;
    // Nucleo negro a maxima expansion. 0.66 con _CoreSoftness=0.16 deja negro solido
    // hasta ~0.5 del radio (la mitad), dejando una zona amplia de distorsion visible.
    [SerializeField] private float endCoreSize      = 0.66f;
    // Borde duro: la banda de distorsion debe ser OPACA, si no se diluye con la escena real.
    [SerializeField] private float endRimSoftness   = 0.12f;
    // El anillo de luz tambien gana intensidad al crecer (sutil: no debe tapar la distorsion).
    [SerializeField] private float endRingIntensity = 0.5f;

    // Valores de inicio: capturados del material (lo que pongas en el inspector)
    private float sStrI, sSwI, sStrO, sSwO, sInner, sBlend, sCore, sRim, sRing;

    // Valores ACTUALES (ya interpolados) que BlackHoleMergeField sube al shader
    // por agujero, para que dos agujeros en distinta etapa de crecimiento se
    // mezclen sin costuras. Mismo empaquetado que _BHParamsA/B/C del shader.
    public Vector4 ParamsA { get; private set; } // strengthInner, swirlInner, strengthOuter, swirlOuter
    public Vector4 ParamsB { get; private set; } // innerRadius, blendWidth, coreSize, rimSoftness
    public float CurrentRingIntensity { get; private set; }

    private Renderer _r;
    private MaterialPropertyBlock _mpb;
    private static readonly int StrI  = Shader.PropertyToID("_StrengthInner");
    private static readonly int SwI   = Shader.PropertyToID("_SwirlInner");
    private static readonly int StrO  = Shader.PropertyToID("_StrengthOuter");
    private static readonly int SwO   = Shader.PropertyToID("_SwirlOuter");
    private static readonly int Inner = Shader.PropertyToID("_InnerRadius");
    private static readonly int Blend = Shader.PropertyToID("_BlendWidth");
    private static readonly int Core  = Shader.PropertyToID("_CoreSize");
    private static readonly int Rim   = Shader.PropertyToID("_RimSoftness");
    private static readonly int Ring  = Shader.PropertyToID("_RingIntensity");

    void Awake()
    {
        _r = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        if (scaleSource == null) scaleSource = transform.parent;

        // Auto-anade el registrador del campo de fusion (metaballs) para que
        // agujeros cercanos se fundan en el shader, sin tener que tocar el prefab.
        if (GetComponent<BlackHoleMergeField>() == null)
            gameObject.AddComponent<BlackHoleMergeField>();

        var m = _r.sharedMaterial; // los valores del inspector = punto de inicio (pequeno)
        sStrI = m.GetFloat(StrI);  sSwI  = m.GetFloat(SwI);
        sStrO = m.GetFloat(StrO);  sSwO  = m.GetFloat(SwO);
        sInner= m.GetFloat(Inner); sBlend= m.GetFloat(Blend);
        sCore = m.GetFloat(Core);  sRim  = m.GetFloat(Rim);
        sRing = m.HasFloat(Ring) ? m.GetFloat(Ring) : 0.6f;

        // Publica los valores de inicio YA, para que el primer upload del campo
        // de fusion (que puede correr antes de nuestro primer LateUpdate) no suba ceros.
        ParamsA = new Vector4(sStrI, sSwI, sStrO, sSwO);
        ParamsB = new Vector4(sInner, sBlend, sCore, sRim);
        CurrentRingIntensity = sRing;
    }

    void LateUpdate()
    {
        if (scaleSource == null) return;
        float t = Mathf.InverseLerp(minScale, maxScale, scaleSource.localScale.x);

        float strI  = Mathf.Lerp(sStrI,  endStrengthInner, t);
        float swI   = Mathf.Lerp(sSwI,   endSwirlInner,    t);
        float strO  = Mathf.Lerp(sStrO,  endStrengthOuter, t);
        float swO   = Mathf.Lerp(sSwO,   endSwirlOuter,    t);
        float inner = Mathf.Lerp(sInner, endInnerRadius,   t);
        float blend = Mathf.Lerp(sBlend, endBlendWidth,    t);
        float core  = Mathf.Lerp(sCore,  endCoreSize,      t);
        float rim   = Mathf.Lerp(sRim,   endRimSoftness,   t);
        float ring  = Mathf.Lerp(sRing,  endRingIntensity, t);

        // Camino real: el shader lee estos valores POR AGUJERO desde los arrays
        // globales que sube BlackHoleMergeField.
        ParamsA = new Vector4(strI, swI, strO, swO);
        ParamsB = new Vector4(inner, blend, core, rim);
        CurrentRingIntensity = ring;

        // El MPB queda como respaldo para el modo fallback del shader
        // (_BlackHoleCount == 0, p. ej. sin BlackHoleMergeField en la escena).
        _r.GetPropertyBlock(_mpb);
        _mpb.SetFloat(StrI,  strI);
        _mpb.SetFloat(SwI,   swI);
        _mpb.SetFloat(StrO,  strO);
        _mpb.SetFloat(SwO,   swO);
        _mpb.SetFloat(Inner, inner);
        _mpb.SetFloat(Blend, blend);
        _mpb.SetFloat(Core,  core);
        _mpb.SetFloat(Rim,   rim);
        _mpb.SetFloat(Ring,  ring);
        _r.SetPropertyBlock(_mpb);
    }
}
