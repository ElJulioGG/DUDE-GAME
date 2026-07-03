using System.Collections.Generic;
using UnityEngine;

// Registra este agujero en el array global _BlackHoles que el shader
// BlackHole2D_URP_RT recorre para FUSIONAR agujeros cercanos (metaballs).
// Va en el MISMO objeto que el Renderer del shader; BlackHoleVisual lo
// auto-anade en Awake, asi que no hace falta tocar el prefab.
//
// Ademas de posicion + radio, sube los parametros ANIMADOS de cada agujero
// (los que BlackHoleVisual interpola con el crecimiento). El shader los mezcla
// por cercania: asi dos agujeros de distinto tamano/etapa se funden sin
// costuras, en vez de que el quad que dibuja imponga sus parametros al otro.
//
// El radio se saca de renderer.bounds (mitad del ancho en mundo), asi
// funciona igual con SpriteRenderer (cualquier PPU) o con un quad de malla,
// y sigue solo la escala que anima tu DOTween.
[RequireComponent(typeof(Renderer))]
public class BlackHoleMergeField : MonoBehaviour
{
    // DEBE coincidir con MAX_BLACKHOLES del shader.
    const int MaxHoles = 16;

    static readonly List<BlackHoleMergeField> Holes = new();
    static readonly Vector4[] Data    = new Vector4[MaxHoles];
    static readonly Vector4[] ParamsA = new Vector4[MaxHoles];
    static readonly Vector4[] ParamsB = new Vector4[MaxHoles];
    static readonly Vector4[] ParamsC = new Vector4[MaxHoles];
    static readonly int HolesID   = Shader.PropertyToID("_BlackHoles");
    static readonly int CountID   = Shader.PropertyToID("_BlackHoleCount");
    static readonly int ParamsAID = Shader.PropertyToID("_BHParamsA");
    static readonly int ParamsBID = Shader.PropertyToID("_BHParamsB");
    static readonly int ParamsCID = Shader.PropertyToID("_BHParamsC");

    private Renderer _r;
    private BlackHoleVisual _visual; // fuente de los parametros animados (puede faltar)

    // Respaldo para agujeros SIN BlackHoleVisual: parametros fijos del material.
    private Vector4 _staticA, _staticB;
    private float _staticRing;

    void OnEnable()
    {
        _r = GetComponent<Renderer>();
        _visual = GetComponent<BlackHoleVisual>();

        if (_visual == null)
        {
            var m = _r.sharedMaterial;
            _staticA = new Vector4(m.GetFloat("_StrengthInner"), m.GetFloat("_SwirlInner"),
                                   m.GetFloat("_StrengthOuter"), m.GetFloat("_SwirlOuter"));
            _staticB = new Vector4(m.GetFloat("_InnerRadius"), m.GetFloat("_BlendWidth"),
                                   m.GetFloat("_CoreSize"), m.GetFloat("_RimSoftness"));
            _staticRing = m.HasFloat("_RingIntensity") ? m.GetFloat("_RingIntensity") : 0.6f;
        }

        Holes.Add(this);
        Upload();
    }

    void OnDisable()
    {
        Holes.Remove(this);
        Upload(); // baja el contador ya, para no dejar un agujero fantasma un frame
    }

    // Solo el primero de la lista sube los datos de TODOS, una vez por frame.
    // LateUpdate: despues de que DOTween (Update) haya aplicado la escala.
    void LateUpdate()
    {
        if (Holes[0] == this) Upload();
    }

    static void Upload()
    {
        int n = 0;
        for (int i = 0; i < Holes.Count && n < MaxHoles; i++)
        {
            var h = Holes[i];
            if (h == null || h._r == null) continue;
            var b = h._r.bounds;
            Data[n] = new Vector4(b.center.x, b.center.y, Mathf.Max(b.extents.x, 1e-4f), 0f);

            if (h._visual != null)
            {
                ParamsA[n] = h._visual.ParamsA;
                ParamsB[n] = h._visual.ParamsB;
                ParamsC[n] = new Vector4(h._visual.CurrentRingIntensity, 0f, 0f, 0f);
            }
            else
            {
                ParamsA[n] = h._staticA;
                ParamsB[n] = h._staticB;
                ParamsC[n] = new Vector4(h._staticRing, 0f, 0f, 0f);
            }
            n++;
        }
        // Siempre el array COMPLETO: Unity fija el tamano del array global con el
        // primer set y rechaza tamanos distintos despues.
        Shader.SetGlobalVectorArray(HolesID,   Data);
        Shader.SetGlobalVectorArray(ParamsAID, ParamsA);
        Shader.SetGlobalVectorArray(ParamsBID, ParamsB);
        Shader.SetGlobalVectorArray(ParamsCID, ParamsC);
        Shader.SetGlobalFloat(CountID, n);
    }
}
