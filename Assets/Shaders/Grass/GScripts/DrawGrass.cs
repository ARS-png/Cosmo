using UnityEngine;

public class DrawGrass : MonoBehaviour
{
    private int mainKernelID;
    private int cullKernelID;

    public Material material;

    [Header("Меши уровней детализации (LOD)")]
    public Mesh meshLOD0;
    public Mesh meshLOD1;
    public Mesh meshLOD2;

    private GraphicsBuffer cullBufLOD0;
    private GraphicsBuffer cullBufLOD1;
    private GraphicsBuffer cullBufLOD2;

    private GraphicsBuffer commandBufLOD0;
    private GraphicsBuffer commandBufLOD1;
    private GraphicsBuffer commandBufLOD2;

    public ComputeShader computeShader;

    [SerializeField] private float spacing = 0.3f;
    [SerializeField] private int gridCount = 100;
    [HideInInspector] private int sqrGridCount;
    [SerializeField] private float baseScaleXZ = 1.0f;
    [SerializeField] private float baseScaleY = 1.0f;
    [SerializeField] private float cullRad = 1.0f;

    private GraphicsBuffer transformBuf;
    private GraphicsBuffer planesBuffer;

    private const int commandCount = 1;
    private int threadGroups;

    private Camera mainCamera;
    private readonly Plane[] cachedPlanes = new Plane[6];
    private readonly Vector4[] cachedVectors = new Vector4[6];

    [Header("Дистанции LOD")]
    public float lod1Distance = 15f;
    public float lod2Distance = 40f;

    private RenderParams renderParamsLOD0;
    private RenderParams renderParamsLOD1;
    private RenderParams renderParamsLOD2;

    private bool isInitialized = false;

    void Start()
    {
        if (meshLOD0 == null || meshLOD1 == null || meshLOD2 == null || computeShader == null || material == null)
        {
            Debug.LogError("DrawGrass: Назначьте все меши LOD, Материал и Compute Shader в инспекторе!", this);
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("DrawGrass: На сцене не найдена камера с тегом MainCamera. Скрипт будет ждать её появления в Update.");
        }

        sqrGridCount = gridCount * gridCount;

        mainKernelID = computeShader.FindKernel("CSMain");
        cullKernelID = computeShader.FindKernel("CSCull");


        commandBufLOD0 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandBufLOD1 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandBufLOD2 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);

