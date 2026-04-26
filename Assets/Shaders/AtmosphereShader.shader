Shader "Custom/AtmosphereShader"
{
    Properties
    {
        [MainColor] _BaseColor("Atmosphere Color", Color) = (0.5, 0.7, 1.0, 1.0)
        _PlanetCenter("Planet Center", Vector) = (0, 0, 0, 0)
        _PlanetRadius("Planet Radius", Float) = 1.0
        _AtmosphereRadius("Atmosphere Radius", Float) = 1.2
        _DensityFalloff("Density Falloff", Float) = 4.0
       _DensityOffset("Density Offset", Float) = 0
        
        _OceanRadius("Ocean Radius", Float) = 1.0
        _SunDir("Sun Direction", Vector) = (0, 1, 0, 0)

      
        _NumInScatteringPoints("In-Scattering Points", Int) = 10
        _NumOpticalDepthPoints("Optical Depth Points", Int) = 10

        _ScatteringCoefficients("Scattering Coefficients", Vector) = (0,0,0,0)
        _InScatteringStrength("In Scattering Strength", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Cull Off 
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD1; 
                float3 viewVector  : TEXCOORD2;
                float4 screenPos   : TEXCOORD3; 
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float3 _PlanetCenter;
                float _PlanetRadius;
                float _AtmosphereRadius;
                float _DensityFalloff;
                float _DensityOffset;
                float _OceanRadius;
                float3 _SunDir;
                int _NumInScatteringPoints;
                int _NumOpticalDepthPoints;
                float3 _ScatteringCoefficients;
                float _InScatteringStrength;
            CBUFFER_END

            float2 raySphere(float3 sphereCenter, float sphereRadius, float3 rayOrigin, float3 rayDir)
            {
                float3 L = sphereCenter - rayOrigin;
                float tc = dot(L, rayDir);
                float d_2 = dot(L, L) - (tc * tc);
                float radius2 = sphereRadius * sphereRadius;

                if (d_2 > radius2) return float2(-1.0, -1.0);

                float t1c = sqrt(radius2 - d_2);
                return float2(tc - t1c, tc + t1c);
            }

            float densityAtPoint(float3 densitySamplePoint)
            {
                float currentRadius = length(densitySamplePoint - _PlanetCenter);
    
                float densityPlanetRadius = _PlanetRadius - _DensityOffset; 
    
                float heightAboveSurface = currentRadius - densityPlanetRadius; 
    
                float thickness = _AtmosphereRadius - densityPlanetRadius;
                float height01 = heightAboveSurface / thickness; 
    
                if (height01 < 0 || height01 > 1) return 0;

 
                float localDensity = exp(-height01 * _DensityFalloff);
    
                return localDensity;
            }


            float opticalDepth(float3 rayOrigin, float3 rayDir, float rayLength)
            {
                float3 densitySamplePoint = rayOrigin;
                float stepSize = rayLength / (_NumOpticalDepthPoints - 1);
                float totalOpticalDepth = 0;

                for(int i = 0; i < _NumOpticalDepthPoints; i++)
                {
                    float localDensity = densityAtPoint(densitySamplePoint);
                    totalOpticalDepth += localDensity * stepSize;
                    densitySamplePoint += rayDir * stepSize;
                }

                return totalOpticalDepth;
            }


           float3 calculateLight(float3 rayOrigin, float3 rayDir, float rayLength, float3 originalColor) {
                float3 inScatterPoint = rayOrigin;
                float stepSize = rayLength / max(1, _NumInScatteringPoints - 1);
                float3 inScatteredLight = 0;
                float viewRayOpticalDepth = 0;
                float3 dirToSun = normalize(_SunDir);

                float cosAngle = dot(rayDir, dirToSun);
                float phaseRayleigh = 0.75 * (1.0 + cosAngle * cosAngle);

                for (int i = 0; i < _NumInScatteringPoints; i++) {
                    float2 hitPlanet = raySphere(_PlanetCenter, _PlanetRadius, inScatterPoint, dirToSun);
                    float sunRayOpticalDepth = 0;

                    if (hitPlanet.y < 0) {
                        float sunRayLength = raySphere(_PlanetCenter, _AtmosphereRadius, inScatterPoint, dirToSun).y;
                        sunRayOpticalDepth = opticalDepth(inScatterPoint, dirToSun, sunRayLength);
                    } 

                    else {
                    sunRayOpticalDepth = 20; 
                    }

                    float localDensity = densityAtPoint(inScatterPoint);

                    viewRayOpticalDepth += localDensity * stepSize;   

                    float3 transmittance = exp(-(sunRayOpticalDepth + viewRayOpticalDepth) * _ScatteringCoefficients);
                    inScatteredLight += localDensity * transmittance * _ScatteringCoefficients * stepSize * _InScatteringStrength;   
                    inScatterPoint += rayDir * stepSize;
                }

                 float originalColorTransmittance = exp(-viewRayOpticalDepth);
                 return _BaseColor * originalColorTransmittance * inScatteredLight * phaseRayleigh;
            }


            Varyings vert(Attributes i)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(i.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.viewVector = posInputs.positionWS - _WorldSpaceCameraPos;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings i) : SV_Target
            {             
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams) * (length(i.viewVector) / i.screenPos.w);
                if (rawDepth <= 0.0) sceneDepth = 1e10; 

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(i.viewVector);

                float2 hitInfo = raySphere(_PlanetCenter, _AtmosphereRadius, rayOrigin, rayDir);
                float t1 = hitInfo.x; 
                float t2 = hitInfo.y; 

                if (t2 <= 0) return half4(0,0,0,0);

                float dstToStart = max(0, t1);
                float dstToEnd = min(t2, sceneDepth); 
                float dstThroughAtmosphere = max(0, dstToEnd - dstToStart);

               if(dstThroughAtmosphere > 0)
               {
                    const float epsilon = 1;


                    float3 pointInAtmosphere = rayOrigin + rayDir * (dstToStart + epsilon);
                    

                    float3 light = calculateLight(pointInAtmosphere, rayDir, dstThroughAtmosphere - epsilon * 2, _BaseColor);
    

                    return half4(light , .3);
                   
                }
                
              return _BaseColor;
             }
            ENDHLSL
        }
    }
}
