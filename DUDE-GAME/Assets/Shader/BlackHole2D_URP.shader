// ============================================================================
//  BlackHole2D_URP_RT.shader  (DOS ZONAS, lee un RenderTexture, MULTI-AGUJERO)
//  Version para URP 2D que SI deforma todo lo que esta detras (incluidos sprites
//  transparentes), porque no usa _CameraOpaqueTexture sino una textura global
//  _SceneTex que llena una segunda camara (ver BlackHoleSceneCapture.cs).
//
//  SETUP: ya NO necesitas "Opaque Texture". Necesitas la segunda camara + el
//  script de captura. Quad CUADRADO, pivote al centro, en una capa propia
//  (ej. "BlackHole") que la camara de captura EXCLUYA, para no muestrearse a si mismo.
//
//  FUSION (metaballs): el shader ya no calcula solo "su" agujero. Lee un array
//  global _BlackHoles (posicion + radio en mundo) que sube BlackHoleMergeField.cs
//  y combina las distancias con un smooth-min -> cuando dos agujeros se acercan,
//  sus nucleos/anillos/siluetas se FUNDEN en vez de dibujarse uno encima del otro.
//  El bloque Stencil evita el doble dibujado donde dos quads se traslapan: como
//  ambos quads calculan el MISMO campo combinado, da igual cual dibuje el pixel.
//
//  ZONAS (de adentro hacia afuera, sobre el campo combinado):
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

        [Header(Fusion de agujeros)]
        _MergeSmooth ("  Suavidad de fusion", Range(0.01, 1)) = 0.35

        [Header(Pixelado mosaico)]
        [Toggle] _PixelateOn ("  Activar pixelado", Float) = 1
        _PixelsPerUnit ("  Celdas por unidad de mundo", Range(1, 64)) = 16
        _AnimFPS       ("  FPS de animacion (0 = suave)", Range(0, 30)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            // Anti doble-dibujado en traslapes: el primer quad que pasa marca el
            // stencil con 200 y los siguientes fallan el test en esos pixeles.
            // Los pixeles descartados con 'discard' (fuera de la silueta) NO
            // escriben stencil, asi que no bloquean al otro agujero.
            Stencil
            {
                Ref 200
                Comp NotEqual
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Textura GLOBAL puesta por BlackHoleSceneCapture via Shader.SetGlobalTexture.
            // NO va en Properties para que se mantenga global (sirve a varios agujeros a la vez).
            TEXTURE2D(_SceneTex);
            SAMPLER(sampler_SceneTex);

            // Array GLOBAL subido por BlackHoleMergeField.cs una vez por frame.
            // xy = centro en mundo, z = radio en mundo, w = libre.
            // Si _BlackHoleCount == 0 (falta el script), cae al transform del propio quad.
            #define MAX_BLACKHOLES 16
            float4 _BlackHoles[MAX_BLACKHOLES];
            float  _BlackHoleCount;

            // Parametros ANIMADOS por agujero (BlackHoleVisual los interpola segun
            // su crecimiento y BlackHoleMergeField los sube). Sin esto, un pixel
            // del traslape usaria los parametros del quad que lo dibuja (via MPB),
            // y con agujeros de distinto tamano se ven costuras rectas y saltos.
            float4 _BHParamsA[MAX_BLACKHOLES]; // strengthInner, swirlInner, strengthOuter, swirlOuter
            float4 _BHParamsB[MAX_BLACKHOLES]; // innerRadius, blendWidth, coreSize, rimSoftness
            float4 _BHParamsC[MAX_BLACKHOLES]; // ringIntensity, libre, libre, libre

            float _StrengthInner, _SwirlInner;
            float _StrengthOuter, _SwirlOuter, _SwirlSpeed;
            float _InnerRadius, _BlendWidth, _RimSoftness;
            float _CoreSize, _CoreSoftness;
            float _RingWidth, _RingIntensity, _ChromaticAberration;
            float4 _RingColor;
            float _MergeSmooth;
            float _PixelateOn, _PixelsPerUnit, _AnimFPS;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings  { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            // Minimo suave (polinomial, Inigo Quilez): con k > 0 las superficies
            // de dos agujeros se funden en un "cuello" en vez de intersectarse.
            float smin (float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (b - a) / k);
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Unidades de mundo por pixel de pantalla. Derivadas ANTES de
                // cuantizar (despues de floor() valen 0 dentro de cada celda).
                float worldPerPx = max(length(ddx(IN.positionWS.xy)), 1e-6);

                float2 ws = IN.positionWS.xy;

                // --- Pixelado / mosaico en ESPACIO DE MUNDO ---
                // La rejilla es la misma para todos los agujeros (no depende del
                // quad), asi el mosaico es continuo cuando dos se fusionan.
                // Iguala _PixelsPerUnit al PPU de tus sprites para que las celdas
                // midan lo mismo que los pixeles de tu arte.
                if (_PixelateOn > 0.5)
                {
                    float ppu = max(_PixelsPerUnit, 0.01);
                    ws = (floor(ws * ppu) + 0.5) / ppu;
                }

                // _AnimFPS > 0 cuantiza el tiempo en pasos -> animacion "a saltos" retro.
                float t = _Time.y;
                if (_AnimFPS > 0.5) t = floor(t * _AnimFPS) / _AnimFPS;

                // Fallback sin BlackHoleMergeField: el propio quad como unico agujero
                // (centro y radio sacados de su matriz; asume quad de 1x1 escalado).
                float4x4 M = GetObjectToWorldMatrix();
                float4 selfHole = float4(M._m03, M._m13,
                                         0.5 * length(float3(M._m00, M._m10, M._m20)), 0);
                int count = (int)_BlackHoleCount;
                int n = max(count, 1);

                // --- Campo combinado + distorsion sumada + mezcla de parametros ---
                // rC: smooth-min de las distancias normalizadas a cada agujero.
                //     El nucleo, el anillo y la silueta se evaluan sobre rC, por eso
                //     se funden como metaballs.
                // offsetWorld: suma de los tirones de lente/remolino de cada agujero
                //     (en unidades de mundo), para que en el traslape ambos jalen.
                // coreSizeC/rimSoftC/ringIntC: parametros por agujero mezclados por
                //     cercania (peso 1/di^4) -> transicion CONTINUA entre agujeros de
                //     distinto tamano/etapa, sin costuras en los bordes de los quads.
                float  rC = 1e5;
                float2 offsetWorld = 0;
                float  pullMax = 0;
                float  wSum = 0, coreSizeC = 0, rimSoftC = 0, ringIntC = 0;

                [loop] for (int i = 0; i < n; i++)
                {
                    float4 h, pa, pb;
                    float  ringInt;
                    if (count == 0) // fallback: sin registrador, parametros del material
                    {
                        h  = selfHole;
                        pa = float4(_StrengthInner, _SwirlInner, _StrengthOuter, _SwirlOuter);
                        pb = float4(_InnerRadius, _BlendWidth, _CoreSize, _RimSoftness);
                        ringInt = _RingIntensity;
                    }
                    else
                    {
                        h  = _BlackHoles[i];
                        pa = _BHParamsA[i];
                        pb = _BHParamsB[i];
                        ringInt = _BHParamsC[i].x;
                    }

                    float  R = max(h.z, 1e-4);
                    float2 pv = ws - h.xy;
                    float  di = length(pv) / R;   // 0 en el centro, 1 en el borde de ESTE agujero

                    rC = smin(rC, di, _MergeSmooth);

                    // Peso de influencia: enorme dentro del agujero, cae rapido fuera.
                    float wi = 1.0 / (di * di * di * di + 1e-3);
                    wSum      += wi;
                    coreSizeC += pb.z * wi;
                    rimSoftC  += pb.w * wi;
                    ringIntC  += ringInt * wi;

                    if (di < 1.0)
                    {
                        float zone_i = smoothstep(pb.x - pb.y, pb.x + pb.y, di);
                        float lens_i = 1.0 - smoothstep(pb.z, 1.0, di);
                        float strength_i = lerp(pa.x, pa.z, zone_i);
                        float swirl_i    = lerp(pa.y, pa.w, zone_i);

                        float angle = (swirl_i + _SwirlSpeed * t * zone_i) * lens_i;
                        float s, c;
                        sincos(angle, s, c);
                        float2 dir = normalize(pv + 1e-5);
                        float2 swirled = float2(dir.x * c - dir.y * s, dir.x * s + dir.y * c);

                        // Tira al centro + remolino, como FRACCION del radio -> mundo.
                        float pull = strength_i * lens_i;
                        offsetWorld += (-dir + (swirled - dir)) * pull * R;
                        pullMax = max(pullMax, pull);
                    }
                }

                coreSizeC /= wSum;
                rimSoftC  /= wSum;
                ringIntC  /= wSum;

                float r = rC;
                // 'discard' (no return 0): un pixel descartado NO escribe stencil,
                // asi las esquinas transparentes de este quad no tapan a otro agujero.
                if (r > 1.0) discard;

                // Desvanecido del borde de la silueta COMBINADA.
                float fall = 1.0 - smoothstep(1.0 - rimSoftC, 1.0, r);

                // screenUV desde la posicion (ya cuantizada si hay pixelado): la
                // escena muestreada dentro del agujero tambien sale en mosaico.
                float4 sp = ComputeScreenPos(TransformWorldToHClip(float3(ws, IN.positionWS.z)));
                float2 screenUV = sp.xy / sp.w;

                // Desplazamiento: mundo -> pixeles -> UV (maneja aspecto).
                float2 offset = (offsetWorld / worldPerPx) / _ScreenParams.xy;

                // --- Aberracion cromatica: cada canal se desvia distinto al cruzar la lente.
                float ca = _ChromaticAberration * pullMax;
                half R2 = SAMPLE_TEXTURE2D(_SceneTex, sampler_SceneTex, screenUV + offset * (1.0 + ca)).r;
                half G  = SAMPLE_TEXTURE2D(_SceneTex, sampler_SceneTex, screenUV + offset).g;
                half B  = SAMPLE_TEXTURE2D(_SceneTex, sampler_SceneTex, screenUV + offset * (1.0 - ca)).b;
                half3 scene = half3(R2, G, B);

                // --- Nucleo negro SOLIDO con borde suave (sobre el campo combinado:
                //     dos nucleos cercanos forman UNA mancha negra con cuello) ---
                float core = 1.0 - smoothstep(coreSizeC - _CoreSoftness, coreSizeC, r);
                scene *= (1.0 - core);

                // --- Anillo de luz (foton / acrecion): al fusionarse, el anillo
                //     envuelve la mancha combinada en vez de cruzarla ---
                float ringD = (r - coreSizeC) / max(_RingWidth, 1e-3);
                float ring  = exp(-ringD * ringD) * ringIntC * fall * (1.0 - core);
                scene += _RingColor.rgb * ring;

                return half4(scene, fall);
            }
            ENDHLSL
        }
    }
}
