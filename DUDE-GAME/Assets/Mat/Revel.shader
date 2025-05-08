Shader "Custom/BlinkingSpriteShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlinkSpeed ("Blink Speed", Range(0.1, 10)) = 2.0
        _MaxAlpha ("Max Alpha", Range(0, 1)) = 1.0
        _MinAlpha ("Min Alpha", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            float _BlinkSpeed;
            float _MaxAlpha;
            float _MinAlpha;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float alpha = lerp(_MinAlpha, _MaxAlpha, (sin(_Time.y * _BlinkSpeed) * 0.5 + 0.5));
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;
                texColor.a *= alpha;
                return texColor;
            }
            ENDCG
        }
    }
}
