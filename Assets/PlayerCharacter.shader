Shader "Custom/PlayerCharacter"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _NewColor ("New Color", Color) = (1,0,0,1) // Default Replacement Color (Red)
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
            half4 _NewColor;

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

                // Calculate the brightness (luminance) of the original color
                half brightness = col.b;
                // half brightness = 0.299 * col.r + 0.587 * col.g + 0.114 * col.b;

                // Check if Blue is the dominant color (greater than both Red and Green)
                if (col.r < col.b && col.g < col.b && col.g < 0.01 && col.r < 0.01)
                {
                    // Adjust the new color based on the original brightness
                    // Multiply the _NewColor by the brightness of the original color
                    col.rgb = _NewColor.rgb * brightness;
                }

                return col;
            }

            ENDHLSL
        }
    }
}
