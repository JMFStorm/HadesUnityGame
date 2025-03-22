Shader "Custom/Forcefield"
{
    Properties
    {
        _Color ("Main Color", Color) = (0, 0, 0, 1)
        _Distortion ("Distortion Strength", Range(0, 1)) = 0.2
        _Speed ("Animation Speed", Range(0, 5)) = 1
        _UVScaleX ("UV Scale X", Float) = 1.0
        _UVScaleY ("UV Scale Y", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha // Semi-transparent blending
        Cull Off // Render both sides

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color; // Color with alpha channel
            float _Distortion;
            float _Speed;
            float _UVScaleX;
            float _UVScaleY;

            // Procedural noise function
            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 ip = floor(p);
                float2 u = frac(p);
                u = u * u * (3.0 - 2.0 * u);
                
                float res = lerp(
                    lerp(rand(ip), rand(ip + float2(1.0, 0.0)), u.x),
                    lerp(rand(ip + float2(0.0, 1.0)), rand(ip + float2(1.0, 1.0)), u.x),
                    u.y);
                
                return res;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Apply UV scaling
                float2 scaledUV = v.uv * float2(_UVScaleX, _UVScaleY);
                o.uv = scaledUV + noise(scaledUV * 10 + _Time.y * _Speed) * _Distortion;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Use oscillating time instead of linear scrolling
                float timeFactor = sin(_Time.y * _Speed * 0.1) * 0.5 + 0.5; // Oscillates between 0 and 1

                // Modify UVs dynamically based on time
                float2 noiseUV = i.uv * 5 + timeFactor; // Subtle time-based change

                // Compute noise and glow effect
                float n = noise(noiseUV); // Apply noise function
                float glow = smoothstep(0.3, 0.7, n); // Glow effect based on noise intensity

                // Ensure the alpha is correctly used with the color
                return float4(_Color.rgb * glow, glow * _Color.a); // Transparent glow effect, including alpha from _Color
            }
            ENDCG
        }
    }
}
