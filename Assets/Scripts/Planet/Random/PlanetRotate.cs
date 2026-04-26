using UnityEngine;

public class PlanetRotate : MonoBehaviour
{
    public Vector3 axis;
    public float speed = 20f;


    private void Start()
    {
        axis.Normalize();
    }


    private void Update()
    {
        transform.Rotate(axis, speed * Time.deltaTime);

    }
}
