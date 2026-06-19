// ============================================================================
//  BlackHole2D_URP_RT.shader  (DOS ZONAS, lee un RenderTexture)
//  Version para URP 2D que SI deforma todo lo que esta detras (incluidos sprites
//  transparentes), porque no usa _CameraOpaqueTexture sino una textura global
//  _SceneTex que llena una segunda camara (ver BlackHoleSceneCapture.cs).
//
//  SETUP: ya NO necesitas "Opaque Texture". Necesitas la segunda camara + el
//  script de captura. Quad CUADRADO, pivote al centro, en una capa propia
//  (ej. "BlackHole") que la camara de captura EXCLUYA, para no muestrearse a si mismo.
//
//  ZONAS (de adentro hacia afuera):
//   1) Nucleo negro SOLIDO  (r < _CoreSize - _CoreSoftness)
//   2) Anillo de luz / foton (gaussiana centrada en r = _CoreSize)
//   3) Lente gravitatoria + remolino con aberracion cromatica (_CoreSize < r < 1)
//   4) Desvanecido del borde para mezclar con la escena (r -> 1)
// ============================================================================
Shader "Custom/BlackHole2D_URP_RT"
{
    Properties
    {
        [Header(Zona interna)]
        _StrengthInner ("  Fuerza interna", Range(0,1)) = 0.45
        _SwirlInner    ("  Remolino interno", Range(-12,12)) = 7

        [Header(Zona externa)]
        _StrengthOuter ("  Fuerza externa", Range(0,1)) = 0.15
        _SwirlOuter    ("  Remolino externo", Range(-12,12)) = 2
        _SwirlSpeed    ("  Velocidad giro animado", Range(-8,8)) = 1.5

        [Header(Forma)]
        _InnerRadius ("  Limite interna/externa", Range(0,1)) = 0.45
        _BlendWidth  ("  Suavidad del blend", Range(0.001,0.5)) = 0.12
        _RimSoftness ("  Suavidad del borde", Range(0.001,0.5)) = 0.1

        [Header(Nucleo negro)]
        _CoreSize     ("  Radio nucleo negro", Range(0,1)) = 0.5
        _CoreSoftness ("  Suavidad borde nucleo", Range(0.001,0.6)) = 0.16

        [Header(Anillo de luz)]
        [HDR] _RingColor ("  Color anillo", Color) = (0.65, 0.8, 1.0, 1)
        _RingWidth     ("  Grosor anillo", Range(0.001,0.5)) = 0.05
        _RingIntensity ("  Intensidad anillo", Range(0,4)) = 0.6

        [Header(Aberracion cromatica)]
        _ChromaticAberration ("  Aberracion cromatica", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Textura GLOBAL puesta por BlackHoleSceneCapture via Shader.SetGlobalTexture.
            // NO va en Properties para que se mantenga global (sirve a varios agujeros a la vez).
            TEXTURE2D(_SceneTex);
            SAMPLER(sampler_SceneTex);

            float _StrengthInner, _SwirlInner;
            float _StrengthOuter, _SwirlOuter, _SwirlSpeed;
            float _InnerRadius, _BlendWidth, _RimSoftness;
            float _CoreSize, _CoreSoftness;
            float _RingWidth, _RingIntensity, _ChromaticAberration;
            float4 _RingColor;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings  { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float4 screenPos : TEXCOORD1; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 p = IN.uv - 0.5;
                float  r = length(p) * 2.0;            // 0 en el centro, 1 en el borde del circulo inscrito
                if (r > 1.0) return half4(0,0,0,0);    // recorta las esquinas -> efecto circular

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                // Desvanecido del borde: controla el alpha y atenua la distorsion hacia afuera,
                // para que el efecto se funda con la escena sin un corte duro.
                float fall = 1.0 - smoothstep(1.0 - _RimSoftness, 1.0, r);

                // --- Lente gravitatoria + remolino (dos zonas mezcladas) ---
                float zone     = smoothstep(_InnerRadius - _BlendWidth, _InnerRadius + _BlendWidth, r);
                float strength = lerp(_StrengthInner, _StrengthOuter, zone);
                float swirl    = lerp(_SwirlInner,    _SwirlOuter,    zone);

                // Perfil de la lente: 1 justo al borde del nucleo y decae suave hasta el borde
                // exterior, cubriendo TODA la banda (no se corta antes como 'fall').
                float lens = 1.0 - smoothstep(_CoreSize, 1.0, r);

                float2 dir = normalize(p + 1e-5);
                // Remolino: base estatica (swirl) + giro ANIMADO en el tiempo.
                // '* zone' concentra la animacion en la seccion EXTERIOR; '* lens' la apaga al borde.
                float animSwirl = _SwirlSpeed * _Time.y * zone;
                float angle = (swirl + animSwirl) * lens;
                float s, c;
                sincos(angle, s, c);
                float2 swirled = float2(dir.x * c - dir.y * s, dir.x * s + dir.y * c);

                // Desplazamiento como FRACCION del radio del agujero (tira al centro + remolino).
                float pull = strength * lens;
                float2 offsetN = (-dir + (swirled - dir)) * pull;

                // Convierte esa fraccion al tamano REAL del agujero en pantalla.
                // fwidth(r) = cuanto cambia r por pixel  ->  1/fwidth(r) = radio del agujero en px.
                // Asi la lente escala con el tamano del agujero y nunca muestrea fuera de rango
                // (que era lo que pasaba con un offset fijo + fuerzas altas).
                float quadRadiusPx = 1.0 / max(fwidth(r), 1e-5);
                float2 offset = (offsetN * quadRadiusPx) / _ScreenParams.xy; // px -> UV (maneja aspecto)

                // --- Aberracion cromatica: cada canal se desvia distinto al cruzar la lente.
                //     Crece con 'pull', asi que al inicio (distorsion pequena) es casi nula.
                float ca = _ChromaticAberration * pull;
                half R = SAMPLE_TEXTURE2D(_SceneTex, sampler_SceneTex, screenUV + offset * (1.0 + ca)).r;
                half G = SAMPLE_TEXTURE2D(_SceneTex, sampler_SceneTex, screenUV + offset).g;
                half B = SAMPLE_TEXTURE2D(_SceneTex, sampler_SceneTex, screenUV + offset * (1.0 - ca)).b;
                half3 scene = half3(R, G, B);

                // --- Nucleo negro SOLIDO con borde suave ---
                // core = 1 dentro del radio negro, baja a 0 en una banda de ancho _CoreSoftness.
                float core = 1.0 - smoothstep(_CoreSize - _CoreSoftness, _CoreSize, r);
                scene *= (1.0 - core);

                // --- Anillo de luz (foton / acrecion) justo en el borde del nucleo ---
                // Gaussiana centrada en r = _CoreSize. Enmascarado por (1-core) para que no
                // invada el negro puro, y por 'fall' para que muera en el borde exterior.
                float ringD = (r - _CoreSize) / max(_RingWidth, 1e-3);
                float ring  = exp(-ringD * ringD) * _RingIntensity * fall * (1.0 - core);
                scene += _RingColor.rgb * ring;

                return half4(scene, fall);
            }
            ENDHLSL
        }
    }
}
