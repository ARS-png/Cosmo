

Shader "Custom/GrassURPShader"
{
    Properties
    {
        _ColorBottom ("Very Dark Bottom", Color) = (0.01, 0.08, 0.02, 1.0)
        _ColorTop ("Soft Grass Top", Color) = (0.22, 0.48, 0.14, 1.0)
        
        _WindSpeed ("Wind Speed", Float) = 2.0
        _WindStrength ("Wind Strength", Float) = 0.3
        _WindFrequency ("Wind Frequency", Float) = 0.5
        _BaseWidth ("Base Width", Range(0.1, 5.0)) = 1.0
        _TipWidth ("Tip Width", Range(0.0, 2.0)) = 0.0
        
        _BendingStrength ("Bending Strength", Float) = 1.5
        _GrassFadeDistance ("Grass Fade Distance", Float) = 40

        [MainTexture] _MainTex ("Grass Texture (RGBA)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
            };

            StructuredBuffer<float4x4> _CullBuf;
            
            float4 _ColorBottom;
            float4 _ColorTop;

            float _WindSpeed;    
            float _WindStrength;
            float _WindFrequency;

            float _BaseWidth;
            float _TipWidth;
            
            float _BendingStrength;
            float3 _TestPlayerPos;
            float _GrassFadeDistance; 

            // Объявляем текстуру и сэмплер по правилам URP
            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            float GetDitherNoise(float2 screenPos)
            {
                float3x3 ditherMatrix = float3x3(
                    1.0, 9.0, 3.0,
                    7.0, 5.0, 11.0,
                    13.0, 15.0, 14.0
                ) / 16.0;
                
                int x = int(fmod(screenPos.x, 3.0));
                int y = int(fmod(screenPos.y, 3.0));
                return ditherMatrix[x][y];
            }

            void ApplyWind(inout float3 worldPosition, float3 worldInstancePos, float2 vertexUV)
            {
                float windPhase = _Time.y * _WindSpeed + (worldInstancePos.x + worldInstancePos.z) * _WindFrequency;
                float windWave = sin(windPhase) * cos(windPhase * 0.5f); 
                float heightMask = saturate(vertexUV.y); 

                worldPosition.z += windWave * _WindStrength * 0.5f * heightMask; 
            }

            void ApplyForm(inout float3 localPosition, float2 vertexUV)
            {
                float heightFactor = saturate(vertexUV.y);
                float widthScale = lerp(_BaseWidth, _TipWidth, heightFactor);

                localPosition.x *= widthScale;
                localPosition.z *= widthScale;
            }

            void ApplyBending(inout float3 worldPos, float3 worldInstancePos, float2 vertexUV)
            {
                float3 dirToGrass = worldInstancePos - _TestPlayerPos;
                float distance = length(dirToGrass);
                float testRadius = 3.0f; 

                if (distance < testRadius)
                {
                    float mask = saturate(1.0f - (distance / testRadius));
                    mask *= mask; 

                    float3 bendDir = normalize(dirToGrass);
                    float bendEffect = mask * saturate(vertexUV.y) * _BendingStrength;

                    worldPos.xyz += bendDir * bendEffect;
                    worldPos.xyz -= bendDir * mask * saturate(vertexUV.y) * 0.2f;
                }
            }

            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;
                       
                float4x4 instanceMatrix = _CullBuf[instanceID];
                float3 worldInstancePos = mul(instanceMatrix, float4(0.0, 0.0, 0.0, 1.0)).xyz;
                  
                float3 localPosition = input.positionOS.xyz;
                ApplyForm(localPosition, input.uv);

                float4 positionWS = mul(instanceMatrix, float4(localPosition, 1.0));
                
                ApplyBending(positionWS.xyz, worldInstancePos, input.uv); 
                ApplyWind(positionWS.xyz, worldInstancePos, input.uv);

                output.worldPos = positionWS.xyz;
                output.positionCS = TransformWorldToHClip(positionWS.xyz);
                output.uv = input.uv;
                            
                return output;
            }

            float4 frag(Varyings input, float4 pixelScreenPos : SV_POSITION) : SV_Target
            {
                float distToCamera = distance(input.worldPos, _WorldSpaceCameraPos);
                float fadeStart = _GrassFadeDistance - 10.0f; 
                float fadeEnd = _GrassFadeDistance;

                float fadeAlpha = 1.0f - saturate((distToCamera - fadeStart) / (fadeEnd - fadeStart));
                float ditherNoise = GetDitherNoise(pixelScreenPos.xy);

                clip(fadeAlpha - ditherNoise);


                float4 texColor = _MainTex.Sample(sampler_MainTex, input.uv);

                clip(texColor.a - 0.1f); 

          
                float gradient = smoothstep(0.0f, 1.0f, input.uv.y);
                float4 grassColor = lerp(_ColorBottom, _ColorTop, gradient);

             
                float brightness = (texColor.r + texColor.g + texColor.b) * 0.333f;
                grassColor = lerp(float4(0.0, 0.0, 0.0, 1.0), grassColor, brightness);

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                float4 shadowCoord = TransformWorldToShadowCoord(input.worldPos);
                float shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
                #else
                float shadowAttenuation = 1.0; 
                #endif

                float shadowMask = lerp(0.2f, 1.0f, shadowAttenuation); 
                return grassColor * shadowMask;
            }   
            ENDHLSL
        }
    }
}

