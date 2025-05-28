using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using System.Linq;

public class PlayerJoinHandler : MonoBehaviour
{
    public GameObject[] playerPrefabs;
    private int joinedPlayers = 1;

    void Start()
    {
        // Activar solo el primer jugador, desactivar los dem�s
        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            playerPrefabs[i].SetActive(i == 0);
        }
    }

    void Update()
    {
        // Si ya est�n todos unidos, salir
        if (joinedPlayers >= playerPrefabs.Length) return;

        foreach (var device in Gamepad.all)
        {
            // Este dispositivo ya ha sido usado por un jugador
            if (IsDeviceAlreadyUsed(device)) continue;

            // Esperamos que presione el bot�n sur (A o X) para unirse
            if (device.wasUpdatedThisFrame && device.buttonSouth.wasPressedThisFrame)
            {
                JoinPlayer(device);
                break;
            }
        }
    }

    bool IsDeviceAlreadyUsed(InputDevice device)
    {
        // Verificamos si ya est� emparejado con alg�n jugador manualmente
        foreach (var player in playerPrefabs)
        {
            var input = player.GetComponent<PlayerInput>();
            if (input != null && input.devices.Contains(device))
            {
                return true;
            }
        }

        // Tambi�n verificamos si ya est� emparejado en InputSystem
        return InputUser.FindUserPairedToDevice(device) != null;
    }

    void JoinPlayer(InputDevice device)
    {
        if (joinedPlayers >= playerPrefabs.Length) return;

        var playerObject = playerPrefabs[joinedPlayers];
        playerObject.SetActive(true);

        var playerInput = playerObject.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            // Emparejamos dispositivo con nuevo usuario
            InputUser user = InputUser.PerformPairingWithDevice(device);
            user.AssociateActionsWithUser(playerInput.actions);

            // Tambi�n asociamos expl�citamente el dispositivo con el PlayerInput
            playerInput.SwitchCurrentControlScheme(device);
        }

        joinedPlayers++;
    }
}
