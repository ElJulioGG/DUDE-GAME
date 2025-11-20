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

    // Construir la lista de dispositivos conectados en orden deseado
    List<InputDevice> orderedDevices = new List<InputDevice>();

    // Primero los gamepads (hasta 4)
    foreach (var pad in Gamepad.all)
    {
        if (orderedDevices.Count < 4)
            orderedDevices.Add(pad);
    }

    // Luego el teclado si hay espacio
    if (orderedDevices.Count < 4 && Keyboard.current != null)
        orderedDevices.Add(Keyboard.current);

    // Ordenar los handlers según el índice del dispositivo conectado
    playerInputHandlers = handlers.OrderBy(handler =>
    {
        var device = handler.playerInput.devices.FirstOrDefault();
        int index = orderedDevices.IndexOf(device);
        return index >= 0 ? index : int.MaxValue; // Dejar al final los desconocidos
    }).ToArray();
}

 /* private void InitializeCursors()
{
    EnableCursors();

    List<InputDevice> assignedDevices = new List<InputDevice>();

    // 1. Agregar hasta 4 Gamepads
    foreach (var pad in Gamepad.all)
    {
        assignedDevices.Add(pad);
        if (assignedDevices.Count >= 4) break;
    }

    // 2. Si hay menos de 4, usar teclado como último dispositivo
    if (assignedDevices.Count < 4)
    {
        assignedDevices.Add(Keyboard.current);
    }

    for (int i = 0; i < playerInputHandlers.Length; i++)
    {
        if (i < assignedDevices.Count)
        {
            playerCursors[i].Initialize(assignedDevices[i], playerInputHandlers[i]);
            playerCursors[i].gameObject.SetActive(true);
        }
        else
        {
            playerCursors[i].gameObject.SetActive(false);
        }
    }
} */
private void InitializeCursors()
{
    EnableCursors();

    List<InputDevice> connectedDevices = new();

    foreach (var pad in Gamepad.all)
    {
        if (!connectedDevices.Contains(pad))
            connectedDevices.Add(pad);
    }

    if (connectedDevices.Count < 4 && Keyboard.current != null)
        connectedDevices.Add(Keyboard.current);

    // Reset if device count changed (optional)
    if (deviceToCursorMap.Count != connectedDevices.Count)
        deviceToCursorMap.Clear();

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
        if (cursorIndex >= playerInputHandlers.Length) continue;

        playerCursors[cursorIndex].Initialize(device, playerInputHandlers[cursorIndex]);
        playerCursors[cursorIndex].gameObject.SetActive(true);
    }

    // Desactivar cursores no usados
    for (int i = connectedDevices.Count; i < playerCursors.Length; i++)
    {
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




