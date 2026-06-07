using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class FirstPersonPlanetController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public LayerMask groundLayer;
    public float jumpForce = 5f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 15f;
    [Tooltip("Перетащите сюда дочернюю камеру из головы персонажа")]
    public Transform playerCamera;

    [Header("Gravity Reference")]
    [Tooltip("Перетащите сюда объект планеты с компонентом FauxGravityAttractor")]
    public FauxGravityAttractor attractor;

    private Rigidbody rb;
    private Animator animator;

    private Vector3 moveInput;
    private bool isGrounded;
    private float cameraVerticalRotation = 0f;


    private readonly int animInputMagnitudeHash = Animator.StringToHash("InputMagnitude");
    private readonly int animIsGroundedHash = Animator.StringToHash("IsGrounded");

    private @InputSystem_Actions _controls => InputManager.Instance.Controls;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        rb.useGravity = false;


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void OnEnable()
    {
        if (InputManager.Instance == null || _controls == null) return;

        _controls.PlayerControls.Jump.started += OnJumpPressed;
    }

    private void OnDisable()
    {
        if (InputManager.Instance == null || _controls == null) return;
        _controls.PlayerControls.Jump.started -= OnJumpPressed;
    }

    private void Update()
    {
        if (_controls == null || playerCamera == null) return;


        Vector2 inputDir = _controls.PlayerControls.Move.ReadValue<Vector2>();
        moveInput = new Vector3(inputDir.x, 0, inputDir.y).normalized;


        float currentMagnitude = moveInput.magnitude;


        UpdateAnimatorValues(currentMagnitude);


        Vector2 mouseInput = _controls.PlayerControls.Look.ReadValue<Vector2>();



        float mouseX = mouseInput.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);


        float mouseY = mouseInput.y * mouseSensitivity * Time.deltaTime;
        cameraVerticalRotation -= mouseY;
        cameraVerticalRotation = Mathf.Clamp(cameraVerticalRotation, -85f, 85f);
        playerCamera.localRotation = Quaternion.Euler(cameraVerticalRotation, 0f, 0f);

    }

    private void FixedUpdate()
    {
        if (attractor != null)
        {
            attractor.Attract(transform, rb);
        }

        FindNearestPlanet();


        Vector3 normalizedInput = moveInput.normalized;
        Vector3 targetVelocity = transform.TransformDirection(normalizedInput) * moveSpeed;

       
        Vector3 verticalVelocity = Vector3.Project(rb.linearVelocity, transform.up);

      
        rb.linearVelocity = targetVelocity + verticalVelocity;


        Ray ray = new Ray(transform.position + transform.up * 0.1f, -transform.up);
        isGrounded = Physics.Raycast(ray, 0.35f, groundLayer);
    }

    private void OnJumpPressed(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);

            animator.SetTrigger("Jump");
        }
    }

    private void FindNearestPlanet()
    {
        // Проверяем, есть ли вообще планеты в глобальном списке
        if (FauxGravityAttractor.AllAttractors == null || FauxGravityAttractor.AllAttractors.Count == 0) return;

        FauxGravityAttractor nearest = null;
        float shortestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        // Ищем планету с минимальным расстоянием до игрока
        foreach (FauxGravityAttractor currentAttractor in FauxGravityAttractor.AllAttractors)
        {
            float distance = Vector3.Distance(currentPosition, currentAttractor.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = currentAttractor;
            }
        }

        attractor = nearest;
    }




    private void UpdateAnimatorValues(float magnitude)
    {

        animator.SetFloat(animInputMagnitudeHash, Mathf.Lerp(animator.GetFloat(animInputMagnitudeHash), magnitude, 10f * Time.deltaTime));

        animator.SetBool(animIsGroundedHash, isGrounded);
    }
}
