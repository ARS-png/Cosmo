
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
    public uint hashValue;

    public bool[] neighbours = new bool[4];//East, west, north, south. True if less detailed (Lower LOD).

    public TerrainFace terrainFace;



    /// <summary> 
    /// Neighbour chunk indexer. 
    /// </summary>
    public static class Direction
    {
        public const int East = 0, West = 1, North = 2, South = 3;
        /// <summary> East = 0, West = 1, North = 2, South = 3 </summary>
        public const int E = 0, W = 1, N = 2, S = 3;
    }


    /// <summary> 
    /// Child chunk indexer. 
    /// </summary>
    public static class Quadrant
    {
        public const int NorthWest = 0, NorthEast = 1, SouthEast = 2, SouthWest = 3;
        /// <summary> North West = 0, North East = 1, South East = 2, South West = 3 </summary>
        public const int NW = 0, NE = 1, SE = 2, SW = 3;
    }


    //Constructor
    public Chunk(uint hashValue, TerrainFace terrainFace, Chunk[] children, Chunk parent, Vector3 position, float radius, int detailLevel, Vector3 localUp, Vector3 axisA, Vector3 axisB, Planet planetScript, byte corner)
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
        this.corner = corner;
        this.hashValue = hashValue;
        this.terrainFace = terrainFace;
    }


    public void GenerateChildren()
    {
        stack.Clear();

        stack.Push(this);

        while (stack.Count > 0)
        {
            Chunk current = stack.Pop();


            if (current.detailLevel < Planet.maxDetailLevel && current.detailLevel >= 0)
            {

                Vector3 worldSurfacePos = planetScript.transform.TransformPoint(current.position.normalized * planetScript.radius);


                float sqrDist = (worldSurfacePos - planetScript.player.transform.position).sqrMagnitude;


                if (sqrDist <= Planet.GetSqrDistance(current.detailLevel))
                {
                    float halfRadius = current.radius * 0.5f;
                    current.children = new Chunk[4];


                    float half = current.radius * 0.5f;

                    current.children[0] = new Chunk(hashValue * 4, terrainFace, new Chunk[0], current, current.position + current.axisA * half - current.axisB * half, half, current.detailLevel + 1, current.localUp, current.axisA, current.axisB, current.planetScript, Quadrant.NW); //top left
                    current.children[1] = new Chunk(hashValue * 4 + 1, terrainFace, new Chunk[0], current, current.position + current.axisA * half + current.axisB * half, half, current.detailLevel + 1, current.localUp, current.axisA, current.axisB, current.planetScript, Quadrant.NE); //top right
                    current.children[2] = new Chunk(hashValue * 4 + 2, terrainFace, new Chunk[0], current, current.position - current.axisA * half + current.axisB * half, half, current.detailLevel + 1, current.localUp, current.axisA, current.axisB, current.planetScript, Quadrant.SE); //bottom right
                    current.children[3] = new Chunk(hashValue * 4 + 3, terrainFace, new Chunk[0], current, current.position - current.axisA * half - current.axisB * half, half, current.detailLevel + 1, current.localUp, current.axisA, current.axisB, current.planetScript, Quadrant.SW); //bottom left


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
        Stack<Chunk> localStack = new Stack<Chunk>();
        localStack.Push(this);

        while (localStack.Count > 0)
        {
            var current = localStack.Pop();

            if (current.children == null || current.children.Length == 0)
            {
                float r = planetScript.radius;
                float d = planetScript.distanceToPlayer;


                Vector3 chunkSurfacePos = planetScript.transform.position + (current.normalizedPos * r);
                float c = Vector3.Distance(chunkSurfacePos, planetScript.player.transform.position);

                float cosAngle = (r * r + d * d - c * c) / (2 * r * d);
                cosAngle = Mathf.Clamp(cosAngle, -1f, 1f);

                float angleToChunk = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;


                if (angleToChunk < planetScript.cullingMinAngle)
                {
                    yield return current;
                }
            }
            else
            {
                foreach (Chunk child in current.children)
                {
                    localStack.Push(child);
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


    #region I_CANT_DO_THIS_SHIT




    public void GetNeighbourLOD()
    {
        bool[] newNeighbours = new bool[4];

        if (corner == Quadrant.NorthWest) // Top left
        {
            newNeighbours[Direction.West] = CheckNeighbourLOD(Direction.West, hashValue); // West
            newNeighbours[Direction.North] = CheckNeighbourLOD(Direction.North, hashValue); // North
        }
        else if (corner == Quadrant.NorthEast) // Top right
        {
            newNeighbours[Direction.East] = CheckNeighbourLOD(Direction.East, hashValue); // East
            newNeighbours[Direction.North] = CheckNeighbourLOD(Direction.North, hashValue); // North
        }
        else if (corner == Quadrant.SouthEast) // Bottom right
        {
            newNeighbours[Direction.East] = CheckNeighbourLOD(Direction.East, hashValue); // East
            newNeighbours[Direction.South] = CheckNeighbourLOD(Direction.South, hashValue); // South
        }
        else if (corner == Quadrant.SouthWest) // Bottom left
        {
            newNeighbours[Direction.West] = CheckNeighbourLOD(Direction.West, hashValue); // West
            newNeighbours[Direction.South] = CheckNeighbourLOD(Direction.South, hashValue); // South
        }

        neighbours = newNeighbours;
    }



    private bool CheckNeighbourLOD(int direction, uint hash)
    {
        uint bitmask = 0;
        byte count = 0;
        uint localChunkQuadrant;


        while (count < detailLevel * 2) // 0 through 3 can be represented as a two bit number
        {
            count += 2;
            localChunkQuadrant = (hash & 3); // Get the two last bits of the hash. 0b_10011 --> 0b_11

            bitmask = bitmask * 4; // Add zeroes to the end of the bitmask. 0b_10011 --> 0b_1001100

            //Create mask to get the quad on the opposite side. 2 = 0b_10 and generates the mask 0b_11 which flips it to 1 = 0b_01
            if (direction == Direction.North || direction == Direction.South)
            {
                bitmask += 3; // Add 0b_11 to the bitmask
            }
            else
            {
                bitmask += 1; // Add 0b_01 to the bitmask
            }

            //Break if the hash goes in the opposite direction
            if ((direction == Direction.E && (localChunkQuadrant == Quadrant.NW || localChunkQuadrant == Quadrant.SW)) ||
                (direction == Direction.W && (localChunkQuadrant == Quadrant.NE || localChunkQuadrant == Quadrant.SE)) ||
                (direction == Direction.N && (localChunkQuadrant == Quadrant.SW || localChunkQuadrant == Quadrant.SE)) ||
                (direction == Direction.S && (localChunkQuadrant == Quadrant.NW || localChunkQuadrant == Quadrant.NE)))
            {
                break;
            }

            //Remove already processed bits. 0b_1001100-- > 0b_10011
            hash = hash >> 2;
        }

        //Return true if the quad in quadstorage is less detailed. REACH BEYOND THIS FACE IF THE CHUNK IS ON THE FACE'S BORDER.
        return terrainFace.parentChunk.GetNeighbourDetailLevel(hashValue ^ bitmask, detailLevel) < detailLevel;
    }

    //Find the detail level of the neighbouring quad using the querryHash as a map
    public int GetNeighbourDetailLevel(uint querryHash, int dl)
    {
        int dlResult = 0; // dl = detail level

        if (hashValue == querryHash)
        {
            dlResult = detailLevel;
        }
        else
        {
            if (children.Length > 0)
            {
                dlResult += children[((querryHash >> ((dl - 1) * 2)) & 3)].GetNeighbourDetailLevel(querryHash, dl - 1);
            }
        }

        return dlResult; // Returns 0 if no quad with the given hash is found
    }

}

    #endregion