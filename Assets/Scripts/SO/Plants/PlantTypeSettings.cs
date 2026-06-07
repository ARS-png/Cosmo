using UnityEngine;

[System.Serializable]
public struct PlantTypeSettings
{
    public string name;
    public Mesh lod0;
    public Mesh lod1;
    public Mesh lod2;
    public Material material;
    [Range(0, 1)] public float spawnChance;
}
