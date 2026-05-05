Shader "Custom/GrayscaleLerpFancy"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LerpAmount ("Lerp Amount", Range(0,1)) = 1
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.5
        _BlurStrength ("Blur Strength", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _LerpAmount;
            float _GlowStrength;
            float _BlurStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 SampleBlurredColor(float2 uv)
            {
                float2 offset = _BlurStrength * 0.005;
                fixed4 col = tex2D(_MainTex, uv) * 0.4;
                col += tex2D(_MainTex, uv + float2(offset.x, 0)) * 0.15;
                col += tex2D(_MainTex, uv - float2(offset.x, 0)) * 0.15;
                col += tex2D(_MainTex, uv + float2(0, offset.y)) * 0.15;
                col += tex2D(_MainTex, uv - float2(0, offset.y)) * 0.15;
                return col;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 original = tex2D(_MainTex, i.uv);
                float gray = dot(original.rgb, float3(0.299, 0.587, 0.114));
                float3 grayscaleColor = float3(gray, gray, gray);

                fixed4 blurred = SampleBlurredColor(i.uv);
                float3 glow = blurred.rgb * _GlowStrength * _LerpAmount;

                float3 finalColor = lerp(grayscaleColor, original.rgb + glow, _LerpAmount);

                return fixed4(finalColor, original.a);
            }
            ENDCG
        }
    }
}
