using JetBrains.Annotations;
using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.Rendering;

public class Effect : MonoBehaviour
{
    [Header("Underwater effect")]
    public Volume underWaterEffect;

    [SerializeField] LayerMask waterLayer;


    private void Start()
    {
        if (underWaterEffect != null)
            underWaterEffect.enabled = false;    
    }


    private void OnEnable()
    {
        GameEventsManager.Instance.effectsEvents.onUseEffect += SetUnderwater;
    }


    private void OnDisable()
    {
        GameEventsManager.Instance.effectsEvents.onUseEffect -= SetUnderwater;
    }


    void SetUnderwater(LayerMask mask, bool isUnder)
    {
        if (mask == waterLayer && underWaterEffect != null)
            underWaterEffect.enabled = isUnder;
        Debug.Log("sdfgdfg");
    }


}
