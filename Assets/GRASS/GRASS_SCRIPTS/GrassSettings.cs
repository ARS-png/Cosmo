//using UnityEngine;

//[CreateAssetMenu(fileName = "NewGrassSettings", menuName = "Planet/Grass Settings")]
//public class GrassSettings : ScriptableObject
//{
//    [Header("Ресурсы графики")]
//    public ComputeShader grassComputeShader;
//    public Material grassMaterial;

//    [Header("Меши уровней детализации (LOD)")]
//    public Mesh grassMeshLOD0;
//    public Mesh grassMeshLOD1;
//    public Mesh grassMeshLOD2;

//    [Header("Параметры инстансов")]
//    [Range(1000, 1000000)] public int maxInstancesPerFace = 50000;
//    public float grassScaleXZ = 1f;
//    public float grassScaleY = 1f;

//    [Header("Куллинг и Дистанции LOD")]
//    public float grassCullRadius = 1.5f;
//    public float grassLod1Dist = 30f;
//    public float grassLod2Dist = 70f;

//    [Range(1, 128)]
//    public int grassPerTriangle = 4;


//    [Header("Grass Noise Settings")]
//    [Range(0.05f, 1)]
//    public float grassDensityThreshold;

//    [Range(1, 100)]
//    public float grassNoiseScale;


//    [Header("Other masks")]
//    public float grassSlopeThreshold;





//}
