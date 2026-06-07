using UnityEngine;

public class PlanetaryCamera : MonoBehaviour
{
    [Header("Связь")]
    public Transform target; // Сюда кидай игрока

    [Header("Настройки")]
    public float distance = 5f;
    public float heightOffset = 1.5f;
    public float sensitivity = 2f;
    public float cameraSmoothSpeed = 20f;

    private float mouseX;
    private float mouseY;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Просто собираем чистый ввод мыши
        mouseX += Input.GetAxis("Mouse X") * sensitivity;
        mouseY -= Input.GetAxis("Mouse Y") * sensitivity;
        mouseY = Mathf.Clamp(mouseY, -30f, 60f); // Ограничение вверх-вниз
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Создаем локальный поворот мыши
        Quaternion mouseRotation = Quaternion.Euler(mouseY, mouseX, 0f);

        // Умножаем его на текущее вращение игрока. Это заставляет камеру 
        // автоматически огибать планету вместе с его ногами без переворотов векторов
        Quaternion targetCameraRotation = target.rotation * mouseRotation;

        // Позиционируем камеру сзади
        Vector3 targetCenter = target.position + target.up * heightOffset;
        Vector3 desiredPosition = targetCenter - (targetCameraRotation * Vector3.forward * distance);

        // Применяем плавно, чтобы убрать микро-рывки от полигонов сферы
        transform.position = Vector3.Lerp(transform.position, desiredPosition, cameraSmoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetCameraRotation, cameraSmoothSpeed * Time.deltaTime);
    }
}
