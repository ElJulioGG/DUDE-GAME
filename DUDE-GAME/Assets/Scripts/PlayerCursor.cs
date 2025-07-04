using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PlayerCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private GameObject[] playerObjects; // Jugadores 0 a 3

    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private float deadZone = 0.1f;
private Button assignedButton = null;

    [Header("UI References")]
    [SerializeField] private Image cursorImage;
    [SerializeField] private TextMeshProUGUI playerLabel;
    [SerializeField] private TextMeshProUGUI playerLabel2;
    [SerializeField] private GameObject assignmentIndicator;

    [Header("Player Assignment Buttons")]
    [SerializeField] private Button player1Button;
    [SerializeField] private Button player2Button;
    [SerializeField] private Button player3Button;
    [SerializeField] private Button player4Button;

    [Header("Colors")]
    [SerializeField] private Color unassignedColor = Color.black;

    private Button hoveredButton = null;
    private static int assignedCursorCount = 0;
    [SerializeField] private GameObject readyImage; // Show this when all assigned


    private Color[] playerColors = {
        Color.red,      // Player 1
        Color.blue,     // Player 2
        Color.green,    // Player 3
        new Color(0.5f, 0f, 0.5f) // Player 4 (Purple)
    };

    // Private fields
    private InputDevice inputDevice;
    [SerializeField] private int deviceIndex = -1;
    [SerializeField]private int assignedPlayerIndex = -1;
    private bool isAssigned = false;
    private bool isInitialized = false;

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Canvas parentCanvas;

    private ControllerMapper controllerMapper;
    private PlayerInputHandler playerInputHandler;

    // Public properties
    public bool IsAssigned => isAssigned;
    public int AssignedPlayerIndex => assignedPlayerIndex;

    private void Awake()
    {
        controllerMapper = FindFirstObjectByType<ControllerMapper>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (rectTransform == null)
        {
            Debug.LogError("PlayerCursor requires a RectTransform component!");
            enabled = false;
            return;
        }

        if (assignmentIndicator != null)
            assignmentIndicator.SetActive(false);
    }

    private void Start()
    {
        Invoke("SetInitialPosition", 0.1f);
        //SetInitialPosition();
    }

    private void Update()
    {
        if (!isInitialized || inputDevice == null) return;

        HandleInput();

        if (!isAssigned)
        {
            HandleMovement();
            HandleButtonHover();
        }


    }
    private bool IsButtonHoveredByAnotherCursor(Button button)
    {
        var allCursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);
        foreach (var cursor in allCursors)
        {
            if (cursor == this) continue;
            if (cursor.hoveredButton == button)
                return true;
        }
        return false;
    }

private void HandleButtonHover()
{
    Vector2 cursorPos = rectTransform.anchoredPosition;

    Button currentHover = null;

    if (IsCursorOverButton(player1Button, cursorPos)) currentHover = player1Button;
    else if (IsCursorOverButton(player2Button, cursorPos)) currentHover = player2Button;
    else if (IsCursorOverButton(player3Button, cursorPos)) currentHover = player3Button;
    else if (IsCursorOverButton(player4Button, cursorPos)) currentHover = player4Button;

    if (hoveredButton != currentHover)
    {
        if (hoveredButton != null && !isAssigned && !IsButtonHoveredByAnotherCursor(hoveredButton))
        {
            SetButtonColor(hoveredButton, Color.white);
            SetButtonTextColor(hoveredButton, Color.black);
        }


        hoveredButton = currentHover;

        if (hoveredButton != null && !isAssigned)
        {
            // Determine color based on which button we're hovering
            Color hoverColor = Color.white;

            if (hoveredButton == player1Button) hoverColor = playerColors[0];
            else if (hoveredButton == player2Button) hoverColor = playerColors[1];
            else if (hoveredButton == player3Button) hoverColor = playerColors[2];
            else if (hoveredButton == player4Button) hoverColor = playerColors[3];

            SetButtonColor(hoveredButton, hoverColor);
            SetButtonTextColor(hoveredButton, Color.white);
            //cursorImage.color = hoverColor;
        }
        else if (!isAssigned)
        {
            cursorImage.color = unassignedColor;
        }
    }
}

private void SetButtonTextColor(Button btn, Color color)
{
    TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
    if (text != null)
        text.color = color;
}

