using UnityEngine;

public class DrawGrass : MonoBehaviour
{
    public Material material;
    public Mesh mesh;
    public ComputeShader computeShader;

    [SerializeField] private float spacing = 0.3f;
    [SerializeField] int gridCount;
    [HideInInspector] int sqrGridCount;
    [SerializeField] private float baseScaleXZ = 1.0f;
    [SerializeField] private float baseScaleY = 1.0f;
    [SerializeField] private float cullRad = 1;


    int mainKernelID;
    int cullKernelID;
    MaterialPropertyBlock matProps;

    GraphicsBuffer commandBuf;
    GraphicsBuffer transformBuf;
    
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    GraphicsBuffer planesBuffer;
    GraphicsBuffer cullBuf;
    const int commandCount = 1;
    private int threadGroups;


  
    void Start()
    {

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);//


        Vector4[] PlaneVectors = new Vector4[6];
        for (int i = 0; i < 6; i++)
        {
            Plane p = frustumPlanes[i];
            PlaneVectors[i] = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
        }


        sqrGridCount = gridCount * gridCount;


        mainKernelID = computeShader.FindKernel("CSMain");
        cullKernelID = computeShader.FindKernel("CSCull");




        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        transformBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, sqrGridCount, sizeof(float) * 4 * 4);
        planesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 4);
        cullBuf = new GraphicsBuffer(GraphicsBuffer.Target.Append, sqrGridCount, sizeof(float) * 4 * 4);



        computeShader.SetBuffer(mainKernelID, "_CommandBuf", commandBuf);
        computeShader.SetBuffer(mainKernelID, "_TransformBuf", transformBuf);
        computeShader.SetInt("_IndexCount", (int)mesh.GetIndexCount(0));


        computeShader.SetInt("_GridCount", gridCount);
        computeShader.SetInt("_SqrGridCount", sqrGridCount);
        computeShader.SetFloat("_Spacing", spacing);


        computeShader.SetFloat("_BaseScaleXZ", baseScaleXZ);
        computeShader.SetFloat("_BaseScaleY", baseScaleY);

        computeShader.SetFloat("_Radius", cullRad); //


        planesBuffer.SetData(PlaneVectors);
        computeShader.SetBuffer(cullKernelID, "_PlanesBuf", planesBuffer); //
        computeShader.SetBuffer(cullKernelID, "_CullBuf", cullBuf);

        matProps = new MaterialPropertyBlock();


        threadGroups = Mathf.CeilToInt(sqrGridCount / 64f);
        computeShader.Dispatch(mainKernelID, threadGroups, 1, 1);
    }



    void Update()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);//


        Vector4[] PlaneVectors = new Vector4[6];
        for (int i = 0; i < 6; i++)
        {
            Plane p = frustumPlanes[i];
            PlaneVectors[i] = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
        }
        planesBuffer.SetData(PlaneVectors);

        RenderParams rp = new RenderParams(material);

        rp.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; //shadows
        rp.receiveShadows = true;

        rp.worldBounds = new Bounds(Vector3.zero, new Vector3(2000f, 500f, 2000f));
        rp.matProps = matProps;


        computeShader.SetBuffer(cullKernelID, "_TransformBuf", transformBuf);


        cullBuf.SetCounterValue(0);

        computeShader.Dispatch(cullKernelID, threadGroups, 1, 1);
        GraphicsBuffer.CopyCount(cullBuf, commandBuf, 4);  //Copy the counter value to another buffer


        rp.matProps.SetBuffer("_CullBuf", cullBuf);


        Graphics.RenderMeshIndirect(rp, mesh, commandBuf, commandCount);

    }


    void OnDestroy() => ClearBuffers();
  

    private void ClearBuffers()
    {
        commandBuf?.Release();
        commandBuf = null;

        transformBuf?.Release();
        transformBuf = null;

        planesBuffer?.Release();
        planesBuffer = null;

        cullBuf?.Release();
        cullBuf = null;
    }
}
