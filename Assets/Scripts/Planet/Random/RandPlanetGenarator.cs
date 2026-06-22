using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using static NoiseSettings;


public class RandPlanetGenarator : MonoBehaviour
{

    [System.Serializable]
    public class VegetationTypeTemplate
    {
        public string name = "New Vegetation";
        public Mesh lod0;
        public Mesh lod1;
        public Mesh lod2;
        public Material baseMaterial;

        [Header("Настройки рандомизации")]
        public Vector2Int grassPerTriangleRange = new Vector2Int(1, 3);
        public Vector2 slopeThresholdRange = new Vector2(0.3f, 0.7f);
        public Vector2 noiseScaleRange = new Vector2(0.1f, 0.5f);
        public Vector2 densityThresholdRange = new Vector2(0.2f, 0.6f);
        public Vector2 scaleXZRange = new Vector2(0.8f, 1.5f);
        public Vector2 scaleYRange = new Vector2(0.5f, 1.4f);
    }//


    [Tooltip("Список шаблонов для генерации разных видов растительности")]
    public List<VegetationTypeTemplate> vegetationTemplates = new List<VegetationTypeTemplate>();

    [Header("Global Vegetation Culling/LOD settings")]
    [Range(1000, 1000000)] public int maxInstancesPerFace = 50000;
    public float cullRadius = 5f;
    public float lod1Dist = 30f;
    public float lod2Dist = 70f;


    [Header("Random Planet Attributes")]
    public RandomPlanetSettings rndSettings;

    [HideInInspector]
    public bool settingsFoldout = false;

    [Header("Other Attributes")]
    public Material copyMaterial;

    public Material copyAtmosphereMaterial;

    public Material copyWaterMaterial;


    [Header("Grass Settings")]

    public ComputeShader vegetationCS;
    public Material copyGrassMaterial;
    


    [Header("Слои ландшафта (Всегда 3 слоя)")]
    [Tooltip("Настройки для Element 0 (Simple)")]
    public SimpleNoiseSettings element0_SimpleSettings;

    [Tooltip("Настройки для Element 1 (Simple)")]
    public SimpleNoiseSettings element1_SimpleSettings;

    [Tooltip("Настройки для Element 2 (Rigid)")]
    public RigidNoiseSettings element2_RigidSettings;



    private GameObject planetGO;
    private Planet planet;


    private void Awake()
    {
        CreateBasicPlanetObject();

        FindPlayerPosition();
        planet.cullingMinAngle = 120f;

        ShapeSettings shapeSettings = ScriptableObject.CreateInstance<ShapeSettings>();
        ShapeSettingsRandomization(shapeSettings);


        ColorSettings colorSettings = ScriptableObject.CreateInstance<ColorSettings>();
        ColorSettingsRandomization(colorSettings);


        VegetationSettings vegetationSettings = ScriptableObject.CreateInstance<VegetationSettings>();
        VegetationSettingsRandomization(vegetationSettings);


        PlanetConfigSettings planetConfigSettings = ScriptableObject.CreateInstance<PlanetConfigSettings>();
        PlanetConfigRandomization(planetConfigSettings);


        planet.ConstructRandomPlanet(rndSettings.resolution.PickRandomValue(), planetConfigSettings, shapeSettings, colorSettings, vegetationSettings);
    }

  
    private void VegetationSettingsRandomization(VegetationSettings vegSettings)
    {
        vegSettings.vegetationCS = vegetationCS;
        vegSettings.maxInstancesPerFace = maxInstancesPerFace;
        vegSettings.cullRadius = cullRadius;
        vegSettings.lod1Dist = lod1Dist;
        vegSettings.lod2Dist = lod2Dist;

        vegSettings.vegetationTypes = new List<VegetationTypeSettings>();

        foreach (var template in vegetationTemplates)
        {
            if (template.baseMaterial == null) continue;

            VegetationTypeSettings typeSettings = new VegetationTypeSettings();
            typeSettings.name = template.name;
            typeSettings.lod0 = template.lod0;
            typeSettings.lod1 = template.lod1;
            typeSettings.lod2 = template.lod2;

            typeSettings.material = new Material(template.baseMaterial);

            if (rndSettings.grassColor != null)
            {
                typeSettings.material.SetColor("_ColorTop", rndSettings.grassColor.PickRandomValue());
            }

            typeSettings.grassPerTriangle = UnityEngine.Random.Range(template.grassPerTriangleRange.x, template.grassPerTriangleRange.y + 1);
            typeSettings.slopeThreshold = UnityEngine.Random.Range(template.slopeThresholdRange.x, template.slopeThresholdRange.y);
            typeSettings.noiseScale = UnityEngine.Random.Range(template.noiseScaleRange.x, template.noiseScaleRange.y);
            typeSettings.densityThreshold = UnityEngine.Random.Range(template.densityThresholdRange.x, template.densityThresholdRange.y);
            typeSettings.scaleXZ = UnityEngine.Random.Range(template.scaleXZRange.x, template.scaleXZRange.y);
            typeSettings.scaleY = UnityEngine.Random.Range(template.scaleYRange.x, template.scaleYRange.y);

            vegSettings.vegetationTypes.Add(typeSettings);
        }
    }


