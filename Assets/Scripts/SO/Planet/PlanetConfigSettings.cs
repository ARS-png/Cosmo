using UnityEngine;

[CreateAssetMenu(fileName = "NewPlanetConfig", menuName = "SpaceGame/Planet Config")]
public class PlanetConfigSettings : ScriptableObject
{
    [Header("Ecosystem")]
    public bool hasWater;
    public bool hasGrass;
}
