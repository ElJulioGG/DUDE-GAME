using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drop this on a canvas object in your menu scene.
/// Wire up the panel GameObjects and UI elements in the Inspector.
/// Buttons in the menu call the public methods directly.
///
/// Panel hierarchy you need to build in the scene:
///   OnlineMenuUI (this script)
///   ├── MainOnlinePanel       — "Create Lobby" button, "Browse" button, "Join by Code" button, "Back" button
///   ├── CreatePanel           — Name input, Public/Private toggle, Passcode input (hides when public), "Create" button
///   ├── BrowsePanel           — Refresh button, scroll view with LobbyRowUI rows, "Back" button
///   └── JoinByCodePanel       — Code input, "Join" button, status text, "Back" button
/// </summary>
public class OnlineMenuUI : MonoBehaviour
{
    // --- Panels ---
    [Header("Panels")]
    [SerializeField] private GameObject mainOnlinePanel;
    [SerializeField] private GameObject createPanel;
    [SerializeField] private GameObject browsePanel;
    [SerializeField] private GameObject joinByCodePanel;

    // --- Create panel ---
    [Header("Create Panel")]
    [SerializeField] private TMP_InputField createNameInput;
    [SerializeField] private Toggle         publicToggle;       // on = public, off = private
    [SerializeField] private GameObject     passcodeRow;        // shown only when private
    [SerializeField] private TMP_InputField passcodeInput;
    [SerializeField] private TextMeshProUGUI createStatusText;
    [SerializeField] private TextMeshProUGUI lobbyCodeDisplay;  // shows generated code after creation

    // --- Browse panel ---
    [Header("Browse Panel")]
    [SerializeField] private Transform      lobbyListContent;   // ScrollRect → Viewport → Content
    [SerializeField] private GameObject     lobbyRowPrefab;     // needs LobbyRowUI component
    [SerializeField] private TextMeshProUGUI browseStatusText;

    // --- Join by code panel ---
    [Header("Join By Code Panel")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private TextMeshProUGUI joinCodeStatusText;

    // --- General ---
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private float  panelFadeDuration = 0.18f;

    private readonly List<GameObject> _rows = new();

    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        SteamLobbyManager.Instance.OnLobbyCreated      += HandleLobbyCreated;
        SteamLobbyManager.Instance.OnLobbyFailed       += HandleLobbyFailed;
        SteamLobbyManager.Instance.OnPublicListReceived += HandlePublicList;
        SteamLobbyManager.Instance.OnCodeSearchReceived += HandleCodeSearch;
        SteamLobbyManager.Instance.OnLobbyJoined       += HandleLobbyJoined;
    }

    private void OnDisable()
    {
        if (SteamLobbyManager.Instance == null) return;
        SteamLobbyManager.Instance.OnLobbyCreated      -= HandleLobbyCreated;
        SteamLobbyManager.Instance.OnLobbyFailed       -= HandleLobbyFailed;
        SteamLobbyManager.Instance.OnPublicListReceived -= HandlePublicList;
        SteamLobbyManager.Instance.OnCodeSearchReceived -= HandleCodeSearch;
        SteamLobbyManager.Instance.OnLobbyJoined       -= HandleLobbyJoined;
    }

    private void Start()
    {
        // All panels off by default; only shown when the user navigates to them
        SetPanelVisible(mainOnlinePanel,   false, instant: true);
        SetPanelVisible(createPanel,       false, instant: true);
        SetPanelVisible(browsePanel,       false, instant: true);
        SetPanelVisible(joinByCodePanel,   false, instant: true);

        if (publicToggle != null)
            publicToggle.onValueChanged.AddListener(OnPublicToggleChanged);

        OnPublicToggleChanged(publicToggle != null && publicToggle.isOn);
    }

    // =========================================================================
    // Navigation — call these from UI buttons
    // =========================================================================

    public void OpenOnlineMenu()
    {
        SetPanelVisible(mainOnlinePanel, true);
    }

    public void CloseOnlineMenu()
    {
        SetPanelVisible(mainOnlinePanel, false);
    }

    public void OpenCreatePanel()
    {
        SetPanelVisible(createPanel, true);
        if (createStatusText)   createStatusText.text   = string.Empty;
        if (lobbyCodeDisplay)   lobbyCodeDisplay.text   = string.Empty;
        if (createNameInput)    createNameInput.text    = string.Empty;
        if (passcodeInput)      passcodeInput.text      = string.Empty;
    }

    public void CloseCreatePanel()  => SetPanelVisible(createPanel, false);

    public void OpenBrowsePanel()
    {
        SetPanelVisible(browsePanel, true);
        RefreshLobbyList();
    }

    public void CloseBrowsePanel()  => SetPanelVisible(browsePanel, false);

