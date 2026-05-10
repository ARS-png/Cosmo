
using System.Collections.Generic;
using UnityEngine;


public class Chunk
{
    public Chunk[] children;
    public Chunk parent;
    public Vector3 position;
    public float radius;
    public int detailLevel;
    public Vector3 localUp;
    public Vector3 axisA;
    public Vector3 axisB;
    private Chunk[] chunks;
    public Vector3 normalizedPos;
    Planet planetScript;

    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
    public Vector2[] uvs;
    

    private Stack<Chunk> stack = new Stack<Chunk>();

    public byte corner;

    //Constructor
    public Chunk(Chunk[] children, Chunk parent, Vector3 position, float radius, int detailLevel, Vector3 localUp, Vector3 axisA, Vector3 axisB, Planet planetScript)
    {
        this.children = children;
        this.parent = parent;
        this.position = position;
        this.normalizedPos = position.normalized;
        this.radius = radius;
        this.detailLevel = detailLevel;
        this.localUp = localUp;
        this.axisA = axisA;
        this.axisB = axisB;
        this.planetScript = planetScript;

    }




    public void GenerateChildren()
    {
        stack.Clear();

        stack.Push(this);

        while (stack.Count > 0)
        {
            Chunk current = stack.Pop();


            if (current.detailLevel < Planet.maxDetailLevel && current.detailLevel >= 0) //можно потом изменить на большее расстояние
            {

                Vector3 worldSurfacePos = planetScript.transform.TransformPoint(current.position.normalized * planetScript.radius);


                float sqrDist = (worldSurfacePos - planetScript.player.transform.position).sqrMagnitude;


                if (sqrDist <= Planet.GetSqrDistance(current.detailLevel))
                {
                    float halfRadius = current.radius * 0.5f;
                    current.children = new Chunk[4];


                    float half = current.radius * 0.5f;

                    current.children[0] = new Chunk(new Chunk[0], current, current.position + current.axisA * half - current.axisB * half, half, current.detailLevel + 1, current.localUp, current.axisA, current.axisB, current.planetScript);
                    current.children[1] = new Chunk(new Chunk[0], current, current.position + current.axisA * half + current.axisB * half, half, current.detailLevel + 1, current.localUp, current.axisA, current.axisB, current.planetScript);
                    current.children[2] = new Chunk(new Chunk[0], current, current.position - current.axisA * half + current.axisB * half, half, current.detailLevel + 1, current.localUp, current.axisA, current.axisB, current.planetScript);
                    current.children[3] = new Chunk(new Chunk[0], current, current.position - current.axisA * half - current.axisB * half, half, current.detailLevel + 1, current.localUp, current.axisA, current.axisB, current.planetScript);



                    foreach (Chunk child in current.children)
                    {
                        stack.Push(child);
                    }
                }
            }
        }
    }


    public IEnumerable<Chunk> GetVisibleChildren()
    {
        stack.Clear();

        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current.children == null || current.children.Length == 0)
            {
                float b = Vector3.Distance(normalizedPos * planetScript.radius + planetScript.transform.position, planetScript.player.transform.position);

                float r = planetScript.radius;
                float d = planetScript.distanceToPlayer;

                Vector3 surfacePoint = planetScript.transform.position + (position.normalized * r);


                float c = Vector3.Distance(surfacePoint, planetScript.player.transform.position);

                float cosAngle = (r * r + d * d - c * c) / (2 * r * d);

                cosAngle = Mathf.Clamp(cosAngle, -1f, 1f);


                if (Mathf.Acos(cosAngle) < planetScript.cullingMinAngle)
                {
                    yield return current;
                }
            }

            else
            {
                foreach (Chunk child in current.children)
                {

                    stack.Push(child);
                }
            }
        }
    }


    public (Vector3[] vertices, int[] triangles, Vector2[] uvs) CalculateVerticesAndTriangles(int vertexOffset)
    {
        int resolution = 16; //  Ыыыыынести в настройки планеты

        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector2[] uvs = new Vector2[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;

                Vector2 percent = new Vector2(x, y) / (resolution - 1);


                Vector3 pointOnUnitCube = position + ((percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB) * radius;

                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;



                float unscaledElevation = planetScript.shapeGenerator.CalculateUnscaledElevation(pointOnUnitSphere);
                float scaledElevation = planetScript.shapeGenerator.GetScaledElevation(unscaledElevation);


                vertices[i] = pointOnUnitSphere * scaledElevation;

                // ЗАПОЛНЕНИЕ UV (Данные для шейдера)
                // x - для биомов (из ColorGenerator), y - высота (для градиента)
                uvs[i].x = planetScript.colorGenerator.BiomePercentFromPoint(pointOnUnitSphere);
                uvs[i].y = unscaledElevation;


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

        this.vertices = vertices;
        this.triangles = triangles;
        this.uvs = uvs;

        return (vertices, GetTrianglesWithOffset(vertexOffset), uvs);
    }

    public int[] GetTrianglesWithOffset(int triangleOffset)
    {
        int[] newTriangles = new int[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            newTriangles[i] = triangles[i] + triangleOffset;
        }

        return newTriangles;
    }

    public bool UpdateChunk()
    {
        stack.Clear();
        stack.Push(this);

        bool anyChanged = false;

        while (stack.Count > 0)
        {
            Chunk current = stack.Pop();


            Vector3 worldSurfacePos = planetScript.transform.TransformPoint(current.position.normalized * planetScript.radius);
            float distSq = (worldSurfacePos - planetScript.player.transform.position).sqrMagnitude;
            float thresholdSq = Planet.sqrDetailDistances[current.detailLevel];

            if (distSq <= thresholdSq && current.detailLevel < Planet.maxDetailLevel)
            {
                if (current.children == null || current.children.Length == 0)
                {
                    current.GenerateChildren();
                    anyChanged = true;
                }
            }
            else if (distSq > thresholdSq)
            {
                if (current.children != null && current.children.Length > 0)
                {
                    current.children = null;
                    anyChanged = true;
                }
            }


            if (current.children != null)
            {
                foreach (Chunk child in current.children)
                {
                    stack.Push(child);
                }
            }
        }


        return anyChanged;
    }



    public void GetNeighbourLOD()
    {
        bool[] newNeighbours = new bool[4];

        //if (corner)
        //{

        //}
    }

}
