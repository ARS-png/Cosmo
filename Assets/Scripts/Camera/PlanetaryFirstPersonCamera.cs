using UnityEngine;

public class PlanetaryFirstPersonCamera : MonoBehaviour
{
    [Header("Mouse Settings")]
    [Tooltip("Чувствительность мыши")]
    public float mouseSensitivity = 2f;

    [Header("References")]
    [Tooltip("ПЕРЕТАЩИТЕ СЮДА КАМЕРУ ИЗ ИЕРАРХИИ")]
    public Camera targetCamera;
    [Tooltip("Перетащите сюда самого игрока (родительский объект)")]
    public Transform playerBody;

    private float xRotation = 0f;

    private void Start()
    {
        // Блокируем курсор в центре экрана и делаем его невидимым
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Если забыли перетащить камеру, пытаемся найти её на этом же объекте
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        // Если игрок не назначен, берем родительский объект
        if (playerBody == null)
        {
            playerBody = transform.parent;
        }
    }

    private void Update()
    {
        if (InputManager.Instance == null || InputManager.Instance.Controls == null) return;
        if (targetCamera == null) return; // Защита от ошибок, если камера так и не найдена

        // Читаем дельту движения мыши из Новой Системы Ввода
        Vector2 lookInput = InputManager.Instance.Controls.PlayerControls.Look.ReadValue<Vector2>();

        // Вычисляем углы поворота
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime * 50f;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime * 50f;

        // 1. Поворот самой камеры вверх/вниз (локальная ось X)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f); // Ограничение взгляда
        targetCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 2. Поворот всего тела игрока влево/вправо
        if (playerBody != null)
        {
            playerBody.Rotate(playerBody.up, mouseX, Space.World);
        }
    }
}
