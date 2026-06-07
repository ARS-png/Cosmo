using UnityEngine;

[CreateAssetMenu()]
public class ShapeSettings : ScriptableObject
{
    public float planetRadius = 1f;

    public float waterRadiusMultiplier = 1f;

    public float atmosphereRadiusMultiplier;

    public NoiseLayer[] noiseLayers;

    [System.Serializable]
    public class NoiseLayer
    {
        public bool enabled = true;

        public bool useFirstLayerAsTheMask = false;

        public NoiseSettings noiseSettings;
    }
}
