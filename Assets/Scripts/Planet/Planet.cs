
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

    GameObject atmoshereGO;


    [SerializeField, HideInInspector]
    TerrainFace[] waterTerrainFaces;


    [Range(2, 256)]
    [SerializeField] int resolution = 10;
    public bool autoUpdate = true;
    public enum FaceRenderMask { All, Up, Down, Left, Right, Forward, Back }
    public FaceRenderMask faceRenderMask;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    public WaterSettings waterSettings;

    [SerializeField] ColorGenerator colorGenerator = new ColorGenerator();
    [SerializeField] ShapeGenerator shapeGenerator = new ShapeGenerator();

    [HideInInspector]
    public bool shapeSettingsFoldout;

    [HideInInspector]
    public bool colorSettingsFoldout;


    private bool proceduralyGenerated = false;

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

            terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].mesh, resolution, directions[i]);
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

                if (meshObj.TryGetComponent<Collider>(out Collider collider)) 
                {
                    Destroy(collider);
                }

                if (meshObj.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                

            }


            waterTerrainFaces[i] = new TerrainFace(shapeGenerator, waterMeshFilters[i].mesh, resolution, directions[i]);
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            waterMeshFilters[i].gameObject.SetActive(renderFace);
        }
    }

    public void OnShapeSettingsUpdated()
    {
        if (autoUpdate == true)
        {
            Initialize();
            InitializeWater();
            GenerateMesh();
            GenerateWaterMesh();
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
        GenerateWaterMesh();
        GenerateColors();
        GenerateAtmosphere();
    }


    void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
            {
                terrainFaces[i].ConstructMesh(proceduralyGenerated);

                if (meshFilters[i].TryGetComponent<MeshCollider>(out var collider))
                {
                    collider.sharedMesh = null;
                    collider.sharedMesh = meshFilters[i].mesh;
                }
            }
        }

        colorGenerator.UpdateElavation(shapeGenerator.elevationMinMax);
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
        float atmosphereRadius = planetRadius * (shapeSettings.atmosphereRadiusMultiplier == 0 ? 1.3f: shapeSettings.atmosphereRadiusMultiplier);
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
            mat.SetColor("_BaseColor", RandomXT.RandomColor());
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
                terrainFaces[i].UpdateUVs(colorGenerator);
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
        if (!proceduralyGenerated)
            GeneratePlanet();
    }

    private void OnDisable()
    {
        colorGenerator.Cleanup();
    }

}
