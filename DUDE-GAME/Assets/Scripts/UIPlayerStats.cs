using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIPlayerStats : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] private TMP_Text playerPointsText;
    [SerializeField] private Image playerIcon;
    [SerializeField] private Image playerPowerIcon;
    [SerializeField] private Image[] controllerIcons; // 0: Xbox, 1: Switch, 2: PS5

    private Tween pulseTween;
    private int lastPowerUpIndex = -1;
    private Image currentActiveIcon = null;

    private void Update()
    {
        int score = 0;
        Sprite icon = null;
        int powerUpIndex = 0;
        int controllerTypeIndex = 0;

        switch (playerIndex)
        {
            case 0:
                score = GameManager.instance.player1Score;
                icon = GameManager.instance.player1Icon;
                powerUpIndex = GameManager.instance.player1PowerUp;
                controllerTypeIndex = GameManager.instance.player1ControllerType;
                break;
            case 1:
                score = GameManager.instance.player2Score;
                icon = GameManager.instance.player2Icon;
                powerUpIndex = GameManager.instance.player2PowerUp;
                controllerTypeIndex = GameManager.instance.player2ControllerType;
                break;
            case 2:
                score = GameManager.instance.player3Score;
                icon = GameManager.instance.player3Icon;
                powerUpIndex = GameManager.instance.player3PowerUp;
                controllerTypeIndex = GameManager.instance.player3ControllerType;
                break;
            case 3:
                score = GameManager.instance.player4Score;
                icon = GameManager.instance.player4Icon;
                powerUpIndex = GameManager.instance.player4PowerUp;
                controllerTypeIndex = GameManager.instance.player4ControllerType;
                break;
        }

        // Update text and icons
        playerPointsText.text = score.ToString();
        playerIcon.sprite = icon;
        playerPowerIcon.sprite = GameManager.instance.powerUpIcons[powerUpIndex];

  
        // Deactivate all controller icons first
        foreach (var iconObj in controllerIcons)
        {
            iconObj.gameObject.SetActive(false);
        }

        // Only show icon if there is an active power-up
        if (powerUpIndex != 0 && controllerTypeIndex >= 0 && controllerTypeIndex < controllerIcons.Length)
        {
            currentActiveIcon = controllerIcons[controllerTypeIndex];
            currentActiveIcon.gameObject.SetActive(true);
        }
        else
        {
            currentActiveIcon = null;
        }


        // Power-up effect: Show icon and pulse only when there's an active power-up
        if (powerUpIndex != 0 && currentActiveIcon != null)
        {
            if (!currentActiveIcon.gameObject.activeSelf)
            {
                currentActiveIcon.gameObject.SetActive(true);
            }

            if (lastPowerUpIndex != powerUpIndex)
            {
                StartButtonPulse();
            }
        }
        else
        {
            StopButtonPulse();
        }

        lastPowerUpIndex = powerUpIndex;
    }

    private void StartButtonPulse()
    {
        StopButtonPulse(); // Stop any previous tween

        if (currentActiveIcon != null)
        {
            pulseTween = currentActiveIcon.transform
                .DOScale(1.1f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    private void StopButtonPulse()
    {
        if (pulseTween != null && pulseTween.IsActive())
        {
            pulseTween.Kill();
        }

        if (currentActiveIcon != null)
        {
            currentActiveIcon.transform.localScale = Vector3.one;
        }
    }
}
