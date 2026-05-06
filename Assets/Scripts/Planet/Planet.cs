
using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using UnityEditor.SettingsManagement;
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

    //than destroy
    public GameObject testGO;


    [Range(2, 256)]
    [SerializeField] int resolution = 10;
    public bool autoUpdate = true;
    public enum FaceRenderMask { All, Up, Down, Left, Right, Forward, Back }
    public FaceRenderMask faceRenderMask;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    public WaterSettings waterSettings;

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
    public float size;


    public static Dictionary<int, float> detailLevelDistances = new Dictionary<int, float>()
    {
        { 0, Mathf.Infinity},
        { 1, 60f},
        { 2, 25f},
    };

    private void Awake()
    {
        playerTransform = GameObject.FindWithTag("Player").transform;
    }


    

    void Initialize()
    {

        shapeGenerator.UpdateSettings(shapeSettings);
        colorGenerator.UpdateSettings(colorSettings);

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

            terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].mesh, resolution, directions[i], shapeSettings.planetRadius, this);
            bool isRenderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            meshFilters[i].gameObject.SetActive(isRenderFace);
        }
    }


    //Now is unenabled 
    void InitializeWater()
    {
        //if (waterMeshFilters == null || waterMeshFilters.Length == 0)
        //{
        //    waterMeshFilters = new MeshFilter[6];
        //}

        //waterTerrainFaces = new TerrainFace[6];
        //Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };


        //for (int i = 0; i < waterMeshFilters.Length; i++)
        //{
        //    if (waterMeshFilters[i] == null)
        //    {
        //        GameObject meshObj = new GameObject("water_mesh");
        //        meshObj.transform.parent = transform;
        //        meshObj.transform.position = meshObj.transform.parent.position;


        //        meshObj.AddComponent<MeshRenderer>();
        //        waterMeshFilters[i] = meshObj.AddComponent<MeshFilter>();
        //        waterMeshFilters[i].mesh = new Mesh();


        //        if (colorSettings.waterMaterial != null)
        //        {
        //            waterMeshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.waterMaterial;


        //            MeshRenderer meshRenderer = meshObj.GetComponent<MeshRenderer>();
        //            Material mat = meshRenderer.material = colorSettings.waterMaterial;


        //            mat.SetVector("_Deep_Water_Color", colorSettings.waterColor); //иожно и больше различий

        //            Color horizonColor = colorSettings.atmosphereColor;
        //            mat.SetVector("_Horizon_Color", new Vector4(horizonColor.r, horizonColor.g, horizonColor.b, 1));
        //        }


        //        if (meshObj.TryGetComponent<Collider>(out Collider collider))
        //        {
        //            Destroy(collider);
        //        }


        //        if (meshObj.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
        //        {
        //            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        //        }
        //    }
        //    waterTerrainFaces[i] = new TerrainFace(shapeGenerator, waterMeshFilters[i].mesh, resolution, directions[i], 2012, this);//
        //    bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
        //    waterMeshFilters[i].gameObject.SetActive(renderFace);
        //}



        //CreateUnderWaterTriggerSphere();

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


        float diameter = shapeSettings.planetRadius * shapeSettings.waterRadiusMultiplier * 2;
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
            GenerateColors();
        }
    }

    public void GeneratePlanet()
    {
        Initialize();
        InitializeWater();
        GenerateMesh();
        //GenerateWaterMesh();s
        GenerateColors();
        GenerateAtmosphere();
    }




    //ConstructMeshTrees
    void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
            {
                terrainFaces[i].ConstructTree();
       

                //if (meshFilters[i].TryGetComponent<MeshCollider>(out var collider))
                //{
                //    collider.sharedMesh = null;
                //    collider.sharedMesh = meshFilters[i].mesh;
                //}
            }
        }

        colorGenerator.UpdateElavation(shapeGenerator.elevationMinMax);
    }


    public IEnumerator ConstructMeshCourutine()
    {
        Debug.Log("new mesh is generated");
        GenerateMesh();
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;
    }

    private void Update()
    {
        StartCoroutine(ConstructMeshCourutine());
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


    void GenerateColors()
    {
        colorGenerator.UpdateSettings(colorSettings);
        colorGenerator.UpdateColors(colorSettings);

        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
            {
                //terrainFaces[i].UpdateUVs(colorGenerator);
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


    private void Start()
    {
        position = this.gameObject.transform.position;

        if (!proceduralyGenerated)
            GeneratePlanet();



    }


    private void OnDisable()
    {
        colorGenerator.Cleanup();
    }

}