    private void CreateBasicPlanetObject()
    {
        planetGO = new GameObject("Generated Planet");


        var ga = planetGO.AddComponent<FauxGravityAttractor>();

        ga.gravityIntensity = -500; //hAed hode

        planetGO.transform.position = this.transform.position;
        planet = planetGO.AddComponent<Planet>();
    }


    private void FindPlayerPosition()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");

        if (playerGO != null)
        {
            planet.currentCamera = playerGO.transform;
            planet.distanceToCamera = Vector3.Distance(planet.transform.position, planet.currentCamera.position);
        }
    }

    private void PlanetConfigRandomization(PlanetConfigSettings planetConfigSettings)
    {
        planetConfigSettings.hasWater = RandomXT.RandomBool();
        planetConfigSettings.hasGrass = RandomXT.RandomBool();
    }

    private void ShapeSettingsRandomization(ShapeSettings shapeSettings)
    {
        // 1. Применяем базовые параметры планеты
        shapeSettings.planetRadius = rndSettings.planetRadius.PickRandomValue();
        shapeSettings.atmosphereRadiusMultiplier = rndSettings.atmosphereRadiusMultiplier.PickRandomValue();
        shapeSettings.waterRadiusMultiplier = rndSettings.waterRadiusMultiplier.PickRandomValue();


        shapeSettings.noiseLayers = new ShapeSettings.NoiseLayer[3];

        // ==========================================================
        // СЛОЙ 0 (Element 0): Simple, Маска отключена
        // ==========================================================
        ShapeSettings.NoiseLayer layer0 = new ShapeSettings.NoiseLayer();
        layer0.enabled = true;
        layer0.useFirstLayerAsTheMask = false;

        NoiseSettings noise0 = new NoiseSettings();
        noise0.filterType = NoiseSettings.FilterType.Simple;


        noise0.simpleNoiseSettings = new NoiseSettings.SimpleNoiseSettings();

        var generatedSimple0 = rndSettings.element0_SimpleSettings.PickRandomValue();


        noise0.simpleNoiseSettings.strength = generatedSimple0.strength;
        noise0.simpleNoiseSettings.roughness = generatedSimple0.roughness;
        noise0.simpleNoiseSettings.baseRoughness = generatedSimple0.baseRoughness;
        noise0.simpleNoiseSettings.center = generatedSimple0.center;
        noise0.simpleNoiseSettings.numLayers = generatedSimple0.numLayers;
        noise0.simpleNoiseSettings.persistence = generatedSimple0.persistence;
        noise0.simpleNoiseSettings.minValue = generatedSimple0.minValue;

        layer0.noiseSettings = noise0;
        shapeSettings.noiseLayers[0] = layer0;

        // ==========================================================
        // СЛОЙ 1 (Element 1): Simple, Маска включена
        // ==========================================================
        ShapeSettings.NoiseLayer layer1 = new ShapeSettings.NoiseLayer();
        layer1.enabled = true;
        layer1.useFirstLayerAsTheMask = true;

        NoiseSettings noise1 = new NoiseSettings();
        noise1.filterType = NoiseSettings.FilterType.Simple;


        noise1.simpleNoiseSettings = new NoiseSettings.SimpleNoiseSettings();

        var generatedSimple1 = rndSettings.element1_SimpleSettings.PickRandomValue();

        noise1.simpleNoiseSettings.strength = generatedSimple1.strength;
        noise1.simpleNoiseSettings.roughness = generatedSimple1.roughness;
        noise1.simpleNoiseSettings.baseRoughness = generatedSimple1.baseRoughness;
        noise1.simpleNoiseSettings.center = generatedSimple1.center;
        noise1.simpleNoiseSettings.numLayers = generatedSimple1.numLayers;
        noise1.simpleNoiseSettings.persistence = generatedSimple1.persistence;
        noise1.simpleNoiseSettings.minValue = generatedSimple1.minValue;

        layer1.noiseSettings = noise1;
        shapeSettings.noiseLayers[1] = layer1;

        // ==========================================================
        // СЛОЙ 2 (Element 2): Rigid, Маска включена
        // ==========================================================
        ShapeSettings.NoiseLayer layer2 = new ShapeSettings.NoiseLayer();
        layer2.enabled = true;
        layer2.useFirstLayerAsTheMask = true;

        NoiseSettings noise2 = new NoiseSettings();
        noise2.filterType = NoiseSettings.FilterType.Rigid;


        noise2.rigidNoiseSettings = new NoiseSettings.RigidNoiseSettings();

        var generatedRigid2 = rndSettings.element2_RigidSettings.PickRandomValue();

        noise2.rigidNoiseSettings.strength = generatedRigid2.strength;
        noise2.rigidNoiseSettings.roughness = generatedRigid2.roughness;
        noise2.rigidNoiseSettings.baseRoughness = generatedRigid2.baseRoughness;
        noise2.rigidNoiseSettings.center = generatedRigid2.center;
        noise2.rigidNoiseSettings.numLayers = generatedRigid2.numLayers;
        noise2.rigidNoiseSettings.persistence = generatedRigid2.persistence;
        noise2.rigidNoiseSettings.minValue = generatedRigid2.minValue;
        noise2.rigidNoiseSettings.weightMultiplier = generatedRigid2.weightMultiplier;

        layer2.noiseSettings = noise2;
        shapeSettings.noiseLayers[2] = layer2;

        Debug.Log("[Генератор]: Успешно выделена память и создана структура из 3 слоев ландшафта.");
    }


    private void ColorSettingsRandomization(ColorSettings colorSettings)
    {
        colorSettings.planetMaterial = new Material(copyMaterial);
        colorSettings.atmosphereMaterial = new Material(copyAtmosphereMaterial);


        int randomIndex = UnityEngine.Random.Range(0, rndSettings.scatteringCoefficients.Length);

        Vector3 sccf = rndSettings.scatteringCoefficients[randomIndex].PickRandomValue(); //


        Vector4 newCoefficients = new Vector4(sccf.x, sccf.y, sccf.z, 0f);

        colorSettings.atmosphereMaterial.SetVector("_ScatteringCoefficients", newCoefficients);


        colorSettings.waterMaterial = copyWaterMaterial;

        colorSettings.biomeColorSettings = new ColorSettings.BiomeColorSettings();
        colorSettings.biomeColorSettings.blendAmount = rndSettings.biomeBlendAmount.PickRandomValue();
        colorSettings.biomeColorSettings.noiseOffset = rndSettings.biomeNoiseOffset.PickRandomValue();
        colorSettings.biomeColorSettings.noiseStrength = rndSettings.biomeNoiseStrength.PickRandomValue();

        NoiseSettings biomeNoiseSettings = new NoiseSettings();
        biomeNoiseSettings.filterType = NoiseSettings.FilterType.Simple;
        biomeNoiseSettings.simpleNoiseSettings = rndSettings.biomeNoiseSettings.PickRandomValue();
        colorSettings.biomeColorSettings.noise = biomeNoiseSettings;

        colorSettings.oceanColor = RandomXT.RandomGradient(new Color[] {
        rndSettings.ground.PickRandomValue(),
        rndSettings.cliff.PickRandomValue(),
        rndSettings.clifftop.PickRandomValue()
    });

        colorSettings.waterColor = rndSettings.waterColor.PickRandomValue();
        colorSettings.atmosphereColor = rndSettings.atmosphereColor.PickRandomValue();

        colorSettings.biomeColorSettings.biomes = new ColorSettings.BiomeColorSettings.Biome[rndSettings.biomeCount.PickRandomValue()];
        float startHeigth = 0f;
        float increment = 1f / (float)rndSettings.biomeCount.lastValue;

        for (int i = 0; i < colorSettings.biomeColorSettings.biomes.Length; ++i)
        {
            colorSettings.biomeColorSettings.biomes[i] = new ColorSettings.BiomeColorSettings.Biome();
            colorSettings.biomeColorSettings.biomes[i].tintPercent = rndSettings.biomeTintPercent.PickRandomValue();
            colorSettings.biomeColorSettings.biomes[i].startHeigth = startHeigth;
            colorSettings.biomeColorSettings.biomes[i].gradient = RandomXT.RandomGradient(new Color[] {
            rndSettings.ground.PickRandomValue(),
            rndSettings.cliff.PickRandomValue(),
            rndSettings.clifftop.PickRandomValue()
        });

            colorSettings.biomeColorSettings.biomes[i].tint = colorSettings.biomeColorSettings.biomes[i].gradient.Evaluate(UnityEngine.Random.Range(0.2f, 0f));
            startHeigth += increment;
        }
    }

}

