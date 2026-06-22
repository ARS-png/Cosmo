using System.Collections.Generic;
using System.Linq;
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

    public GraphicsBuffer faceVerticesBuffer { get; private set; }//


    public Chunk parentChunk;

    private int chunkRes;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct GrassVertexData
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uvs;
    }
    private VegetationRenderer vegetationRenderer;

    private List<GrassVertexData> maxDetailVertexData = new List<GrassVertexData>();

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp, float radius, Planet planetScript, GameObject meshHolder, int chunkRes)
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

        vegetationRenderer = new VegetationRenderer(planetScript, localUp);

        this.chunkRes = chunkRes;
    }

    public void ConstructTree()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        visibleChildren.Clear();

        parentChunk = new Chunk(1, this, new Chunk[0], null, localUp, 1, 0, localUp, axisA, axisB, planetScript, 0, chunkRes);
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

        GetMaxDetailedChildren();
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

        GetMaxDetailedChildren();
    }


    private void GetMaxDetailedChildren()
    {
        maxDetailVertexData.Clear();
        int totalTrianglesCount = 0;



        int totalLeaves = 0;
        int nullVerticesChunks = 0;

        var leaves = parentChunk.GetVisibleMaxDetailLeaves();
        if (leaves != null)
        {
            totalLeaves = leaves.Count();
        }

        foreach (Chunk visibleChild in leaves)
        {
            if (visibleChild.vertices == null || visibleChild.vertices.Length == 0)
            {
                nullVerticesChunks++;
            }

            if (visibleChild.vertices == null || visibleChild.triangles == null) continue;

            Vector3[] chunkVertices = visibleChild.vertices;
            Vector3[] chunkNormals = visibleChild.normals;
            Vector2[] chunkUVs = visibleChild.uvs;
            int[] chunkTriangles = visibleChild.triangles;

            for (int i = 0; i < chunkTriangles.Length; i++)
            {
                int vertexIndex = chunkTriangles[i];
                if (vertexIndex >= chunkVertices.Length) continue;

                GrassVertexData data;
                data.position = chunkVertices[vertexIndex];
                data.normal = (chunkNormals != null && chunkNormals.Length > vertexIndex)
                    ? chunkNormals[vertexIndex]
                    : chunkVertices[vertexIndex].normalized;

                data.uvs = (chunkUVs != null && chunkUVs.Length > vertexIndex)
                    ? chunkUVs[vertexIndex]
                    : Vector2.zero;

                maxDetailVertexData.Add(data);
            }

            totalTrianglesCount += chunkTriangles.Length / 3;
        }




        UpdateFaceVerticesBuffer(maxDetailVertexData);

    }

    private void UpdateFaceVerticesBuffer(List<GrassVertexData> vertexData)
    {
        if (vertexData == null || vertexData.Count == 0)
        {
            ReleaseFaceBuffer();
            return;
        }


        if (faceVerticesBuffer == null || faceVerticesBuffer.count != vertexData.Count)
        {
            ReleaseFaceBuffer();


            faceVerticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexData.Count, sizeof(float) * 8);
        }


        faceVerticesBuffer.SetData(vertexData);
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

            if (meshHolder != null && meshHolder.TryGetComponent<MeshCollider>(out var meshCollider))
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;
            }
        }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
    }



    public void InitializeGrass(int instancesCount)
    {
        if (planetScript.vegetationSettings != null && planetScript.vegetationSettings.vegetationTypes != null)
        {

            vegetationRenderer.Initialize(
                planetScript.vegetationSettings.vegetationTypes,
                instancesCount,
                planetScript.transform.position
            );
        }
    }



    public void RenderGrass()
    {
        // Если буфер геометрии чанка пустой — рисовать нечего
        if (faceVerticesBuffer == null || faceVerticesBuffer.count == 0) return;

        // Берём матрицу трансформации у планеты
        Matrix4x4 localMatrix = planetScript.transform.localToWorldMatrix;

        // Вызываем метод Render БЕЗ параметра totalInstances
        vegetationRenderer.Render(
            planetScript.vegetationSettings.vegetationTypes,
            faceVerticesBuffer,
            localMatrix
        );
    }


    public void ReleaseFaceBuffer()
    {
        if (faceVerticesBuffer != null)
        {
            faceVerticesBuffer.Release();
            faceVerticesBuffer = null;
        }
    }

    public void ReleaseVegetationBuffers()
    {
        // Если в VegetationRenderer нет метода Shutdown, можете закомментировать эту строчку
        // vegetationRenderer?.Shutdown(); 
        maxDetailVertexData?.Clear();
        ReleaseFaceBuffer();
    }

}
