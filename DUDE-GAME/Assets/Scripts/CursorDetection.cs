using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CursorDetection : MonoBehaviour
{
    private GraphicRaycaster gr;
    private PointerEventData pointerEventData;
    public Transform currentCharacter;
    public Transform token;
    public bool hasToken;

    [Header("Player Index")]
    public int playerIndex;

    void Start()
    {
        gr = GetComponentInParent<GraphicRaycaster>();
        pointerEventData = new PointerEventData(EventSystem.current);
        SmashCSS.instance.ShowCharacterInSlot(playerIndex, null);
    }

    void Update()
    {
        // Confirmar selección
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            if (currentCharacter != null)
            {
                TokenFollow(false);
                Character selected = SmashCSS.instance.characters[currentCharacter.GetSiblingIndex()];
                SmashCSS.instance.ConfirmCharacter(playerIndex, selected);
            }
        }

        // Cancelar selección
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            SmashCSS.instance.ClearConfirmedCharacter(playerIndex);
            TokenFollow(true);
        }

        // Mover token con el cursor
        if (hasToken)
        {
            token.position = transform.position;
        }

        // Raycasting UI
        pointerEventData.position = Camera.main.WorldToScreenPoint(transform.position);
        List<RaycastResult> results = new List<RaycastResult>();
        gr.Raycast(pointerEventData, results);

        if (hasToken)
        {
            if (results.Count > 0)
            {
                Transform raycastChar = results[0].gameObject.transform;

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
            t.Find("selectedBorder").GetComponent<Image>().color = Color.white;
            int index = t.GetSiblingIndex();
            Character character = SmashCSS.instance.characters[index];
            SmashCSS.instance.ShowCharacterInSlot(playerIndex, character);
        }
        else
        {
            SmashCSS.instance.ShowCharacterInSlot(playerIndex, null);
        }
    }

    void ClearCurrentHighlight()
    {
        if (currentCharacter != null)
        {
            currentCharacter.Find("selectedBorder").GetComponent<Image>().color = Color.clear;
        }
    }

    void TokenFollow(bool trigger)
    {
        hasToken = trigger;
    }
}
