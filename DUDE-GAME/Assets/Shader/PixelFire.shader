Shader "UI/PixelFire"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Fire Colors)]
        _ColorBright ("Brighter Color", Color) = (1.0, 0.65, 0.1, 1)
        _ColorDark ("Darker Color", Color) = (1.0, 0.0, 0.15, 1)
        _BackgroundColor ("Background Color", Color) = (0.1, 0.1, 0.1, 1)
        
        [Header(Settings)]
        _PixelRes ("Pixel Resolution", Float) = 64.0
        _NoiseScale ("Noise Scale", Float) = 10.0
        _ScrollSpeed ("Scroll Speed", Float) = 1.0
        _Threshold ("Fire Height", Range(0, 2)) = 1.0

        // --- UI REQUIRED PROPERTIES ---
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
        
        // --- UI STENCIL BLOCK ---
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
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc" // Required for UI Clipping

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
                float4 worldPosition : TEXCOORD1; // Required for Clipping
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect; // Provided by Unity UI automatically
            float4 _MainTex_ST;
            
            // Custom Properties
            float4 _ColorBright;
            float4 _ColorDark;
            float4 _BackgroundColor;
            float _PixelRes;
            float _NoiseScale;
            float _ScrollSpeed;
            float _Threshold;

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

            // --- NOISE FUNCTIONS ---
            float rand(float2 co) {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }
            float hermite(float t) {
                return t * t * (3.0 - 2.0 * t);
            }
            float noise(float2 co, float frequency) {
                float2 v = float2(co.x * frequency, co.y * frequency);
                float ix1 = floor(v.x); float iy1 = floor(v.y);
                float ix2 = floor(v.x + 1.0); float iy2 = floor(v.y + 1.0);
                float fx = hermite(frac(v.x)); float fy = hermite(frac(v.y));
                float fade1 = lerp(rand(float2(ix1, iy1)), rand(float2(ix2, iy1)), fx);
                float fade2 = lerp(rand(float2(ix1, iy2)), rand(float2(ix2, iy2)), fx);
                return lerp(fade1, fade2, fy);
            }
            float pnoise(float2 co, float freq, int steps, float persistence) {
                float value = 0.0; float ampl = 1.0; float sum = 0.0;
                for (int i = 0; i < steps; i++) {
                    sum += ampl; value += noise(co, freq) * ampl;
                    freq *= 2.0; ampl *= persistence;
                }
                return value / sum;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Sample Texture (Mask)
                half4 spriteColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
                
                // If alpha is low, don't calculate fire
                if (spriteColor.a < 0.01) discard;

                // 2. Pixelation
                float2 pixelUV = floor(IN.texcoord * _PixelRes) / _PixelRes;

                // 3. Fire Logic
                float gradient = _Threshold - pixelUV.y; 
                float gradientStep = 0.2;
                float2 pos = pixelUV;
                pos.y -= _Time.y * 0.3125 * _ScrollSpeed;

                float4 middleColor = lerp(_ColorBright, _ColorDark, 0.5);
                float noiseTexel = pnoise(pos, _NoiseScale, 5, 0.5);

                float firstStep = smoothstep(0.0, noiseTexel, gradient);
                float darkerColorStep = smoothstep(0.0, noiseTexel, gradient - gradientStep);
                float darkerColorPath = firstStep - darkerColorStep;
                float4 fireColor = lerp(_ColorBright, _ColorDark, darkerColorPath);
                float middleColorStep = smoothstep(0.0, noiseTexel, gradient - 0.2 * 2.0);
                fireColor = lerp(fireColor, middleColor, darkerColorStep - middleColorStep);
                
                float4 finalColor = lerp(fireColor, _BackgroundColor, firstStep);

                // 4. Apply Sprite Alpha & Vertex Color (Tint)
                finalColor.a = spriteColor.a * IN.color.a;
                finalColor.rgb *= IN.color.rgb; // Apply Tint

                // 5. UI CLIPPING (Essential for Masks/ScrollViews)
                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}