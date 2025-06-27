using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

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
        InitializeUI();
    }
   private void OnEnable(){
    controllerMapper.DisableCursors();
    controllerMapper.DisablePlayerButtons();
   }
   
   
    private void InitializeUI()
    {
        // Set up button listeners
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButtonClicked);
        }
        
        // Initialize player slots
        for (int i = 0; i < playerSlotPanels.Length && i < playerSlotBackgrounds.Length && i < playerSlotTexts.Length && i < playerSlotAssignedIndicators.Length; i++)
        {
            if (playerSlotPanels[i] != null)
            {
                playerSlotPanels[i].SetActive(true);
            }
            
            if (playerSlotBackgrounds[i] != null)
            {
                playerSlotBackgrounds[i].color = unassignedColor;
            }
            
            if (playerSlotTexts[i] != null)
            {
                playerSlotTexts[i].text = $"Player {i + 1}";
                playerSlotTexts[i].color = Color.white;
            }
            
            if (playerSlotAssignedIndicators[i] != null)
            {
                playerSlotAssignedIndicators[i].SetActive(false);
            }
        }
        
        // Set initial text
        UpdateInstructionText();
        UpdateStartButtonState();
    }
    
    private void Update()
    {
        UpdateInstructionText();
        UpdateStartButtonState();
        controllerMapper.EnablePlayerButtons();
        controllerMapper.EnableCursors();
    }
    
    private void UpdateInstructionText()
    {
        if (instructionText == null) return;
        
        int connectedCount = Mathf.Min(Gamepad.all.Count, 4);
        if (connectedCount < 4) connectedCount += 1; // Se incluye el teclado como dispositivo

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
        
        // Count how many cursors are assigned
        int assignedCount = 0;
        var cursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);
        foreach (var cursor in cursors)
        {
            if (cursor.IsAssigned)
            {
                assignedCount++;
            }
        }
        return assignedCount;
    }
    
    private void UpdateStartButtonState()
    {
        if (startGameButton == null) return;
        
        int connectedCount = Gamepad.all.Count;
        int assignedCount = GetAssignedPlayerCount();
        
        bool canStart = assignedCount >= 2 && assignedCount == connectedCount;
        
        startGameButton.interactable = canStart;
        
        // Update button text
        var buttonText = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = canStart ? "Start Game" : "Need 2+ Players";
        }
    }
    
    private void OnPlayerAssigned(int playerIndex, Gamepad gamepad)
    {
        if (playerIndex >= 0 && playerIndex < playerSlotPanels.Length && playerIndex < playerSlotBackgrounds.Length && playerIndex < playerSlotTexts.Length && playerIndex < playerSlotAssignedIndicators.Length)
        {
            // Update slot appearance
            if (playerSlotBackgrounds[playerIndex] != null)
            {
                playerSlotBackgrounds[playerIndex].color = GetPlayerColor(playerIndex);
            }
            
            if (playerSlotTexts[playerIndex] != null)
            {
                playerSlotTexts[playerIndex].text = $"Player {playerIndex + 1}\n{gamepad.displayName}";
                playerSlotTexts[playerIndex].color = Color.white;
            }
            
            if (playerSlotAssignedIndicators[playerIndex] != null)
            {
                playerSlotAssignedIndicators[playerIndex].SetActive(true);
            }
            
            // Play assignment effect
            PlayAssignmentEffect(playerIndex);
        }
    }
    
    private void OnPlayerUnassigned(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerSlotPanels.Length && playerIndex < playerSlotBackgrounds.Length && playerIndex < playerSlotTexts.Length && playerIndex < playerSlotAssignedIndicators.Length)
        {
            // Reset slot appearance
            if (playerSlotBackgrounds[playerIndex] != null)
            {
                playerSlotBackgrounds[playerIndex].color = unassignedColor;
            }
            
            if (playerSlotTexts[playerIndex] != null)
            {
                playerSlotTexts[playerIndex].text = $"Player {playerIndex + 1}";
                playerSlotTexts[playerIndex].color = Color.white;
            }
            
            if (playerSlotAssignedIndicators[playerIndex] != null)
            {
                playerSlotAssignedIndicators[playerIndex].SetActive(false);
            }
        }
    }
    
    private void OnGameStarted()
    {
        // Hide the assignment UI
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }
        
        Debug.Log("Player assignment UI hidden - game started");
    }
    
    private Color GetPlayerColor(int playerIndex)
    {
        switch (playerIndex)
        {
            case 0: return player1Color;
            case 1: return player2Color;
            case 2: return player3Color;
            case 3: return player4Color;
            default: return Color.white;
        }
    }
    
    private void PlayAssignmentEffect(int playerIndex)
    {
        // You can add visual effects here, like:
        // - Particle effects
        // - Screen shake
        // - Color flash
        // - Sound effects
        
        // For now, just log the assignment
        Debug.Log($"Player {playerIndex + 1} assigned with effect!");
    }
    
    // Button event handlers
    private void OnStartGameButtonClicked()
    {
        // Hide the UI and start the game
        OnGameStarted();
        
        // You can add additional game start logic here
        Debug.Log("Game started!");
    }
    
    private void OnResetButtonClicked()
    {
        // Reset all player assignments
        var cursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);
        foreach (var cursor in cursors)
        {
            if (cursor.IsAssigned)
            {
               // cursor.UnassignPlayer();
            }
        }
        
        // Reset UI
        for (int i = 0; i < playerSlotPanels.Length && i < playerSlotBackgrounds.Length && i < playerSlotTexts.Length && i < playerSlotAssignedIndicators.Length; i++)
        {
            OnPlayerUnassigned(i);
        }
    }
    
    // Public methods for external access
    public void ShowUI()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
    }
    
    public void HideUI()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }
    }
    
    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }
    
    public void SetInstructions(string instructions)
    {
        if (instructionText != null)
        {
            instructionText.text = instructions;
        }
    }
} 