        var argsLOD0 = new GraphicsBuffer.IndirectDrawIndexedArgs[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = (uint)meshLOD0.GetIndexCount(0), instanceCount = 0 } };
        var argsLOD1 = new GraphicsBuffer.IndirectDrawIndexedArgs[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = (uint)meshLOD1.GetIndexCount(0), instanceCount = 0 } };
        var argsLOD2 = new GraphicsBuffer.IndirectDrawIndexedArgs[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = (uint)meshLOD2.GetIndexCount(0), instanceCount = 0 } };

        commandBufLOD0.SetData(argsLOD0);
        commandBufLOD1.SetData(argsLOD1);
        commandBufLOD2.SetData(argsLOD2);

    
        transformBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, sqrGridCount, sizeof(float) * 4 * 4);
        planesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 4);

        cullBufLOD0 = new GraphicsBuffer(GraphicsBuffer.Target.Append, sqrGridCount, sizeof(float) * 4 * 4);
        cullBufLOD1 = new GraphicsBuffer(GraphicsBuffer.Target.Append, sqrGridCount, sizeof(float) * 4 * 4);
        cullBufLOD2 = new GraphicsBuffer(GraphicsBuffer.Target.Append, sqrGridCount, sizeof(float) * 4 * 4);

  
        //computeShader.SetBuffer(mainKernelID, "_CommandBuf", commandBufLOD0);
        computeShader.SetBuffer(mainKernelID, "_TransformBuf", transformBuf);
        computeShader.SetInt("_IndexCount", (int)meshLOD0.GetIndexCount(0));
        computeShader.SetInt("_GridCount", gridCount);
        computeShader.SetInt("_SqrGridCount", sqrGridCount);
        computeShader.SetFloat("_Spacing", spacing);
        computeShader.SetFloat("_BaseScaleXZ", baseScaleXZ);
        computeShader.SetFloat("_BaseScaleY", baseScaleY);
        computeShader.SetFloat("_Radius", cullRad);

        threadGroups = Mathf.CeilToInt(sqrGridCount / 64f);




        computeShader.Dispatch(mainKernelID, threadGroups, 1, 1); //

   
        MaterialPropertyBlock matPropsLOD0 = new MaterialPropertyBlock();
        matPropsLOD0.SetBuffer("_CullBuf", cullBufLOD0);

        MaterialPropertyBlock matPropsLOD1 = new MaterialPropertyBlock();
        matPropsLOD1.SetBuffer("_CullBuf", cullBufLOD1);

        MaterialPropertyBlock matPropsLOD2 = new MaterialPropertyBlock();
        matPropsLOD2.SetBuffer("_CullBuf", cullBufLOD2);

        renderParamsLOD0 = CreateRenderParams(matPropsLOD0);
        renderParamsLOD1 = CreateRenderParams(matPropsLOD1);
        renderParamsLOD2 = CreateRenderParams(matPropsLOD2);

 
        UpdateFrustumPlanes();

        isInitialized = true; //?
    }

    private RenderParams CreateRenderParams(MaterialPropertyBlock props)
    {
        return new RenderParams(material)
        {
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
            receiveShadows = true,
            worldBounds = new Bounds(Vector3.zero, new Vector3(2000f, 500f, 2000f)),
            matProps = props
        };
    }

    void Update()
    {
        if (!isInitialized) return;


        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Shader.SetGlobalVector("_TestPlayerPos", player.transform.position);
        }

     
        computeShader.SetVector("_CameraPosition", mainCamera.transform.position);
        computeShader.SetFloat("_LOD1DistSqr", lod1Distance * lod1Distance);
        computeShader.SetFloat("_LOD2DistSqr", lod2Distance * lod2Distance);

        UpdateFrustumPlanes();

        
        computeShader.SetBuffer(cullKernelID, "_TransformBuf", transformBuf);
        computeShader.SetBuffer(cullKernelID, "_CullBufLOD0", cullBufLOD0);
        computeShader.SetBuffer(cullKernelID, "_CullBufLOD1", cullBufLOD1);
        computeShader.SetBuffer(cullKernelID, "_CullBufLOD2", cullBufLOD2);

        cullBufLOD0.SetCounterValue(0);
        cullBufLOD1.SetCounterValue(0);
        cullBufLOD2.SetCounterValue(0);

        computeShader.Dispatch(cullKernelID, threadGroups, 1, 1);

        
        GraphicsBuffer.CopyCount(cullBufLOD0, commandBufLOD0, 4);
        GraphicsBuffer.CopyCount(cullBufLOD1, commandBufLOD1, 4);
        GraphicsBuffer.CopyCount(cullBufLOD2, commandBufLOD2, 4);

        
        Graphics.RenderMeshIndirect(renderParamsLOD0, meshLOD0, commandBufLOD0, commandCount);
        Graphics.RenderMeshIndirect(renderParamsLOD1, meshLOD1, commandBufLOD1, commandCount);
        Graphics.RenderMeshIndirect(renderParamsLOD2, meshLOD2, commandBufLOD2, commandCount);
    }

    private void UpdateFrustumPlanes()
    {
        if (mainCamera == null || planesBuffer == null) return;

        GeometryUtility.CalculateFrustumPlanes(mainCamera, cachedPlanes);

        for (int i = 0; i < 6; i++)
        {
            Plane p = cachedPlanes[i];
            cachedVectors[i].x = p.normal.x;
            cachedVectors[i].y = p.normal.y;
            cachedVectors[i].z = p.normal.z;
            cachedVectors[i].w = p.distance;
        }

        planesBuffer.SetData(cachedVectors);
        computeShader.SetBuffer(cullKernelID, "_PlanesBuf", planesBuffer);
    }

    void OnDestroy() => ClearBuffers();

    private void ClearBuffers()
    {
        commandBufLOD0?.Release(); commandBufLOD0 = null;
        commandBufLOD1?.Release(); commandBufLOD1 = null;
        commandBufLOD2?.Release(); commandBufLOD2 = null;

        transformBuf?.Release(); transformBuf = null;
        planesBuffer?.Release(); planesBuffer = null;

        cullBufLOD0?.Release(); cullBufLOD0 = null;
        cullBufLOD1?.Release(); cullBufLOD1 = null;
        cullBufLOD2?.Release(); cullBufLOD2 = null;

        isInitialized = false;
    }
}
