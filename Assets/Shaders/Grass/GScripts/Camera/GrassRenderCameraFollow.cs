using UnityEngine;

public class GrassRenderCameraFollow : MonoBehaviour
{
    [Header("Character")]
    public Transform playerTransform; //can by an error I have 2 copies of the same character 

    [Header("Settings")]
    public float cameraHeight = 20f;


    private void Start()
    {
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

   
        Vector3 targetPosition = playerTransform.position;

        transform.position = new Vector3(targetPosition.x, cameraHeight, targetPosition.z);  
    }
}
