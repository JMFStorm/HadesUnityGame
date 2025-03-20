Shader "Custom/EnemyCharacter"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _ZWrite("ZWrite", Float) = 0

        _IsShadowVariant("Is Shadow Variant", Float) = 0 // Fänne: If is shadow variant boolean
        _OutlineColor("Outline Color", Color) = (0.5, 0.5, 0.5, 1)
        _InlineColor("Inline Color", Color) = (0.5, 0.5, 0.5, 1)
        _OutlineThickness("Outline Thickness", Float) = 0.3
        _DamageColor("Damage color of enemy", Color) = (1.0, 1.0, 1.0, 1)

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                half2   lightingUV  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _IsShadowVariant; // Fänne: variable
                half4 _OutlineColor;
                float _OutlineThickness;
                half4 _InlineColor;
                half4 _DamageColor;
            CBUFFER_END

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // SetUpSpriteInstanceProperties(); // Fänne: unknown function
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);

                o.color = v.color * _Color * unity_SpriteColor;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            half Luminance(half3 color) // Fänne: Additional function
            {
                return dot(color, half3(0.299, 0.587, 0.114));
            }

            bool IsTransparent(float2 uv) {
                half4 sampleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return sampleColor.a < 0.5; // Adjust threshold as needed
            }

            half4 CombinedShapeLightFragment(Varyings i) : SV_Target // Fänne: modified fragment shader
            {
                half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Sample surrounding pixels
                float2 texelSize = 1.0 / _ScreenParams.xy * _OutlineThickness; // Adjust based on screen size

                // Track transparency of surrounding pixels
                int transparentCount = 0;
                const int totalSamples = 8; // Number of surrounding pixels checked

                transparentCount += IsTransparent(i.uv + float2(-texelSize.x, 0)); // Left
                transparentCount += IsTransparent(i.uv + float2(texelSize.x, 0));  // Right
                transparentCount += IsTransparent(i.uv + float2(0, -texelSize.y)); // Down
                transparentCount += IsTransparent(i.uv + float2(0, texelSize.y));  // Up
                transparentCount += IsTransparent(i.uv + float2(-texelSize.x, -texelSize.y)); // Bottom-left
                transparentCount += IsTransparent(i.uv + float2(texelSize.x, -texelSize.y));  // Bottom-right
                transparentCount += IsTransparent(i.uv + float2(-texelSize.x, texelSize.y));  // Top-left
                transparentCount += IsTransparent(i.uv + float2(texelSize.x, texelSize.y));   // Top-right

                // Check if the pixel is pure red (or close to pure red)
                if (0.05 < main.r && main.g < 0.01 && main.b < 0.01)
                {
                    // Sample the mask texture
                    const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);

                    // Initialize surface and input data for lighting calculations
                    SurfaceData2D surfaceData;
                    InputData2D inputData;
                    InitializeSurfaceData(main.rgb, main.a, mask, surfaceData);
                    InitializeInputData(i.uv, i.lightingUV, inputData);

                    // Calculate the final lit color using CombinedShapeLightShared
                    half4 litColor = CombinedShapeLightShared(surfaceData, inputData);

                    // Extract the light intensity (e.g., use the luminance of the lit color)
                    half lightIntensity = Luminance(litColor.rgb);

                    // Set the min brightness of the red channel in darkness
                    half minRedInDarkness = 0.6;

                    // Scale the red value based on light intensity
                    // In darkness (lightIntensity = 0), redValue = minRedInDarkness
                    // As light intensity increases, redValue can exceed minRedInDarkness
                    half redValue = minRedInDarkness + (main.r * lightIntensity);

                    // Return the final color with adjusted red channel
                    return half4(redValue, 0.0, 0.0, main.a);
                }

                if (_IsShadowVariant == 0) // Fänne: Normal color calculation
                {
                    // For non-red pixels, proceed with normal lighting calculations
                    const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                    SurfaceData2D surfaceData;
                    InputData2D inputData;

                    if (0.01 < _DamageColor.r + _DamageColor.g + _DamageColor.b)
                    {
                        if (!(main.r < 0.01 && main.g < 0.01 && main.b < 0.01))
                        {
                            if (transparentCount <= 0)
                            {
                                // If damage is on, set damage color to body areas
                                main = _DamageColor;
                            }
                        }
                    }

                    InitializeSurfaceData(main.rgb, main.a, mask, surfaceData);
                    InitializeInputData(i.uv, i.lightingUV, inputData);

                    return CombinedShapeLightShared(surfaceData, inputData);
                }
                else // Fänne: Color calculation for shadow variant
                {
                    // Fänne: Outlines
                    {
                        // If at least one surrounding pixel is non-transparent, but not all => apply the outline color
                        if (0 < transparentCount && transparentCount < totalSamples) {
                             return _OutlineColor;
                        }
                    }

                    // Sample the main texture
                    half4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
  
                    half3 modifiedColor;
    
                    if (mainTexColor.r < 0.01 && mainTexColor.g < 0.01 && mainTexColor.b < 0.01)
                    {
                        // Invert dark colors to brighter gray
                        modifiedColor = _InlineColor;
                    }
                    else
                    {
                        if (0.01 < _DamageColor.r + _DamageColor.g + _DamageColor.b)
                        {
                            // If damage is on, set damage color to body areas
                            modifiedColor = _DamageColor;
                        }
                        else
                        {
                            // Set the pixel to black
                            modifiedColor = half3(0, 0, 0);
                        }
                    }

                    // Initialize surface and input data for lighting calculations
                    half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                    SurfaceData2D surfaceData;
                    InputData2D inputData;

                    // Pass the modified color to lighting
                    InitializeSurfaceData(modifiedColor, mainTexColor.a, mask, surfaceData);
                    InitializeInputData(i.uv, i.lightingUV, inputData);

                    // Get the final lit color
                    return CombinedShapeLightShared(surfaceData, inputData);
                }
            }
            ENDHLSL
        }

        Pass
        {
            ZWrite Off

            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float4 tangent      : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                half4   color           : COLOR;
                float2  uv              : TEXCOORD0;
                half3   normalWS        : TEXCOORD1;
                half3   tangentWS       : TEXCOORD2;
                half3   bitangentWS     : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // SetUpSpriteInstanceProperties(); // Fänne: unknown function
                attributes.positionOS = UnityFlipSprite(attributes.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.uv = attributes.uv;
                o.color = attributes.color * _Color * unity_SpriteColor;
                o.normalWS = -GetViewForwardDir();
                o.tangentWS = TransformObjectToWorldDir(attributes.tangent.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * attributes.tangent.w;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

            half4 NormalsRenderingFragment(Varyings i) : SV_Target
            {
                const half4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));

                return NormalsRenderingShared(mainTex, normalTS, i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            // GPU Instancing
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                float4  color           : COLOR;
                float2  uv              : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
            CBUFFER_END

            Varyings UnlitVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // SetUpSpriteInstanceProperties(); // Fänne: unknown function
                attributes.positionOS = UnityFlipSprite( attributes.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.uv = attributes.uv;
                o.color = attributes.color * _Color * unity_SpriteColor;
                return o;
            }

            float4 UnlitFragment(Varyings i) : SV_Target
            {
                return float4(1, 0, 0, 1);
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                return mainTex;
            }
            ENDHLSL
        }
    }
}
