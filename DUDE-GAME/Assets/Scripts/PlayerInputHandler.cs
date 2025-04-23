using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private GunHolder gunHolder;
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        int index = playerInput.playerIndex;

        // Link movement
        var allMovers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        playerMovement = allMovers.FirstOrDefault(m => m.getPlayerIndex() == index);

        // Link gun system
        var allHolders = FindObjectsByType<GunHolder>(FindObjectsSortMode.None);
        gunHolder = allHolders.FirstOrDefault(h => h.GetPlayerIndex() == index);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (playerMovement != null)
        {
            playerMovement.SetInputVector(context.ReadValue<Vector2>());
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

}
