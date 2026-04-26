using UnityEngine;
[System.Serializable]
public class ColorGenerator
{
    ColorSettings settings;

    Texture2D texture;
    const int textureResolution = 50;

    INoiseFilter biomeNoiseFilter;


    public void UpdateSettings(ColorSettings settings)
    {
        this.settings = settings;

        if (texture == null || texture.height != settings.biomeColorSettings.biomes.Length)
        {
            texture = new Texture2D(textureResolution * 2, settings.biomeColorSettings.biomes.Length, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
        }

        biomeNoiseFilter = NoiseFilterFactory.CreateNoiseFilter(settings.biomeColorSettings.noise);
    }


    public float BiomePercentFromPoint(Vector3 pointOnUnitSphere)
    {
        float heigthPercent = (pointOnUnitSphere.y + 1) / 2f;
        heigthPercent += biomeNoiseFilter.Evaluate(pointOnUnitSphere) - settings.biomeColorSettings.noiseOffset * settings.biomeColorSettings.noiseStrength;

        float biomeIndex = 0f;

        int numBiomes = settings.biomeColorSettings.biomes.Length;

        float blendRange = settings.biomeColorSettings.blendAmount / 2 + .001f;


        for (int i = 0; i < numBiomes; i++)
        {
            float dst = heigthPercent - settings.biomeColorSettings.biomes[i].startHeigth;
            float weigth = Mathf.InverseLerp(-blendRange, blendRange, dst);
            biomeIndex *= (1 - weigth);
            biomeIndex += i * weigth;
        }

        return biomeIndex / Mathf.Max(1, (numBiomes - 1));
    }

    public void UpdateElavation(MinMax elevationMinMax)
    {
        settings.planetMaterial.SetVector("_ElevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max));
    }

    public void UpdateColors(ColorSettings colorSettings)
    {
        Color[] colors = new Color[texture.width * texture.height];
        int colorIndex = 0;
        foreach (var biome in settings.biomeColorSettings.biomes)
        {
            for (int i = 0; i < textureResolution * 2; i++)
            {
                Color gradientCol;
                if (i < textureResolution)   
                {
                    gradientCol = settings.oceanColor.Evaluate(i / (textureResolution - 1f));
                }
                else
                {
                    gradientCol = biome.gradient.Evaluate((i - textureResolution) / (textureResolution - 1f));
                }

                Color tintCol = biome.tint;
                colors[colorIndex] = gradientCol * (1 - biome.tintPercent) + tintCol * biome.tintPercent;
                colorIndex++;
            }
        }


        texture.SetPixels(colors);
        texture.Apply();
        settings.planetMaterial.SetTexture("_Planet_Texture", texture);
    }


    public void Cleanup()
    {
        if (texture != null)
        {
            UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
        }
    }


}
