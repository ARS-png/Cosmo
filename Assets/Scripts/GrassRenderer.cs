using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GrassRenderer
{
    private GrassDataContainer dataContainer;
    private Planet planet;
    private Vector3 localUp;
    private Matrix4x4 localToWorldMatrix;

    private RenderParams renderParamsLOD0;
    private RenderParams renderParamsLOD1;
    private RenderParams renderParamsLOD2;

    private Camera mainCamera;
    private readonly Plane[] cachedPlanes = new Plane[6];
    private readonly Vector4[] cachedVectors = new Vector4[6];

    ComputeShader cs;

    public GrassRenderer(Planet planet, Vector3 localUp)
    {
        this.planet = planet;
        this.localUp = localUp;
        this.dataContainer = new GrassDataContainer();
        this.mainCamera = Camera.main;

        cs = planet.grassSettings.grassComputeShader;
    }

    public void Initialize(Mesh lod0, Mesh lod1, Mesh lod2, Material material, int maxInstances, Vector3 planetCenter)
    {


        dataContainer.Initialize(
            maxInstances,
            (uint)lod0.GetIndexCount(0),
            (uint)lod1.GetIndexCount(0),
            (uint)lod2.GetIndexCount(0)
        );

        MaterialPropertyBlock mp0 = new MaterialPropertyBlock(); mp0.SetBuffer("_CullBuf", dataContainer.cullBufLOD0);
        MaterialPropertyBlock mp1 = new MaterialPropertyBlock(); mp1.SetBuffer("_CullBuf", dataContainer.cullBufLOD1);
        MaterialPropertyBlock mp2 = new MaterialPropertyBlock(); mp2.SetBuffer("_CullBuf", dataContainer.cullBufLOD2);

        float planetBoundsSize = planet.shapeSettings.planetRadius * 5f;
        Bounds b = new Bounds(planetCenter, Vector3.one * planetBoundsSize);

        renderParamsLOD0 = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = b, matProps = mp0 };
        renderParamsLOD1 = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = b, matProps = mp1 };
        renderParamsLOD2 = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = b, matProps = mp2 };
    }

    public void UpdateGeometry(List<Vector3> vertices, Mesh unityMesh, Matrix4x4 localMatrix)
    {
        if (!dataContainer.isInitialized || vertices.Count == 0 || planet.grassSettings == null) return;

        this.localToWorldMatrix = localMatrix;

        var gvd = new TerrainFace.GrassVertexData[vertices.Count];
        Vector3[] normals = unityMesh.normals;

        for (int i = 0; i < vertices.Count; i++)
        {
            gvd[i].position = vertices[i];
            gvd[i].normal = (normals != null && normals.Length > i) ? normals[i] : vertices[i].normalized;
        }

        dataContainer.UpdateVerticesBuffer(vertices.Count, gvd);

     
        ComputeShader cs = planet.grassSettings.grassComputeShader;
        if (cs == null) return;

        int mainKernel = cs.FindKernel("CSMain");
        int dynamicTotalInstances = Mathf.Min(vertices.Count, dataContainer.maxInstances);
        int threadGroups = Mathf.CeilToInt(dynamicTotalInstances / 64f);

       
        cs.SetBuffer(mainKernel, "_TransformBuf", dataContainer.transformBuffer);
        cs.SetBuffer(mainKernel, "_PlanetVertices", dataContainer.planetVerticesBuffer);

        
        var settings = planet.grassSettings;
        cs.SetInt("_TotalInstances", dynamicTotalInstances);
        cs.SetFloat("_BaseScaleXZ", settings.grassScaleXZ);
        cs.SetFloat("_BaseScaleY", settings.grassScaleY);

       
        cs.SetFloat("_WaterRadius", planet.shapeSettings.waterRadiusMultiplier * planet.shapeSettings.planetRadius);
        cs.SetMatrix("_LocalToWorldMatrix", localToWorldMatrix);

        if (dynamicTotalInstances > 0)
        {
            cs.Dispatch(mainKernel, threadGroups, 1, 1);
        }
    }

    public void Render(Mesh lod0, Mesh lod1, Mesh lod2, int vertexCount)
    {
        if (!dataContainer.isInitialized || vertexCount == 0 || planet.grassSettings == null) return;

        if (mainCamera == null) { mainCamera = Camera.main; if (mainCamera == null) return; }

        
 
        if (cs == null) return;

        int cullKernel = cs.FindKernel("CSCull");

        GeometryUtility.CalculateFrustumPlanes(mainCamera, cachedPlanes);
        for (int i = 0; i < 6; i++) cachedVectors[i] = new Vector4(cachedPlanes[i].normal.x, cachedPlanes[i].normal.y, cachedPlanes[i].normal.z, cachedPlanes[i].distance);
        dataContainer.planesBuffer.SetData(cachedVectors);

        dataContainer.cullBufLOD0.SetCounterValue(0);
        dataContainer.cullBufLOD1.SetCounterValue(0);
        dataContainer.cullBufLOD2.SetCounterValue(0);

        int dynamicTotalInstances = Mathf.Min(vertexCount, dataContainer.maxInstances);
        int cullThreadGroups = Mathf.CeilToInt(dynamicTotalInstances / 64f);

     
        var settings = planet.grassSettings;
        cs.SetVector("_CameraPosition", mainCamera.transform.position);
        cs.SetFloat("_LOD1DistSqr", settings.grassLod1Dist * settings.grassLod1Dist);
        cs.SetFloat("_LOD2DistSqr", settings.grassLod2Dist * settings.grassLod2Dist);
        cs.SetFloat("_Radius", settings.grassCullRadius);
        cs.SetInt("_TotalInstances", dynamicTotalInstances);

        cs.SetBuffer(cullKernel, "_TransformBuf", dataContainer.transformBuffer);
        cs.SetBuffer(cullKernel, "_PlanesBuf", dataContainer.planesBuffer);
        cs.SetBuffer(cullKernel, "_CullBufLOD0", dataContainer.cullBufLOD0);
        cs.SetBuffer(cullKernel, "_CullBufLOD1", dataContainer.cullBufLOD1);
        cs.SetBuffer(cullKernel, "_CullBufLOD2", dataContainer.cullBufLOD2);

        cs.Dispatch(cullKernel, cullThreadGroups, 1, 1);

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
