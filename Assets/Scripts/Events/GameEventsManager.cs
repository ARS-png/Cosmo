using UnityEngine;

public class GameEventsManager : MonoBehaviour
{
    public static GameEventsManager Instance { get; private set; }

    [HideInInspector] public EffectsEvents effectsEvents;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            //ManagersInit
            effectsEvents = new EffectsEvents();
           
        }
        else
        {
            Destroy(this.gameObject);
        }

    }
}
