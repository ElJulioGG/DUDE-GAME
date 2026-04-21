using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerMapper : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler[] playerInputHandlers;
    [SerializeField] private PlayerCursor[] playerCursors;
    [SerializeField] private GameObject mapperCanvas;
    [SerializeField] private GameObject[] playerButtons;

    private Dictionary<InputDevice, int> deviceToCursorMap = new();
    private List<InputDevice> activatedDevices = new();
    private bool _dirty = false;

    private void Start()
    {
        initializeInputHandlers();
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
        {
            if (activatedDevices.Remove(device))
            {
                deviceToCursorMap.Remove(device);
                _dirty = true;
            }
        }
    }

    private void Update()
    {
        DetectNewActivations();
        if (_dirty)
        {
            initializeInputHandlers();
            UpdateCursorAssignments();
            _dirty = false;
        }
    }

    private void DetectNewActivations()
    {
        foreach (var pad in Gamepad.all)
        {
            if (activatedDevices.Contains(pad)) continue;
            if (HasSignificantInput(pad))
            {
                activatedDevices.Add(pad);
                _dirty = true;
            }
        }

        if (Keyboard.current != null && !activatedDevices.Contains(Keyboard.current))
        {
            if (Keyboard.current.anyKey.isPressed)
            {
                activatedDevices.Add(Keyboard.current);
                _dirty = true;
            }
        }
    }

    private bool HasSignificantInput(Gamepad pad)
    {
        return pad.leftStick.ReadValue().magnitude > 0.2f ||
               pad.rightStick.ReadValue().magnitude > 0.2f ||
               pad.buttonSouth.isPressed || pad.buttonNorth.isPressed ||
               pad.buttonEast.isPressed || pad.buttonWest.isPressed ||
               pad.startButton.isPressed || pad.selectButton.isPressed ||
               pad.leftShoulder.isPressed || pad.rightShoulder.isPressed ||
               pad.leftTrigger.isPressed || pad.rightTrigger.isPressed ||
               pad.dpad.ReadValue() != Vector2.zero;
    }

    private void initializeInputHandlers()
    {
        var handlers = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);

        List<InputDevice> orderedDevices = activatedDevices.Take(4).ToList();

        playerInputHandlers = handlers.OrderBy(handler =>
        {
            var device = handler.playerInput.devices.FirstOrDefault();
            int index = orderedDevices.IndexOf(device);
            return index >= 0 ? index : int.MaxValue;
        }).ToArray();
    }

    private void UpdateCursorAssignments()
    {
        List<InputDevice> connectedDevices = activatedDevices.Take(4).ToList();

        var stale = deviceToCursorMap.Keys.Where(d => !connectedDevices.Contains(d)).ToList();
        foreach (var d in stale) deviceToCursorMap.Remove(d);

        for (int i = 0; i < connectedDevices.Count && i < playerCursors.Length; i++)
        {
            var device = connectedDevices[i];

            if (!deviceToCursorMap.ContainsKey(device))
            {
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

            var cursor = playerCursors[cursorIndex];
            if (!cursor.IsInitializedFor(device))
            {
                cursor.Initialize(device, matchingHandler, cursorIndex);
                cursor.gameObject.SetActive(true);
            }
        }

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
        Debug.Log($"Controller {controllerIndex} reassigned to player {playerIndex}");
    }

    public void EnableCursors()
    {
        for (int i = 0; i < playerCursors.Length; i++)
            playerCursors[i].gameObject.SetActive(true);
    }

    public void DisableCursors()
    {
        for (int i = 0; i < playerCursors.Length; i++)
            playerCursors[i].gameObject.SetActive(false);
    }

    public void EnablePlayerButtons()
    {
        for (int i = 0; i < playerButtons.Length; i++)
            playerButtons[i].SetActive(true);
    }

    public void DisablePlayerButtons()
    {
        for (int i = 0; i < playerButtons.Length; i++)
            playerButtons[i].SetActive(false);
    }

    public void InitializeControllerMapping()
    {
        mapperCanvas.SetActive(true);
        initializeInputHandlers();
        UpdateCursorAssignments();
    }

    public void FinalizeControllerMapping()
    {
        mapperCanvas.SetActive(false);
        DisableCursors();
    }
}
