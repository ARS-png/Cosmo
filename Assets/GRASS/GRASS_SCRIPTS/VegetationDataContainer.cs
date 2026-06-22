using UnityEngine;

public class VegetationDataContainer
{
    public bool isInitialized { get; private set; }
    public int maxInstances { get; private set; }

    public GraphicsBuffer transformBuffer { get; private set; }
    public GraphicsBuffer planesBuffer { get; private set; }
    public GraphicsBuffer planetVerticesBuffer { get; private set; }

    public GraphicsBuffer cullBufLOD0 { get; private set; }
    public GraphicsBuffer cullBufLOD1 { get; private set; }
    public GraphicsBuffer cullBufLOD2 { get; private set; }

    public GraphicsBuffer commandBufLOD0 { get; private set; }
    public GraphicsBuffer commandBufLOD1 { get; private set; }
    public GraphicsBuffer commandBufLOD2 { get; private set; }



    public void SetSharedVerticesBuffer(GraphicsBuffer sharedBuffer)
    {
        planetVerticesBuffer = sharedBuffer;
    }



    public void Initialize(int instancesCount, uint indexCountLOD0, uint indexCountLOD1, uint indexCountLOD2)
    {
        if (isInitialized) Release();

        maxInstances = instancesCount;

        commandBufLOD0 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandBufLOD1 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandBufLOD2 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);

        commandBufLOD0.SetData(new[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = indexCountLOD0 } });
        commandBufLOD1.SetData(new[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = indexCountLOD1 } });
        commandBufLOD2.SetData(new[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = indexCountLOD2 } });

        transformBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxInstances, sizeof(float) * 16);
        planesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 4);

        cullBufLOD0 = new GraphicsBuffer(GraphicsBuffer.Target.Append, maxInstances, sizeof(float) * 16);
        cullBufLOD1 = new GraphicsBuffer(GraphicsBuffer.Target.Append, maxInstances, sizeof(float) * 16);
        cullBufLOD2 = new GraphicsBuffer(GraphicsBuffer.Target.Append, maxInstances, sizeof(float) * 16);

        isInitialized = true;
    }


    public void Release()
    {
        commandBufLOD0?.Release();
        commandBufLOD1?.Release();
        commandBufLOD2?.Release();
        transformBuffer?.Release();
        planesBuffer?.Release();
        planetVerticesBuffer?.Release();
        cullBufLOD0?.Release();
        cullBufLOD1?.Release();
        cullBufLOD2?.Release();
        isInitialized = false;
    }
}

