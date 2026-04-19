using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;
using System.Data;
using System.Linq;

public class ControllerMapper : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler[] playerInputHandlers;
    [SerializeField] private PlayerCursor[] playerCursors;
    [SerializeField] private GameObject mapperCanvas;
    [SerializeField] private GameObject[] playerButtons;
    private Dictionary<InputDevice, int> deviceToCursorMap = new();
    private List<InputDevice> activatedDevices = new();

    private void Start()
    {
        //find all player input handlers
        initializeInputHandlers();
        //assign controllers to players
    }
    private void Update(){
        initializeInputHandlers();
        InitializeCursors();
    }
    private void initializeInputHandlers()
{
    var handlers = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);

    // Build ordered devices from activation order (first input = P1)
    List<InputDevice> orderedDevices = new List<InputDevice>();
    foreach (var device in activatedDevices)
    {
        if (orderedDevices.Count >= 4) break;
        orderedDevices.Add(device);
    }

    // Ordenar los handlers según el índice del dispositivo conectado
    playerInputHandlers = handlers.OrderBy(handler =>
    {
        var device = handler.playerInput.devices.FirstOrDefault();
        int index = orderedDevices.IndexOf(device);
        return index >= 0 ? index : int.MaxValue; // Dejar al final los desconocidos
    }).ToArray();
}

private void InitializeCursors()
{
    // Check for gamepad activation (real input from inactive gamepads)
    foreach (var pad in Gamepad.all)
    {
        if (activatedDevices.Contains(pad)) continue;

        if (pad.leftStick.ReadValue().magnitude > 0.2f ||
            pad.rightStick.ReadValue().magnitude > 0.2f ||
            pad.buttonSouth.isPressed ||
            pad.buttonNorth.isPressed ||
            pad.buttonEast.isPressed ||
            pad.buttonWest.isPressed ||
            pad.startButton.isPressed ||
            pad.selectButton.isPressed ||
            pad.leftShoulder.isPressed ||
            pad.rightShoulder.isPressed ||
            pad.leftTrigger.isPressed ||
            pad.rightTrigger.isPressed ||
            pad.dpad.ReadValue() != Vector2.zero)
        {
            activatedDevices.Add(pad);
        }
    }

    // Keyboard activates on real input (same as gamepads)
    if (Keyboard.current != null && !activatedDevices.Contains(Keyboard.current))
    {
        if (Keyboard.current.anyKey.isPressed)
            activatedDevices.Add(Keyboard.current);
    }

    // Build connected devices in activation order (first input = P1)
    List<InputDevice> connectedDevices = new();
    foreach (var device in activatedDevices)
    {
        if (connectedDevices.Count >= 4) break;
        connectedDevices.Add(device);
    }

    // Remove disconnected devices from map and activatedDevices
    var disconnected = deviceToCursorMap.Keys
        .Where(d => !connectedDevices.Contains(d))
        .ToList();
    foreach (var d in disconnected)
        deviceToCursorMap.Remove(d);

    // Cleanup activatedDevices for gamepads no longer physically connected
    activatedDevices.RemoveAll(d => d is Gamepad && !Gamepad.all.Contains((Gamepad)d));

    // Asignar dispositivos a cursores disponibles
    for (int i = 0; i < connectedDevices.Count && i < playerCursors.Length; i++)
    {
        var device = connectedDevices[i];

        if (!deviceToCursorMap.ContainsKey(device))
        {
            // Buscar primer índice libre
            for (int j = 0; j < playerCursors.Length; j++)
            {
                if (!deviceToCursorMap.ContainsValue(j))
                {
                    deviceToCursorMap[device] = j;
                    break;
                }
            }
        }

        if (!deviceToCursorMap.TryGetValue(device, out int cursorIndex)) continue;
        if (cursorIndex < 0 || cursorIndex >= playerCursors.Length) continue;

        // Find the handler that owns this device (not by array index)
        PlayerInputHandler matchingHandler = null;
        foreach (var handler in playerInputHandlers)
        {
            if (handler.playerInput != null && handler.playerInput.devices.Contains(device))
            {
                matchingHandler = handler;
                break;
            }
        }
        if (matchingHandler == null) continue;

        playerCursors[cursorIndex].Initialize(device, matchingHandler, cursorIndex);
        playerCursors[cursorIndex].gameObject.SetActive(true);
    }

    // Desactivar cursores que no tienen dispositivo asignado
    var activeIndices = new HashSet<int>(deviceToCursorMap.Values);
    for (int i = 0; i < playerCursors.Length; i++)
    {
        if (!activeIndices.Contains(i))
            playerCursors[i].gameObject.SetActive(false);
    }
}

    public void AssignControllerToPlayer(int controllerIndex, int playerIndex)
    {
        playerInputHandlers[controllerIndex].reasignController(playerIndex);
        print("controller "+controllerIndex+"reassigned to player "+playerIndex);
    }
    public void EnableCursors(){
        
        for(int i = 0; i < playerInputHandlers.Length; i++){
            playerCursors[i].gameObject.SetActive(true);
        }
    }
    public void DisableCursors(){
        for(int i = 0; i < playerInputHandlers.Length; i++){
            playerCursors[i].gameObject.SetActive(false);
        }
    }
    public void EnablePlayerButtons(){
        for(int i = 0; i < playerInputHandlers.Length; i++){
            playerButtons[i].SetActive(true);
        }
    }
    public void DisablePlayerButtons(){
        for(int i = 0; i < playerInputHandlers.Length; i++){
            playerButtons[i].SetActive(false);
        }
    }
    public void InitializeControllerMapping(){
        mapperCanvas.SetActive(true);
        InitializeCursors();
        initializeInputHandlers();
    }
    public void FinalizeControllerMapping(){
        mapperCanvas.SetActive(false);
        DisableCursors();
    }
}




