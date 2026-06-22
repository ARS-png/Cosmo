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

        _ScatteringCoefficients("Scattering Coefficients", Vector) = (1.0, 2.1, 4.3, 0) 
        _InScatteringStrength("In Scattering Strength", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" }
        
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always 

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

              
                return exp(-height01 * _DensityFalloff);
            }

            float opticalDepth(float3 rayOrigin, float3 rayDir, float rayLength)
            {
                float3 densitySamplePoint = rayOrigin;
                float stepSize = rayLength / max(1, _NumOpticalDepthPoints - 1);
                float totalOpticalDepth = 0;

                for(int i = 0; i < _NumOpticalDepthPoints; i++)
                {
                    float localDensity = densityAtPoint(densitySamplePoint);
                    totalOpticalDepth += localDensity * stepSize;
                    densitySamplePoint += rayDir * stepSize;
                }

                return totalOpticalDepth;
            }
                        float3 calculateLight(float3 rayOrigin, float3 rayDir, float rayLength, float3 originalColor, out float accumulatedOpticalDepth) {
                float3 inScatterPoint = rayOrigin;
                float stepSize = rayLength / max(1, _NumInScatteringPoints - 1);
                float3 inScatteredLight = 0;
                float viewRayOpticalDepth = 0;
                float3 dirToSun = normalize(_SunDir);

                float cosAngle = dot(rayDir, dirToSun);
                
                float phaseRayleigh = 0.75 * (1.0 + cosAngle * cosAngle);
                float phaseMie = 0.15 * (1.0 + cosAngle * cosAngle) / pow(abs(1.25 - 0.6 * cosAngle), 1.5);

                float blockingRadius = _PlanetRadius;

                for (int i = 0; i < _NumInScatteringPoints; i++) {
                    float2 hitPlanet = raySphere(_PlanetCenter, blockingRadius, inScatterPoint, dirToSun);
                    float sunRayOpticalDepth = 0;

                    if (hitPlanet.y < 0 || hitPlanet.x > hitPlanet.y) {
                        float sunRayLength = raySphere(_PlanetCenter, _AtmosphereRadius, inScatterPoint, dirToSun).y;
                        sunRayOpticalDepth = opticalDepth(inScatterPoint, dirToSun, sunRayLength);
                    } 
                    else {
                        sunRayOpticalDepth = 0.5; 
                    }

                    float localDensity = densityAtPoint(inScatterPoint);
                    viewRayOpticalDepth += localDensity * stepSize;   

                    float3 transmittance = exp(-(sunRayOpticalDepth + viewRayOpticalDepth) * _ScatteringCoefficients);
                    
                    float3 scatteringIntensity = _ScatteringCoefficients * (phaseRayleigh + phaseMie * 1.5);
                    inScatteredLight += localDensity * transmittance * scatteringIntensity * stepSize * _InScatteringStrength;   
                    
                    inScatterPoint += rayDir * stepSize;
                }

                accumulatedOpticalDepth = viewRayOpticalDepth;
                return inScatteredLight * originalColor;
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

                float camRadius = length(rayOrigin - _PlanetCenter);

             
                float2 hitAtmosphere = raySphere(_PlanetCenter, _AtmosphereRadius, rayOrigin, rayDir);
                float t1 = hitAtmosphere.x; 
                float t2 = hitAtmosphere.y; 

        
                if (t2 <= 0) return half4(0,0,0,0);

                float dstToStart = max(0.0, t1);
                if (camRadius <= _AtmosphereRadius)
                {
                    dstToStart = 0.0; 
                }

                float dstToEnd = min(t2, sceneDepth); 

        
                float2 hitOcean = raySphere(_PlanetCenter, _OceanRadius, rayOrigin, rayDir);
                if (hitOcean.x > 0 && hitOcean.x < dstToEnd)
                {
                    dstToEnd = hitOcean.x; 
                }

                float dstThroughAtmosphere = max(0.0, dstToEnd - dstToStart);

                if (dstThroughAtmosphere > 0)
                {
                    const float epsilon = 0.0005;
                    float3 pointInAtmosphere = rayOrigin + rayDir * (dstToStart + epsilon);
                    
                    float viewRayOpticalDepth = 0;
                    float3 light = calculateLight(pointInAtmosphere, rayDir, max(0.0, dstThroughAtmosphere - epsilon * 2), _BaseColor.rgb, viewRayOpticalDepth);
    

                    float3 transmittance = exp(-viewRayOpticalDepth * _ScatteringCoefficients);
                    float alpha = 1.0 - (transmittance.r + transmittance.g + transmittance.b) / 3.0;

               
                    alpha = saturate(alpha * 1.0);

                    return half4(light, alpha);
                }
                
                return half4(0,0,0,0);
             }
            ENDHLSL
        }
    }
}
