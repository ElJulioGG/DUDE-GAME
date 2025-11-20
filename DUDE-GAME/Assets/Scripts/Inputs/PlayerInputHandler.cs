using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class PlayerInputHandler : MonoBehaviour
{
    public PlayerMovement playerMovement;
    public PlayerStats playerStats;
    public GunHolder gunHolder;
    public PlayerInput playerInput;
    public int index;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        index = playerInput.playerIndex;

        // Link Stats
        var allStats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        playerStats = allStats.FirstOrDefault(s => s.GetPlayerIndex() == index);
        // Link movement
        var allMovers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        playerMovement = allMovers.FirstOrDefault(m => m.GetPlayerIndex() == index);

        // Link gun system
        var allHolders = FindObjectsByType<GunHolder>(FindObjectsSortMode.None);
        gunHolder = allHolders.FirstOrDefault(h => h.GetPlayerIndex() == index);

        DetectAndSetControllerType();
    }
    
    public void reasignController(int newIndex){
        playerInput = GetComponent<PlayerInput>();
        index = playerInput.playerIndex;

        // Link Stats
        var allStats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        playerStats = allStats.FirstOrDefault(s => s.GetPlayerIndex() == newIndex);
        // Link movement
        var allMovers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        playerMovement = allMovers.FirstOrDefault(m => m.GetPlayerIndex() == newIndex);

        // Link gun system
        var allHolders = FindObjectsByType<GunHolder>(FindObjectsSortMode.None);
        gunHolder = allHolders.FirstOrDefault(h => h.GetPlayerIndex() == newIndex);

        DetectAndSetControllerType();
    }
    private void DetectAndSetControllerType()
    {
        var device = playerInput.devices.FirstOrDefault();
        if (device == null) return;

        int controllerType = 0; // Default to Xbox

        string deviceName = device.name.ToLower();

        if (deviceName.Contains("xbox"))
            controllerType = 0;
        else if (deviceName.Contains("switch") || deviceName.Contains("joycon"))
            controllerType = 1;
        else if (deviceName.Contains("dualshock") || deviceName.Contains("dualsense") || deviceName.Contains("ps"))
            controllerType = 2;

        switch (playerInput.playerIndex)
        {
            case 0: GameManager.instance.player1ControllerType = controllerType; break;
            case 1: GameManager.instance.player2ControllerType = controllerType; break;
            case 2: GameManager.instance.player3ControllerType = controllerType; break;
            case 3: GameManager.instance.player4ControllerType = controllerType; break;
        }

        Debug.Log($"Player {index} is using controller: {device.displayName}, type: {controllerType}, raw name: {device.name}");
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if ((playerMovement != null) && GameManager.instance.playersCanMove)
        {
            playerMovement.SetInputVector(context.ReadValue<Vector2>());
        }
        else
        {
            playerMovement.SetInputVector(Vector2.zero);
        }
    }
    
    public void OnAim(InputAction.CallbackContext context)
    {
        if(!GameManager.instance.playersCanAim) return;
        if (gunHolder != null)
        {
            Vector2 aimDirection = context.ReadValue<Vector2>();
            gunHolder.SetAimDirection(aimDirection);
        }
    }
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        if(!GameManager.instance.playersCanPickDrop) return;
        if (context.performed && gunHolder != null)
        {
            print("Player " + playerInput.playerIndex + " pick up weapon");
            gunHolder.HandlePickDrop();
        }
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if(!GameManager.instance.playersCanShoot) return;
        if (gunHolder == null) return;

        if (context.performed)
        {
            gunHolder.HandleShoot(); // Called on press
        }
        else if (context.canceled)
        {
            gunHolder.HandleStopShoot(); // Called on release
        }
    }
    
    public void OnReload(InputAction.CallbackContext context)
    {
        if(!GameManager.instance.playersCanReload) return;
        print("Reload 1");
        if (context.performed && gunHolder != null)
        {
            print("Reload 2");
            gunHolder.HandleReload();
        }
    }
    
    public void OnPowerUp(InputAction.CallbackContext context)
    {
        if(!GameManager.instance.playersCanPowerUp) return;
        if (context.performed && playerStats != null && GameManager.instance.playersCanMove && playerStats.playerAlive)
        {
            playerStats.usingPowerUp = true;
        }
    }
}
