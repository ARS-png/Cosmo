using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{
    [Header("Random Planet Attributes")]
    public RandomPlanetSettings settings;

    [HideInInspector]
    public bool settingsFoldout = false;

    [Header("Other Attributes")]
    public Material copyMaterial;

    public Material copyAtmosphereMaterial;

    private GameObject planetGO;
    private Planet planet;

    private const int CONST_LAYERS_AMOUNT = 3;


    private void Awake()
    {
        planetGO = new GameObject("Generated Planet");
        planetGO.transform.position = this.transform.position;
        planet = planetGO.AddComponent<Planet>();

        //-- terrain settings randomization
        ShapeSettings shapeSettings = ScriptableObject.CreateInstance<ShapeSettings>();
        shapeSettings.planetRadius = settings.planetRadius.PickRandomValue();
        shapeSettings.noiseLayers = new ShapeSettings.NoiseLayer[settings.noiseLayersAmount.PickRandomValue()];



        for (int i = 0; i < shapeSettings.noiseLayers.Length; ++i) 
        {
            ShapeSettings.NoiseLayer randLayer = new();
            randLayer.enabled = true;
            randLayer.useFirstLayerAsTheMask = i == 0 ? false : true;

            shapeSettings.atmosphereRadiusMultiplier = settings.atmosphereRadiusMultiplier.PickRandomValue();
            shapeSettings.waterRadiusMultiplier = settings.waterRadiusMultiplier.PickRandomValue();

            NoiseSettings randomNoiseSettings = new NoiseSettings();


            if (i < CONST_LAYERS_AMOUNT) 
            {
                randomNoiseSettings.filterType = NoiseSettings.FilterType.Simple;

                randomNoiseSettings.simpleNoiseSettings = settings.terrainConstants[i].PickRandomValue();

                randLayer.noiseSettings = randomNoiseSettings;
                shapeSettings.noiseLayers[i] = randLayer;
            }

            else
            {

                randomNoiseSettings.filterType = RandomXT.RandomBool() == false ? NoiseSettings.FilterType.Rigid : NoiseSettings.FilterType.Simple;

                if (randomNoiseSettings.filterType == NoiseSettings.FilterType.Simple)
                {
                    randomNoiseSettings.simpleNoiseSettings = settings.simpleTerrainSettings.PickRandomValue();

                    //Debug.Log("simple");
                }
                else
                {
                    randomNoiseSettings.rigidNoiseSettings = settings.rigidTerrainSettings.PickRandomValue();

                    //Debug.Log("rigid");
                }
                randLayer.noiseSettings = randomNoiseSettings;
                shapeSettings.noiseLayers[i] = randLayer;
            }


        }


        //-- color settings randomization
        ColorSettings colorSettings = ScriptableObject.CreateInstance<ColorSettings>();

        colorSettings.planetMaterial = new Material(copyMaterial);
        colorSettings.atmosphereMaterial = new Material(copyAtmosphereMaterial);

        colorSettings.biomeColorSettings = new ColorSettings.BiomeColorSettings();
        colorSettings.biomeColorSettings.blendAmount = settings.biomeBlendAmount.PickRandomValue();
        colorSettings.biomeColorSettings.noiseOffset = settings.biomeNoiseOffset.PickRandomValue();
        colorSettings.biomeColorSettings.noiseStrength = settings.biomeNoiseStrength.PickRandomValue();


        NoiseSettings biomeNoiseSettings = new NoiseSettings();
        biomeNoiseSettings.filterType = NoiseSettings.FilterType.Simple;
        biomeNoiseSettings.simpleNoiseSettings = settings.biomeNoiseSettings.PickRandomValue();
        colorSettings.biomeColorSettings.noise = biomeNoiseSettings;


        colorSettings.oceanColor = RandomXT.RandomGradient(new Color[] {
            settings.ground.PickRandomValue(),
            settings.cliff.PickRandomValue(),
            settings.clifftop.PickRandomValue()
        });

        colorSettings.atmosphereColor = RandomXT.RandomGradient(new Color[] {
            settings.ground.PickRandomValue(),
            settings.cliff.PickRandomValue(),
            settings.clifftop.PickRandomValue()
        });

      
     




        colorSettings.biomeColorSettings.biomes = new ColorSettings.BiomeColorSettings.Biome[settings.biomeCount.PickRandomValue()];
        float startHeigth = 0f;
        float increment = 1f / (float)settings.biomeCount.lastValue;


        for (int i = 0; i < colorSettings.biomeColorSettings.biomes.Length; ++i)
        {
            colorSettings.biomeColorSettings.biomes[i] = new ColorSettings.BiomeColorSettings.Biome();
            colorSettings.biomeColorSettings.biomes[i].tintPercent = settings.biomeTintPercent.PickRandomValue();
            colorSettings.biomeColorSettings.biomes[i].startHeigth = startHeigth;
            colorSettings.biomeColorSettings.biomes[i].gradient = RandomXT.RandomGradient(new Color[] {
                settings.ground.PickRandomValue(),
                settings.cliff.PickRandomValue(),
                settings.clifftop.PickRandomValue()
            });


            colorSettings.biomeColorSettings.biomes[i].tint = colorSettings.biomeColorSettings.biomes[i].gradient.Evaluate(Random.Range(0.2f, 0f));
            startHeigth += increment;
        }


        planet.ConstructRandomPlanet(settings.resolution.PickRandomValue(), shapeSettings, colorSettings);

    }
}