    public void OpenJoinByCodePanel()
    {
        SetPanelVisible(joinByCodePanel, true);
        if (joinCodeStatusText) joinCodeStatusText.text = string.Empty;
        if (codeInput)          codeInput.text          = string.Empty;
    }

    public void CloseJoinByCodePanel() => SetPanelVisible(joinByCodePanel, false);

    // =========================================================================
    // Create lobby
    // =========================================================================

    public void ConfirmCreateLobby()
    {
        if (SteamLobbyManager.Instance == null) return;

        bool isPublic    = publicToggle  == null || publicToggle.isOn;
        string lobbyName = createNameInput != null ? createNameInput.text.Trim() : string.Empty;
        string passcode  = passcodeInput   != null ? passcodeInput.text.Trim()   : string.Empty;

        if (createStatusText)
            createStatusText.text = "Creating lobby...";

        OnlineLobbyManager.Instance?.PrepareOnlineSession();

        if (isPublic)
            SteamLobbyManager.Instance.HostPublicLobby(lobbyName);
        else
            SteamLobbyManager.Instance.HostPrivateLobby(lobbyName, passcode);
    }

    private void OnPublicToggleChanged(bool isPublic)
    {
        if (passcodeRow != null)
            passcodeRow.SetActive(!isPublic);
    }

    // =========================================================================
    // Browse
    // =========================================================================

    public void RefreshLobbyList()
    {
        ClearRows();
        if (browseStatusText) browseStatusText.text = "Searching...";
        SteamLobbyManager.Instance?.RequestPublicLobbies();
    }

    private void HandlePublicList(LobbyInfo[] lobbies)
    {
        ClearRows();

        if (lobbies.Length == 0)
        {
            if (browseStatusText) browseStatusText.text = "No public lobbies found.";
            return;
        }

        if (browseStatusText) browseStatusText.text = $"{lobbies.Length} lobby(s) found";

        foreach (var info in lobbies)
        {
            GameObject row = Instantiate(lobbyRowPrefab, lobbyListContent);
            row.GetComponent<LobbyRowUI>().Initialize(info, OnRowJoinClicked);
            _rows.Add(row);
        }
    }

    private void OnRowJoinClicked(LobbyInfo info)
    {
        OnlineLobbyManager.Instance?.PrepareOnlineSession();
        SteamLobbyManager.Instance.JoinLobby(info.LobbyID);
    }

    // =========================================================================
    // Join by code
    // =========================================================================

    public void ConfirmJoinByCode()
    {
        if (codeInput == null || string.IsNullOrWhiteSpace(codeInput.text)) return;
        if (joinCodeStatusText) joinCodeStatusText.text = "Searching...";

        OnlineLobbyManager.Instance?.PrepareOnlineSession();
        SteamLobbyManager.Instance?.RequestLobbyByCode(codeInput.text.Trim());
    }

    private void HandleCodeSearch(LobbyInfo[] results)
    {
        // If exactly 1 found, SteamLobbyManager auto-joins it; otherwise show count
        if (results.Length > 1 && joinCodeStatusText)
            joinCodeStatusText.text = $"Found {results.Length} matches — showing first.";
    }

    // =========================================================================
    // Steam event handlers
    // =========================================================================

    private void HandleLobbyCreated()
    {
        if (createStatusText)
            createStatusText.text = "Lobby created! Waiting in game scene...";

        // Show the code the host can share with friends (for private lobbies)
        string code = SteamLobbyManager.Instance.CurrentLobbyCode;
        if (lobbyCodeDisplay)
            lobbyCodeDisplay.text = string.IsNullOrEmpty(code) ? string.Empty : $"Code: {code}";

        // Load the game scene — lobby members will join there
        SceneManager.LoadScene(gameSceneName);
    }

    private void HandleLobbyJoined()
    {
        // Client has joined the lobby and FishNet is connecting — load game scene
        SceneManager.LoadScene(gameSceneName);
    }

    private void HandleLobbyFailed(string error)
    {
        if (createStatusText)    createStatusText.text    = error;
        if (browseStatusText)    browseStatusText.text    = error;
        if (joinCodeStatusText)  joinCodeStatusText.text  = error;
        Debug.LogError($"[OnlineMenuUI] {error}");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void SetPanelVisible(GameObject panel, bool visible, bool instant = false)
    {
        if (panel == null) return;

        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        if (instant)
        {
            cg.alpha          = visible ? 1f : 0f;
            cg.interactable   = visible;
            cg.blocksRaycasts = visible;
            panel.SetActive(visible);
            return;
        }

        if (visible) panel.SetActive(true);

        cg.DOFade(visible ? 1f : 0f, panelFadeDuration)
          .OnComplete(() =>
          {
              cg.interactable   = visible;
              cg.blocksRaycasts = visible;
              if (!visible) panel.SetActive(false);
          });
    }

    private void ClearRows()
    {
        foreach (var r in _rows)
            if (r != null) Destroy(r);
        _rows.Clear();
    }
}
