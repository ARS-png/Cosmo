

using UnityEngine;

[System.Serializable]
public class NoiseSettings
{
    public enum FilterType { Simple, Rigid };
    public FilterType filterType;

    [ConditionalHide("filterType", 0)]
    public SimpleNoiseSettings simpleNoiseSettings;

    [ConditionalHide("filterType", 1)]
    public RigidNoiseSettings rigidNoiseSettings;


    [System.Serializable]
    public class SimpleNoiseSettings
    {
        public float strength = 1f;
        public float roughness = 2f;
        public float baseRoughness = 1f;
        public Vector3 center;
        [Range(1, 8)]
        public int numLayers;
        public float persistence = .5f;
        public float minValue;
    }

    [System.Serializable]
    public class RigidNoiseSettings : SimpleNoiseSettings
    {
        public float weightMultiplier = 1f;
    }
}
