using UnityEngine;

[CreateAssetMenu()]
public class ColorSettings : ScriptableObject
{
    public Material planetMaterial;
    public Material atmosphereMaterial;
    public BiomeColorSettings biomeColorSettings;
    public Gradient oceanColor;
    public Gradient atmosphereColor;


    [System.Serializable]
    public class BiomeColorSettings 
    {
        public Biome[] biomes;
        public NoiseSettings noise;
        public float noiseOffset;
        public float noiseStrength;
        [Range(0,1)]
        public float blendAmount;
        [System.Serializable]
        public class Biome
        {
            public Gradient gradient;
            public Color tint;
            [Range(0,1)]
            public float startHeigth;
            [Range(0, 1)]
            public float tintPercent;
        }
    }
}
