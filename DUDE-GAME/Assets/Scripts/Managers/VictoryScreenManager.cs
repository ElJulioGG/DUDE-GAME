using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class VictoryScreenManager : MonoBehaviour
{
    [Header("Winner Announcement")]
    [SerializeField] private TextMeshProUGUI winnerText;

    [Header("Player 1 UI")]
    [SerializeField] private GameObject p1Panel;
    [SerializeField] private Image p1Sprite;
    [SerializeField] private TextMeshProUGUI p1ScoreText;

    [Header("Player 2 UI")]
    [SerializeField] private GameObject p2Panel;
    [SerializeField] private Image p2Sprite;
    [SerializeField] private TextMeshProUGUI p2ScoreText;

    [Header("Player 3 UI")]
    [SerializeField] private GameObject p3Panel;
    [SerializeField] private Image p3Sprite;
    [SerializeField] private TextMeshProUGUI p3ScoreText;

    [Header("Player 4 UI")]
    [SerializeField] private GameObject p4Panel;
    [SerializeField] private Image p4Sprite;
    [SerializeField] private TextMeshProUGUI p4ScoreText;

    void Start()
    {
        // Unlock the cursor so players can click buttons on the UI if you add a "Main Menu" button later
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        PopulateVictoryScreen();
    }

    private void PopulateVictoryScreen()
    {
        int highestScore = -1;
        int winnerIndex = -1;

        // --- Player 1 ---
        if (GameManager.instance.player1Playable)
        {
            p1Panel.SetActive(true);
            p1Sprite.sprite = GameManager.instance.player1Icon;
            p1ScoreText.text = $"Score: {GameManager.instance.player1Score}";

            if (GameManager.instance.player1Score > highestScore)
            {
                highestScore = GameManager.instance.player1Score;
                winnerIndex = 1;
            }
        }
        else p1Panel.SetActive(false);

        // --- Player 2 ---
        if (GameManager.instance.player2Playable)
        {
            p2Panel.SetActive(true);
            p2Sprite.sprite = GameManager.instance.player2Icon;
            p2ScoreText.text = $"Score: {GameManager.instance.player2Score}";

            if (GameManager.instance.player2Score > highestScore)
            {
                highestScore = GameManager.instance.player2Score;
                winnerIndex = 2;
            }
        }
        else p2Panel.SetActive(false);

        // --- Player 3 ---
        if (GameManager.instance.player3Playable)
        {
            p3Panel.SetActive(true);
            p3Sprite.sprite = GameManager.instance.player3Icon;
            p3ScoreText.text = $"Score: {GameManager.instance.player3Score}";

            if (GameManager.instance.player3Score > highestScore)
            {
                highestScore = GameManager.instance.player3Score;
                winnerIndex = 3;
            }
        }
        else p3Panel.SetActive(false);

        // --- Player 4 ---
        if (GameManager.instance.player4Playable)
        {
            p4Panel.SetActive(true);
            p4Sprite.sprite = GameManager.instance.player4Icon;
            p4ScoreText.text = $"Score: {GameManager.instance.player4Score}";

            if (GameManager.instance.player4Score > highestScore)
            {
                highestScore = GameManager.instance.player4Score;
                winnerIndex = 4;
            }
        }
        else p4Panel.SetActive(false);

        // --- Set Winner Text ---
        if (winnerIndex != -1)
        {
            string colorName = "PLAYER";
            string hexColor = "#FFFFFF"; // Default to white

            // Map the winning index (1-4) to your specific character colors
            switch (winnerIndex)
            {
                case 1:
                    colorName = "RED";
                    hexColor = "#FF0000";
                    break;
                case 2:
                    colorName = "BLUE";
                    hexColor = "#0000FF";
                    break;
                case 3:
                    colorName = "GREEN";
                    hexColor = "#00FF00";
                    break;
                case 4:
                    colorName = "PURPLE";
                    hexColor = "#800080";
                    break;
            }

            // Apply the text and use TMPro Rich Text to color it!
            winnerText.text = $"<color={hexColor}>{colorName}</color>";
        }
    }

    public void ReturnToMenu()
    {
        // 1. Reset all scores back to zero for the next game
        GameManager.instance.player1Score = 0;
        GameManager.instance.player2Score = 0;
        GameManager.instance.player3Score = 0;
        GameManager.instance.player4Score = 0;

        // 2. You can also reset powerups or other temporary match data here if needed
        GameManager.instance.player1PowerUp = 0;
        GameManager.instance.player2PowerUp = 0;
        GameManager.instance.player3PowerUp = 0;
        GameManager.instance.player4PowerUp = 0;

        // 3. Find your SimpleSceneManager and load the Main Menu
        SimpleSceneManager sceneManager = FindFirstObjectByType<SimpleSceneManager>();

        if (sceneManager != null)
        {
            // Replace "MainMenu" with the exact name of your menu or character select scene!
            sceneManager.LoadSceneByName("Menu");
        }
        else
        {
            Debug.LogError("SimpleSceneManager not found! Cannot leave Victory Screen.");
        }
    }
}