using UnityEngine;

public class FauxGravityAttractor : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("Сила притяжения (для Земли ~ 9.81)")]
    public float gravityIntensity = -9.81f;


    public static System.Collections.Generic.List<FauxGravityAttractor> AllAttractors { get; private set; } = new System.Collections.Generic.List<FauxGravityAttractor>();

    // Добавьте автоматическую регистрацию планет при их включении и выключении
    private void OnEnable()
    {
        if (!AllAttractors.Contains(this)) AllAttractors.Add(this);
    }

    private void OnDisable()
    {
        if (AllAttractors.Contains(this)) AllAttractors.Remove(this);
    }
    //это полная ерудна



    public void Attract(Transform body, Rigidbody rididBody)
    {
        Vector3 gravityUp = (body.position - transform.position).normalized;
        Vector3 bodyUp = body.up;

        rididBody.AddForce(gravityUp * gravityIntensity);

        Quaternion targetRotation = Quaternion.FromToRotation(bodyUp, gravityUp) * body.rotation;

        body.rotation = Quaternion.Slerp(body.rotation, targetRotation, 50 * Time.deltaTime);
    }
}
