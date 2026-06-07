using UnityEngine;
using UnityEngine.InputSystem; 

namespace Invector.vCharacterController
{
    public class vThirdPersonInput : MonoBehaviour
    {
        #region Variables       

        private @InputSystem_Actions _controls => InputManager.Instance.Controls;

        [Header("Camera Settings")]
        [Tooltip("Чувствительность мыши для Новой системы ввода. Рекомендуется от 0.01 до 0.1")]
        public float mouseSensitivity = 0.04f;

        [HideInInspector] public vThirdPersonController cc;
        [HideInInspector] public vThirdPersonCamera tpCamera;

        // Сделали публичной, чтобы вы могли увидеть её в инспекторе без режима Debug
        [Header("Debug Info (Сюда можно перетащить камеру вручную)")]
        public Camera cameraMain;

        private bool _isSprintButtonHeld;

        #endregion

        protected virtual void Start()
        {
            InitilizeController();
            InitializeTpCamera();

            // Первичная проверка при старте
            if (cameraMain == null && Camera.main != null)
            {
                cameraMain = Camera.main;
                Debug.Log($"<color=green>[Invector Debug]</color> Камера успешно привязана в Start(): {cameraMain.name}");
            }
        }

        protected virtual void OnEnable()
        {
            if (InputManager.Instance == null || _controls == null)
            {
                Debug.LogError("<color=red>[Invector Debug]</color> InputManager.Instance или Controls равны null! Проверьте порядок инициализации скриптов.");
                return;
            }

            _controls.PlayerControls.Jump.started += OnJump;
            _controls.PlayerControls.Strafe.started += OnStrafe;
            _controls.PlayerControls.Sprint.started += OnSprintStart;
            _controls.PlayerControls.Sprint.canceled += OnSprintStop;
        }

        protected virtual void OnDisable()
        {
            if (InputManager.Instance == null || _controls == null) return;

            _controls.PlayerControls.Jump.started -= OnJump;
            _controls.PlayerControls.Strafe.started -= OnStrafe;
            _controls.PlayerControls.Sprint.started -= OnSprintStart;
            _controls.PlayerControls.Sprint.canceled -= OnSprintStop;

            if (cc != null) cc.input = Vector3.zero;
            _isSprintButtonHeld = false;
        }

        protected virtual void FixedUpdate()
        {
            if (cc != null)
            {
                if (cameraMain != null)
                {
                    cc.UpdateMoveDirection(cameraMain.transform);
                }
                else
                {
                    // Если это сработает — персонаж пойдет по мировым осям
                    Debug.LogWarning("<color=yellow>[Invector Debug]</color> ВНИМАНИЕ: cc.UpdateMoveDirection() вызван БЕЗ трансформа камеры. Персонаж двигается по мировым осям.");
                    cc.UpdateMoveDirection(null);
                }

                cc.UpdateMotor();
                cc.ControlLocomotionType();
                cc.ControlRotationType();
            }
        }

        protected virtual void Update()
        {
            InputHandle();
            if (cc != null) cc.UpdateAnimator();
        }

        public virtual void OnAnimatorMove()
        {
            if (cc != null) cc.ControlAnimatorRootMotion();
        }

        #region Basic Locomotion Inputs

        protected virtual void InitilizeController()
        {
            cc = GetComponent<vThirdPersonController>();
            if (cc != null)
            {
                cc.Init();
            }
            else
            {
                Debug.LogError("<color=red>[Invector Debug]</color> Компонент vThirdPersonController не найден на этом объекте!");
            }
        }

        protected virtual void InitializeTpCamera()
        {
            if (tpCamera == null)
            {
                tpCamera = FindFirstObjectByType<vThirdPersonCamera>();
                if (tpCamera == null)
                {
                    Debug.LogWarning("<color=yellow>[Invector Debug]</color> vThirdPersonCamera не найдена на сцене.");
                    return;
                }
                tpCamera.SetMainTarget(this.transform);
                tpCamera.Init();
            }
        }

        protected virtual void InputHandle()
        {
            MoveInput();
            CameraInput();
            SprintInput();
        }

        public virtual void MoveInput()
        {
            if (InputManager.Instance == null || _controls == null) return;

            Vector2 moveDir = _controls.PlayerControls.Move.ReadValue<Vector2>();

            if (cc != null)
            {
                cc.input.x = moveDir.x;
                cc.input.z = moveDir.y;
            }
        }

        protected virtual void CameraInput()
        {
            if (!cameraMain)
            {
                if (!Camera.main)
                {
                    Debug.LogError("<color=red>[Invector Debug]</color> На сцене отсутствует камера с тегом 'MainCamera'! Пожалуйста, назначьте тег вашей камере.");
                }
                else
                {
                    cameraMain = Camera.main;
                    if (cc != null) cc.rotateTarget = cameraMain.transform;
                    Debug.Log($"<color=cyan>[Invector Debug]</color> Камера найдена в Update() через Camera.main: {cameraMain.name}");
                }
            }

            if (tpCamera == null || InputManager.Instance == null || _controls == null) return;

            // Считываем дельту мыши из карты PlayerControls
            Vector2 lookDir = _controls.PlayerControls.Look.ReadValue<Vector2>();

            // Корректируем значения пикселей мыши через deltaTime и ползунок чувствительности
            float mouseX = lookDir.x * mouseSensitivity * Time.deltaTime * 100f;
            float mouseY = lookDir.y * mouseSensitivity * Time.deltaTime * 100f;

            // Передаем правильные и сглаженные значения в камеру Invector
            tpCamera.RotateCamera(mouseX, mouseY);
        }

        protected virtual void SprintInput() { if (cc != null) cc.Sprint(_isSprintButtonHeld); }
        private void OnSprintStart(InputAction.CallbackContext context) => _isSprintButtonHeld = true;
        private void OnSprintStop(InputAction.CallbackContext context) => _isSprintButtonHeld = false;
        protected virtual void OnStrafe(InputAction.CallbackContext context) { if (cc != null) cc.Strafe(); }
        protected virtual bool JumpConditions() => cc != null && cc.isGrounded && cc.GroundAngle() < cc.slopeLimit && !cc.isJumping && !cc.stopMove;

        protected virtual void OnJump(InputAction.CallbackContext context)
        {
            if (JumpConditions() && cc != null) cc.Jump();
        }

        #endregion       
    }
}
