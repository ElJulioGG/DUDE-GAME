using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PlayerCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private GameObject[] playerObjects;
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
    private Slider hoveredSlider = null;
    private TMP_InputField hoveredInputField = null;
    private Toggle hoveredToggle = null;
    private Scrollbar hoveredScrollbar = null;
    private Scrollbar _grabbedScrollbar = null;
    private bool _isGrabbingScrollbar = false;
    private static int assignedCursorCount = 0;
    [SerializeField] private GameObject readyImage;

    private Color[] playerColors = {
        Color.red,
        Color.blue,
        Color.green,
        new Color(0.5f, 0f, 0.5f)
    };

    // Private fields
    private InputDevice inputDevice;
    [SerializeField] private int deviceIndex = -1;
    [SerializeField] private int assignedPlayerIndex = -1;
    private bool isAssigned = false;
    private bool isInitialized = false;

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Canvas parentCanvas;

    private ControllerMapper controllerMapper;
    private PlayerInputHandler playerInputHandler;

    // UI raycasting — cached to avoid per-frame allocations
    private GraphicRaycaster raycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    // Cached cursor list to avoid FindObjectsByType every frame
    private static PlayerCursor[] _allCursors;

    public bool IsAssigned => isAssigned;
    public int AssignedPlayerIndex => assignedPlayerIndex;
    public bool IsInitializedFor(InputDevice device) => isInitialized && inputDevice == device;
    public static PlayerCursor[] All => _allCursors;

    public void TrySetHandler(PlayerInputHandler handler)
    {
        if (playerInputHandler == null && handler != null)
            playerInputHandler = handler;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        assignedCursorCount = 0;
        _allCursors = null;
    }

    private void Awake()
    {
        controllerMapper = FindFirstObjectByType<ControllerMapper>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;
        pointerEventData = new PointerEventData(eventSystem);

        _allCursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);

        if (rectTransform == null)
        {
            Debug.LogError("PlayerCursor requires a RectTransform component!");
            enabled = false;
            return;
        }

        if (cursorImage != null) cursorImage.raycastTarget = false;
        if (assignmentIndicator != null) assignmentIndicator.SetActive(false);
    }

    private void Start()
    {
        Invoke("SetInitialPosition", 0.1f);
    }

    private void Update()
    {
        if (!isInitialized || inputDevice == null) return;

        HandleInput();

        if (!isAssigned)
        {
            HandleMovement();
            HandleButtonHover();
            HandleSliderInput();
            HandleScrollbarDrag();
        }
    }

    private void HandleButtonHover()
    {
        GameObject hitObject = GetUIObjectUnderCursor();

        Button foundButton = null;
        Slider foundSlider = null;
        TMP_InputField foundInputField = null;
        Toggle foundToggle = null;
        Scrollbar foundScrollbar = null;

        if (hitObject != null)
        {
            foundButton = hitObject.GetComponentInParent<Button>();
            if (foundButton != null && !foundButton.interactable) foundButton = null;

            if (foundButton == null)
            {
                foundSlider     = hitObject.GetComponentInParent<Slider>();
                foundInputField = hitObject.GetComponentInParent<TMP_InputField>();
                foundToggle     = hitObject.GetComponentInParent<Toggle>();
                foundScrollbar  = hitObject.GetComponentInParent<Scrollbar>();
            }
        }

        if (hoveredButton != foundButton)
        {
            if (hoveredButton != null && IsAssignmentButton(hoveredButton))
            {
                if (!isAssigned && !IsButtonHoveredByAnotherCursor(hoveredButton))
                {
                    SetButtonColor(hoveredButton, Color.white);
                    //SetButtonTextColor(hoveredButton, Color.black);
                }
            }

            hoveredButton = foundButton;

            if (hoveredButton != null && IsAssignmentButton(hoveredButton))
            {
                if (!isAssigned)
                {
                    Color targetColor = Color.white;
                    if (hoveredButton == player1Button) targetColor = playerColors[0];
                    else if (hoveredButton == player2Button) targetColor = playerColors[1];
                    else if (hoveredButton == player3Button) targetColor = playerColors[2];
                    else if (hoveredButton == player4Button) targetColor = playerColors[3];

                    SetButtonColor(hoveredButton, targetColor);
                    //SetButtonTextColor(hoveredButton, Color.white);
                }
            }
            else if (!isAssigned)
            {
                cursorImage.color = unassignedColor;
            }
        }

        hoveredSlider     = foundSlider;
        hoveredInputField = foundInputField;
        hoveredToggle     = foundToggle;
        hoveredScrollbar  = foundScrollbar;
    }

    private void PressButtonUnderCursor()
    {
        if (hoveredButton != null)
        {
            if (IsAssignmentButton(hoveredButton))
            {
                if (hoveredButton == player1Button) AssignPlayer(0);
                else if (hoveredButton == player2Button) AssignPlayer(1);
                else if (hoveredButton == player3Button) AssignPlayer(2);
                else if (hoveredButton == player4Button) AssignPlayer(3);
            }
            else
            {
                hoveredButton.onClick.Invoke();
            }
            return;
        }

        if (hoveredInputField != null)
        {
            hoveredInputField.Select();
            hoveredInputField.ActivateInputField();
            return;
        }

        if (hoveredToggle != null && hoveredToggle.interactable)
        {
            hoveredToggle.isOn = !hoveredToggle.isOn;
            return;
        }
    }

    private void HandleScrollbarDrag()
    {
        if (_grabbedScrollbar == null) return;

        RectTransform scrollbarRect = _grabbedScrollbar.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        scrollbarRect.GetWorldCorners(corners);

        // corners[0] = bottom-left, corners[1] = top-left in screen/world space
        float minY = corners[0].y;
        float maxY = corners[1].y;

        float cursorY = GetCursorScreenPosition().y;
        float t = Mathf.InverseLerp(minY, maxY, cursorY);

        if (_grabbedScrollbar.direction == Scrollbar.Direction.TopToBottom)
            t = 1f - t;

        _grabbedScrollbar.value = Mathf.Clamp01(t);
    }

    private void HandleSliderInput()
    {
        if (hoveredSlider == null || !hoveredSlider.interactable) return;

        float stickX = 0f;
        if (inputDevice is Gamepad pad)
            stickX = pad.rightStick.ReadValue().x;

        if (Mathf.Abs(stickX) < deadZone) return;

        float range = hoveredSlider.maxValue - hoveredSlider.minValue;
        hoveredSlider.value = Mathf.Clamp(
            hoveredSlider.value + stickX * range * Time.unscaledDeltaTime,
            hoveredSlider.minValue,
            hoveredSlider.maxValue
        );
    }

    private bool IsAssignmentButton(Button btn)
    {
        return btn == player1Button || btn == player2Button || btn == player3Button || btn == player4Button;
    }

    private bool IsButtonHoveredByAnotherCursor(Button button)
    {
        foreach (var cursor in _allCursors)
        {
            if (cursor == this) continue;
            if (cursor.hoveredButton == button) return true;
        }
        return false;
    }

    private void SetButtonTextColor(Button btn, Color color)
    {
        TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.color = color;
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

    public void Initialize(InputDevice device, PlayerInputHandler inputHandler, int stableCursorIndex)
    {
        inputDevice = device;
        playerInputHandler = inputHandler;
        deviceIndex = stableCursorIndex;
        if (playerLabel != null && !isAssigned)
        {
            playerLabel.text = $"P {deviceIndex + 1}";
            playerLabel2.text = $"P {deviceIndex + 1}";
        }
        isInitialized = true;
    }

    private void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;
        if (inputDevice is Gamepad pad) moveInput = pad.leftStick.ReadValue();
        else if (inputDevice is Keyboard kb)
        {
            moveInput.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
            moveInput.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
        }

        if (moveInput.magnitude < deadZone) moveInput = Vector2.zero;

        if (_isGrabbingScrollbar) moveInput.x = 0f;

        if (moveInput != Vector2.zero)
        {
            Vector2 movement = moveInput.normalized * moveSpeed * Time.unscaledDeltaTime;
            rectTransform.anchoredPosition += movement;
            ClampToScreenBounds();
        }
    }

    private void HandleInput()
    {
        bool pressedSelect = false;
        bool releasedSelect = false;
        bool pressedCancel = false;
        bool pressedStart = false;

        if (inputDevice is Gamepad pad)
        {
            if (pad.buttonSouth.wasPressedThisFrame)  pressedSelect  = true;
            if (pad.buttonSouth.wasReleasedThisFrame) releasedSelect = true;
            if (pad.buttonEast.wasPressedThisFrame)   pressedCancel  = true;
            if (pad.startButton.wasPressedThisFrame)  pressedStart   = true;
        }
        else if (inputDevice is Keyboard kb)
        {
            if (kb.spaceKey.wasPressedThisFrame)               pressedSelect  = true;
            if (kb.spaceKey.wasReleasedThisFrame)              releasedSelect = true;
            if (kb.spaceKey.wasPressedThisFrame && isAssigned) pressedCancel  = true;
            if (kb.enterKey.wasPressedThisFrame)               pressedStart   = true;
        }

        if (releasedSelect && _isGrabbingScrollbar)
        {
            _grabbedScrollbar    = null;
            _isGrabbingScrollbar = false;
        }

        if (!isAssigned && pressedSelect)
        {
            if (hoveredScrollbar != null)
            {
                _grabbedScrollbar    = hoveredScrollbar;
                _isGrabbingScrollbar = true;
            }
            else
            {
                PressButtonUnderCursor();
            }
        }
        else if (isAssigned && pressedCancel) UnassignPlayer();

        if (pressedStart && CanStartGame())
        {
            if (GameSession.IsOnline)
            {
                // Online: send CSS results to server; server responds with AssignBroadcast
                // which triggers ApplyNetworkAssignment() → assignController = false
                OnlineLobbyManager.Instance?.SendRegistration();
            }
            else
            {
                GameManager.instance.assignController = false;
                parentCanvas.gameObject.SetActive(false);
                Debug.Log("Game Started!");
            }
        }
    }

    private Vector2 GetCursorScreenPosition()
    {
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return rectTransform.position;
        else
            return RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, rectTransform.position);
    }

    private GameObject GetUIObjectUnderCursor()
    {
        pointerEventData.position = GetCursorScreenPosition();
        _raycastResults.Clear();
        raycaster.Raycast(pointerEventData, _raycastResults);
        return _raycastResults.Count > 0 ? _raycastResults[0].gameObject : null;
    }

    public void NotifyRemoteStateChanged() => CheckIfAllReady();

    private bool CanStartGame()
    {
        int localAssigned = 0;
        foreach (var cursor in _allCursors)
            if (cursor.IsAssigned) localAssigned++;
        if (localAssigned == 0) return false;
        // Online: at least 1 local player selected is enough — we're already in a lobby.
        // Offline: need at least 2 players on this machine.
        if (GameSession.IsOnline) return true;
        return localAssigned >= 2;
    }

    private void AssignPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerColors.Length) return;
        if (isAssigned) return;

        foreach (var cursor in _allCursors)
        {
            if (cursor != this && cursor.IsAssigned && cursor.AssignedPlayerIndex == playerIndex)
            {
                AudioManager.Instance.PlaySound(FMODEvents.Instance.CharSelectDeselect, transform.position);
                GetComponent<Shaker>()?.Shake();
                return;
            }
        }

        if (GameSession.IsOnline && CSSCursorSync.IsSlotClaimedByRemote(playerIndex))
        {
            AudioManager.Instance.PlaySound(FMODEvents.Instance.CharSelectDeselect, transform.position);
            GetComponent<Shaker>()?.Shake();
            return;
        }

        //SoundFXManager.instance.PlaySoundByName("Select", transform, 0.6f, 1f);
        AudioManager.Instance.PlaySound(FMODEvents.Instance.CharSelectSelect, transform.position);
        isAssigned = true;
        assignedPlayerIndex = playerIndex;
        assignedCursorCount++;

        switch (assignedPlayerIndex)
        {
            case 0: GameManager.instance.player1Playable = true; break;
            case 1: GameManager.instance.player2Playable = true; break;
            case 2: GameManager.instance.player3Playable = true; break;
            case 3: GameManager.instance.player4Playable = true; break;
        }

        switch (assignedPlayerIndex)
        {
            case 0: GameManager.instance.player1DisplayOrder = deviceIndex; break;
            case 1: GameManager.instance.player2DisplayOrder = deviceIndex; break;
            case 2: GameManager.instance.player3DisplayOrder = deviceIndex; break;
            case 3: GameManager.instance.player4DisplayOrder = deviceIndex; break;
        }

        if (playerInputHandler == null && controllerMapper != null)
            playerInputHandler = controllerMapper.GetOrPairHandlerForDevice(inputDevice);

        if (playerInputHandler != null)
            playerInputHandler.reasignController(playerIndex);

        if (cursorImage != null) cursorImage.color = playerColors[playerIndex];

        if (assignmentIndicator != null) assignmentIndicator.SetActive(true);
        if (hoveredButton != null) SetButtonColor(hoveredButton, playerColors[playerIndex]);

        CheckIfAllReady();
        Debug.Log($"Assigned device to Player {playerIndex + 1}");
    }

    public void UnassignPlayer()
    {
        if (!isAssigned) return;
        //SoundFXManager.instance.PlaySoundByName("Deselect", transform, 1f, 1f);
        AudioManager.Instance.PlaySound(FMODEvents.Instance.CharSelectDeselect, transform.position);
        isAssigned = false;

        switch (assignedPlayerIndex)
        {
            case 0: GameManager.instance.player1Playable = false; break;
            case 1: GameManager.instance.player2Playable = false; break;
            case 2: GameManager.instance.player3Playable = false; break;
            case 3: GameManager.instance.player4Playable = false; break;
        }

        switch (assignedPlayerIndex)
        {
            case 0: GameManager.instance.player1DisplayOrder = -1; break;
            case 1: GameManager.instance.player2DisplayOrder = -1; break;
            case 2: GameManager.instance.player3DisplayOrder = -1; break;
            case 3: GameManager.instance.player4DisplayOrder = -1; break;
        }

        assignedPlayerIndex = -1;
        assignedCursorCount--;

        if (cursorImage != null) cursorImage.color = unassignedColor;
        if (playerLabel != null)
        {
            playerLabel.text = $"P {deviceIndex + 1}";
            playerLabel2.text = $"P {deviceIndex + 1}";
        }

        if (assignmentIndicator != null) assignmentIndicator.SetActive(false);

        if (hoveredButton != null && IsAssignmentButton(hoveredButton))
            SetButtonColor(hoveredButton, Color.white);
        else if (assignedButton != null)
        {
            SetButtonColor(assignedButton, Color.white);
            assignedButton = null;
        }

        readyImage?.SetActive(false);
        Debug.Log("Player unassigned");
    }

    private void CheckIfAllReady()
    {
        int localActive = 0, localAssigned = 0;
        foreach (var cursor in _allCursors)
        {
            if (!cursor.isActiveAndEnabled) continue;
            localActive++;
            if (cursor.IsAssigned) localAssigned++;
        }
        bool allLocalReady = localActive > 0 && localAssigned == localActive;
        bool canStart;
        if (GameSession.IsOnline)
            canStart = localAssigned >= 1; // lobby guarantees other players exist
        else
            canStart = allLocalReady && localAssigned >= 2;
        if (readyImage != null) readyImage.SetActive(canStart);
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
