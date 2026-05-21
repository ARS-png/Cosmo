Shader "Custom/GrassURPShader"
{
    Properties
    {
        _ColorBottom ("Very Dark Bottom", Color) = (0.01, 0.08, 0.02, 1.0)
        _ColorTop ("Soft Grass Top", Color) = (0.22, 0.48, 0.14, 1.0)
        
        _WindSpeed ("Wind Speed", Float) = 2.0
        _WindStrength ("Wind Strength", Float) = 0.3
        _WindFrequency ("Wind Frequency", Float) = 0.5
  
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl" //

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

          
            StructuredBuffer<float4x4> _CullBuf;


            //colors
            float4 _ColorBottom;
            float4 _ColorTop;

            //wind
            float _WindSpeed;    
            float _WindStrength;
            float _WindFrequency;



            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;
           
                float4x4 instanceMatrix = _CullBuf[instanceID];

            
                float3 worldInstancePos = float3(instanceMatrix[0][3], instanceMatrix[1][3], instanceMatrix[2][3]);

                float4 positionWS = mul(instanceMatrix, float4(input.positionOS.xyz, 1.0));
                        
                float windPhase = _Time.y * _WindSpeed + (worldInstancePos.x + worldInstancePos.z) * _WindFrequency;
                float windWave = sin(windPhase) * cos(windPhase * 0.5f); 

                float mask = saturate(input.positionOS.y); 
                
           
                positionWS.x += windWave * _WindStrength * mask;
                positionWS.z += windWave * _WindStrength * 0.5f * mask; 

                output.worldPos = positionWS.xyz;//


                output.positionCS = TransformWorldToHClip(positionWS.xyz);
                
                output.uv = input.uv;
                return output;
            }

           float4 frag(Varyings input) : SV_Target
           {
                float gradient = smoothstep(0.0f, 1.0f, input.uv.y);
                float4 grassColor = lerp(_ColorBottom, _ColorTop, gradient);

              
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
