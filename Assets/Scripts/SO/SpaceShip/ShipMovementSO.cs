using UnityEngine;
using UnityEngine.AdaptivePerformance;

[CreateAssetMenu(fileName = "NewShipMovementSO", menuName = "Ship/MovementSO", order = 1)]
public class ShipMovementSO : ScriptableObject
{
    public float yawTorque = 500f;

    public float pitchTorque = 1000f;

    public float rollTorque = 1000f;

    public float thrust = 100f;

    public float upThrust = 50f;

    public float strafeThrust = 50f;



    [Header("Boost Settings")]

    public float maxBoostAmount = 2f;

    public float boostDeprecationRate = 0.25f;

    public float boostRechangeRate = 0.5f;

    public float boostMultiplier = 5f;
    
}
