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

        var m = _r.sharedMaterial; // los valores del inspector = punto de inicio (pequeno)
        sStrI = m.GetFloat(StrI);  sSwI  = m.GetFloat(SwI);
        sStrO = m.GetFloat(StrO);  sSwO  = m.GetFloat(SwO);
        sInner= m.GetFloat(Inner); sBlend= m.GetFloat(Blend);
        sCore = m.GetFloat(Core);  sRim  = m.GetFloat(Rim);
        sRing = m.HasFloat(Ring) ? m.GetFloat(Ring) : 0.6f;
    }

    void LateUpdate()
    {
        if (scaleSource == null) return;
        float t = Mathf.InverseLerp(minScale, maxScale, scaleSource.localScale.x);

        _r.GetPropertyBlock(_mpb);
        _mpb.SetFloat(StrI,  Mathf.Lerp(sStrI,  endStrengthInner, t));
        _mpb.SetFloat(SwI,   Mathf.Lerp(sSwI,   endSwirlInner,    t));
        _mpb.SetFloat(StrO,  Mathf.Lerp(sStrO,  endStrengthOuter, t));
        _mpb.SetFloat(SwO,   Mathf.Lerp(sSwO,   endSwirlOuter,    t));
        _mpb.SetFloat(Inner, Mathf.Lerp(sInner, endInnerRadius,   t));
        _mpb.SetFloat(Blend, Mathf.Lerp(sBlend, endBlendWidth,    t));
        _mpb.SetFloat(Core,  Mathf.Lerp(sCore,  endCoreSize,      t));
        _mpb.SetFloat(Rim,   Mathf.Lerp(sRim,   endRimSoftness,   t));
        _mpb.SetFloat(Ring,  Mathf.Lerp(sRing,  endRingIntensity, t));
        _r.SetPropertyBlock(_mpb);
    }
}
