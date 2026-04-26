using UnityEngine;

public class ShapeGenerator
{
    ShapeSettings settings;

    INoiseFilter[] noiseFilters;

    public MinMax elevationMinMax;

    public void UpdateSettings(ShapeSettings shapeSettings)
    {
        this.settings = shapeSettings;
        noiseFilters = new INoiseFilter[settings.noiseLayers.Length];

        for (int i = 0; i < noiseFilters.Length; i++)
        {
            noiseFilters[i] = NoiseFilterFactory.CreateNoiseFilter(settings.noiseLayers[i].noiseSettings);
        }

        elevationMinMax = new MinMax();
    }

    public float CalculateUnscaledElevation(Vector3 pointOnUnitSphere)
    {
        float elevation = 0f;
        float firstLayerValue = 0f;


        if (noiseFilters.Length > 0f)
        {
            firstLayerValue = noiseFilters[0].Evaluate(pointOnUnitSphere);

            if (settings.noiseLayers[0].enabled)
            {
                elevation = firstLayerValue;
            }
        }

        for (int i = 1; i < noiseFilters.Length; i++)
        {
            if (settings.noiseLayers[i].enabled)
            {
                float mask = (settings.noiseLayers[i].useFirstLayerAsTheMask) ? firstLayerValue : 1f;
                elevation += noiseFilters[i].Evaluate(pointOnUnitSphere) * mask;
            }   
        }

        elevationMinMax.AddValue(elevation);

        return elevation;
    }


    public float GetScaledElevation(float unscaledElevation)
    {
        float elevation = Mathf.Max(0, unscaledElevation);
        elevation = settings.planetRadius * (1 + elevation);
        return elevation;
    }
    
    public float GetWaterRadius()
    {
        return settings.waterRadiusMultiplier;
    }
}
 