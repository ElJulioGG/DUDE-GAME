using System.Collections.Generic;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Collects RegisterBroadcasts from all connected machines, assigns global player slots
/// respecting each machine's CSS choice, then fires a single MatchStartBroadcast to all
/// machines simultaneously so the match begins in sync.
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    // 4 player NetworkPlayerControllers — drag in Inspector in slot order 0-3
    [SerializeField] private NetworkPlayerController[] _playerSlots;

    // Server-side state
    private readonly Dictionary<int, PendingRegistration> _pending          = new();
    private readonly Dictionary<int, NetworkConnection>   _pendingConns     = new();
    private readonly Dictionary<int, int>                 _sessionIds       = new(); // conn.ClientId → SessionId
    private readonly List<int>                            _registrationOrder = new();
    private readonly HashSet<int>                         _registeredClients = new();
    private readonly bool[]                               _slotTaken        = new bool[4];
    private readonly int[]                                _charSelections   = { -1, -1, -1, -1 };
    private bool                                          _matchStarted     = false;

    private struct PendingRegistration
    {
        public int Count;
        public int Chosen0, Chosen1, Chosen2; // CSS P1-P4 slot chosen (-1 = none)
    }

    // -------------------------------------------------------------------------
    // Broadcast types
    // -------------------------------------------------------------------------

    public struct RegisterBroadcast : IBroadcast
    {
        public int SessionId;        // CSSCursorSync.LocalId — unique per process
        public int LocalPlayerCount;
        public int ChosenSlot0, ChosenSlot1, ChosenSlot2; // P1-P4 index chosen in CSS (-1 = none)
    }

    /// <summary>
    /// Sent to ALL machines simultaneously once every machine has registered.
    /// Each machine identifies its own slots by matching LocalSessionId against OwnerSession0-3.
    /// </summary>
    public struct MatchStartBroadcast : IBroadcast
    {
        public int MapIndex;
        // For each global player slot (0-3): the SessionId of the owning machine. -1 = unowned.
        public int OwnerSession0, OwnerSession1, OwnerSession2, OwnerSession3;
    }

    // Sent to ALL clients whenever lobby state changes.
    public struct LobbyStateBroadcast : IBroadcast
    {
        public int Char0, Char1, Char2, Char3;
    }

    // Sent by server at the start of every new round so all machines load the same map.
    public struct RoundMapBroadcast : IBroadcast
    {
        public int MapIndex;
    }

    // Streamed at ~20 fps during CSS so all machines can render remote ghost cursors.
    public struct CSSCursorStateBroadcast : IBroadcast
    {
        public int ClientId;
        public float X0, Y0; public bool Active0; public bool Assigned0; public sbyte PlayerIndex0;
        public float X1, Y1; public bool Active1; public bool Assigned1; public sbyte PlayerIndex1;
        public float X2, Y2; public bool Active2; public bool Assigned2; public sbyte PlayerIndex2;
    }

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // Scene NetworkObjects with _isNetworked=0 are skipped by FishNet's spawn pass,
        // leaving NetworkManager null and crashing GiveOwnership. Force true before Start.
        var nob = GetComponent<NetworkObject>();
        if (nob != null) nob.SetIsNetworked(true);

        if (_playerSlots != null)
        {
            foreach (var slot in _playerSlots)
            {
                if (slot == null) continue;
                var slotNob = slot.GetComponent<NetworkObject>();
                if (slotNob != null) slotNob.SetIsNetworked(true);
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InstanceFinder.ServerManager.RegisterBroadcast<RegisterBroadcast>(OnServerReceiveRegister);
        InstanceFinder.ServerManager.RegisterBroadcast<CSSCursorStateBroadcast>(OnServerReceiveCSSCursors);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        InstanceFinder.ServerManager.UnregisterBroadcast<RegisterBroadcast>(OnServerReceiveRegister);
        InstanceFinder.ServerManager.UnregisterBroadcast<CSSCursorStateBroadcast>(OnServerReceiveCSSCursors);
        _pending.Clear();
        _pendingConns.Clear();
        _sessionIds.Clear();
        _registrationOrder.Clear();
        _registeredClients.Clear();
        System.Array.Clear(_slotTaken, 0, _slotTaken.Length);
        for (int i = 0; i < _charSelections.Length; i++) _charSelections[i] = -1;
        _matchStarted = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // MatchStartBroadcast is handled by OnlineLobbyManager.OnEnable (always registered).
        InstanceFinder.ClientManager.RegisterBroadcast<LobbyStateBroadcast>(OnClientReceiveLobbyState);
        InstanceFinder.ClientManager.RegisterBroadcast<RoundMapBroadcast>(OnClientReceiveRoundMap);

        if (FindFirstObjectByType<CSSCursorSync>() == null)
            new GameObject("[CSSCursorSync]").AddComponent<CSSCursorSync>();

        // Required by the match-start flow — auto-create if the user didn't place them in a scene.
        if (LocalPlayerRegistry.Instance == null)
            new GameObject("[LocalPlayerRegistry]").AddComponent<LocalPlayerRegistry>();
        if (OnlineLobbyManager.Instance == null)
            new GameObject("[OnlineLobbyManager]").AddComponent<OnlineLobbyManager>();
        OnlineLobbyManager.Instance?.ResetForNewSession();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        InstanceFinder.ClientManager.UnregisterBroadcast<LobbyStateBroadcast>(OnClientReceiveLobbyState);
        InstanceFinder.ClientManager.UnregisterBroadcast<RoundMapBroadcast>(OnClientReceiveRoundMap);
    }

    // -------------------------------------------------------------------------
    // Server: relay CSS cursor positions to all clients
    // -------------------------------------------------------------------------

    private void OnServerReceiveCSSCursors(NetworkConnection conn, CSSCursorStateBroadcast msg, Channel channel)
    {
        InstanceFinder.ServerManager.Broadcast(msg);
        CSSCursorSync.ProcessRemoteOnHost(msg);
    }

    // -------------------------------------------------------------------------
    // Server: collect registrations then fire MatchStart when everyone is ready
    // -------------------------------------------------------------------------

    private void OnServerReceiveRegister(NetworkConnection conn, RegisterBroadcast msg, Channel channel)
    {
        if (_registeredClients.Contains(conn.ClientId))
        {
            Debug.Log($"[NetworkGameManager] Duplicate registration from ClientId {conn.ClientId} — ignored");
            return;
        }

        _registeredClients.Add(conn.ClientId);
        _registrationOrder.Add(conn.ClientId);
        _pendingConns[conn.ClientId]  = conn;
        _sessionIds[conn.ClientId]    = msg.SessionId;
        _pending[conn.ClientId] = new PendingRegistration
        {
            Count   = msg.LocalPlayerCount,
            Chosen0 = msg.ChosenSlot0,
            Chosen1 = msg.ChosenSlot1,
            Chosen2 = msg.ChosenSlot2,
        };

        int total = InstanceFinder.ServerManager.Clients.Count;
        Debug.Log($"[NetworkGameManager] Registered ClientId {conn.ClientId} session={msg.SessionId} " +
                  $"count={msg.LocalPlayerCount} chosen=[{msg.ChosenSlot0},{msg.ChosenSlot1},{msg.ChosenSlot2}] " +
                  $"({_registeredClients.Count}/{total} ready)");

        // Fire MatchStart only when every connected machine has registered.
        if (!_matchStarted && _registeredClients.Count >= total)
            SendMatchStart();
    }

    private void SendMatchStart()
    {
        _matchStarted = true;
        var ownerSessions = new int[] { -1, -1, -1, -1 };

        // Assign slots in registration order so early registrants get priority on their choice.
        foreach (int connId in _registrationOrder)
        {
            var conn      = _pendingConns[connId];
            var reg       = _pending[connId];
            int sessionId = _sessionIds[connId];
            int[] choices = { reg.Chosen0, reg.Chosen1, reg.Chosen2 };

            for (int i = 0; i < reg.Count; i++)
            {
                int gi = PickSlot(choices[i]);
                if (gi < 0) { Debug.LogWarning("[NetworkGameManager] No slots left!"); break; }

                if (_playerSlots == null || gi >= _playerSlots.Length || _playerSlots[gi] == null)
                {
                    Debug.LogError($"[NetworkGameManager] _playerSlots[{gi}] is null — assign all 4 NetworkPlayerController slots in the NetworkGameManager Inspector.");
                    continue;
                }

                _slotTaken[gi]      = true;
                ownerSessions[gi]   = sessionId;
                _playerSlots[gi].NetworkObject.GiveOwnership(conn);
                _playerSlots[gi].ServerInit(gi);

                Debug.Log($"[NetworkGameManager] Global slot {gi} → session {sessionId} (ClientId {connId})");
            }
        }

        int mapIndex = GameController.instance != null ? GameController.instance.CurrentMapIndex() : 0;
        var matchMsg = new MatchStartBroadcast
        {
            MapIndex      = mapIndex,
            OwnerSession0 = ownerSessions[0],
            OwnerSession1 = ownerSessions[1],
            OwnerSession2 = ownerSessions[2],
            OwnerSession3 = ownerSessions[3],
        };

        Debug.Log($"[NetworkGameManager] MatchStart → map={mapIndex} " +
                  $"owners=[{ownerSessions[0]},{ownerSessions[1]},{ownerSessions[2]},{ownerSessions[3]}]");

        // ServerManager.Broadcast goes to all non-host clients.
        // ProcessMatchStartOnHost handles the host directly (no loopback).
        InstanceFinder.ServerManager.Broadcast(matchMsg);
        OnlineLobbyManager.ProcessMatchStartOnHost(matchMsg);
    }

    // Prefers the requested slot; falls back to the next free slot if taken.
    private int PickSlot(int preferred)
    {
        if (preferred >= 0 && preferred < _playerSlots.Length && !_slotTaken[preferred])
            return preferred;
        for (int s = 0; s < _playerSlots.Length; s++)
            if (!_slotTaken[s]) return s;
        return -1;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    public NetworkPlayerController GetPlayerSlot(int globalIndex) =>
        (globalIndex >= 0 && globalIndex < _playerSlots.Length) ? _playerSlots[globalIndex] : null;

    [Server]
    public void BroadcastLobbyState()
    {
        InstanceFinder.ServerManager.Broadcast(new LobbyStateBroadcast
        {
            Char0 = _charSelections[0],
            Char1 = _charSelections[1],
            Char2 = _charSelections[2],
            Char3 = _charSelections[3],
        });
    }

    private void OnClientReceiveLobbyState(LobbyStateBroadcast msg, Channel channel) { }

    // -------------------------------------------------------------------------
    // Round map sync — called by GameController.NextMatch() on the server
    // -------------------------------------------------------------------------

    public void ServerBroadcastRoundMap(int mapIndex)
    {
        if (!IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new RoundMapBroadcast { MapIndex = mapIndex });
    }

    // Received by non-host clients — host already applied the map inside GameController.NextMatch().
    private void OnClientReceiveRoundMap(RoundMapBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsHostStarted) return;
        GameController.instance?.SetMapByIndex(msg.MapIndex);
        GameController.instance?.AssignPlayerPositions();
    }
}
