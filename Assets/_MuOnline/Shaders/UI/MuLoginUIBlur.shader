// Ligero blur multi-muestra para RawImage (vídeo / RT). Canvas Screen Space Overlay.
Shader "Mu/UI/LoginBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Blur ("Blur (UV)", Range(0, 3)) = 1.2
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            half _Blur;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float2 k = _MainTex_TexelSize.xy * _Blur;
                fixed4 c = tex2D(_MainTex, uv) * 0.34;
                c += tex2D(_MainTex, uv + float2(k.x, 0)) * 0.11;
                c += tex2D(_MainTex, uv - float2(k.x, 0)) * 0.11;
                c += tex2D(_MainTex, uv + float2(0, k.y)) * 0.11;
                c += tex2D(_MainTex, uv - float2(0, k.y)) * 0.11;
                c += tex2D(_MainTex, uv + float2(k.x, k.y) * 0.75) * 0.075;
                c += tex2D(_MainTex, uv - float2(k.x, k.y) * 0.75) * 0.075;
                c += tex2D(_MainTex, uv + float2(-k.x, k.y) * 0.75) * 0.075;
                return c * i.color;
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
