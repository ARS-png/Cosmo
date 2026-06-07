using UnityEngine;
using static RandomRigidNoise;

[CreateAssetMenu(fileName = "NewRandomPlanetSettings", menuName = "Planet/Random Settings")]
public class RandomPlanetSettings : ScriptableObject
{
    [Header("Base Settings")]
    public RandomInt resolution;
    public RandomInt planetRadius;
    public RandomFloat atmosphereRadiusMultiplier;
    public RandomFloat waterRadiusMultiplier;

    [Header("Shape Noise Layers (Always 3 Layers)")]
    [Tooltip("Одиночная настройка для Element 0 (Тип: Simple, Маска: Выкл)")]
    public RandomSimpleNoise element0_SimpleSettings;

    [Tooltip("Одиночная настройка для Element 1 (Тип: Simple, Маска: Вкл)")]
    public RandomSimpleNoise element1_SimpleSettings; 

    [Tooltip("Одиночная настройка для Element 2 (Тип: Rigid, Маска: Вкл)")]
    public RandomRigidNoise element2_RigidSettings;   

    [Header("Visuals & Colors")]
    public RandomColor oceandepth;
    public RandomColor oceansurface;
    public RandomColor ground;
    public RandomColor cliff;
    public RandomColor clifftop;
    public RandomColor atmosphereColor; //old
    public RandomColor waterColor;
    public RandomColor grassColor;
    public RandomFloat smoothness;
    public RandomVector3[] scatteringCoefficients;


    [Header("Biomes Settings")]
    public RandomInt biomeCount;
    public RandomFloat biomeBlendAmount;
    public RandomFloat biomeTintPercent;
    public RandomSimpleNoise biomeNoiseSettings;
    public RandomFloat biomeNoiseStrength;
    public RandomFloat biomeNoiseOffset;


}
