using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private GameObject invectorCameraGO;
    [SerializeField] private GameObject mainCameraGO;

    private Camera invectorCamera;
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheCameras();
        ActivatePlayerCamera();
    }

    private void CacheCameras()
    {
        if (invectorCameraGO != null)
        {
            invectorCameraGO.TryGetComponent<Camera>(out invectorCamera);
        }

        if (mainCameraGO != null)
        {
            mainCameraGO.TryGetComponent<Camera>(out mainCamera);
        }

        if (invectorCamera == null) Debug.LogError("[CameraManager]: Компонент Camera НЕ НАЙДЕН на invectorCameraGO!");
        if (mainCamera == null) Debug.LogError("[CameraManager]: Компонент Camera НЕ НАЙДЕН на mainCameraGO!");
    }

    public Camera GetActiveCamera()
    {
        if (invectorCameraGO != null && invectorCameraGO.activeInHierarchy)
        {
            return invectorCamera;
        }

        if (mainCameraGO != null && mainCameraGO.activeInHierarchy)
        {
            return mainCamera;
        }

        return Camera.main;
    }

    public void ActivatePlayerCamera()
    {
        if (mainCameraGO != null) mainCameraGO.SetActive(false);
        if (invectorCameraGO != null) invectorCameraGO.SetActive(true);

        var playerInput = Object.FindFirstObjectByType<Invector.vCharacterController.vThirdPersonInput>();
        if (playerInput != null && invectorCamera != null)
        {
            playerInput.cameraMain = invectorCamera;
            if (playerInput.cc != null)
                playerInput.cc.rotateTarget = invectorCamera.transform;
        }

        Debug.Log("[CameraManager]: Включена физическая камера ИГРОКА (Invector)");
    }

    public void ActivateShipCamera()
    {
        if (invectorCameraGO != null) invectorCameraGO.SetActive(false);
        if (mainCameraGO != null) mainCameraGO.SetActive(true);

        Debug.Log("[CameraManager]: Включена камера КОРАБЛЯ (MainCamera + Cinemachine)");
    }




    public Vector3 GetCurrentCameraPosition()
    {
        if (invectorCameraGO != null && invectorCameraGO.activeInHierarchy)
        {
            return invectorCameraGO.transform.position;
        }

        if (mainCameraGO != null && mainCameraGO.activeInHierarchy)
        {
            return mainCameraGO.transform.position;
        }

        return Vector3.zero;
    }


    public Transform GetCurrentCameraTransform()
    {
        if (invectorCameraGO != null && invectorCameraGO.activeInHierarchy)
        {
            return invectorCameraGO.transform;
        }

        if (mainCameraGO != null && mainCameraGO.activeInHierarchy)
        {
            return mainCameraGO.transform;
        }

        return null;
    }
}
