using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class PlayerJoinHandler : MonoBehaviour
{
    public GameObject[] playerPrefabs; // Prefabs: mano1, mano2, etc.
    private int joinedPlayers = 0;

    void Update()
    {
        if (joinedPlayers >= playerPrefabs.Length) return;

        foreach (var device in Gamepad.all)
        {
            if (InputUser.FindUserPairedToDevice(device) != null) continue;

            if (device.wasUpdatedThisFrame && device.buttonSouth.wasPressedThisFrame)
            {
                JoinPlayer(device);
                break;
            }
        }
    }

    void JoinPlayer(InputDevice device)
    {
        if (joinedPlayers >= playerPrefabs.Length) return;

        var prefab = playerPrefabs[joinedPlayers];

        // Usamos PlayerInput.Instantiate que se encarga del emparejamiento
        var playerInput = PlayerInput.Instantiate(prefab, controlScheme: "Gamepad", pairWithDevice: device);

        if (playerInput == null)
        {
            Debug.LogError("¡No se pudo instanciar el jugador!");
            return;
        }

        joinedPlayers++;
    }
}
