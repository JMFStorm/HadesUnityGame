Shader "Custom/EnemyOutlines"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
        _Alpha ("Alpha", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BlurSize;
            float _Alpha;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 offset = float2(_BlurSize / _ScreenParams.x, _BlurSize / _ScreenParams.y);
                float alpha = 0.0;

                // 9-sample blur
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        alpha += tex2D(_MainTex, i.uv + float2(x, y) * offset).a;
                    }
                }

                alpha /= 9.0;

                return float4(1.0, 0.05, 0.05, alpha * _Alpha); // pure red with blurred alpha
            }
            ENDCG
        }
    }
}
