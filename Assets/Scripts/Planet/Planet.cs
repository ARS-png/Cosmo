using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;

    [SerializeField, HideInInspector]
    TerrainFace[] terrainFaces;

    [SerializeField, HideInInspector]
    MeshFilter[] waterMeshFilters;

    [SerializeField, HideInInspector]
    TerrainFace[] waterTerrainFaces;

    GameObject atmoshereGO;

    GameObject underWaterTriggerSphereGO;
    public static float[] sqrDetailDistances;

    public Transform player;

    public static int maxDetailLevel = 5; //  can change


    public float cullingMinAngle = 120;
    public float distanceToPlayer;

    [Range(2, 256)]
    [SerializeField] int resolution = 10;
    public bool autoUpdate = true;
    public enum FaceRenderMask { All, Up, Down, Left, Right, Forward, Back }
    public FaceRenderMask faceRenderMask;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    //public WaterSettings waterSettings;

    public ColorGenerator colorGenerator = new ColorGenerator();
    public ShapeGenerator shapeGenerator = new ShapeGenerator();


    [HideInInspector]
    public bool shapeSettingsFoldout;

    [HideInInspector]
    public bool colorSettingsFoldout;


    private bool proceduralyGenerated = false;


    //For Chunks
    public static Transform playerTransform;
    public Vector3 position;
    public float radius;


    public GrassSettings grassSettings; // Ссылка на наш ScriptableObject профиль

    public static float[] detailLevelDistances = new float[] {
        Mathf.Infinity,
        1500f,
        700f,
        500f,
        300f,
        150f,
        40f,

    };

    public static void InitializeSqrDistances()
    {

        sqrDetailDistances = new float[detailLevelDistances.Length];

        for (int i = 0; i < detailLevelDistances.Length; i++)
        {

            float dist = detailLevelDistances[i];


            if (float.IsPositiveInfinity(dist))
            {
                sqrDetailDistances[i] = float.PositiveInfinity;
            }
            else
            {
                sqrDetailDistances[i] = dist * dist;
            }
        }
    }


    public static float GetSqrDistance(int level)
    {
        if (sqrDetailDistances == null || sqrDetailDistances.Length == 0) InitializeSqrDistances();

        return sqrDetailDistances[level];
    }




    private void Start()
    {
        position = this.gameObject.transform.position;

        if (!proceduralyGenerated)
            GeneratePlanet();

        if (terrainFaces != null)
        {
            foreach (var face in terrainFaces)
            {
                face?.InitializeGrass(
                     grassSettings.grassComputeShader,
                     grassSettings.grassMaterial,
                     grassSettings.grassMeshLOD0,
                     grassSettings.grassMeshLOD1,
                     grassSettings.grassMeshLOD2,
                     grassSettings.maxInstancesPerFace
                 );

            }
        }

        proceduralyGenerated = true;

        StartCoroutine(PlanetGenerationLoop());
    }

    private IEnumerator PlanetGenerationLoop()
    {
        GenerateMesh();
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            UpdateMesh();
        }
    }


    void Initialize()
    {

        shapeGenerator.UpdateSettings(shapeSettings);
        colorGenerator.UpdateSettings(colorSettings);


        //
        this.radius = shapeSettings.planetRadius; //переместить



        //

        if (meshFilters == null || meshFilters.Length == 0)
        {
            meshFilters = new MeshFilter[6];
        }

        terrainFaces = new TerrainFace[6];

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("mesh");
                meshObj.transform.parent = transform;
                meshObj.transform.position = meshObj.transform.parent.position;

                meshObj.AddComponent<MeshRenderer>();



                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].mesh = new Mesh();
                meshFilters[i].gameObject.AddComponent<MeshCollider>();

            }

            meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.planetMaterial;

            terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].mesh, resolution, directions[i], shapeSettings.planetRadius, this, meshFilters[i].gameObject); //
            bool isRenderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            meshFilters[i].gameObject.SetActive(isRenderFace);
        }
    }


    //Now is unenabled 
    void InitializeWater()
    {
        if (waterMeshFilters == null || waterMeshFilters.Length == 0)
        {
            waterMeshFilters = new MeshFilter[6];
        }

        waterTerrainFaces = new TerrainFace[6];
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };


        for (int i = 0; i < waterMeshFilters.Length; i++)
        {
            if (waterMeshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("water_mesh");
                meshObj.transform.parent = transform;
                meshObj.transform.position = meshObj.transform.parent.position;


                meshObj.AddComponent<MeshRenderer>();
                waterMeshFilters[i] = meshObj.AddComponent<MeshFilter>();
                waterMeshFilters[i].mesh = new Mesh();


                if (colorSettings.waterMaterial != null)
                {
                    waterMeshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.waterMaterial;


                    MeshRenderer meshRenderer = meshObj.GetComponent<MeshRenderer>();
                    Material mat = meshRenderer.material = colorSettings.waterMaterial;


                    mat.SetVector("_Deep_Water_Color", colorSettings.waterColor); //иожно и больше различий

                    Color horizonColor = colorSettings.atmosphereColor;
                    mat.SetVector("_Horizon_Color", new Vector4(horizonColor.r, horizonColor.g, horizonColor.b, 1));
                }


                if (meshObj.TryGetComponent<Collider>(out Collider collider))
                {
                    Destroy(collider);
                }


                if (meshObj.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            waterTerrainFaces[i] = new TerrainFace(shapeGenerator, waterMeshFilters[i].mesh, resolution, directions[i], shapeSettings.planetRadius * shapeSettings.waterRadiusMultiplier, this, waterMeshFilters[i].gameObject);//
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            waterMeshFilters[i].gameObject.SetActive(renderFace);
        }



        CreateUnderWaterTriggerSphere();

    }


    private void CreateUnderWaterTriggerSphere()
    {
        const string triggerName = "UnderWaterTriggerSphere";
        Transform triggerTransform = transform.Find(triggerName);

        if (triggerTransform == null)
        {
            underWaterTriggerSphereGO = new GameObject(triggerName);
            underWaterTriggerSphereGO.transform.SetParent(transform);
            underWaterTriggerSphereGO.transform.localPosition = Vector3.zero;
        }
        else
        {
            underWaterTriggerSphereGO = triggerTransform.gameObject;
        }


        if (!underWaterTriggerSphereGO.TryGetComponent<SphereCollider>(out var collider))
        {
            collider = underWaterTriggerSphereGO.AddComponent<SphereCollider>();
        }
        collider.isTrigger = true;


        if (!underWaterTriggerSphereGO.TryGetComponent<WaterTrigger>(out var myScript))
        {
            myScript = underWaterTriggerSphereGO.AddComponent<WaterTrigger>();
            myScript.WaterLayer = LayerMask.GetMask("Water");
        }


        float diameter = shapeSettings.planetRadius * shapeSettings.waterRadiusMultiplier * 2 - 1.75f;//изменить позже
        underWaterTriggerSphereGO.transform.localScale = Vector3.one * diameter;
    }



    public void OnShapeSettingsUpdated()
    {
        if (autoUpdate == true)
        {
            Initialize();
            InitializeWater();
            GenerateMesh();
            //GenerateWaterMesh();
        }
    }

    public void OnColorSettingsUpdated()
    {
        if (autoUpdate == true)
        {
            Initialize();
            InitializeWater();
            GenerateMesh();
            UpdateColorGeneratorSettings();
        }
    }

    public void GeneratePlanet()
    {
        Initialize();
        InitializeWater();
        GenerateMesh();
        GenerateWaterMesh();
        UpdateColorGeneratorSettings();
        GenerateAtmosphere();
    }






    //ConstructMeshTrees
    void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            terrainFaces[i].ConstructTree();
        }

        colorGenerator.UpdateElavation(shapeGenerator.elevationMinMax);
    }


    void UpdateMesh()
    {
        foreach (TerrainFace face in terrainFaces)
        {
            face.UpdateTree();
        }
    }


    void GenerateAtmosphere()
    {
        Transform atmosphereTransform = transform.Find("Atmosphere");

        if (atmosphereTransform == null)
        {
            atmoshereGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atmoshereGO.name = "Atmosphere";
            atmoshereGO.transform.parent = transform;
            atmoshereGO.transform.localPosition = Vector3.zero;

            if (atmoshereGO.TryGetComponent<SphereCollider>(out var collider))
            {
                DestroyImmediate(collider);
            }
        }
        else
        {
            atmoshereGO = atmosphereTransform.gameObject;
        }


        float planetRadius = shapeSettings.planetRadius;


        float arm = shapeSettings.atmosphereRadiusMultiplier;
        float atmosphereRadius = planetRadius * (arm == 0 ? 1.3f : arm);


        float oceanRadius = shapeSettings.waterRadiusMultiplier;


        atmoshereGO.transform.localScale = Vector3.one * atmosphereRadius * 2f;


        if (colorSettings.atmosphereMaterial != null)
        {
            MeshRenderer renderer = atmoshereGO.GetComponent<MeshRenderer>();


            Material mat = renderer.material = colorSettings.atmosphereMaterial;


            mat.SetVector("_PlanetCenter", transform.position);
            mat.SetFloat("_PlanetRadius", planetRadius);
            mat.SetFloat("_AtmosphereRadius", atmosphereRadius);
            mat.SetFloat("_OceanRadius", oceanRadius);
            mat.SetColor("_BaseColor", colorSettings.atmosphereColor);
        }
    }



    void GenerateWaterMesh()
    {
        float waterRadiusMul = shapeSettings.waterRadiusMultiplier;
        for (int i = 0; i < 6; i++)
        {
            if (waterMeshFilters[i].gameObject.activeSelf)
            {
                waterTerrainFaces[i].ConstructWaterMesh(shapeSettings.planetRadius, shapeSettings.waterRadiusMultiplier);
            }
        }
    }


    void UpdateColorGeneratorSettings()
    {
        colorGenerator.UpdateSettings(colorSettings);
        colorGenerator.UpdateColors(colorSettings);
    }

    private void Update()
    {
        if (player != null)
        {
            playerTransform = player;
            position = transform.position;

            distanceToPlayer = Vector3.Distance(transform.position, player.position);
        }


        if (proceduralyGenerated && terrainFaces != null)
        {
            for (int i = 0; i < terrainFaces.Length; i++)
            {
                if (terrainFaces[i] != null && meshFilters[i].gameObject.activeSelf)
                {
                    if (grassSettings != null) 
                    {

                        terrainFaces[i].RenderGrass(grassSettings.grassMeshLOD0, grassSettings.grassMeshLOD1, grassSettings.grassMeshLOD2);
                    }
                   
                }
            }
        }
    }


    public void ConstructRandomPlanet(int res, ShapeSettings shapeSettings, ColorSettings colorSettings)
    {
        this.resolution = res;
        this.shapeSettings = shapeSettings;
        this.colorSettings = colorSettings;
        proceduralyGenerated = true;

        shapeGenerator.UpdateSettings(shapeSettings);
        colorGenerator.UpdateSettings(colorSettings);

        GeneratePlanet();

    }

    private void OnDisable()
    {
        colorGenerator.Cleanup();
    }

    private void OnDestroy()
    {
        if (terrainFaces != null)
        {
            foreach (var face in terrainFaces)
            {
                face?.ReleaseGrassBuffers();
            }
        }
    }


}