private void SetButtonColor(Button btn, Color color)
{
    var colors = btn.colors;
    colors.normalColor = color;
    colors.selectedColor = color;
    colors.highlightedColor = color;
    colors.pressedColor = color;
    btn.colors = colors;
}

    public void Initialize(InputDevice device, PlayerInputHandler inputHandler)
    {
        inputDevice = device;
        playerInputHandler = inputHandler;

        deviceIndex = GetDeviceIndex(device);

        // Set unassigned appearance
        if (cursorImage != null)
            //cursorImage.color = unassignedColor;

        if (playerLabel != null){
            playerLabel.text = $"P {deviceIndex + 1}";
            playerLabel2.text = $"P {deviceIndex + 1}";
        }
        isInitialized = true;
    }

    private int GetDeviceIndex(InputDevice device)
    {
        if (device is Keyboard)
        {
            int gamepadCount = Mathf.Min(Gamepad.all.Count, 4);
            return gamepadCount; // keyboard is last slot
        }

        var allGamepads = Gamepad.all;
        for (int i = 0; i < allGamepads.Count; i++)
        {
            if (allGamepads[i] == device)
                return i;
        }

        return -1;
    }

    private void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;

        if (inputDevice is Gamepad pad)
        {
            moveInput = pad.leftStick.ReadValue();
        }
        else if (inputDevice is Keyboard kb)
        {
            moveInput.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
            moveInput.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
        }

        if (moveInput.magnitude < deadZone)
        {
            moveInput = Vector2.zero;
        }

        if (moveInput != Vector2.zero)
        {
            Vector2 movement = moveInput.normalized * moveSpeed * Time.unscaledDeltaTime;
            rectTransform.anchoredPosition += movement;
            ClampToScreenBounds();
        }
    }

    private void HandleInput()
{
    if (inputDevice is Gamepad pad)
    {
        if (!isAssigned && pad.buttonSouth.wasPressedThisFrame) // A
            CheckButtonAssignment();
        else if (isAssigned && pad.buttonEast.wasPressedThisFrame) // B
            UnassignPlayer();

        if (pad.startButton.wasPressedThisFrame && CanStartGame())
        {
            GameManager.instance.assignController = false;
            parentCanvas.gameObject.SetActive(false);
            
            Debug.Log("Game Started!");
        }

    }
    else if (inputDevice is Keyboard kb)
    {
        if (!isAssigned && kb.spaceKey.wasPressedThisFrame)
            CheckButtonAssignment();
        else if (isAssigned && kb.spaceKey.wasPressedThisFrame)
            UnassignPlayer();

        if (kb.enterKey.wasPressedThisFrame && readyImage != null && readyImage.activeSelf)
        {
            GameManager.instance.assignController = false;
            parentCanvas.gameObject.SetActive(false);
            Debug.Log("Game Started!");
        }

    }
}


private bool CanStartGame()
{
    var allCursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);
    int assignedCount = 0;
    foreach (var cursor in allCursors)
    {
        if (cursor.IsAssigned)
            assignedCount++;
    }

    return assignedCount >= 2;
}


    private void CheckButtonAssignment()
    {
        Vector2 cursorPos = rectTransform.anchoredPosition;

        if (IsCursorOverButton(player1Button, cursorPos)) AssignPlayer(0);
        else if (IsCursorOverButton(player2Button, cursorPos)) AssignPlayer(1);
        else if (IsCursorOverButton(player3Button, cursorPos)) AssignPlayer(2);
        else if (IsCursorOverButton(player4Button, cursorPos)) AssignPlayer(3);
    }

