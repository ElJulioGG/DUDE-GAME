/* using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerInputHandler : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    
    [Header("Player Assignment")]
    [SerializeField] private int playerIndex = -1;
    [SerializeField] private bool isAssigned = false;
    
    private PlayerInput playerInput;
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction acceptAction;
    private InputAction cancelAction;
    
    // Events
    public System.Action<Vector2> OnMoveInput;
    public System.Action OnAcceptPressed;
    public System.Action OnCancelPressed;
    
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput != null)
        {
            playerIndex = playerInput.playerIndex;
        }
        
        SetupInputActions();
    }
    
    private void Start()
    {
        // Subscribe to controller mapper events
        if (ControllerMapper.Instance != null)
        {
            ControllerMapper.Instance.OnPlayerAssigned += OnPlayerAssigned;
            ControllerMapper.Instance.OnPlayerUnassigned += OnPlayerUnassigned;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ControllerMapper.Instance != null)
        {
            ControllerMapper.Instance.OnPlayerAssigned -= OnPlayerAssigned;
            ControllerMapper.Instance.OnPlayerUnassigned -= OnPlayerUnassigned;
        }
        
        // Disable input actions
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }
    }
    
    private void SetupInputActions()
    {
        if (inputActions == null)
        {
            Debug.LogError("Input Actions not assigned to ControllerInputHandler!");
            return;
        }
        
        // Get the player action map
        playerActionMap = inputActions.FindActionMap(actionMapName);
        if (playerActionMap == null)
        {
            Debug.LogError($"Action map '{actionMapName}' not found!");
            return;
        }
        
        // Get specific actions
        moveAction = playerActionMap.FindAction("Move");
        acceptAction = playerActionMap.FindAction("Interact"); // Using Interact as Accept
        cancelAction = playerActionMap.FindAction("Reload"); // Using Reload as Cancel
        
        if (moveAction == null || acceptAction == null || cancelAction == null)
        {
            Debug.LogError("Required input actions not found!");
            return;
        }
        
        // Subscribe to action events
        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
        acceptAction.performed += OnAcceptPerformed;
        cancelAction.performed += OnCancelPerformed;
        
        // Enable the action map
        playerActionMap.Enable();
    }
    
    private void OnPlayerAssigned(int assignedPlayerIndex, Gamepad gamepad)
    {
        // Check if this handler is for the assigned player
        if (playerIndex == assignedPlayerIndex)
        {
            isAssigned = true;
            Debug.Log($"ControllerInputHandler {playerIndex} assigned to Player {assignedPlayerIndex + 1}");
        }
    }
    
    private void OnPlayerUnassigned(int unassignedPlayerIndex)
    {
        // Check if this handler is for the unassigned player
        if (playerIndex == unassignedPlayerIndex)
        {
            isAssigned = false;
            Debug.Log($"ControllerInputHandler {playerIndex} unassigned from Player {unassignedPlayerIndex + 1}");
        }
    }
    
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (isAssigned)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            OnMoveInput?.Invoke(moveInput);
        }
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        if (isAssigned)
        {
            OnMoveInput?.Invoke(Vector2.zero);
        }
    }
    
    private void OnAcceptPerformed(InputAction.CallbackContext context)
    {
        if (isAssigned)
        {
            OnAcceptPressed?.Invoke();
        }
    }
    
    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (isAssigned)
        {
            OnCancelPressed?.Invoke();
        }
    }
    
    // Public methods
    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }
    
    public int GetPlayerIndex()
    {
        return playerIndex;
    }
    
    public bool IsPlayerAssigned()
    {
        return isAssigned;
    }
    
    public void EnableInput()
    {
        if (playerActionMap != null)
        {
            playerActionMap.Enable();
        }
    }
    
    public void DisableInput()
    {
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }
    }
    
    // Method to get the current gamepad for this player
    public Gamepad GetAssignedGamepad()
    {
        if (ControllerMapper.Instance != null && isAssigned)
        {
            var mappings = ControllerMapper.Instance.PlayerMappings;
            if (mappings.TryGetValue(playerIndex, out Gamepad gamepad))
            {
                return gamepad;
            }
        }
        return null;
    }
}  */