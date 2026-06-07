using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }


    public @InputSystem_Actions Controls { get; private set; }

    private void Awake()
    {
     
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 

     
        Controls = new @InputSystem_Actions();

      
        SwitchToPlayer();
    }

    
    public void SwitchToPlayer()
    {
        if (Controls == null) return;

        Controls.ShipControls.Disable();
        Controls.PlayerControls.Enable();

        Debug.Log("[InputManager]: Активирована карта PlayerControls. Карта ShipControls отключена.");
    }


    public void SwitchToShip()
    {
        if (Controls == null) return;

        Controls.PlayerControls.Disable();
        Controls.ShipControls.Enable();

        Debug.Log("[InputManager]: Активирована карта ShipControls. Карта PlayerControls отключена.");
    }

    private void OnDestroy()
    {
        Controls?.Disable();
    }
}
