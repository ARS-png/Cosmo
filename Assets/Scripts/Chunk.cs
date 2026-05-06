


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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



    //Constructor
    public Chunk(Chunk[] children, Chunk parent, Vector3 position, float radius, int detailLevel, Vector3 localUp, Vector3 axisA, Vector3 axisB, Planet planetScript)
    {
        this.children = children;
        this.parent = parent;
        this.position = position;
        this.normalizedPos = (Vector3)position.normalized;
        this.radius = radius;
        this.detailLevel = detailLevel;
        this.localUp = localUp;
        this.axisA = axisA;
        this.axisB = axisB;
        this.planetScript = planetScript;

    }


    public void GenerateChildren() //position?Q??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    {

        //дистанция высчитвается до центра планеты а не до конкретного меша!
        Stack<Chunk> stack = new Stack<Chunk>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            Chunk current = stack.Pop();

   
            if (current.detailLevel < 3 && current.detailLevel >= 0)
            {
                //another fuck
                float dist = Vector3.Distance(current.position.normalized * planetScript.size, planetScript.testGO.transform.position); //spaceship or player positon


                Debug.Log("dstDif:" + dist); //дистанция не учитывает смещение центра планеты


                if (dist <= Planet.detailLevelDistances[current.detailLevel])
                {
                    float halfRadius = current.radius * 0.5f;
                    current.children = new Chunk[4];


                
                    float half = current.radius * 0.5f; 
                     //how is the pos damping works???
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



    public IEnumerable<Chunk> GetVisibleChildren() //
    {
        var stack = new Stack<Chunk>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current.children == null || current.children.Length == 0)
            {
                yield return current;
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
        int resolution = 8; //  Ыыыыынести в настройки планеты

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

               
                Vector3 pointOnUnitCube = position + ((percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB) * radius;  //position*

                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

             
         
                float unscaledElevation = planetScript.shapeGenerator.CalculateUnscaledElevation(pointOnUnitSphere);
                float scaledElevation = planetScript.shapeGenerator.GetScaledElevation(unscaledElevation);


                vertices[i] = pointOnUnitSphere * scaledElevation; /*pointOnUnitSphere*/ /** scaledElevation;*/

                // ЗАПОЛНЕНИЕ UV (Данные для шейдера)
                // x - для биомов (из ColorGenerator), y - высота (для градиента)
                uvs[i].x = planetScript.colorGenerator.BiomePercentFromPoint(pointOnUnitSphere);
                uvs[i].y = unscaledElevation;


                if (x != resolution - 1 && y != resolution - 1)
                {
                    triangles[triIndex] = i + vertexOffset;
                    triangles[triIndex + 1] = i + resolution + 1 + vertexOffset;
                    triangles[triIndex + 2] = i + resolution + vertexOffset;

                    triangles[triIndex + 3] = i + vertexOffset;
                    triangles[triIndex + 4] = i + 1 + vertexOffset;
                    triangles[triIndex + 5] = i + resolution + 1 + vertexOffset;
                    triIndex += 6;
                }
            }
        }

        return (vertices, triangles, uvs);
    }

}
