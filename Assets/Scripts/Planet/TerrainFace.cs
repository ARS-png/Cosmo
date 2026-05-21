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

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp, float radius, Planet planetScript, GameObject meshHolder)
    {
        this.mesh = mesh;
        this.resolution = resolution; //not need more
        this.localUp = localUp;
        this.radius = radius;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);

        this.shapeGenerator = shapeGenerator;
        this.planetScript = planetScript;

        this.meshHolder = meshHolder;
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

        // Обновляем меш
        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();


        TryBakeMesh();
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
        }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext()); //Выполняет в основном потоке
    }
}


