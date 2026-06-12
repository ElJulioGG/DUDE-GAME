using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class VictoryCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private int playerIndex; // 0 for P1, 1 for P2, etc.
    [SerializeField] private float moveSpeed = 600f;
    [SerializeField] private float deadZone = 0.1f;

    [Header("UI References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private Image cursorImage;

    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    private PointerEventData pointerEventData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();
    private Button hoveredButton = null;

    // The exact physical device passed from the GameManager
    private InputDevice myDevice = null;

    private Color[] playerColors = {
        Color.red,
        Color.blue,
        Color.green,
        new Color(0.5f, 0f, 0.5f) // Purple
    };

    private void Awake()
    {
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null) raycaster = parentCanvas.GetComponent<GraphicRaycaster>();

        if (cursorImage == null)
        {
            Transform imgTransform = transform.Find("CursorImage");
            if (imgTransform != null) cursorImage = imgTransform.GetComponent<Image>();
        }

        //Transform label = transform.Find("PlayerLabel");
        //if (label != null) label.gameObject.SetActive(false);
        Transform indicator = transform.Find("AssignmentIndicator");
        if (indicator != null) indicator.gameObject.SetActive(false);

        eventSystem = EventSystem.current;
        pointerEventData = new PointerEventData(eventSystem);
    }

    private void Start()
    {
        bool isPlaying = false;

        if (GameManager.instance != null)
        {
            switch (playerIndex)
            {
                case 0: isPlaying = GameManager.instance.player1Playable; myDevice = GameManager.instance.player1Device; break;
                case 1: isPlaying = GameManager.instance.player2Playable; myDevice = GameManager.instance.player2Device; break;
                case 2: isPlaying = GameManager.instance.player3Playable; myDevice = GameManager.instance.player3Device; break;
                case 3: isPlaying = GameManager.instance.player4Playable; myDevice = GameManager.instance.player4Device; break;
            }
        }

        gameObject.SetActive(isPlaying);

        if (cursorImage != null && playerIndex >= 0 && playerIndex < playerColors.Length)
        {
            Color finalColor = playerColors[playerIndex];
            finalColor.a = 1f;
            cursorImage.color = finalColor;
        }

        if (myDevice != null)
            Debug.Log($"<color=green>[VICTORY CURSOR]</color> Player {playerIndex} locked directly to device: {myDevice.name}");
        else if (isPlaying)
            Debug.LogWarning($"<color=red>[VICTORY CURSOR]</color> Player {playerIndex} is playing, but GameManager didn't have a saved device for them!");
        if (rectTransform != null) rectTransform.anchoredPosition = Vector2.zero;
        // Force the cursor to center itself
        if (rectTransform != null) rectTransform.anchoredPosition = Vector2.zero;

        // Force the scale to be normal so Unity doesn't squish it!
        if (rectTransform != null) rectTransform.localScale = Vector3.one;
        // --- Reactivate and set the Player Label Text ---
        Transform labelTransform = transform.Find("PlayerLabel");
        if (labelTransform != null)
        {
            // Make sure the label is turned on
            labelTransform.gameObject.SetActive(true);

            // Find all TextMeshPro components on this label (and its children/shadows)
            TextMeshProUGUI[] texts = labelTransform.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in texts)
            {
                // This will automatically turn Index 0 into "P 1", Index 1 into "P 2", etc.
                t.text = $"P {playerIndex + 1}";
            }
        }
        transform.SetAsLastSibling();
    }

    private void Update()
    {
        if (myDevice == null) return; // Wait until device is grabbed

        HandleMovement();
        HandleRaycast();
        HandleClick();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;

        if (myDevice is Gamepad pad)
        {
            moveInput = pad.leftStick.ReadValue();
            if (moveInput.magnitude < deadZone) moveInput = Vector2.zero;
        }
        else if (myDevice is Keyboard kb)
        {
            moveInput.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
            moveInput.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
        }

        if (moveInput != Vector2.zero)
        {
            rectTransform.anchoredPosition += moveInput.normalized * moveSpeed * Time.unscaledDeltaTime;
            ClampToScreenBounds();
        }
    }

    private void HandleRaycast()
    {
        Vector2 screenPos = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? rectTransform.position
            : RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, rectTransform.position);

        pointerEventData.position = screenPos;
        raycastResults.Clear();
        raycaster.Raycast(pointerEventData, raycastResults);

        hoveredButton = raycastResults.Count > 0 ? raycastResults[0].gameObject.GetComponentInParent<Button>() : null;
    }

    private void HandleClick()
    {
        bool pressedSelect = false;

        if (myDevice is Gamepad pad)
        {
            if (pad.buttonSouth.wasPressedThisFrame) pressedSelect = true;
        }
        else if (myDevice is Keyboard kb)
        {
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) pressedSelect = true;
        }

        if (pressedSelect && hoveredButton != null)
        {
            hoveredButton.onClick.Invoke();

            if (AudioManager.Instance != null && FMODEvents.Instance != null)
            {
                AudioManager.Instance.PlaySound(FMODEvents.Instance.CharSelectSelect, transform.position);
            }
        }
    }

    private void ClampToScreenBounds()
    {
        if (parentCanvas == null || rectTransform == null) return;
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 currentPos = rectTransform.anchoredPosition;
        float halfWidth = rectTransform.rect.width * 0.5f;
        float halfHeight = rectTransform.rect.height * 0.5f;

        currentPos.x = Mathf.Clamp(currentPos.x, -canvasSize.x * 0.5f + halfWidth, canvasSize.x * 0.5f - halfWidth);
        currentPos.y = Mathf.Clamp(currentPos.y, -canvasSize.y * 0.5f + halfHeight, canvasSize.y * 0.5f - halfHeight);
        rectTransform.anchoredPosition = currentPos;
    }
}