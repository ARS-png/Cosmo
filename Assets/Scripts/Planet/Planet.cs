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

    public Transform currentCamera;


    public static int maxDetailLevel = 9; //  can change


    public float cullingMinAngle = 120;


    private bool hasGrass = true;
    private bool hasWater = true;


    [HideInInspector] public float distanceToCamera;

    [Range(2, 256)]
    [SerializeField] int resolution = 167;

    [SerializeField] private int chunkRes = 4;
    public bool autoUpdate = true;
    public enum FaceRenderMask { All, Up, Down, Left, Right, Forward, Back }
    public FaceRenderMask faceRenderMask;


    [Header("Configs")]
    public PlanetConfigSettings planetConfigSettings;
    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    public VegetationSettings vegetationSettings;

    [HideInInspector]
    public ColorGenerator colorGenerator = new ColorGenerator();

    [HideInInspector]
    public ShapeGenerator shapeGenerator = new ShapeGenerator();

    //for editor
    [HideInInspector]
    public bool shapeSettingsFoldout;

    [HideInInspector]
    public bool colorSettingsFoldout;


    private bool proceduralyGenerated = false;


    //For Chunks
    public Transform cameraTransform;


    [HideInInspector]
    public Vector3 position;

    [HideInInspector]
    public float radius;







    public static float[] detailLevelDistances = new float[] {
        Mathf.Infinity, // LOD 0 (Виден из космоса)
        20000f,         // LOD 1
        12000f,         // LOD 2
        6000f, 
        3000f,
        2500f,          // LOD 4
        1000f,          // LOD 5
        400f,           // LOD 6
        150f,       

        60f,            // LOD 8 
        40f             // LOD 9 (Самая высокая детализация и трава прямо под ногами)
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




    private IEnumerator Start()
    {
        position = this.gameObject.transform.position;

        if (!proceduralyGenerated)
        {
            yield return StartCoroutine(GeneratePlanetAsync());
        }


        if (terrainFaces != null)
        {
            foreach (var face in terrainFaces)
            {
                // ИСПРАВЛЕНИЕ: Передаем только один параметр — максимальное количество инстансов.
                // Все меши, шейдеры и материалы метод InitializeGrass теперь возьмет из списка настроек сам!
                face?.InitializeGrass(vegetationSettings.maxInstancesPerFace);

                yield return null;
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
        this.radius = shapeSettings.planetRadius;

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
            }


            if (!meshFilters[i].gameObject.TryGetComponent<MeshCollider>(out var collider))
            {
                meshFilters[i].gameObject.AddComponent<MeshCollider>();
            }


            if (meshFilters[i].TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.sharedMaterial = colorSettings.planetMaterial;
            }


            terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i], shapeSettings.planetRadius, this, meshFilters[i].gameObject, chunkRes);

            bool isRenderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            meshFilters[i].gameObject.SetActive(isRenderFace);
        }
    }


    void InitializeWater()
    {
        if (waterMeshFilters == null || waterMeshFilters.Length == 0)
        {
            waterMeshFilters = new MeshFilter[6];
        }

        waterTerrainFaces = new TerrainFace[6];
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        if (colorSettings.waterMaterial != null)
        {
            mpb.SetVector("_Deep_Water_Color", colorSettings.waterColor);
            Color horizonColor = colorSettings.atmosphereColor;
            mpb.SetVector("_Horizon_Color", new Vector4(horizonColor.r, horizonColor.g, horizonColor.b, 1));
        }

        for (int i = 0; i < waterMeshFilters.Length; i++)
        {
            if (waterMeshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("water_mesh");
                meshObj.transform.parent = transform;
                meshObj.transform.position = meshObj.transform.parent.position;

                MeshRenderer meshRenderer = meshObj.AddComponent<MeshRenderer>();
                waterMeshFilters[i] = meshObj.AddComponent<MeshFilter>();
                waterMeshFilters[i].mesh = new Mesh();

                if (colorSettings.waterMaterial != null)
                {
                    meshRenderer.sharedMaterial = colorSettings.waterMaterial;
                    meshRenderer.SetPropertyBlock(mpb);
                }

                if (meshObj.TryGetComponent<Collider>(out Collider collider))
                {
                    Destroy(collider);
                }

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else
            {
                if (waterMeshFilters[i].TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
                {
                    meshRenderer.SetPropertyBlock(mpb);
                }
            }

            waterTerrainFaces[i] = new TerrainFace(shapeGenerator, waterMeshFilters[i].sharedMesh, resolution, directions[i], shapeSettings.planetRadius * shapeSettings.waterRadiusMultiplier, this, waterMeshFilters[i].gameObject, chunkRes);
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            waterMeshFilters[i].gameObject.SetActive(renderFace);
        }

        CreateUnderWaterTriggerSphere();
    }


    private void CreateUnderWaterTriggerSphere()
    {
        if (!hasWater)
        {
            return;
        }


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


    public IEnumerator GeneratePlanetAsync()
    {
        Initialize();
        InitializeWater();


        yield return StartCoroutine(GenerateMeshesAsync());

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

        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }


    public IEnumerator GenerateMeshesAsync()
    {
        if (terrainFaces == null) yield break;

        for (int i = 0; i < 6; i++)
        {
            bool isRenderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            if (isRenderFace)
            {

                terrainFaces[i]?.ConstructTree();

                if (hasWater)
                {
                    waterTerrainFaces[i]?.ConstructWaterMesh(shapeSettings.planetRadius, shapeSettings.waterRadiusMultiplier); 
                }
              

                yield return null;
            }
        }

        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
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
                Destroy(collider);
            }
        }
        else
        {

            atmoshereGO = atmosphereTransform.gameObject;
        }


        float planetRadius = shapeSettings.planetRadius;
        float arm = shapeSettings.atmosphereRadiusMultiplier;
        float atmosphereRadius = planetRadius * (arm == 0 ? 1.3f : arm);
        float oceanRadius = shapeSettings.planetRadius * shapeSettings.waterRadiusMultiplier;


        atmoshereGO.transform.localScale = Vector3.one * atmosphereRadius * 2f;


        if (colorSettings.atmosphereMaterial != null && atmoshereGO.TryGetComponent<MeshRenderer>(out var renderer))
        {

            renderer.sharedMaterial = colorSettings.atmosphereMaterial;


            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propBlock);


            propBlock.SetVector("_PlanetCenter", transform.position);
            propBlock.SetFloat("_PlanetRadius", planetRadius);
            propBlock.SetFloat("_AtmosphereRadius", atmosphereRadius);
            propBlock.SetFloat("_OceanRadius", oceanRadius);
            propBlock.SetColor("_BaseColor", colorSettings.atmosphereColor);


            renderer.SetPropertyBlock(propBlock);
        }
    }


    void GenerateWaterMesh()
    {
        if (hasWater == false)
        {
            Debug.Log("Planet has not water");
            return;
        }

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

        if (CameraManager.Instance != null)
        {
            Transform activeTarget = CameraManager.Instance.GetCurrentCameraTransform();
            if (activeTarget != null)
            {
                currentCamera = activeTarget;
            }
        }


        if (currentCamera != null)
        {
            cameraTransform = currentCamera;
            position = transform.position;

            distanceToCamera = Vector3.Distance(transform.position, currentCamera.position);
        }





        if (proceduralyGenerated && terrainFaces != null && hasGrass) //sd;lfkjasd;lfkjasdl;fjas df;asldfkjas;dlfkjasd l;fkas;dflkasjdf;kasj df;lkasjdf;lkasjdf;lkasjf;ljasdfl;kashgk;lurlvk,djhgo;ielkj'sldn
        {
            for (int i = 0; i < terrainFaces.Length; i++)
            {
                if (terrainFaces[i] != null && meshFilters[i].gameObject.activeSelf)
                {
                    if (vegetationSettings != null)
                    {

                        terrainFaces[i].RenderGrass();
                    }
                }
            }
        }
    }


    public void ConstructRandomPlanet(int res, PlanetConfigSettings planetConfigSettings, ShapeSettings shapeSettings, ColorSettings colorSettings, VegetationSettings vegetationSettings)
    {
        this.resolution = res;

        this.planetConfigSettings = planetConfigSettings;  //@!
        this.shapeSettings = shapeSettings;
        this.colorSettings = colorSettings;

        this.vegetationSettings = vegetationSettings;


        //this.hasWater = planetConfigSettings.hasWater;
        //this.hasGrass = planetConfigSettings.hasGrass;


       


        proceduralyGenerated = true;

        shapeGenerator.UpdateSettings(shapeSettings);
        colorGenerator.UpdateSettings(colorSettings);


        GeneratePlanet();


        if (Application.isPlaying)
        {
            StartCoroutine(PlanetGenerationLoop()); //
        }
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
                face?.ReleaseVegetationBuffers();
            }
        }
    }


}
