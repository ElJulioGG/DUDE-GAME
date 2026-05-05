Shader "UI/PixelDissolveWithSeed"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Control)]
        // 0 = Normal Sprite, 1 = Max Fire Effect
        _EmissionAmount ("Fire Progress", Range(0.0, 1.0)) = 0.0
        
        [Header(Variation)]
        // Change this number to get a different noise pattern
        _Seed ("Random Seed", Float) = 0.0
        
        [Header(Configuration)]
        _BurnLevel ("Max Burn Level", Range(0.0, 1.0)) = 0.6
        _DissolveSpeed ("Dissolve Speed (X, Y)", Vector) = (0.0, 0.5, 0, 0)
        _NoiseStrength ("Noise Strength", Range(0.0, 1.0)) = 0.2
        _SideCurve ("Side Curve", Range(0.0, 2.0)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.1
        _PixelSize ("Pixel Size (X, Y)", Vector) = (0.01, 0.01, 0, 0) 

        // UI Required Properties
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
            #pragma target 2.0

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

            float _EmissionAmount;
            float _Seed; // New Variable
            float _BurnLevel;
            float2 _DissolveSpeed;
            float _NoiseStrength;
            float _SideCurve;
            float _EdgeSoftness;
            float2 _PixelSize;

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

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Pixelate UVs
                float2 safePixelSize = max(_PixelSize, float2(0.001, 0.001));
                float2 pixelUV = floor(IN.texcoord / safePixelSize) * safePixelSize;

                // 2. Sample Noise (Scrolling + SEED)
                float2 scrollUV = pixelUV;
                
                // Offset the noise based on the Seed property
                // We multiply Y by a non-whole number so it doesn't just slide diagonally
                scrollUV += float2(_Seed, _Seed * 0.618);
                
                scrollUV -= _Time.y * _DissolveSpeed; 
                scrollUV = frac(scrollUV); // Force Loop
                
                float noiseVal = tex2D(_NoiseTex, scrollUV).r;

                // 3. Realistic Sides Calculation
                float distFromCenter = abs(pixelUV.x - 0.5);
                float sideOffset = distFromCenter * distFromCenter * _SideCurve;

                // 4. Calculate Logic
                float currentBaseHeight = lerp(1.5, _BurnLevel, _EmissionAmount);
                float noiseOffset = (noiseVal - 0.5) * _NoiseStrength;
                float currentThreshold = (currentBaseHeight - sideOffset) + noiseOffset;
                
                // 5. Calculate Alpha
                float dissolveAlpha = 1.0 - smoothstep(currentThreshold, currentThreshold + _EdgeSoftness, pixelUV.y);

                // 6. Final Color Assembly
                fixed4 mainColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
                
                fixed4 finalColor = mainColor;
                finalColor.a = mainColor.a * dissolveAlpha * IN.color.a;
                finalColor.rgb *= IN.color.rgb;

                // 7. UI Clipping
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