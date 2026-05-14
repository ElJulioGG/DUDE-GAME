using System.Collections.Generic;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Sits on the NetworkManager GameObject. Handles the handshake that assigns
/// global player slots to each connected machine.
///
/// Flow:
///   1. Client connects → sends RegisterBroadcast (how many local players)
///   2. Server buffers the count and waits for OnClientLoadedStartScenes
///   3. Once both arrive → gives ownership of the correct PlayerSlots + sends AssignBroadcast back
///   4. Client receives AssignBroadcast → tells LocalPlayerRegistry which slots it owns
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    // --- Set this before calling StartConnection (from lobby UI / character select) ---
    // 1 = solo online, 2 = two players on same couch going online together
    public static int LocalPlayerCount = 1;

    // Assign all 4 player NetworkPlayerControllers here in the Inspector (slots 0-3)
    [SerializeField] private NetworkPlayerController[] _playerSlots;

    // Server-side state
    private readonly Dictionary<int, int> _pendingCounts    = new(); // clientId → localPlayerCount
    private readonly HashSet<int>         _readyConnections = new(); // clientIds that finished loading scenes
    private int _nextGlobalIndex = 0;

    // -------------------------------------------------------------------------
    // Broadcast structs
    // -------------------------------------------------------------------------

    public struct RegisterBroadcast : IBroadcast
    {
        public int LocalPlayerCount;
    }

    public struct AssignBroadcast : IBroadcast
    {
        public int Index0; // global slot index for this machine's local player 0 (-1 = none)
        public int Index1; // global slot index for this machine's local player 1 (-1 = none)
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
        InstanceFinder.NetworkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedScenes;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        InstanceFinder.ServerManager.UnregisterBroadcast<RegisterBroadcast>(OnServerReceiveRegister);
        InstanceFinder.NetworkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedScenes;
        _pendingCounts.Clear();
        _readyConnections.Clear();
        _nextGlobalIndex = 0;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        InstanceFinder.ClientManager.RegisterBroadcast<AssignBroadcast>(OnClientReceiveAssign);

        // Tell the server how many local players this machine has.
        // The host also does this — it goes through the same server handler.
        InstanceFinder.ClientManager.Broadcast(new RegisterBroadcast { LocalPlayerCount = LocalPlayerCount });
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        InstanceFinder.ClientManager.UnregisterBroadcast<AssignBroadcast>(OnClientReceiveAssign);
    }

    // -------------------------------------------------------------------------
    // Server: receive player count from a connection
    // -------------------------------------------------------------------------

    private void OnServerReceiveRegister(NetworkConnection conn, RegisterBroadcast msg, Channel channel)
    {
        _pendingCounts[conn.ClientId] = msg.LocalPlayerCount;
        TryAssign(conn);
    }

    // -------------------------------------------------------------------------
    // Server: a client finished loading all start scenes — safe to give ownership
    // -------------------------------------------------------------------------

    private void OnClientLoadedScenes(NetworkConnection conn, bool asServer)
    {
        if (!asServer) return;
        _readyConnections.Add(conn.ClientId);
        TryAssign(conn);
    }

    // -------------------------------------------------------------------------
    // Assign player slots once BOTH conditions are met for a connection
    // -------------------------------------------------------------------------

    private void TryAssign(NetworkConnection conn)
    {
        // Both pieces of info must be available before we can assign
        if (!_pendingCounts.ContainsKey(conn.ClientId))    return;
        if (!_readyConnections.Contains(conn.ClientId))    return;

        int count = _pendingCounts[conn.ClientId];
        _pendingCounts.Remove(conn.ClientId);

        int available = _playerSlots.Length - _nextGlobalIndex;
        count = Mathf.Clamp(count, 1, Mathf.Min(2, available));
        if (count <= 0)
        {
            Debug.LogWarning($"[NetworkGameManager] No player slots left for clientId {conn.ClientId}.");
            return;
        }

        int idx0 = -1, idx1 = -1;

        for (int i = 0; i < count; i++)
        {
            int gi = _nextGlobalIndex++;
            _playerSlots[gi].NetworkObject.GiveOwnership(conn);
            _playerSlots[gi].ServerInit(gi);

            if (i == 0) idx0 = gi;
            else        idx1 = gi;
        }

        InstanceFinder.ServerManager.Broadcast(conn, new AssignBroadcast { Index0 = idx0, Index1 = idx1 });
        Debug.Log($"[NetworkGameManager] ClientId {conn.ClientId} → slots [{idx0}, {idx1}]");
    }

    // -------------------------------------------------------------------------
    // Client: receive assigned global indices
    // -------------------------------------------------------------------------

    private void OnClientReceiveAssign(AssignBroadcast msg, Channel channel)
    {
        LocalPlayerRegistry.Instance?.SetAssignment(msg.Index0, msg.Index1);
        Debug.Log($"[NetworkGameManager] This machine owns global slots [{msg.Index0}, {msg.Index1}]");
    }
}
