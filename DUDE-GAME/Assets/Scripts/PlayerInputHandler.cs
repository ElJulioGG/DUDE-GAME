using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        int index = playerInput.playerIndex;

        var allMovers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        playerMovement = allMovers.FirstOrDefault(m => m.getPlayerIndex() == index);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (playerMovement != null)
        {
            playerMovement.SetInputVector(context.ReadValue<Vector2>());
        }
    }
}
