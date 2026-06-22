using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VegetationTypeSettings 
{
    public string name;
    public Mesh lod0;
    public Mesh lod1;
    public Mesh lod2;
    public Material material;

    public int grassPerTriangle;
    public float slopeThreshold;
    public float noiseScale;
    public float densityThreshold;
    public float scaleXZ;
    public float scaleY;

   
    [System.NonSerialized] public GraphicsBuffer cullBufLOD0;
    [System.NonSerialized] public GraphicsBuffer cullBufLOD1;
    [System.NonSerialized] public GraphicsBuffer cullBufLOD2;

    [System.NonSerialized] public GraphicsBuffer commandBufLOD0;
    [System.NonSerialized] public GraphicsBuffer commandBufLOD1;
    [System.NonSerialized] public GraphicsBuffer commandBufLOD2;

   
    [System.NonSerialized] public MaterialPropertyBlock mpBlockLOD0;
    [System.NonSerialized] public MaterialPropertyBlock mpBlockLOD1;
    [System.NonSerialized] public MaterialPropertyBlock mpBlockLOD2;


    public void InitializeBuffers(int maxInstances)
    {
        
        cullBufLOD0 = new GraphicsBuffer(GraphicsBuffer.Target.Append, maxInstances, sizeof(float) * 16);
        cullBufLOD1 = new GraphicsBuffer(GraphicsBuffer.Target.Append, maxInstances, sizeof(float) * 16);
        cullBufLOD2 = new GraphicsBuffer(GraphicsBuffer.Target.Append, maxInstances, sizeof(float) * 16);

       
        commandBufLOD0 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandBufLOD1 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandBufLOD2 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);

  
        commandBufLOD0.SetData(new[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = lod0.GetIndexCount(0) } });
        commandBufLOD1.SetData(new[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = lod1.GetIndexCount(0) } });
        commandBufLOD2.SetData(new[] { new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = lod2.GetIndexCount(0) } });

      
        mpBlockLOD0 = new MaterialPropertyBlock();
        mpBlockLOD1 = new MaterialPropertyBlock();
        mpBlockLOD2 = new MaterialPropertyBlock();

        mpBlockLOD0.SetBuffer("_CullBuf", cullBufLOD0);
        mpBlockLOD1.SetBuffer("_CullBuf", cullBufLOD1);
        mpBlockLOD2.SetBuffer("_CullBuf", cullBufLOD2);
    }



    public void ReleaseBuffers()
    {
        cullBufLOD0?.Release(); cullBufLOD1?.Release(); cullBufLOD2?.Release();
        commandBufLOD0?.Release(); commandBufLOD1?.Release(); commandBufLOD2?.Release();
    }
}


[CreateAssetMenu(fileName = "NewGrassSettings", menuName = "Planet/Vegetation Settings")]
public class VegetationSettings : ScriptableObject
{
    [Header("Ресурсы графики")]
    public ComputeShader vegetationCS;

    [Header("СПИСОК ВСЕХ ВИДОВ РАСТЕНИЙ")]
    public List<VegetationTypeSettings> vegetationTypes = new List<VegetationTypeSettings>();

    [Header("Параметры инстансов")]
    [Range(1000, 1000000)] public int maxInstancesPerFace = 50000;


    [Header("Куллинг и Дистанции LOD")]
    public float cullRadius = 1.5f;
    public float lod1Dist = 30f;
    public float lod2Dist = 70f;

}
