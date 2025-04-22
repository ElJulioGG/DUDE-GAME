using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerChecker : MonoBehaviour
{
    public bool checkControllers = false;

    void Start()
    {
        int connectedGamepads = Gamepad.all.Count;
        Debug.Log("Connected gamepads: " + connectedGamepads);

        foreach (var gamepad in Gamepad.all)
        {
            Debug.Log("Controller: " + gamepad.displayName);
        }
    }

    private void Update()
    {
        if (checkControllers)
        {
            int connectedControllers = Gamepad.all.Count;

#if UNITY_2022_1_OR_NEWER
            int spawnedPlayers = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None).Length;
#else
            int spawnedPlayers = FindObjectsOfType<PlayerInput>().Length;
#endif

            Debug.Log($"Controllers connected: {connectedControllers}");
            Debug.Log($"PlayerInput clones in scene: {spawnedPlayers}");
        }
    }
}
