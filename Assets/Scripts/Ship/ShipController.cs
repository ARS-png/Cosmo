using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Ship Settings")]
    [SerializeField] private ShipMovementSO movementData;
    [SerializeField] private InputValuesStruct inputValues;

    [Header("Gravity (Определяется автоматически)")]
    //private FauxGravityAttractor attractor;
    private float planetSearchTimer = 0f;

    private GameObject _cachedPlayer;

    // Ссылка на инпуты через центральный менеджер
    private @InputSystem_Actions _controls => InputManager.Instance.Controls;

    private Rigidbody rb;
    private bool boosting = false;
    private float currentBoostAmount;

    private const float NORMAL_MASS = 1f;
    private const float PLAYER_NOT_CONTACT_MASS = 100000f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentBoostAmount = movementData.maxBoostAmount;

        // По умолчанию скрипт полета ВЫКЛЮЧЕН, пока игрок на ногах
        this.enabled = false;
    }

    private void OnDisable()
    {
        inputValues.thrust1D = 0f;
        inputValues.upDown1D = 0f;
        inputValues.strafe1D = 0f;
        inputValues.roll1D = 0f;
        inputValues.pitchYaw = Vector2.zero;
        boosting = false;
    }

    private void Update()
    {
        inputValues.thrust1D = _controls.ShipControls.Thrust.ReadValue<float>();
        inputValues.upDown1D = _controls.ShipControls.UpDown.ReadValue<float>();
        inputValues.strafe1D = _controls.ShipControls.Strafe.ReadValue<float>();
        inputValues.roll1D = _controls.ShipControls.Roll.ReadValue<float>();
        inputValues.pitchYaw = _controls.ShipControls.PitchYaw.ReadValue<Vector2>();
        boosting = _controls.ShipControls.Boost.IsPressed();

        if (_controls.ShipControls.Interact.triggered)
        {
            if (IsSafeToExit())
            {
                ExitShip();
            }
        }
    }

    private void FixedUpdate()
    {
        // 1. ОПТИМИЗИРОВАННЫЙ АВТОПОИСК БЛИЖАЙШЕЙ ПЛАНЕТЫ
        planetSearchTimer += Time.fixedDeltaTime;
        if (planetSearchTimer >= 0.25f)
        {
            FindNearestPlanet();
            planetSearchTimer = 0f;
        }

        // 2. ФИЗИЧЕСКОЕ ПРИТЯЖЕНИЕ КОРАБЛЯ К СФЕРЕ
        //if (attractor != null)
        //{
        //    attractor.Attract(transform, rb);
        //}

        HandleBoosting();
        HandleInput();
    }

    private void FindNearestPlanet()
    {
        // Если планет в глобальном списке нет, выходим
        if (FauxGravityAttractor.AllAttractors == null || FauxGravityAttractor.AllAttractors.Count == 0) return;

        FauxGravityAttractor nearest = null;
        float shortestSquareDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        // Быстрый поиск через квадрат расстояния (без вычисления квадратного корня)
        foreach (FauxGravityAttractor currentAttractor in FauxGravityAttractor.AllAttractors)
        {
            Vector3 directionToPlanet = currentAttractor.transform.position - currentPosition;
            float sqrDistance = directionToPlanet.sqrMagnitude;

            if (sqrDistance < shortestSquareDistance)
            {
                shortestSquareDistance = sqrDistance;
                nearest = currentAttractor;
            }
        }

   /*     attractor = nearest*/;
    }

    #region Посадка и Выход по одной кнопке Interact (E)

    private void OnTriggerStay(Collider other)
    {
        if (_cachedPlayer != null) return;

        if (other.CompareTag("Player"))
        {
            if (_controls.PlayerControls.Interact.triggered)
            {
                EnterShip(other.gameObject);
            }
        }
    }

    private void EnterShip(GameObject player)
    {
        rb.mass = NORMAL_MASS;
        _cachedPlayer = player;

        if (_cachedPlayer.TryGetComponent<Invector.vCharacterController.vThirdPersonInput>(out var playerInput))
        {
            playerInput.enabled = false;
        }

        _cachedPlayer.SetActive(false);

        CameraManager.Instance.ActivateShipCamera();

        this.enabled = true;

        InputManager.Instance.SwitchToShip();

        Debug.Log("[Корабль]: Вход выполнен успешно!");
    }

    private bool IsSafeToExit()
    {
        float maxSafeLinearSpeed = 1.5f;
        float maxSafeAngularSpeed = 0.8f;

        bool isLinearStop = rb.linearVelocity.magnitude <= maxSafeLinearSpeed;
        bool isAngularStop = rb.angularVelocity.magnitude <= maxSafeAngularSpeed;

        if (!isLinearStop || !isAngularStop) return false;

        Vector3 sphereCenter = transform.position - (transform.up * 1.5f);
        float sphereRadius = 2.2f;

        bool isGrounded = Physics.CheckSphere(sphereCenter, sphereRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        return isGrounded;
    }

    public void ExitShip()
    {
        if (_cachedPlayer == null) return;
        rb.mass = PLAYER_NOT_CONTACT_MASS;

        this.enabled = false;

        // Высаживаем игрока справа от корабля и выравниваем его ноги по направлению корабля
        Vector3 exitPosition = transform.position + (transform.right * 5f);
        _cachedPlayer.transform.position = exitPosition;
        _cachedPlayer.transform.rotation = Quaternion.FromToRotation(_cachedPlayer.transform.up, transform.up) * _cachedPlayer.transform.rotation;

        _cachedPlayer.SetActive(true);

        if (_cachedPlayer.TryGetComponent<Invector.vCharacterController.vThirdPersonInput>(out var playerInput))
        {
            playerInput.enabled = true;
        }

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.ActivatePlayerCamera();
        }

        InputManager.Instance.SwitchToPlayer();

        _cachedPlayer = null;

        Debug.Log("[Корабль]: Выход выполнен успешно!");
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 sphereCenter = transform.position - (transform.up * 1.5f);
        Gizmos.DrawWireSphere(sphereCenter, 2.2f);
    }

    void HandleBoosting()
    {
        if (boosting && currentBoostAmount > 0f)
        {
            currentBoostAmount -= movementData.boostDeprecationRate;
            if (currentBoostAmount <= 0f) boosting = false;
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
        if (boosting) currentThrust = inputValues.thrust1D * movementData.boostMultiplier;

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
