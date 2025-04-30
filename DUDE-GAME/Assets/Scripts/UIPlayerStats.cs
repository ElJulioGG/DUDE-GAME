using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerStats : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] private TMP_Text playerPointsText;
    [SerializeField] private Image playerIcon;
    [SerializeField] private Image playerPowerIcon;

    void Update()
    {
        switch (playerIndex)
        {
            case 0:
                playerPointsText.text = GameManager.instance.player1Score.ToString();
                playerIcon.sprite = GameManager.instance.player1Icon;
                playerPowerIcon.sprite = GameManager.instance.powerUpIcons[GameManager.instance.player1Power];
                break;
            case 1:
                playerPointsText.text = GameManager.instance.player2Score.ToString();
                playerIcon.sprite = GameManager.instance.player2Icon;
                playerPowerIcon.sprite = GameManager.instance.powerUpIcons[GameManager.instance.player2Power];
                break;
            case 2:
                playerPointsText.text = GameManager.instance.player3Score.ToString();
                playerIcon.sprite = GameManager.instance.player3Icon;
                playerPowerIcon.sprite = GameManager.instance.powerUpIcons[GameManager.instance.player3Power];
                break;
            case 3:
                playerPointsText.text = GameManager.instance.player4Score.ToString();
                playerIcon.sprite = GameManager.instance.player4Icon;
                playerPowerIcon.sprite = GameManager.instance.powerUpIcons[GameManager.instance.player4Power];
                break;
        }
    }
}
