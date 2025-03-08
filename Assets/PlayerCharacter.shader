Shader "Custom/PlayerCharacter"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _TargetColor1 ("First Color to Replace", Color) = (0,0,1,1) // Pure Blue (0,0,255)
        _TargetColor2 ("Second Color to Replace", Color) = (0,0,0.996,1) // Near Blue (0,0,254)
        _NewColor ("New Color", Color) = (1,0,0,1) // Default Replacement Color (Red)
        _Tolerance ("Tolerance", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            half4 _TargetColor1;
            half4 _TargetColor2;
            half4 _NewColor;
            half _Tolerance;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);

                // Check color similarity for the first color
                float diff1 = distance(col.rgb, _TargetColor1.rgb);
                // Check color similarity for the second color
                float diff2 = distance(col.rgb, _TargetColor2.rgb);

                if (diff1 < _Tolerance)
                {
                    col.rgb = _NewColor.rgb; // Replace first color exactly
                }
                else if (diff2 < _Tolerance)
                {
                    col.rgb = float3(1,1,0);
                    // col.rgb = _NewColor.rgb * 0.8; // Replace second color with 80% brightness
                }

                return col;
            }
            ENDHLSL
        }
    }
}
