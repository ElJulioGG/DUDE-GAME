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
        LinkComponents(index);
        DetectAndSetControllerType();
    }

    public void reasignController(int newIndex)
    {
        if (playerMovement != null)
            playerMovement.SetInputVector(Vector2.zero);

        index = newIndex;
        LinkComponents(newIndex);
        DetectAndSetControllerType();
    }

    private void LinkComponents(int targetIndex)
    {
        var allStats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        playerStats = allStats.FirstOrDefault(s => s.GetPlayerIndex() == targetIndex);

        var allMovers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        playerMovement = allMovers.FirstOrDefault(m => m.GetPlayerIndex() == targetIndex);

        var allHolders = FindObjectsByType<GunHolder>(FindObjectsSortMode.None);
        gunHolder = allHolders.FirstOrDefault(h => h.GetPlayerIndex() == targetIndex);
    }

    private void DetectAndSetControllerType()
    {
        var device = playerInput.devices.FirstOrDefault();
        if (device == null) return;

        int controllerType = 0;
        string deviceName = device.name.ToLower();

        if (deviceName.Contains("xbox"))
            controllerType = 0;
        else if (deviceName.Contains("switch") || deviceName.Contains("joycon"))
            controllerType = 1;
        else if (deviceName.Contains("dualshock") || deviceName.Contains("dualsense") || deviceName.Contains("ps"))
            controllerType = 2;

        switch (index)
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
        if (playerMovement != null)
            playerMovement.SetInputVector(context.ReadValue<Vector2>());
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        if (!GameManager.instance.playersCanAim) return;
        if (gunHolder != null)
            gunHolder.SetAimDirection(context.ReadValue<Vector2>());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!GameManager.instance.playersCanPickDrop) return;
        if (context.performed && gunHolder != null)
            gunHolder.HandlePickDrop();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!GameManager.instance.playersCanShoot) return;
        if (gunHolder == null) return;

        if (context.performed)
            gunHolder.HandleShoot();
        else if (context.canceled)
            gunHolder.HandleStopShoot();
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (!GameManager.instance.playersCanReload) return;
        if (context.performed && gunHolder != null)
            gunHolder.HandleReload();
    }

    public void OnPowerUp(InputAction.CallbackContext context)
    {
        if (!GameManager.instance.playersCanPowerUp)
        {
            AudioManager.Instance.PlaySound(FMODEvents.Instance.NoPowerUp, transform.position);
           return; 
        } 
        if (context.performed && playerStats != null && GameManager.instance.playersCanMove && playerStats.playerAlive)
            playerStats.usingPowerUp = true;
    }
}
