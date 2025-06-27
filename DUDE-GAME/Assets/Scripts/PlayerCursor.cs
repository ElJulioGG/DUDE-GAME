using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PlayerCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private float deadZone = 0.1f;

    [Header("UI References")]
    [SerializeField] private Image cursorImage;
    [SerializeField] private TextMeshProUGUI playerLabel;
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
    private int assignedPlayerIndex = -1;
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
        SetInitialPosition();
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
        // Reset previous button color
        if (hoveredButton != null)
            SetButtonColor(hoveredButton, Color.white);

        hoveredButton = currentHover;

        if (hoveredButton != null && !isAssigned)
        {
            SetButtonColor(hoveredButton, playerColors[deviceIndex]);
            cursorImage.color = playerColors[deviceIndex];
        }
        else if (!isAssigned)
        {
            cursorImage.color = unassignedColor;
        }
    }
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
            cursorImage.color = unassignedColor;

        if (playerLabel != null)
            playerLabel.text = $"Player {deviceIndex + 1}";

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

    private void SetInitialPosition()
    {
        if (rectTransform != null)
        {
            Vector2 centerPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            rectTransform.anchoredPosition = centerPosition;
        }
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

        if (pad.startButton.wasPressedThisFrame && readyImage != null && readyImage.activeSelf)
        {
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
            parentCanvas.gameObject.SetActive(false);
            Debug.Log("Game Started!");
        }
    }
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

    RectTransform buttonRect = button.GetComponent<RectTransform>();
    if (buttonRect == null) return false;

    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, rectTransform.position);
    return RectTransformUtility.RectangleContainsScreenPoint(buttonRect, screenPoint, parentCanvas.worldCamera);
}


    private void AssignPlayer(int playerIndex)
{
    if (playerIndex < 0 || playerIndex >= playerColors.Length) return;
    if (isAssigned) return;

    isAssigned = true;
    assignedPlayerIndex = playerIndex;
    assignedCursorCount++;

    controllerMapper?.AssignControllerToPlayer(deviceIndex, playerIndex);

    if (cursorImage != null)
        cursorImage.color = playerColors[playerIndex];

    if (playerLabel != null)
        playerLabel.text = $"Player {playerIndex + 1}";

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

    isAssigned = false;
    assignedPlayerIndex = -1;
    assignedCursorCount--;

    if (cursorImage != null)
        cursorImage.color = unassignedColor;

    if (playerLabel != null)
        playerLabel.text = $"Player {deviceIndex + 1}";

    if (assignmentIndicator != null)
        assignmentIndicator.SetActive(false);

    if (hoveredButton != null)
        SetButtonColor(hoveredButton, Color.white);

    readyImage?.SetActive(false);

    Debug.Log("Player unassigned");
}
private void CheckIfAllReady()
{
    var allCursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);

    bool allAssigned = true;
    foreach (var cursor in allCursors)
    {
        if (!cursor.isActiveAndEnabled) continue;
        if (!cursor.IsAssigned)
        {
            allAssigned = false;
            break;
        }
    }

    if (readyImage != null)
        readyImage.SetActive(allAssigned);
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
}
