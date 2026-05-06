using System;
using System.Collections.Generic;
using Unity.Cinemachine;
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
    public Planet planetScript;


    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp, float radius, Planet planetScript)
    {
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;
        this.radius = radius;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);

        this.shapeGenerator = shapeGenerator;
        this.planetScript = planetScript;
    }



    public void ConstructTree()
    {
        vertices.Clear();
        triangles.Clear();


        //Generate chunks

        //fuck
        Chunk parentChunk = new Chunk(new Chunk[0], null, localUp, 1, 0, localUp, axisA, axisB, planetScript);
        parentChunk.GenerateChildren();

        //Get Chunk Mesh Data
        int vertexOffset = 0;
        foreach (Chunk visibleChild in parentChunk.GetVisibleChildren())
        {
            (Vector3[], int[], Vector2[] uvs) verticesAndTriangles = visibleChild.CalculateVerticesAndTriangles(vertexOffset);
            vertices.AddRange(verticesAndTriangles.Item1);
            triangles.AddRange(verticesAndTriangles.Item2);
            vertexOffset += verticesAndTriangles.Item1.Length;
        }


        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
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


    public void UpdateUVs(ColorGenerator colorGenerator)
    {
        Vector2[] uv = mesh.uv;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;

                Vector2 percent = new Vector2(x, y) / (resolution - 1);

                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;

                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;



                uv[i].x = colorGenerator.BiomePercentFromPoint(pointOnUnitSphere);
            }
        }

        mesh.uv = uv;
    }
}


