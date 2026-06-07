using UnityEngine;

public class FauxGravityBody : MonoBehaviour
{
    public FauxGravityAttractor attractor;

    private Transform myTransform;
    private Rigidbody myRigidbody;

    private void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        myRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        myRigidbody.useGravity = false;
        myTransform = transform;
    }

    private void Update()
    {
        if (myRigidbody != null) 
        {
            attractor.Attract(myTransform, myRigidbody);
        }

    }
}
