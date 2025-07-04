using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerAssignmentUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private GameObject playerSlotsContainer;
    [SerializeField] private GameObject[] playerSlotPanels;

    [Header("Player Slot UI")]
    [SerializeField] private Image[] playerSlotBackgrounds;
    [SerializeField] private TextMeshProUGUI[] playerSlotTexts;
    [SerializeField] private Image[] playerSlotIcons;
    [SerializeField] private GameObject[] playerSlotAssignedIndicators;

    [Header("Colors")]
    [SerializeField] private Color player1Color = Color.red;
    [SerializeField] private Color player2Color = Color.blue;
    [SerializeField] private Color player3Color = Color.green;
    [SerializeField] private Color player4Color = new Color(0.5f, 0f, 0.5f); // Purple
    [SerializeField] private Color unassignedColor = Color.gray;

    [SerializeField] private ControllerMapper controllerMapper;

    private void Start()
    {
        controllerMapper.EnablePlayerButtons();
        controllerMapper.EnableCursors();
        InitializeUI();
    }

    private void OnEnable()
    {
        // Buttons and cursors are always visible now
        controllerMapper.EnablePlayerButtons();
        controllerMapper.EnableCursors();
    }

    private void InitializeUI()
    {
        // Set up button listeners
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);

        // Initialize player slots
        for (int i = 0; i < playerSlotPanels.Length && i < playerSlotBackgrounds.Length && i < playerSlotTexts.Length && i < playerSlotAssignedIndicators.Length; i++)
        {
            if (playerSlotPanels[i] != null)
                playerSlotPanels[i].SetActive(true);

            if (playerSlotBackgrounds[i] != null)
                playerSlotBackgrounds[i].color = unassignedColor;

            if (playerSlotTexts[i] != null)
            {
                playerSlotTexts[i].text = $"Player {i + 1}";
                playerSlotTexts[i].color = Color.white;
            }

            if (playerSlotAssignedIndicators[i] != null)
                playerSlotAssignedIndicators[i].SetActive(false);
        }

        UpdateInstructionText();
        UpdateStartButtonState();
    }

    private void Update()
    {
        UpdateInstructionText();
        UpdateStartButtonState();
    }

    private void UpdateInstructionText()
    {
        if (instructionText == null) return;

        int connectedCount = Mathf.Min(Gamepad.all.Count, 4);
        if (connectedCount < 4) connectedCount += 1; // Include keyboard if space

        int assignedCount = GetAssignedPlayerCount();

        if (connectedCount == 0)
        {
            instructionText.text = "Connect controllers to begin...";
        }
        else if (assignedCount == 0)
        {
            instructionText.text = $"Use your controller to select a player slot.\nConnected controllers: {connectedCount}";
        }
        else if (assignedCount < connectedCount)
        {
            instructionText.text = $"Assign remaining controllers to player slots.\nAssigned: {assignedCount}/{connectedCount}";
        }
        else
        {
            instructionText.text = "All controllers assigned! Press Start Game to begin.";
        }
    }

    private int GetAssignedPlayerCount()
    {
        if (controllerMapper == null) return 0;

        int assignedCount = 0;
        var cursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);
        foreach (var cursor in cursors)
        {
            if (cursor.IsAssigned)
                assignedCount++;
        }

        return assignedCount;
    }

    private void UpdateStartButtonState()
    {
        if (startGameButton == null) return;

        int connectedCount = Gamepad.all.Count;
        int assignedCount = GetAssignedPlayerCount();

        bool canStart = assignedCount >= 2 && assignedCount <=4;

        startGameButton.interactable = canStart;

        var buttonText = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
            buttonText.text = canStart ? "Prsss \"Start\" to begin!" : "At least 2 Players To Start...";
    }

    private void OnPlayerAssigned(int playerIndex, Gamepad gamepad)
    {
        if (playerIndex >= 0 && playerIndex < playerSlotPanels.Length && playerIndex < playerSlotBackgrounds.Length && playerIndex < playerSlotTexts.Length && playerSlotAssignedIndicators.Length > playerIndex)
        {
            playerSlotBackgrounds[playerIndex].color = GetPlayerColor(playerIndex);
            playerSlotTexts[playerIndex].text = $"Player {playerIndex + 1}\n{gamepad.displayName}";
            playerSlotTexts[playerIndex].color = Color.white;
            playerSlotAssignedIndicators[playerIndex].SetActive(true);
            PlayAssignmentEffect(playerIndex);
        }
    }

    private void OnPlayerUnassigned(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerSlotPanels.Length && playerIndex < playerSlotBackgrounds.Length && playerIndex < playerSlotTexts.Length && playerSlotAssignedIndicators.Length > playerIndex)
        {
            playerSlotBackgrounds[playerIndex].color = unassignedColor;
            playerSlotTexts[playerIndex].text = $"Player {playerIndex + 1}";
            playerSlotTexts[playerIndex].color = Color.white;
            playerSlotAssignedIndicators[playerIndex].SetActive(false);
        }
    }

    private void OnGameStarted()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);

        Debug.Log("Player assignment UI hidden - game started");
    }

    private Color GetPlayerColor(int playerIndex)
    {
        return playerIndex switch
        {
            0 => player1Color,
            1 => player2Color,
            2 => player3Color,
            3 => player4Color,
            _ => Color.white
        };
    }

    private void PlayAssignmentEffect(int playerIndex)
    {
        Debug.Log($"Player {playerIndex + 1} assigned with effect!");
    }

    private void OnStartGameButtonClicked()
    {
        OnGameStarted();
        Debug.Log("Game started!");
    }

    private void OnResetButtonClicked()
    {
        var cursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);
        foreach (var cursor in cursors)
        {
            if (cursor.IsAssigned)
            {
                // cursor.UnassignPlayer(); // Uncomment if needed
            }
        }

        for (int i = 0; i < playerSlotPanels.Length; i++)
            OnPlayerUnassigned(i);
    }

    public void ShowUI()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);
    }

    public void HideUI()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);
    }

    public void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }

    public void SetInstructions(string instructions)
    {
        if (instructionText != null)
            instructionText.text = instructions;
    }
}
