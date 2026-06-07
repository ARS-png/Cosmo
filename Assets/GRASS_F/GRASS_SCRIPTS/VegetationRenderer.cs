using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class VegetationRenderer
{
    private GrassDataContainer dataContainer;
    private Planet planet;
    private Vector3 localUp;
    private Matrix4x4 localToWorldMatrix;

    private RenderParams renderParamsLOD0;
    private RenderParams renderParamsLOD1;
    private RenderParams renderParamsLOD2;

    private MaterialPropertyBlock mp0;
    private MaterialPropertyBlock mp1;
    private MaterialPropertyBlock mp2;

    private Camera mainCamera;


    private Transform currentCameraTranform;

    private Transform currentPlayerTransform;


    private readonly Plane[] cachedPlanes = new Plane[6];
    private readonly Vector4[] cachedVectors = new Vector4[6];

    private ComputeShader cs;

    private int cachedMainKernelID;


    private int currentVertexCount = 0;



    public VegetationRenderer(Planet planet, Vector3 localUp)
    {
        this.planet = planet;
        this.localUp = localUp;
        this.dataContainer = new GrassDataContainer();
        this.mainCamera = Camera.main;


        if (planet.grassSettings != null)
        {
            cs = planet.grassSettings.grassComputeShader;
        }
    }


    public void Initialize(Mesh lod0, Mesh lod1, Mesh lod2, Material material, int maxInstances, Vector3 planetCenter)
    {

        if (cs == null) return;


        cachedMainKernelID = cs.FindKernel("CSMain");


        dataContainer.Initialize(
            maxInstances,
            (uint)lod0.GetIndexCount(0),
            (uint)lod1.GetIndexCount(0),
            (uint)lod2.GetIndexCount(0)
        );

        var settings = planet.grassSettings;


        cs.SetFloat("_LOD1DistSqr", settings.grassLod1Dist * settings.grassLod1Dist);
        cs.SetFloat("_LOD2DistSqr", settings.grassLod2Dist * settings.grassLod2Dist);
        cs.SetFloat("_CullRadius", settings.grassCullRadius);


        cs.SetFloat("_GrassDensityThreshold", settings.grassDensityThreshold);
        cs.SetFloat("_GrassNoiseScale", settings.grassNoiseScale);
        cs.SetFloat("_GrassSlopeThreshold", settings.grassSlopeThreshold); //
        cs.SetVector("_PlanetFaceUp", localUp);


        cs.SetVector("_PlanetWorldCenter", planet.transform.position);//



        mp0 = new MaterialPropertyBlock(); mp0.SetBuffer("_CullBuf", dataContainer.cullBufLOD0);
        mp1 = new MaterialPropertyBlock(); mp1.SetBuffer("_CullBuf", dataContainer.cullBufLOD1);
        mp2 = new MaterialPropertyBlock(); mp2.SetBuffer("_CullBuf", dataContainer.cullBufLOD2);


        float planetBoundsSize = planet.shapeSettings.planetRadius * 3f;
        Bounds b = new Bounds(planetCenter, Vector3.one * planetBoundsSize);


        renderParamsLOD0 = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = b, matProps = mp0 };
        renderParamsLOD1 = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = b, matProps = mp1 };
        renderParamsLOD2 = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = b, matProps = mp2 };


        GameObject playerObj = GameObject.FindGameObjectWithTag("Player"); 
        if (playerObj != null) currentPlayerTransform = playerObj.transform;
    }

  
    public void UpdateGeometry(List<TerrainFace.GrassVertexData> vertexData, Matrix4x4 localMatrix)
    {
        if (!dataContainer.isInitialized || cs == null) return;

     
        if (vertexData == null || vertexData.Count == 0)
        {
            currentVertexCount = 0;
            return;
        }

        this.localToWorldMatrix = localMatrix;
        dataContainer.UpdateVerticesBuffer(vertexData.Count, vertexData.ToArray());

        currentVertexCount = vertexData.Count;
    }

    private float nextLogTime = 0f;

    public void Render(Mesh lod0, Mesh lod1, Mesh lod2, int totalInstances)
    {

        if (CameraManager.Instance != null)
        {
            mainCamera = CameraManager.Instance.GetActiveCamera();
        }


        currentCameraTranform = planet.currentCamera; 

        int totalTriangles = currentVertexCount / 3;
        int totalGrassInstances = totalTriangles * (planet.grassSettings != null ? planet.grassSettings.grassPerTriangle : 0);
        int dynamicTotalInstances = dataContainer.isInitialized ? Mathf.Min(totalGrassInstances, dataContainer.maxInstances) : 0;



        if (currentVertexCount == 0) return;
        if (!dataContainer.isInitialized || dynamicTotalInstances == 0 || planet.grassSettings == null) return;


        Vector3 currentCameraPosition = Vector3.zero;

        if (CameraManager.Instance != null)
        {
            mainCamera = CameraManager.Instance.GetActiveCamera();
            currentCameraPosition = CameraManager.Instance.GetCurrentCameraPosition();
        }
        else
        {
            mainCamera = Camera.main;
            currentCameraPosition = (mainCamera != null) ? mainCamera.transform.position : Vector3.zero;
        }

        if (mainCamera == null) return;


        Vector3 currentPlayerPosition = Vector3.zero;

        if (currentPlayerTransform != null)
        {
            currentPlayerPosition = currentPlayerTransform.position;
        }


        mp0.SetVector("_TestPlayerPos", currentPlayerPosition);
        mp1.SetVector("_TestPlayerPos", currentPlayerPosition);
        mp2.SetVector("_TestPlayerPos", currentPlayerPosition);



        GeometryUtility.CalculateFrustumPlanes(mainCamera, cachedPlanes);



        for (int i = 0; i < 6; i++) cachedVectors[i] = new Vector4(cachedPlanes[i].normal.x, cachedPlanes[i].normal.y, cachedPlanes[i].normal.z, cachedPlanes[i].distance);
        dataContainer.planesBuffer.SetData(cachedVectors);


        dataContainer.cullBufLOD0.SetCounterValue(0);
        dataContainer.cullBufLOD1.SetCounterValue(0);
        dataContainer.cullBufLOD2.SetCounterValue(0);

        int threadGroups = Mathf.CeilToInt(dynamicTotalInstances / 64f);

        var settings = planet.grassSettings;
        var shape = planet.shapeSettings;
        float calculatedWaterRadius = shape.planetRadius * shape.waterRadiusMultiplier;


        cs.SetFloat("_WaterRadius", calculatedWaterRadius);
        cs.SetInt("_GrassPerTriangle", settings.grassPerTriangle);
        cs.SetInt("_GrassPerTriangle", settings.grassPerTriangle);
        cs.SetFloat("_BaseScaleXZ", settings.grassScaleXZ);
        cs.SetFloat("_BaseScaleY", settings.grassScaleY);
        cs.SetMatrix("_LocalToWorldMatrix", localToWorldMatrix);


        cs.SetVector("_CameraPosition", currentCameraPosition);
        cs.SetInt("_TotalInstances", dynamicTotalInstances);


        cs.SetFloat("_LOD1DistSqr", settings.grassLod1Dist * settings.grassLod1Dist);
        cs.SetFloat("_LOD2DistSqr", settings.grassLod2Dist * settings.grassLod2Dist);
        cs.SetFloat("_CullRadius", settings.grassCullRadius);

        cs.SetFloat("_GrassDensityThreshold", settings.grassDensityThreshold);
        cs.SetFloat("_GrassNoiseScale", settings.grassNoiseScale);
        cs.SetFloat("_GrassSlopeThreshold", settings.grassSlopeThreshold);
        cs.SetVector("_PlanetFaceUp", localUp);
        cs.SetVector("_PlanetWorldCenter", planet.transform.position);


        cs.SetBuffer(cachedMainKernelID, "_PlanetVertices", dataContainer.planetVerticesBuffer);
        cs.SetBuffer(cachedMainKernelID, "_PlanesBuf", dataContainer.planesBuffer);
        cs.SetBuffer(cachedMainKernelID, "_CullBufLOD0", dataContainer.cullBufLOD0);
        cs.SetBuffer(cachedMainKernelID, "_CullBufLOD1", dataContainer.cullBufLOD1);
        cs.SetBuffer(cachedMainKernelID, "_CullBufLOD2", dataContainer.cullBufLOD2);


        if (dynamicTotalInstances > 0 && threadGroups > 0)
        {
            cs.Dispatch(cachedMainKernelID, threadGroups, 1, 1);
        }

        GraphicsBuffer.CopyCount(dataContainer.cullBufLOD0, dataContainer.commandBufLOD0, sizeof(uint));
        GraphicsBuffer.CopyCount(dataContainer.cullBufLOD1, dataContainer.commandBufLOD1, sizeof(uint));
        GraphicsBuffer.CopyCount(dataContainer.cullBufLOD2, dataContainer.commandBufLOD2, sizeof(uint));


        Graphics.RenderMeshIndirect(renderParamsLOD0, lod0, dataContainer.commandBufLOD0);
        Graphics.RenderMeshIndirect(renderParamsLOD1, lod1, dataContainer.commandBufLOD1);
        Graphics.RenderMeshIndirect(renderParamsLOD2, lod2, dataContainer.commandBufLOD2);
    }


    public void Shutdown()
    {
        dataContainer.Release();
    }
}
