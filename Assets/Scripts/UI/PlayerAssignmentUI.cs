using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerAssignmentUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private bool debug;
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
    [SerializeField] private Color player4Color = new Color(0.5f, 0f, 0.5f);
    [SerializeField] private Color unassignedColor = Color.gray;

    [SerializeField] private ControllerMapper controllerMapper;

    // Cached to avoid per-frame searches and GetComponent calls
    private TextMeshProUGUI _startButtonText;

    private void Start()
    {
        _startButtonText = startGameButton != null
            ? startGameButton.GetComponentInChildren<TextMeshProUGUI>()
            : null;

        controllerMapper.EnablePlayerButtons();
        controllerMapper.EnableCursors();
        InitializeUI();
    }

    private void OnEnable()
    {
        controllerMapper.EnablePlayerButtons();
        controllerMapper.EnableCursors();
    }

    private void InitializeUI()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);

        int limit = Mathf.Min(playerSlotPanels.Length, playerSlotBackgrounds.Length,
                              playerSlotTexts.Length, playerSlotAssignedIndicators.Length);
        for (int i = 0; i < limit; i++)
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

        UpdateUI(GetAssignedPlayerCount());
    }

    private void Update()
    {
        UpdateUI(GetAssignedPlayerCount());
    }

    private void UpdateUI(int assignedCount)
    {
        UpdateInstructionText(assignedCount);
        UpdateStartButtonState(assignedCount);
    }

    private void UpdateInstructionText(int assignedCount)
    {
        if (instructionText == null || !debug) return;

        int connectedCount = Mathf.Min(Gamepad.all.Count + 1, 4);

        if (connectedCount == 0)
            instructionText.text = "Connect controllers to begin...";
        else if (assignedCount == 0)
            instructionText.text = $"Use your controller to select a player slot.\nConnected controllers: {connectedCount}";
        else if (assignedCount < connectedCount)
            instructionText.text = $"Assign remaining controllers to player slots.\nAssigned: {assignedCount}/{connectedCount}";
        else
            instructionText.text = "All controllers assigned! Press Start Game to begin.";
    }

    private int GetAssignedPlayerCount()
    {
        var cursors = PlayerCursor.All;
        if (cursors == null) return 0;

        int count = 0;
        foreach (var cursor in cursors)
            if (cursor.IsAssigned) count++;
        return count;
    }

    private void UpdateStartButtonState(int assignedCount)
    {
        if (startGameButton == null) return;

        bool canStart = assignedCount >= 2 && assignedCount <= 4;
        startGameButton.interactable = canStart;

        if (_startButtonText != null)
            _startButtonText.text = canStart ? "Press \"Start\" to begin!" : "At least 2 Players To Start...";
    }

    private void OnPlayerAssigned(int playerIndex, Gamepad gamepad)
    {
        if (playerIndex < 0 || playerIndex >= playerSlotPanels.Length) return;

        playerSlotBackgrounds[playerIndex].color = GetPlayerColor(playerIndex);
        playerSlotTexts[playerIndex].text = $"Player {playerIndex + 1}\n{gamepad.displayName}";
        playerSlotTexts[playerIndex].color = Color.white;
        playerSlotAssignedIndicators[playerIndex].SetActive(true);
    }

    private void OnPlayerUnassigned(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerSlotPanels.Length) return;

        playerSlotBackgrounds[playerIndex].color = unassignedColor;
        playerSlotTexts[playerIndex].text = $"Player {playerIndex + 1}";
        playerSlotTexts[playerIndex].color = Color.white;
        playerSlotAssignedIndicators[playerIndex].SetActive(false);
    }

    private void OnGameStarted()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);
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

    private void OnStartGameButtonClicked()
    {
        OnGameStarted();
    }

    private void OnResetButtonClicked()
    {
        var cursors = PlayerCursor.All;
        if (cursors != null)
        {
            foreach (var cursor in cursors)
            {
                if (cursor.IsAssigned)
                    cursor.UnassignPlayer();
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
