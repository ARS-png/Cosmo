using UnityEngine;
using static UnityEngine.Mathf;


[CreateAssetMenu(menuName = "Atmpsphere")]
public class AtmosphereSettings : ScriptableObject
{
    public Shader atmosphereShader;
    public int numInScaterringPoints = 10;
    public int numOpticalDepthPoints = 10;
    public float densityFalloff = 9;
    public float atmosphereScale = 1f; //
    public Vector3 waveLengths = new Vector3(700, 530, 440);
    public float scatteringStrength = 1;



    public void SetProperties(Material material, float bodyRadius)
    {
        float scatterR = Pow(400 / waveLengths.x, 4) * scatteringStrength;
        float scatterG = Pow(400 / waveLengths.y, 4) * scatteringStrength;
        float scatterB = Pow(400 / waveLengths.z, 4) * scatteringStrength;
        Vector3 scatteringCoefficients = new Vector3(scatterR, scatterG, scatterB);

        material.SetVector("scatteringCoefficients", scatteringCoefficients);
        material.SetInt("_NumInScatteringPoints", numInScaterringPoints);
        material.SetInt("_NumOpticalDepthPoints", numOpticalDepthPoints);
        material.SetFloat("_AtmosphereRadius", (1 + atmosphereScale) * bodyRadius);
        material.SetFloat("_PlanetRadius", bodyRadius);
        material.SetFloat("_DensityFalloff", densityFalloff);

    }
}
