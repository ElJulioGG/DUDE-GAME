using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening; // Asegúrate de tener DOTween importado

public class PlayerSlotColor : MonoBehaviour
{
    void Start()
    {
        PlayerInput input = GetComponent<PlayerInput>();

        if (input == null)
        {
            Debug.LogWarning("Falta el componente PlayerInput.");
            return;
        }

        int playerIndex = input.playerIndex;

        Transform slot = SmashCSS.instance.playerSlotsContainer.GetChild(playerIndex);

        if (slot != null)
        {
            Image artwork = slot.Find("artwork").GetComponent<Image>();
            if (artwork != null)
            {
                artwork.color = Color.gray;
                artwork.DOColor(Color.red, 0.5f);
            }
        }
    }
}
