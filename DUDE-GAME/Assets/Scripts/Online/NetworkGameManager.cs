using System.Collections.Generic;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Handles the handshake that assigns global player slots (0-3) to each connected machine,
/// and keeps a shared lobby-state view of every slot's character selection synced to all clients.
///
/// Variable count rules:
///   - Each machine may request 1-3 local players.
///   - The server allocates min(requested, remaining_slots) — first come, first served.
///   - Total slots = 4. If 4 machines connect they each get 1. If 2 connect, the first
///     can take up to 3 (1 left for the second). No hard-coded cap beyond available slots.
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    // --- Set before StartConnection (from OnlineLobbyManager after local CSS) ---
    public static int   LocalPlayerCount         = 1;
    public static int[] LocalCharacterSelections = new int[3] { -1, -1, -1 };

    // 4 player NetworkPlayerControllers — drag them in the Inspector in slot order 0-3
    [SerializeField] private NetworkPlayerController[] _playerSlots;

    // Server-side
    private readonly Dictionary<int, PendingRegistration> _pending = new();
    private readonly int[]                                _charSelections = { -1, -1, -1, -1 };
    private int _nextGlobalIndex = 0;

    private struct PendingRegistration
    {
        public int Count;
        public int Char0, Char1, Char2;
    }

    // -------------------------------------------------------------------------
    // Broadcast types
    // -------------------------------------------------------------------------

    public struct RegisterBroadcast : IBroadcast
    {
        public int LocalPlayerCount;
        public int CharIndex0, CharIndex1, CharIndex2; // character list index per local player (-1 = none)
    }

    public struct AssignBroadcast : IBroadcast
    {
        public int Index0, Index1, Index2; // global slot assigned to local players 0-2 (-1 = none)
    }

    // Sent to ALL clients whenever any slot's character selection changes.
    public struct LobbyStateBroadcast : IBroadcast
    {
        public int Char0, Char1, Char2, Char3; // character index per global slot (-1 = empty)
    }

    // Streamed every ~50 ms per machine during the CSS phase so all machines can render remote cursors.
    public struct CSSCursorStateBroadcast : IBroadcast
    {
        public int ClientId;  // filled by server relay
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
        _nextGlobalIndex = 0;
        for (int i = 0; i < _charSelections.Length; i++) _charSelections[i] = -1;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        InstanceFinder.ClientManager.RegisterBroadcast<AssignBroadcast>(OnClientReceiveAssign);
        InstanceFinder.ClientManager.RegisterBroadcast<LobbyStateBroadcast>(OnClientReceiveLobbyState);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        InstanceFinder.ClientManager.UnregisterBroadcast<AssignBroadcast>(OnClientReceiveAssign);
        InstanceFinder.ClientManager.UnregisterBroadcast<LobbyStateBroadcast>(OnClientReceiveLobbyState);
    }

    // -------------------------------------------------------------------------
    // Server: relay CSS cursor positions to all clients
    // -------------------------------------------------------------------------

    private void OnServerReceiveCSSCursors(NetworkConnection conn, CSSCursorStateBroadcast msg, Channel channel)
    {
        // ClientId is already stamped by the sender with its own random LocalSessionId.
        // Just relay unchanged to every client.
        InstanceFinder.ServerManager.Broadcast(msg);
    }

    // -------------------------------------------------------------------------
    // Server: receive registration from a machine
    // -------------------------------------------------------------------------

    private void OnServerReceiveRegister(NetworkConnection conn, RegisterBroadcast msg, Channel channel)
    {
        _pending[conn.ClientId] = new PendingRegistration
        {
            Count = msg.LocalPlayerCount,
            Char0 = msg.CharIndex0,
            Char1 = msg.CharIndex1,
            Char2 = msg.CharIndex2,
        };
        TryAssign(conn);
    }

    // -------------------------------------------------------------------------
    // Server: assign slots when registration arrives (CSS is already done)
    // -------------------------------------------------------------------------

    private void TryAssign(NetworkConnection conn)
    {
        if (!_pending.ContainsKey(conn.ClientId)) return;

        PendingRegistration reg = _pending[conn.ClientId];
        _pending.Remove(conn.ClientId);

        int available = _playerSlots.Length - _nextGlobalIndex;
        int count = Mathf.Clamp(reg.Count, 1, available);   // no artificial max-2 cap

        if (count <= 0)
        {
            Debug.LogWarning($"[NetworkGameManager] No slots left for clientId {conn.ClientId}.");
            return;
        }

        int[] charIndices = { reg.Char0, reg.Char1, reg.Char2 };
        int idx0 = -1, idx1 = -1, idx2 = -1;

        for (int i = 0; i < count; i++)
        {
            int gi = _nextGlobalIndex++;
            _playerSlots[gi].NetworkObject.GiveOwnership(conn);
            _playerSlots[gi].ServerInit(gi);

            if (i < charIndices.Length && charIndices[i] >= 0)
                _charSelections[gi] = charIndices[i];

            if      (i == 0) idx0 = gi;
            else if (i == 1) idx1 = gi;
            else if (i == 2) idx2 = gi;
        }

        // Tell this machine which global slots it owns
        InstanceFinder.ServerManager.Broadcast(conn, new AssignBroadcast
        {
            Index0 = idx0, Index1 = idx1, Index2 = idx2
        });

        // Broadcast the updated lobby state to EVERY connected client
        BroadcastLobbyState();

        Debug.Log($"[NetworkGameManager] ClientId {conn.ClientId} → slots [{idx0},{idx1},{idx2}]");
    }

    // -------------------------------------------------------------------------
    // Server: send full lobby state to all clients
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

    // -------------------------------------------------------------------------
    // Client: receive assigned global slots
    // -------------------------------------------------------------------------

    private void OnClientReceiveAssign(AssignBroadcast msg, Channel channel)
    {
        LocalPlayerRegistry.Instance?.SetAssignment(msg.Index0, msg.Index1, msg.Index2);
        OnlineLobbyManager.Instance?.ApplyNetworkAssignment();
        Debug.Log($"[NetworkGameManager] This machine owns slots [{msg.Index0},{msg.Index1},{msg.Index2}]");
    }

    // -------------------------------------------------------------------------
    // Client: receive combined lobby state (reserved for future lobby UI)
    // -------------------------------------------------------------------------

    private void OnClientReceiveLobbyState(LobbyStateBroadcast msg, Channel channel)
    {
        // Future: update lobby UI showing all players' selections
    }
}
