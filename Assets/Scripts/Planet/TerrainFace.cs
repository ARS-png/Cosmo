using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainFace
{
    Mesh mesh;
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;

    float radius;

    ShapeGenerator shapeGenerator;

    public GameObject meshHolder;

    public Planet planetScript;

    public List<Chunk> visibleChildren = new List<Chunk>();

    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    public Chunk parentChunk;

    #region ПЕРЕМЕННЫЕ ДЛЯ СИСТЕМЫ ТРАВЫ
    public struct GrassVertexData
    {
        public Vector3 position;
        public Vector3 normal;
    }
    private GrassRenderer grassModule;
    // Единственная лаконичная ссылка на отдельный модуль травы


    private bool grassInitialized = false;
    private int totalGrassInstances = 0; // Теперь задается динамически из vertices.Count
    private int grassThreadGroups;

    private GraphicsBuffer transformBuf;
    private GraphicsBuffer planesBuffer;
    private GraphicsBuffer planetVerticesBuffer;

    private GraphicsBuffer cullBufLOD0;
    private GraphicsBuffer cullBufLOD1;
    private GraphicsBuffer cullBufLOD2;

    private GraphicsBuffer commandBufLOD0;
    private GraphicsBuffer commandBufLOD1;
    private GraphicsBuffer commandBufLOD2;

    private RenderParams renderParamsLOD0;
    private RenderParams renderParamsLOD1;
    private RenderParams renderParamsLOD2;

    private Camera mainCamera;
    private readonly Plane[] cachedPlanes = new Plane[6];
    private readonly Vector4[] cachedVectors = new Vector4[6];
    #endregion

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp, float radius, Planet planetScript, GameObject meshHolder)
    {
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;
        this.radius = radius;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);

        this.shapeGenerator = shapeGenerator;
        this.planetScript = planetScript;
        this.meshHolder = meshHolder;

        grassModule = new GrassRenderer(planetScript, localUp);

    }
    public void ConstructTree()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        visibleChildren.Clear();

        parentChunk = new Chunk(1, this, new Chunk[0], null, localUp, 1, 0, localUp, axisA, axisB, planetScript, 0);
        parentChunk.GenerateChildren();

        int vertexOffset = 0;
        foreach (Chunk visibleChild in parentChunk.GetVisibleChildren())
        {
            var data = visibleChild.CalculateVerticesAndTriangles(vertexOffset);

            vertices.AddRange(data.vertices);
            triangles.AddRange(data.triangles);
            uvs.AddRange(data.uvs);

            vertexOffset += data.vertices.Length;
        }

        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        TryBakeMesh();
        //UpdateGrassGeometryOnGPU();
        grassModule.UpdateGeometry(vertices, mesh, planetScript.transform.localToWorldMatrix);
    }

    public void UpdateTree()
    {
        if (!parentChunk.UpdateChunk())
        {
            return;
        }

        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        visibleChildren.Clear();

        int vertexOffset = 0;
        foreach (Chunk visibleChild in parentChunk.GetVisibleChildren())
        {
            (Vector3[] vertices, int[] triangles, Vector2[] uvs) data;

            if (visibleChild.vertices == null || visibleChild.vertices.Length == 0)
            {
                data = visibleChild.CalculateVerticesAndTriangles(vertexOffset);
            }
            else
            {
                data = (visibleChild.vertices, visibleChild.GetTrianglesWithOffset(vertexOffset), visibleChild.uvs);
            }

            vertices.AddRange(data.vertices);
            triangles.AddRange(data.triangles);
            uvs.AddRange(data.uvs);

            vertexOffset += data.vertices.Length;
        }

        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        TryBakeMesh();
        //UpdateGrassGeometryOnGPU();
        grassModule.UpdateGeometry(vertices, mesh, planetScript.transform.localToWorldMatrix);
    }
    public void ConstructWaterMesh(float planetRadius, float waterRadiusMultiplier)
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                vertices[i] = pointOnUnitSphere * planetRadius * waterRadiusMultiplier;

                if (x != resolution - 1 && y != resolution - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;

                    triIndex += 6;
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private void TryBakeMesh()
    {
        if (mesh == null || mesh.vertexCount == 0) return;

        UnityEngine.EntityId meshID = mesh.GetEntityId();

        System.Threading.Tasks.Task.Run(() =>
        {
            Physics.BakeMesh(meshID, false);
        }).ContinueWith(t =>
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (meshHolder != null && meshHolder.TryGetComponent<MeshCollider>(out var meshCollider))
                {
                    meshCollider.sharedMesh = null;
                    meshCollider.sharedMesh = mesh;
                }
            };
        }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void InitializeGrass(ComputeShader computeShader, Material grassMaterial, Mesh grassMeshLOD0, Mesh grassMeshLOD1, Mesh grassMeshLOD2, int instancesCount)
    {
        // Делегируем инициализацию буферов модулю травы
        grassModule.Initialize(grassMeshLOD0, grassMeshLOD1, grassMeshLOD2, grassMaterial, instancesCount, planetScript.transform.position);
        grassModule.UpdateGeometry(vertices, mesh, planetScript.transform.localToWorldMatrix);
    }

    public void RenderGrass(Mesh grassMeshLOD0, Mesh grassMeshLOD1, Mesh grassMeshLOD2)
    {
        // Делегируем отрисовку модулю травы
        grassModule.Render(grassMeshLOD0, grassMeshLOD1, grassMeshLOD2, vertices.Count);
    }

    public void ReleaseGrassBuffers()
    {
        // Делегируем очистку памяти GPU
        grassModule.Shutdown();
    }
}
