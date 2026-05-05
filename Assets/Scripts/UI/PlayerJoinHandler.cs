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
        // Activar solo el primer jugador, desactivar los demás
        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            playerPrefabs[i].SetActive(i == 0);
        }
    }

    void Update()
    {
        // Si ya están todos unidos, salir
        if (joinedPlayers >= playerPrefabs.Length) return;

        foreach (var device in Gamepad.all)
        {
            // Este dispositivo ya ha sido usado por un jugador
            if (IsDeviceAlreadyUsed(device)) continue;

            // Esperamos que presione el botón sur (A o X) para unirse
            if (device.wasUpdatedThisFrame && device.buttonSouth.wasPressedThisFrame)
            {
                JoinPlayer(device);
                break;
            }
        }
    }

    bool IsDeviceAlreadyUsed(InputDevice device)
    {
        // Verificamos si ya está emparejado con algún jugador manualmente
        foreach (var player in playerPrefabs)
        {
            var input = player.GetComponent<PlayerInput>();
            if (input != null && input.devices.Contains(device))
            {
                return true;
            }
        }

        // También verificamos si ya está emparejado en InputSystem
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

            // También asociamos explícitamente el dispositivo con el PlayerInput
            playerInput.SwitchCurrentControlScheme(device);
        }

        joinedPlayers++;
    }
}
