using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CursorDetection : MonoBehaviour
{
    private GraphicRaycaster gr;
    private PointerEventData pointerEventData;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    public Transform currentCharacter;
    public Transform token;
    public bool hasToken;

    [Header("Player Index")]
    public int playerIndex;
    private PlayerInput playerInput;

    // Cached to avoid per-frame lookups
    private InputAction _acceptAction;
    private InputAction _cancelAction;
    private Camera _mainCamera;
    private Image _currentBorderImage;

    void Start()
    {
        gr = GetComponentInParent<GraphicRaycaster>();
        pointerEventData = new PointerEventData(EventSystem.current);
        playerInput = GetComponent<PlayerInput>();

        _acceptAction = playerInput.actions["Accept"];
        _cancelAction = playerInput.actions["Cancel"];
        _mainCamera = Camera.main;

        SmashCSS.instance.ShowCharacterInSlot(playerIndex, null);
    }

    void Update()
    {
        if (_acceptAction.triggered)
        {
            if (currentCharacter != null)
            {
                TokenFollow(false);
                Character selected = SmashCSS.instance.characters[currentCharacter.GetSiblingIndex()];
                SmashCSS.instance.ConfirmCharacter(playerIndex, selected);
            }
        }

        if (_cancelAction.triggered)
        {
            SmashCSS.instance.ClearConfirmedCharacter(playerIndex);
            TokenFollow(true);
        }

        if (hasToken)
            token.position = transform.position;

        pointerEventData.position = _mainCamera.WorldToScreenPoint(transform.position);
        _raycastResults.Clear();
        gr.Raycast(pointerEventData, _raycastResults);

        if (hasToken)
        {
            if (_raycastResults.Count > 0)
            {
                Transform raycastChar = _raycastResults[0].gameObject.transform;
                if (raycastChar != currentCharacter)
                {
                    ClearCurrentHighlight();
                    SetCurrentCharacter(raycastChar);
                }
            }
            else
            {
                ClearCurrentHighlight();
                SetCurrentCharacter(null);
            }
        }
    }

    void SetCurrentCharacter(Transform t)
    {
        currentCharacter = t;

        if (t != null)
        {
            _currentBorderImage = t.Find("selectedBorder").GetComponent<Image>();
            _currentBorderImage.color = Color.white;

            int index = t.GetSiblingIndex();
            Character character = SmashCSS.instance.characters[index];
            SmashCSS.instance.ShowCharacterInSlot(playerIndex, character);
        }
        else
        {
            _currentBorderImage = null;
            SmashCSS.instance.ShowCharacterInSlot(playerIndex, null);
        }
    }

    void ClearCurrentHighlight()
    {
        if (_currentBorderImage != null)
            _currentBorderImage.color = Color.clear;
    }

    void TokenFollow(bool trigger)
    {
        hasToken = trigger;
    }
}
