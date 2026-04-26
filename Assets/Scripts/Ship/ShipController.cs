using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [SerializeField] private ShipMovementSO movementData;

    [SerializeField] private InputValuesStruct inputValues;

    private Rigidbody rb;

    private bool boosting = false;

    private float currentBoostAmount;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentBoostAmount = movementData.maxBoostAmount;
    }

    public void OnUpDown(InputAction.CallbackContext context)
    {
        inputValues.upDown1D = context.ReadValue<float>();
    }

    public void OnThrust(InputAction.CallbackContext context)
    {
        inputValues.thrust1D = context.ReadValue<float>();
    }

    public void OnStrafe(InputAction.CallbackContext context)
    {
        inputValues.strafe1D = context.ReadValue<float>();
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        inputValues.roll1D = context.ReadValue<float>();
    }

    public void OnPitckYaw(InputAction.CallbackContext context)
    {
        inputValues.pitchYaw = context.ReadValue<Vector2>();
    }

    public void OnBoost(InputAction.CallbackContext context)
    {
        boosting = context.performed;
    }


    private void FixedUpdate()
    {
        HandleBoosting();

        HandleInput();
    }


    void HandleBoosting()
    {


        if (boosting && currentBoostAmount > 0f)
        {
            currentBoostAmount -= movementData.boostDeprecationRate;

            if (currentBoostAmount <= 0f)
            {
                boosting = false;
            }
        }
        else
        {
            if (currentBoostAmount < movementData.maxBoostAmount)
            {
                currentBoostAmount += movementData.boostRechangeRate;
            }
        }

        currentBoostAmount = Mathf.Clamp(currentBoostAmount, 0f, movementData.maxBoostAmount);
    }

    private void HandleInput()
    {
        rb.AddRelativeTorque(Vector3.back * inputValues.roll1D * movementData.rollTorque);

        rb.AddRelativeTorque(Vector3.right * Mathf.Clamp(-inputValues.pitchYaw.y, -1, 1) * movementData.pitchTorque);

        rb.AddRelativeTorque(Vector3.up * Mathf.Clamp(inputValues.pitchYaw.x, -1, 1) * movementData.yawTorque);


        float currentThrust = inputValues.thrust1D;

        if (boosting)
        {
            currentThrust = inputValues.thrust1D * movementData.boostMultiplier;
        }

        rb.AddRelativeForce(Vector3.forward * currentThrust * movementData.thrust);



        if (inputValues.thrust1D > 0.1f || inputValues.thrust1D < -0.1f)
        {

            if (rb.linearVelocity.magnitude > 0.1f)
            {
                float isInverceDirection = Vector3.Dot(rb.linearVelocity.normalized, transform.forward) > 0 ? 1f : -1f;

                rb.linearVelocity = transform.forward * (rb.linearVelocity.magnitude * isInverceDirection);
            }
        }



        rb.AddRelativeForce(Vector3.up * inputValues.upDown1D * movementData.upThrust);

        rb.AddRelativeForce(Vector3.right * inputValues.strafe1D * movementData.strafeThrust);




    }

}

