using System;
using FishNet;
using FishySteamworks;
using Steamworks;
using UnityEngine;

public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance { get; private set; }

    // --- Events ---
    public event Action              OnLobbyCreated;      // host: lobby ready
    public event Action              OnLobbyJoined;       // client: FishNet connecting
    public event Action<string>      OnLobbyFailed;       // error message
    public event Action<LobbyInfo[]> OnPublicListReceived;
    public event Action<LobbyInfo[]> OnCodeSearchReceived;
    public event Action              OnDisconnected;

    // --- Public state ---
    public CSteamID CurrentLobbyID   { get; private set; } = CSteamID.Nil;
    public string   CurrentLobbyCode { get; private set; } = string.Empty;
    public bool     IsHost           => InstanceFinder.IsServerStarted;

    // --- Lobby metadata keys ---
    private const string KEY_HOST  = "hostSteamID";
    private const string KEY_NAME  = "lobbyName";
    private const string KEY_GAME  = "game";
    private const string KEY_TYPE  = "lobbyType";  // "pub" | "priv"
    private const string KEY_CODE  = "lobbyCode";
    private const string GAME_TAG  = "DUDE_GAME";
    private const int    MAX_CONN  = 4;

    private const string PUB  = "pub";
    private const string PRIV = "priv";

    // --- Steam callbacks ---
    private Callback<LobbyCreated_t>           _cbCreated;
    private Callback<GameLobbyJoinRequested_t> _cbInvite;
    private Callback<LobbyEnter_t>             _cbEnter;
    private CallResult<LobbyMatchList_t>       _crPublicList;
    private CallResult<LobbyMatchList_t>       _crCodeSearch;

    private string _pendingName;
    private bool   _pendingIsPublic;
    private string _pendingCode;

    private FishySteamworks.FishySteamworks _fishySteamworks;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _fishySteamworks = InstanceFinder.NetworkManager?.GetComponent<FishySteamworks.FishySteamworks>();
        if (_fishySteamworks == null)
            Debug.LogWarning("[SteamLobbyManager] FishySteamworks component not found on NetworkManager.");

        if (!SteamManager.Initialized) { Debug.LogError("[SteamLobbyManager] Steam not initialized."); return; }

        _cbCreated    = Callback<LobbyCreated_t>.Create(OnCreated_Cb);
        _cbInvite     = Callback<GameLobbyJoinRequested_t>.Create(OnInvite_Cb);
        _cbEnter      = Callback<LobbyEnter_t>.Create(OnEnter_Cb);
        _crPublicList = CallResult<LobbyMatchList_t>.Create(OnPublicList_Cb);
        _crCodeSearch = CallResult<LobbyMatchList_t>.Create(OnCodeSearch_Cb);
    }

    // =========================================================================
    // HOST
    // =========================================================================

    /// <summary>Public lobby — shows up in the browser, no passcode.</summary>
    public void HostPublicLobby(string lobbyName = "")
    {
        _pendingName     = FallbackName(lobbyName);
        _pendingIsPublic = true;
        _pendingCode     = string.Empty;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, MAX_CONN);
    }

    /// <summary>Private lobby — not in browser, joined only by passcode.</summary>
    public void HostPrivateLobby(string lobbyName = "", string passcode = "")
    {
        _pendingName     = FallbackName(lobbyName);
        _pendingIsPublic = false;
        _pendingCode     = passcode.Trim();
        // Still Public type so code-search can find it; the KEY_TYPE tag hides it from the browser
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, MAX_CONN);
    }

    private void OnCreated_Cb(LobbyCreated_t p)
    {
        if (p.m_eResult != EResult.k_EResultOK)
        {
            string err = $"CreateLobby failed: {p.m_eResult}";
            Debug.LogError($"[SteamLobbyManager] {err}");
            OnLobbyFailed?.Invoke(err);
            return;
        }

        CurrentLobbyID   = new CSteamID(p.m_ulSteamIDLobby);
        CurrentLobbyCode = _pendingCode;

        SteamMatchmaking.SetLobbyData(CurrentLobbyID, KEY_HOST, SteamUser.GetSteamID().m_SteamID.ToString());
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, KEY_NAME, _pendingName);
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, KEY_GAME, GAME_TAG);
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, KEY_TYPE, _pendingIsPublic ? PUB : PRIV);

        if (!_pendingIsPublic && !string.IsNullOrEmpty(_pendingCode))
            SteamMatchmaking.SetLobbyData(CurrentLobbyID, KEY_CODE, _pendingCode);

        GameSession.IsOnline = true;
        SwapToFishySteamworks();
        InstanceFinder.NetworkManager.ServerManager.StartConnection();
        InstanceFinder.NetworkManager.ClientManager.StartConnection();

        Debug.Log($"[SteamLobbyManager] Created {(_pendingIsPublic ? "public" : "private")} lobby {CurrentLobbyID}");
        OnLobbyCreated?.Invoke();
    }

    // =========================================================================
    // JOIN
    // =========================================================================

    public void JoinLobby(CSteamID lobbyID) => SteamMatchmaking.JoinLobby(lobbyID);

    private void OnInvite_Cb(GameLobbyJoinRequested_t p) => JoinLobby(p.m_steamIDLobby);

    private void OnEnter_Cb(LobbyEnter_t p)
    {
        if (InstanceFinder.IsServerStarted) return; // host already running

        CurrentLobbyID = new CSteamID(p.m_ulSteamIDLobby);

        string hostID = SteamMatchmaking.GetLobbyData(CurrentLobbyID, KEY_HOST);
        if (string.IsNullOrEmpty(hostID))
        {
            string err = "Could not read host SteamID from lobby.";
            Debug.LogError($"[SteamLobbyManager] {err}");
            OnLobbyFailed?.Invoke(err);
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            CurrentLobbyID = CSteamID.Nil;
            return;
        }

        GameSession.IsOnline = true;
        SwapToFishySteamworks();
        InstanceFinder.NetworkManager.ClientManager.StartConnection(hostID);
        Debug.Log($"[SteamLobbyManager] Joined lobby, connecting to host {hostID}");
        OnLobbyJoined?.Invoke();
    }

    // =========================================================================
    // PUBLIC BROWSER  (sorted closest region first — Steam handles ping ranking)
    // =========================================================================

    public void RequestPublicLobbies()
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter(KEY_GAME, GAME_TAG, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(KEY_TYPE, PUB,      ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault);

        _crPublicList.Set(SteamMatchmaking.RequestLobbyList());
    }

    private void OnPublicList_Cb(LobbyMatchList_t p, bool fail)
    {
        if (fail) { OnLobbyFailed?.Invoke("Lobby list request failed."); return; }
        OnPublicListReceived?.Invoke(Build((int)p.m_nLobbiesMatching));
    }

    // =========================================================================
    // JOIN BY PASSCODE
    // =========================================================================

    /// <summary>
    /// Search Steam for a private lobby whose KEY_CODE matches the given passcode.
    /// If exactly one is found it auto-joins; otherwise fires OnCodeSearchReceived.
    /// </summary>
    public void RequestLobbyByCode(string code)
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter(KEY_GAME, GAME_TAG,      ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(KEY_TYPE, PRIV,          ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(KEY_CODE, code.Trim(),   ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);

        _crCodeSearch.Set(SteamMatchmaking.RequestLobbyList());
    }

    private void OnCodeSearch_Cb(LobbyMatchList_t p, bool fail)
    {
        if (fail) { OnLobbyFailed?.Invoke("Code search failed."); return; }

        LobbyInfo[] results = Build((int)p.m_nLobbiesMatching);
        OnCodeSearchReceived?.Invoke(results);

        if      (results.Length == 1) JoinLobby(results[0].LobbyID);
        else if (results.Length == 0) OnLobbyFailed?.Invoke("No lobby found with that code.");
    }

    // =========================================================================
    // INVITE OVERLAY  (always available for the host)
    // =========================================================================

    public void OpenInviteOverlay()
    {
        if (CurrentLobbyID.IsValid())
            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyID);
    }

    // =========================================================================
    // LEAVE
    // =========================================================================

    public void LeaveLobby()
    {
        if (CurrentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            CurrentLobbyID   = CSteamID.Nil;
            CurrentLobbyCode = string.Empty;
        }

        if (InstanceFinder.IsServerStarted)
            InstanceFinder.NetworkManager.ServerManager.StopConnection(true);
        if (InstanceFinder.IsClientStarted)
            InstanceFinder.NetworkManager.ClientManager.StopConnection();

        GameSession.IsOnline = false;
        OnDisconnected?.Invoke();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void SwapToFishySteamworks()
    {
        if (_fishySteamworks == null) return;
        InstanceFinder.NetworkManager.TransportManager.Transport = _fishySteamworks;
    }

    private LobbyInfo[] Build(int count)
    {
        var arr = new LobbyInfo[count];
        for (int i = 0; i < count; i++)
        {
            CSteamID id = SteamMatchmaking.GetLobbyByIndex(i);
            arr[i] = new LobbyInfo
            {
                LobbyID        = id,
                Name           = SteamMatchmaking.GetLobbyData(id, KEY_NAME),
                CurrentMembers = SteamMatchmaking.GetNumLobbyMembers(id),
                MaxMembers     = SteamMatchmaking.GetLobbyMemberLimit(id),
                IsPublic       = SteamMatchmaking.GetLobbyData(id, KEY_TYPE) == PUB,
                Code           = SteamMatchmaking.GetLobbyData(id, KEY_CODE),
            };
        }
        return arr;
    }

    private string FallbackName(string name) =>
        string.IsNullOrWhiteSpace(name) ? SteamFriends.GetPersonaName() + "'s Game" : name;
}

[Serializable]
public struct LobbyInfo
{
    public CSteamID LobbyID;
    public string   Name;
    public int      CurrentMembers;
    public int      MaxMembers;
    public bool     IsPublic;
    public string   Code;

    public bool HasOpenSlot => CurrentMembers < MaxMembers;
    public bool HasPasscode => !string.IsNullOrEmpty(Code);
}
