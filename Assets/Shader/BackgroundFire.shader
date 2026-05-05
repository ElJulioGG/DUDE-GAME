Shader "UI/ProceduralFire_Pixelated"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Detail Noise (Required)", 2D) = "gray" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _ImageIntensity ("Original Image Intensity", Range(0.0, 1.0)) = 1.0
        _FireIntensity ("Fire Intensity", Range(0.0, 5.0)) = 1.5
        _Scale ("Fire Scale", Float) = 2.0
        _Speed ("Speed Multiplier", Float) = 1.0
        _PixelResolution ("Pixel Resolution", Float) = 64.0
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True" 
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _ImageIntensity;
            float _FireIntensity;
            float _Scale;
            float _Speed;
            float _PixelResolution;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float rand(float2 n) {
                return frac(sin(cos(dot(n, float2(12.9898,12.1414)))) * 83758.5453);
            }

            float noise(float2 n) {
                const float2 d = float2(0.0, 1.0);
                float2 b = floor(n);
                float2 f = smoothstep(float2(0.0, 0.0), float2(1.0, 1.0), frac(n));
                return lerp(
                    lerp(rand(b), rand(b + d.yx), f.x), 
                    lerp(rand(b + d.xy), rand(b + d.yy), f.x), 
                    f.y
                );
            }

            float fbm(float2 n) {
                float total = 0.0, amplitude = 1.0;
                for (int i = 0; i < 5; i++) {
                    total += noise(n) * amplitude;
                    n += n * 1.7;
                    amplitude *= 0.47;
                }
                return total;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 spriteColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
                
                if (spriteColor.a < 0.01) discard;

                float2 uv = IN.texcoord;
                float res = max(_PixelResolution, 1.0);
                float2 fireUV = floor(uv * res) / res;

                const float3 c1 = float3(0.5, 0.0, 0.1);
                const float3 c2 = float3(0.9, 0.1, 0.0);
                const float3 c3 = float3(0.2, 0.1, 0.7);
                const float3 c4 = float3(1.0, 0.9, 0.1);
                const float3 c5 = float3(0.1, 0.1, 0.1);
                const float3 c6 = float3(0.9, 0.9, 0.9);

                float time = _Time.y * _Speed;
                float2 speed = float2(0.1, 0.9);
                
                float dist = 3.5 - sin(time * 0.4) / 1.89;
                
                float2 p = fireUV * dist * _Scale; 
                
                p += sin(p.yx * 4.0 + float2(0.2, -0.3) * time) * 0.04;
                p += sin(p.yx * 8.0 + float2(0.6, 0.1) * time) * 0.01;
                p.x -= time / 1.1;
                
                float q = fbm(p - time * 0.3 + 1.0 * sin(time + 0.5) / 2.0);
                float qb = fbm(p - time * 0.4 + 0.1 * cos(time) / 2.0);
                float q2 = fbm(p - time * 0.44 - 5.0 * cos(time) / 2.0) - 6.0;
                float q3 = fbm(p - time * 0.9 - 10.0 * cos(time) / 15.0) - 4.0;
                float q4 = fbm(p - time * 1.4 - 20.0 * sin(time) / 14.0) + 2.0;
                q = (q + qb - 0.4 * q2 - 2.0 * q3 + 0.6 * q4) / 3.8;
                
                float2 r = float2(
                    fbm(p + q / 2.0 + time * speed.x - p.x - p.y), 
                    fbm(p + q - time * speed.y)
                );
                
                float3 c = lerp(c1, c2, fbm(p + r)) + lerp(c3, c4, r.x) - lerp(c5, c6, r.y);
                float3 fireColor = float3(1.0, 0.2, 0.05) / (pow((r.y + r.y) * max(0.0, p.y) + 0.1, 4.0));
                
                float3 texDetail = tex2D(_NoiseTex, fireUV * 0.6 + float2(0.5, 0.1)).xyz;
                
                fireColor += (texDetail * 0.01 * pow((r.y + r.y) * 0.65, 5.0) + 0.055) * lerp(float3(0.9, 0.4, 0.3), float3(0.7, 0.5, 0.2), uv.y);
                
                fireColor = fireColor / (1.0 + max(float3(0,0,0), fireColor));
                
                float3 finalRGB = (spriteColor.rgb * _ImageIntensity) + (fireColor * _FireIntensity);
                
                float finalAlpha = spriteColor.a;

                fixed4 result = fixed4(finalRGB, finalAlpha) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                return result;
            }
            ENDCG
        }
    }
}