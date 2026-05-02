using UnityEngine;

[CreateAssetMenu]
public class RandomPlanetSettings : ScriptableObject
{
    public RandomInt resolution;
    public RandomInt planetRadius;
    public RandomInt noiseLayersAmount;
    public RandomSimpleNoise simpleTerrainSettings;
    public RandomColor oceandepth;
    public RandomColor oceansurface;
    public RandomColor ground;
    public RandomColor cliff;
    public RandomColor clifftop;
    public RandomFloat biomeTintPercent;
    public RandomSimpleNoise biomeNoiseSettings;
    public RandomFloat biomeNoiseStrength;
    public RandomFloat biomeNoiseOffset;
    public RandomInt biomeCount;
    public RandomFloat biomeBlendAmount;
    public RandomFloat smoothness;
    public RandomRigidNoise rigidTerrainSettings;
    public RandomSimpleNoise[] terrainConstants;
    public RandomFloat atmosphereRadiusMultiplier;
    public RandomFloat waterRadiusMultiplier;
    public RandomColor atmosphereColor;
    public RandomColor waterColor;
}
