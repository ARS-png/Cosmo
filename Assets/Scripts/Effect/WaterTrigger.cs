using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask waterLayer; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EffectTrigger"))
            GameEventsManager.Instance.effectsEvents.UseEffect(waterLayer, true);
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("EffectTrigger"))
            GameEventsManager.Instance.effectsEvents.UseEffect(waterLayer, false);
    }
}
