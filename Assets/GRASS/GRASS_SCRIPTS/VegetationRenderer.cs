using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VegetationRenderer
{
   
    private struct PlantSettingsShaderData
    {
        public int grassPerTriangle;
        public float slopeThreshold;
        public float noiseScale;
        public float densityThreshold;
        public float scaleXZ;
        public float scaleY;
    }

    private Planet planet;
    private Vector3 localUp;

    private ComputeShader cs;
    private int cachedMainKernelID;
    private Camera mainCamera;
    private Transform currentPlayerTransform;

    private readonly Plane[] cachedPlanes = new Plane[6];
    private readonly Vector4[] cachedVectors = new Vector4[6];


    public VegetationRenderer(Planet planet, Vector3 localUp)
    {
        this.planet = planet;
        this.localUp = localUp;
        this.mainCamera = Camera.main;

        if (planet.vegetationSettings != null) cs = planet.vegetationSettings.vegetationCS;
    }

  
    public void Initialize(List<VegetationTypeSettings> plantTypes, int maxInstances, Vector3 planetCenter)
    {
        if (cs == null || plantTypes == null || plantTypes.Count == 0) return;
        cachedMainKernelID = cs.FindKernel("CSMain");

    
        for (int i = 0; i < plantTypes.Count; i++)
        {
            plantTypes[i].InitializeBuffers(maxInstances);
        }

        var settings = planet.vegetationSettings;
        cs.SetFloat("_LOD1DistSqr", settings.lod1Dist * settings.lod1Dist);
        cs.SetFloat("_LOD2DistSqr", settings.lod2Dist * settings.lod2Dist);
        cs.SetFloat("_CullRadius", settings.cullRadius);

        cs.SetVector("_PlanetFaceUp", localUp);
        cs.SetVector("_PlanetWorldCenter", planet.transform.position);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) currentPlayerTransform = playerObj.transform;
    }


    public void Render(List<VegetationTypeSettings> plantTypes, GraphicsBuffer faceBuffer, Matrix4x4 localMatrix)
    {
        if (faceBuffer == null || faceBuffer.count == 0 || cs == null || plantTypes == null || plantTypes.Count == 0) return;

        if (plantTypes[0].cullBufLOD0 == null || planet.vegetationSettings == null) return;

        mainCamera = CameraManager.Instance != null ? CameraManager.Instance.GetActiveCamera() : Camera.main;
        if (mainCamera == null) return;

        Vector3 currentCameraPosition = CameraManager.Instance != null ? CameraManager.Instance.GetCurrentCameraPosition() : mainCamera.transform.position;
        Vector3 currentPlayerPosition = currentPlayerTransform != null ? currentPlayerTransform.position : Vector3.zero;

      
        GeometryUtility.CalculateFrustumPlanes(mainCamera, cachedPlanes);
        for (int i = 0; i < 6; i++)
        {
            cachedVectors[i] = new Vector4(cachedPlanes[i].normal.x, cachedPlanes[i].normal.y, cachedPlanes[i].normal.z, cachedPlanes[i].distance);
        }

        int totalTriangles = faceBuffer.count / 3;

        int maxGrassPerTriangle = 1;
        for (int i = 0; i < plantTypes.Count; i++)
        {
            if (plantTypes[i].grassPerTriangle > maxGrassPerTriangle)
            {
                maxGrassPerTriangle = plantTypes[i].grassPerTriangle;
            }
        }

        // Вместо dataContainer.maxInstances используем емкость буфера первого растения
        int maxInstances = plantTypes[0].cullBufLOD0.count;
        int finalInstancesCount = Mathf.Min(totalTriangles * maxGrassPerTriangle, maxInstances);

        int threadGroups = Mathf.CeilToInt(finalInstancesCount / 64f);
        if (threadGroups <= 0) return;

        // Упаковываем настройки всех растений в массив структур
        PlantSettingsShaderData[] shaderDataArray = new PlantSettingsShaderData[plantTypes.Count];
        for (int i = 0; i < plantTypes.Count; i++)
        {
            shaderDataArray[i] = new PlantSettingsShaderData
            {
                grassPerTriangle = plantTypes[i].grassPerTriangle,
                slopeThreshold = plantTypes[i].slopeThreshold,
                noiseScale = plantTypes[i].noiseScale,
                densityThreshold = plantTypes[i].densityThreshold,
                scaleXZ = plantTypes[i].scaleXZ,
                scaleY = plantTypes[i].scaleY
            };
        }


        ComputeBuffer settingsBuffer = new ComputeBuffer(plantTypes.Count, 24); 
        settingsBuffer.SetData(shaderDataArray);
        cs.SetBuffer(cachedMainKernelID, "_AllPlantSettings", settingsBuffer);

 
        cs.SetBuffer(cachedMainKernelID, "_PlanetVertices", faceBuffer);
        cs.SetVectorArray("_PlanesBuf", cachedVectors); 
        cs.SetMatrix("_LocalToWorldMatrix", localMatrix);
        cs.SetVector("_CameraPosition", currentCameraPosition);
        cs.SetInt("_TotalInstances", finalInstancesCount);

        var settings = planet.vegetationSettings;
        cs.SetFloat("_LOD1DistSqr", settings.lod1Dist * settings.lod1Dist);
        cs.SetFloat("_LOD2DistSqr", settings.lod2Dist * settings.lod2Dist);
        cs.SetFloat("_CullRadius", settings.cullRadius);

        float boundsSize = planet.shapeSettings.planetRadius * 3f;
        Bounds b = new Bounds(planet.transform.position, Vector3.one * boundsSize);

    
        for (int i = 0; i < plantTypes.Count; i++)
        {
            VegetationTypeSettings currentPlant = plantTypes[i];

   
            cs.SetInt("_CurrentRenderPlantType", i);

    
            currentPlant.cullBufLOD0.SetCounterValue(0);
            currentPlant.cullBufLOD1.SetCounterValue(0);
            currentPlant.cullBufLOD2.SetCounterValue(0);

          
            cs.SetBuffer(cachedMainKernelID, "_CullBufLOD0", currentPlant.cullBufLOD0);
            cs.SetBuffer(cachedMainKernelID, "_CullBufLOD1", currentPlant.cullBufLOD1);
            cs.SetBuffer(cachedMainKernelID, "_CullBufLOD2", currentPlant.cullBufLOD2);

         
            cs.Dispatch(cachedMainKernelID, threadGroups, 1, 1);

       
            GraphicsBuffer.CopyCount(currentPlant.cullBufLOD0, currentPlant.commandBufLOD0, sizeof(uint));
            GraphicsBuffer.CopyCount(currentPlant.cullBufLOD1, currentPlant.commandBufLOD1, sizeof(uint));
            GraphicsBuffer.CopyCount(currentPlant.cullBufLOD2, currentPlant.commandBufLOD2, sizeof(uint));

        
            currentPlant.mpBlockLOD0.SetVector("_TestPlayerPos", currentPlayerPosition);
            currentPlant.mpBlockLOD1.SetVector("_TestPlayerPos", currentPlayerPosition);
            currentPlant.mpBlockLOD2.SetVector("_TestPlayerPos", currentPlayerPosition);

          
            RenderParams rp0 = new RenderParams(currentPlant.material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = b, matProps = currentPlant.mpBlockLOD0 };
            RenderParams rp1 = new RenderParams(currentPlant.material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = b, matProps = currentPlant.mpBlockLOD1 };
            RenderParams rp2 = new RenderParams(currentPlant.material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = b, matProps = currentPlant.mpBlockLOD2 };

     
            Graphics.RenderMeshIndirect(rp0, currentPlant.lod0, currentPlant.commandBufLOD0);
            Graphics.RenderMeshIndirect(rp1, currentPlant.lod1, currentPlant.commandBufLOD1);
            Graphics.RenderMeshIndirect(rp2, currentPlant.lod2, currentPlant.commandBufLOD2);
        }


        settingsBuffer.Release();
    }
}
