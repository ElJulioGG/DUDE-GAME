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
    [SerializeField] private Image[] playerSlotDarkenImages;
    [SerializeField] private Image[] playerSlotIcons;
    [SerializeField] private GameObject[] playerSlotAssignedIndicators;

    [Header("Darken Image Opacity")]
    [Range(0f, 1f)] [SerializeField] private float darkenAssignedAlpha = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float darkenUnassignedAlpha = 0f;

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
                              playerSlotDarkenImages.Length, playerSlotAssignedIndicators.Length);
        for (int i = 0; i < limit; i++)
        {
            if (playerSlotPanels[i] != null)
                playerSlotPanels[i].SetActive(true);

            if (playerSlotBackgrounds[i] != null)
                playerSlotBackgrounds[i].color = unassignedColor;

            if (playerSlotDarkenImages[i] != null)
                SetImageAlpha(playerSlotDarkenImages[i], darkenUnassignedAlpha);

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
        UpdateDarkenImages();
    }

    private void UpdateDarkenImages()
    {
        if (playerSlotDarkenImages == null) return;

        bool[] slotAssigned = new bool[playerSlotDarkenImages.Length];

        var cursors = PlayerCursor.All;
        if (cursors != null)
        {
            foreach (var cursor in cursors)
            {
                int idx = cursor.AssignedPlayerIndex;
                if (cursor.IsAssigned && idx >= 0 && idx < slotAssigned.Length)
                    slotAssigned[idx] = true;
            }
        }

        for (int i = 0; i < playerSlotDarkenImages.Length; i++)
        {
            if (playerSlotDarkenImages[i] != null)
                SetImageAlpha(playerSlotDarkenImages[i], slotAssigned[i] ? darkenAssignedAlpha : darkenUnassignedAlpha);
        }
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

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void OnGameStarted()
    {
         AudioManager.Instance.PlaySound(FMODEvents.Instance.CharSelectStart, transform.position);
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