private bool IsCursorOverButton(Button button, Vector2 cursorPos)
{
    if (button == null || parentCanvas == null) return false;

    // Only allow interaction if button GameObject is active
    if (!button.gameObject.activeInHierarchy) return false;

    RectTransform buttonRect = button.GetComponent<RectTransform>();
    if (buttonRect == null) return false;

    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, rectTransform.position);
    return RectTransformUtility.RectangleContainsScreenPoint(buttonRect, screenPoint, parentCanvas.worldCamera);
}



    private void AssignPlayer(int playerIndex)
{
    
    if (playerIndex < 0 || playerIndex >= playerColors.Length) return;
    if (isAssigned) return;

    //Verificar si ya hay un cursor asignado a ese índice
    var allCursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);
    foreach (var cursor in allCursors)
    {
        if (cursor != this && cursor.IsAssigned && cursor.AssignedPlayerIndex == playerIndex)
        {
            Debug.LogWarning($"Player {playerIndex + 1} ya está asignado.");
            SoundFXManager.instance.PlaySoundByName("Deselect", transform, 0.6f, 1f);
            GetComponent<Shaker>()?.Shake();

            return; 
        }
    }
    SoundFXManager.instance.PlaySoundByName("Select", transform, 0.6f, 1f);
    isAssigned = true;
    assignedPlayerIndex = playerIndex;
    assignedCursorCount++;
    switch(assignedPlayerIndex){
        case 0:
            GameManager.instance.player1Playable = true;
            break;
        case 1:
            GameManager.instance.player2Playable = true;
            break;
        case 2:
            GameManager.instance.player3Playable = true;
            break;
        case 3:
            GameManager.instance.player4Playable = true;
            break;
    }
    controllerMapper?.AssignControllerToPlayer(deviceIndex, playerIndex);

    if (cursorImage != null)
        cursorImage.color = playerColors[playerIndex];

    if (playerLabel != null){
        playerLabel.text = $"P {playerIndex + 1}";
        playerLabel2.text = $"P {playerIndex + 1}";
    }

    if (assignmentIndicator != null)
        assignmentIndicator.SetActive(true);

    if (hoveredButton != null)
        SetButtonColor(hoveredButton, playerColors[playerIndex]);

    CheckIfAllReady();

    Debug.Log($"Assigned device to Player {playerIndex + 1}");
}


    public void UnassignPlayer()
{
    
    if (!isAssigned) return;
    SoundFXManager.instance.PlaySoundByName("Deselect", transform, 1f, 1f);
    isAssigned = false;
    switch(assignedPlayerIndex){
        case 0:
            GameManager.instance.player1Playable = false;
            break;
        case 1:
            GameManager.instance.player2Playable = false;
            break;
        case 2:
            GameManager.instance.player3Playable = false;
            break;
        case 3:
            GameManager.instance.player4Playable = false;
            break;
    }
    assignedPlayerIndex = -1;
    
    assignedCursorCount--;

    if (cursorImage != null)
        cursorImage.color = unassignedColor;

    if (playerLabel != null)
    {
        playerLabel.text = $"P {deviceIndex + 1}";
        playerLabel2.text = $"P {deviceIndex + 1}";
    }

    if (assignmentIndicator != null)
        assignmentIndicator.SetActive(false);

    if (assignedButton != null)
    {
        SetButtonColor(assignedButton, Color.white);
        assignedButton = null;
    }

    readyImage?.SetActive(false);

    Debug.Log("Player unassigned");
}

private void CheckIfAllReady()
{
    var allCursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);

    int assignedCount = 0;
    foreach (var cursor in allCursors)
    {
        if (!cursor.isActiveAndEnabled) continue;
        if (cursor.IsAssigned)
            assignedCount++;
    }

    // Show ready UI only if all cursors are assigned AND at least 2 players
    bool allAssigned = true;
    foreach (var cursor in allCursors)
    {
        if (cursor.isActiveAndEnabled && !cursor.IsAssigned)
        {
            allAssigned = false;
            break;
        }
    }

    bool canStart = assignedCount >= 2 && allAssigned;

    if (readyImage != null)
        readyImage.SetActive(canStart);
}


    private void ClampToScreenBounds()
    {
        if (parentCanvas == null || rectTransform == null) return;

        Vector2 screenSize = parentCanvas.pixelRect.size;
        Vector2 currentPos = rectTransform.anchoredPosition;

        float halfWidth = rectTransform.rect.width * 0.5f;
        float halfHeight = rectTransform.rect.height * 0.5f;

        currentPos.x = Mathf.Clamp(currentPos.x, -screenSize.x * 0.5f + halfWidth, screenSize.x * 0.5f - halfWidth);
        currentPos.y = Mathf.Clamp(currentPos.y, -screenSize.y * 0.5f + halfHeight, screenSize.y * 0.5f - halfHeight);

        rectTransform.anchoredPosition = currentPos;
    }
    private void ActivateAssignedPlayer()
{
    for (int i = 0; i < playerObjects.Length; i++)
    {
        if (playerObjects[i] != null)
        {
            playerObjects[i].SetActive(i == assignedPlayerIndex);
        }
    }
}

}