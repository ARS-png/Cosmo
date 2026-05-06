using System;
using UnityEngine;

public class EffectsEvents
{
    public event Action<LayerMask, bool> onUseEffect;

    public void UseEffect(LayerMask mask, bool useEffect) => onUseEffect?.Invoke(mask, useEffect);
}
