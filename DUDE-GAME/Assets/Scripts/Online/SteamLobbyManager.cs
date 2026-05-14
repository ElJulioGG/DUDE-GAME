using System;
using FishNet;
using Steamworks;
using UnityEngine;

public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance { get; private set; }

    // --- Events ---
    public event Action OnLobbyCreated;
    public event Action OnLobbyJoined;
    public event Action<string> OnLobbyFailed;          // string = error reason
    public event Action<LobbyInfo[]> OnLobbyListReceived;
    public event Action OnDisconnected;

    // --- Public state ---
    public CSteamID CurrentLobbyID { get; private set; } = CSteamID.Nil;
    public bool IsHost => InstanceFinder.IsServerStarted;

    // --- Const ---
    private const string KEY_HOST_ID   = "hostSteamID";
    private const string KEY_NAME      = "lobbyName";
    private const string KEY_GAME      = "game";
    private const string GAME_TAG      = "DUDE_GAME";
    private const int    MAX_MACHINES  = 4;   // up to 4 separate connections (machines)

    // --- Steam callbacks ---
    private Callback<LobbyCreated_t>              _cbLobbyCreated;
    private Callback<GameLobbyJoinRequested_t>    _cbJoinRequested;   // Steam overlay invite
    private Callback<LobbyEnter_t>                _cbLobbyEnter;
    private CallResult<LobbyMatchList_t>          _crLobbyList;

    private string _pendingLobbyName;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[SteamLobbyManager] Steam not initialized.");
            return;
        }

        _cbLobbyCreated  = Callback<LobbyCreated_t>.Create(OnLobbyCreated_Cb);
        _cbJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested_Cb);
        _cbLobbyEnter    = Callback<LobbyEnter_t>.Create(OnLobbyEnter_Cb);
        _crLobbyList     = CallResult<LobbyMatchList_t>.Create(OnLobbyList_Cb);
    }

    // -------------------------------------------------------------------------
    // Host
    // -------------------------------------------------------------------------

    /// <summary>Create a public lobby visible to everyone in the lobby browser.</summary>
    public void HostPublicLobby(string lobbyName = "")
    {
        CreateLobby(ELobbyType.k_ELobbyTypePublic, lobbyName);
    }

    /// <summary>Create a friends-only lobby (visible to Steam friends; invite required for others).</summary>
    public void HostPrivateLobby(string lobbyName = "")
    {
        CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, lobbyName);
    }

    private void CreateLobby(ELobbyType type, string lobbyName)
    {
        if (!SteamManager.Initialized) return;

        _pendingLobbyName = string.IsNullOrWhiteSpace(lobbyName)
            ? SteamFriends.GetPersonaName() + "'s Game"
            : lobbyName;

        SteamMatchmaking.CreateLobby(type, MAX_MACHINES);
    }

    private void OnLobbyCreated_Cb(LobbyCreated_t param)
    {
        if (param.m_eResult != EResult.k_EResultOK)
        {
            string err = $"CreateLobby failed: {param.m_eResult}";
            Debug.LogError($"[SteamLobbyManager] {err}");
            OnLobbyFailed?.Invoke(err);
            return;
        }

        CurrentLobbyID = new CSteamID(param.m_ulSteamIDLobby);

        // Store metadata clients will read when they enter
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, KEY_HOST_ID, SteamUser.GetSteamID().m_SteamID.ToString());
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, KEY_NAME, _pendingLobbyName);
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, KEY_GAME, GAME_TAG);

        // Start FishNet as host (server + local client)
        InstanceFinder.NetworkManager.ServerManager.StartConnection();
        InstanceFinder.NetworkManager.ClientManager.StartConnection();

        Debug.Log($"[SteamLobbyManager] Lobby created: {CurrentLobbyID} — \"{_pendingLobbyName}\"");
        OnLobbyCreated?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Join
    // -------------------------------------------------------------------------

    /// <summary>Join a lobby by its CSteamID (used by the public browser or direct paste).</summary>
    public void JoinLobby(CSteamID lobbyID)
    {
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    /// <summary>Join using the lobby ID as a string (for a "paste code" UI field).</summary>
    public void JoinLobbyByString(string lobbyIDStr)
    {
        if (ulong.TryParse(lobbyIDStr, out ulong id))
            JoinLobby(new CSteamID(id));
        else
            OnLobbyFailed?.Invoke("Invalid lobby ID.");
    }

    // Called automatically when the user accepts a Steam overlay invite
    private void OnJoinRequested_Cb(GameLobbyJoinRequested_t param)
    {
        JoinLobby(param.m_steamIDLobby);
    }

    private void OnLobbyEnter_Cb(LobbyEnter_t param)
    {
        // The host also gets this callback — ignore it, server is already running
        if (InstanceFinder.IsServerStarted) return;

        CurrentLobbyID = new CSteamID(param.m_ulSteamIDLobby);

        string hostIDStr = SteamMatchmaking.GetLobbyData(CurrentLobbyID, KEY_HOST_ID);
        if (string.IsNullOrEmpty(hostIDStr))
        {
            string err = "Could not read host SteamID from lobby data.";
            Debug.LogError($"[SteamLobbyManager] {err}");
            OnLobbyFailed?.Invoke(err);
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            CurrentLobbyID = CSteamID.Nil;
            return;
        }

        // FishySteamworks parses this string as a ulong SteamID in P2P mode
        InstanceFinder.NetworkManager.ClientManager.StartConnection(hostIDStr);

        Debug.Log($"[SteamLobbyManager] Joined lobby {CurrentLobbyID}, connecting to host {hostIDStr}");
        OnLobbyJoined?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Public lobby browser
    // -------------------------------------------------------------------------

    /// <summary>Fetch a list of open public lobbies for this game.</summary>
    public void RequestPublicLobbies()
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter(KEY_GAME, GAME_TAG, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
        SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
        _crLobbyList.Set(handle);
    }

    private void OnLobbyList_Cb(LobbyMatchList_t param, bool ioFailure)
    {
        if (ioFailure) { OnLobbyFailed?.Invoke("Lobby list request failed."); return; }

        int count = (int)param.m_nLobbiesMatching;
        LobbyInfo[] results = new LobbyInfo[count];
        for (int i = 0; i < count; i++)
        {
            CSteamID id = SteamMatchmaking.GetLobbyByIndex(i);
            results[i] = new LobbyInfo
            {
                LobbyID        = id,
                Name           = SteamMatchmaking.GetLobbyData(id, KEY_NAME),
                CurrentMembers = SteamMatchmaking.GetNumLobbyMembers(id),
                MaxMembers     = SteamMatchmaking.GetLobbyMemberLimit(id),
            };
        }
        OnLobbyListReceived?.Invoke(results);
    }

    // -------------------------------------------------------------------------
    // Invite
    // -------------------------------------------------------------------------

    /// <summary>Send a Steam overlay invite to a friend.</summary>
    public void InviteFriend(CSteamID friendSteamID)
    {
        if (CurrentLobbyID.IsValid())
            SteamMatchmaking.InviteUserToLobby(CurrentLobbyID, friendSteamID);
    }

    /// <summary>Open the Steam overlay friend invite dialog for the current lobby.</summary>
    public void OpenInviteOverlay()
    {
        if (CurrentLobbyID.IsValid())
            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyID);
    }

    // -------------------------------------------------------------------------
    // Leave / Disconnect
    // -------------------------------------------------------------------------

    public void LeaveLobby()
    {
        if (CurrentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            CurrentLobbyID = CSteamID.Nil;
        }

        if (InstanceFinder.IsServerStarted)
            InstanceFinder.NetworkManager.ServerManager.StopConnection(true);

        if (InstanceFinder.IsClientStarted)
            InstanceFinder.NetworkManager.ClientManager.StopConnection();

        OnDisconnected?.Invoke();
    }
}

// -------------------------------------------------------------------------
// Data types
// -------------------------------------------------------------------------

[Serializable]
public struct LobbyInfo
{
    public CSteamID LobbyID;
    public string   Name;
    public int      CurrentMembers;
    public int      MaxMembers;

    public bool HasOpenSlot => CurrentMembers < MaxMembers;
}
