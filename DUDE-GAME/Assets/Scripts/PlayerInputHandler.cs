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
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if ((playerMovement != null)&& GameManager.instance.playersCanMove)
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
        if (gunHolder != null)
        {
            Vector2 aimDirection = context.ReadValue<Vector2>();
            gunHolder.SetAimDirection(aimDirection);
        }
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && gunHolder != null)
        {
            print("Player " + playerInput.playerIndex + " pick up weapon" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "");
            gunHolder.HandlePickDrop();
        }
    }
    public void OnShoot(InputAction.CallbackContext context)
    {
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
        if (context.performed && gunHolder != null)
        {
            //gunHolder.HandleReload();
        }
    }
    public void OnPowerUp(InputAction.CallbackContext context)
    {
        if (playerStats != null && GameManager.instance.playersCanMove && playerStats.playerAlive)
        {
            playerStats.usingPowerUp = true;
        }
    }


}
