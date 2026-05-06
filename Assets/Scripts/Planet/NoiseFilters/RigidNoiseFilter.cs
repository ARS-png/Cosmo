using UnityEngine;

public class RigidNoiseFilter : INoiseFilter
{
    Noise noise = new Noise();

    NoiseSettings.RigidNoiseSettings settings;

    public RigidNoiseFilter(NoiseSettings.RigidNoiseSettings settings)
    {
        this.settings = settings;
    }
    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0f;
        float frequency = settings.baseRoughness;
        Vector3 center = settings.center;
        float amplitude = 1f;
        float weight = 1f;


        for (int i = 0; i < settings.numLayers; i++)
        {
            float v = 1 - Mathf.Abs(noise.Evaluate(point * frequency + center));
            v *= v;
            v *= weight;
            weight = Mathf.Clamp01(v * settings.weightMultiplier);

            noiseValue += v * amplitude;
            frequency *= settings.roughness;
            amplitude *= settings.persistence;
        }

        noiseValue = noiseValue - settings.minValue;

        return noiseValue * settings.strength;
    }
}
