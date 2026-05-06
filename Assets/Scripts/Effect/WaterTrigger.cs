using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask WaterLayer;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EffectTrigger"))
            GameEventsManager.Instance.effectsEvents.UseEffect(WaterLayer, true);
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("EffectTrigger"))
            GameEventsManager.Instance.effectsEvents.UseEffect(WaterLayer, false);
    }
}
