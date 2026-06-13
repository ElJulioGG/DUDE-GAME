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

    // Sent by server at the start of every new round. Clients run the full local round
    // reset (timer, mutators, map, intro sequence) so the whole round flow stays in sync.
    public struct RoundStartBroadcast : IBroadcast
    {
        public int MapIndex;
    }

    // Sent by server the moment a round ends so all machines play the points canvas,
    // fade transition and (when the match is over) load the victory screen together.
    public struct RoundEndBroadcast : IBroadcast
    {
        public int WinnerSlot;   // -1 = draw
        public int Points;
        public bool MatchOver;   // true → victory screen instead of next round
    }

    // Periodic timer sync so client round timers can't drift from the host.
    public struct RoundTimerBroadcast : IBroadcast
    {
        public float TimeLeft;
    }

    // Sent by server when a WeaponBox breaks. Clients find the box by world position and deactivate it.
    public struct BoxBrokenBroadcast : IBroadcast
    {
        public float X, Y;
    }

    // Sent by server when a black hole forms — clients render it at the server's
    // position so the visible hole matches the authoritative kill zone.
    public struct BlackHoleSpawnBroadcast : IBroadcast
    {
        public float X, Y;
    }

    // Sent by server when a player walks over a powerup pickup. Clients mirror the
    // grant (HUD icon), consume their local copy of the pickup (found by position —
    // they spawn at identical coordinates on every machine) and play the feedback.
    public struct PowerUpPickupBroadcast : IBroadcast
    {
        public int Slot;
        public int Type;
        public float X, Y;
    }

    // Sent by server when a player activates their held powerup. Clients play the
    // same banner/voice/particles — gameplay (damage, points) stays server-side.
    public struct PowerUpUseBroadcast : IBroadcast
    {
        public int Slot;
        public int Type;
    }

    // ----- Mutator (chicken) sync: server simulates, clients render puppets -----

    public struct MutatorIndicatorBroadcast : IBroadcast
    {
        public float X, Y;
    }

    public struct MutatorSpawnBroadcast : IBroadcast
    {
        public int MutatorId;
        public int PrefabIndex;
        public float X, Y;
        public bool Golden;
    }

    // Streamed ~20 fps while a mutator is alive.
    public struct MutatorStateBroadcast : IBroadcast
    {
        public int MutatorId;
        public float X, Y;
        public bool FlipX;
        public float TiltZ;
        public bool Headless;
    }

    public struct MutatorHitBroadcast : IBroadcast
    {
        public int MutatorId;
    }

    public struct MutatorDespawnBroadcast : IBroadcast
    {
        public int MutatorId;
        public bool Died; // true = play death effects, false = silent cleanup
    }

    // Children the chicken spawns (drops, revenge minis, egg, speed trail).
    public struct MutatorChildSpawnBroadcast : IBroadcast
    {
        public int MutatorId;
        public byte ChildType;   // see RainbowChicken.ChildType
        public int PrefabIndex;
        public float X, Y;
        public int TargetSlot;   // revenge minis home toward this player slot (-1 = none)
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
        // Networked play must keep running while the window is unfocused.
        Application.runInBackground = true;
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

    // Client broadcast handlers are registered in OnEnable — NOT OnStartClient — because
    // OnStartClient only runs if this scene NetworkObject successfully spawns on the
    // client, which FishNet does not guarantee for scene objects in this project.
    // ClientManager.RegisterBroadcast works regardless of any NetworkObject state.
    private bool _clientBroadcastsRegistered;

    private void OnEnable() => TryRegisterClientBroadcasts();

    private void TryRegisterClientBroadcasts()
    {
        if (_clientBroadcastsRegistered) return;
        if (InstanceFinder.ClientManager == null) return;
        _clientBroadcastsRegistered = true;
        InstanceFinder.ClientManager.RegisterBroadcast<LobbyStateBroadcast>(OnClientReceiveLobbyState);
        InstanceFinder.ClientManager.RegisterBroadcast<RoundStartBroadcast>(OnClientReceiveRoundStart);
        InstanceFinder.ClientManager.RegisterBroadcast<RoundEndBroadcast>(OnClientReceiveRoundEnd);
        InstanceFinder.ClientManager.RegisterBroadcast<RoundTimerBroadcast>(OnClientReceiveRoundTimer);
        InstanceFinder.ClientManager.RegisterBroadcast<BoxBrokenBroadcast>(OnClientReceiveBoxBroken);
        InstanceFinder.ClientManager.RegisterBroadcast<BlackHoleSpawnBroadcast>(OnClientReceiveBlackHole);
        InstanceFinder.ClientManager.RegisterBroadcast<PowerUpPickupBroadcast>(OnClientReceivePowerUpPickup);
        InstanceFinder.ClientManager.RegisterBroadcast<PowerUpUseBroadcast>(OnClientReceivePowerUpUse);
        InstanceFinder.ClientManager.RegisterBroadcast<MutatorIndicatorBroadcast>(OnClientReceiveMutatorIndicator);
        InstanceFinder.ClientManager.RegisterBroadcast<MutatorSpawnBroadcast>(OnClientReceiveMutatorSpawn);
        InstanceFinder.ClientManager.RegisterBroadcast<MutatorStateBroadcast>(OnClientReceiveMutatorState);
        InstanceFinder.ClientManager.RegisterBroadcast<MutatorHitBroadcast>(OnClientReceiveMutatorHit);
        InstanceFinder.ClientManager.RegisterBroadcast<MutatorDespawnBroadcast>(OnClientReceiveMutatorDespawn);
        InstanceFinder.ClientManager.RegisterBroadcast<MutatorChildSpawnBroadcast>(OnClientReceiveMutatorChild);
    }

    private void OnDisable()
    {
        if (!_clientBroadcastsRegistered) return;
        if (InstanceFinder.ClientManager == null) return;
        _clientBroadcastsRegistered = false;
        InstanceFinder.ClientManager.UnregisterBroadcast<LobbyStateBroadcast>(OnClientReceiveLobbyState);
        InstanceFinder.ClientManager.UnregisterBroadcast<RoundStartBroadcast>(OnClientReceiveRoundStart);
        InstanceFinder.ClientManager.UnregisterBroadcast<RoundEndBroadcast>(OnClientReceiveRoundEnd);
        InstanceFinder.ClientManager.UnregisterBroadcast<RoundTimerBroadcast>(OnClientReceiveRoundTimer);
        InstanceFinder.ClientManager.UnregisterBroadcast<BoxBrokenBroadcast>(OnClientReceiveBoxBroken);
        InstanceFinder.ClientManager.UnregisterBroadcast<BlackHoleSpawnBroadcast>(OnClientReceiveBlackHole);
        InstanceFinder.ClientManager.UnregisterBroadcast<PowerUpPickupBroadcast>(OnClientReceivePowerUpPickup);
        InstanceFinder.ClientManager.UnregisterBroadcast<PowerUpUseBroadcast>(OnClientReceivePowerUpUse);
        InstanceFinder.ClientManager.UnregisterBroadcast<MutatorIndicatorBroadcast>(OnClientReceiveMutatorIndicator);
        InstanceFinder.ClientManager.UnregisterBroadcast<MutatorSpawnBroadcast>(OnClientReceiveMutatorSpawn);
        InstanceFinder.ClientManager.UnregisterBroadcast<MutatorStateBroadcast>(OnClientReceiveMutatorState);
        InstanceFinder.ClientManager.UnregisterBroadcast<MutatorHitBroadcast>(OnClientReceiveMutatorHit);
        InstanceFinder.ClientManager.UnregisterBroadcast<MutatorDespawnBroadcast>(OnClientReceiveMutatorDespawn);
        InstanceFinder.ClientManager.UnregisterBroadcast<MutatorChildSpawnBroadcast>(OnClientReceiveMutatorChild);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // MatchStartBroadcast is handled by OnlineLobbyManager.OnEnable (always registered).
        // Round/box/mutator broadcasts are registered in OnEnable above.
        if (FindFirstObjectByType<CSSCursorSync>() == null)
            new GameObject("[CSSCursorSync]").AddComponent<CSSCursorSync>();

        if (FindFirstObjectByType<NetworkStatsHud>() == null)
            new GameObject("[NetworkStatsHud]").AddComponent<NetworkStatsHud>();

        // Required by the match-start flow — auto-create if the user didn't place them in a scene.
        if (LocalPlayerRegistry.Instance == null)
            new GameObject("[LocalPlayerRegistry]").AddComponent<LocalPlayerRegistry>();
        if (OnlineLobbyManager.Instance == null)
            new GameObject("[OnlineLobbyManager]").AddComponent<OnlineLobbyManager>();
        OnlineLobbyManager.Instance?.ResetForNewSession();
    }

    // Server: tell all machines a box at this position broke.
    public void ServerBroadcastBoxBroken(Vector3 worldPos)
    {
        if (!IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new BoxBrokenBroadcast { X = worldPos.x, Y = worldPos.y });
    }

    // ----- Powerups: server decides pickups/activations, clients mirror -----

    public void ServerBroadcastPowerUpPickup(int slot, int type, Vector2 pos)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new PowerUpPickupBroadcast { Slot = slot, Type = type, X = pos.x, Y = pos.y });
    }

    public void ServerBroadcastPowerUpUse(int slot, int type)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new PowerUpUseBroadcast { Slot = slot, Type = type });
    }

    private void OnClientReceivePowerUpPickup(PowerUpPickupBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return; // host already handled it
        GameController.instance?.OnNetworkPowerUpPickup(msg.Slot, msg.Type, new Vector2(msg.X, msg.Y));
    }

    private void OnClientReceivePowerUpUse(PowerUpUseBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return; // host already handled it
        GameController.instance?.OnNetworkPowerUpUse(msg.Slot, msg.Type);
    }

    // Client: find the WeaponBox at this position and deactivate it.
    // If no box matches yet (map still switching, box momentarily inactive) the message
    // is queued and retried for a few seconds instead of being dropped silently.
    private struct PendingBoxBreak { public Vector2 Pos; public float Expire; }
    private readonly List<PendingBoxBreak> _pendingBoxBreaks = new();

    private void OnClientReceiveBoxBroken(BoxBrokenBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return; // host already handled it
        var pos = new Vector2(msg.X, msg.Y);
        if (!TryBreakBoxAt(pos))
            _pendingBoxBreaks.Add(new PendingBoxBreak { Pos = pos, Expire = Time.time + 3f });
    }

    private bool TryBreakBoxAt(Vector2 pos)
    {
        var boxes = FindObjectsByType<WeaponBox>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        const float tolerance = 0.25f;
        WeaponBox match = null;
        float bestSqr = tolerance * tolerance;
        foreach (var box in boxes)
        {
            if (box == null) continue;
            float dx = box.transform.position.x - pos.x;
            float dy = box.transform.position.y - pos.y;
            float sqr = dx * dx + dy * dy;
            if (sqr <= bestSqr) { bestSqr = sqr; match = box; }
        }
        if (match == null) return false;

        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.BoxBreak, match.transform.position);
        match.gameObject.SetActive(false);
        return true;
    }

    private void Update()
    {
        // ClientManager may not exist yet when OnEnable ran — keep trying.
        if (!_clientBroadcastsRegistered) TryRegisterClientBroadcasts();

        if (_pendingBoxBreaks.Count == 0) return;
        for (int i = _pendingBoxBreaks.Count - 1; i >= 0; i--)
        {
            if (TryBreakBoxAt(_pendingBoxBreaks[i].Pos) || Time.time >= _pendingBoxBreaks[i].Expire)
                _pendingBoxBreaks.RemoveAt(i);
        }
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
    // Round lifecycle sync — the server drives the round state machine and
    // clients mirror it through these broadcasts.
    // -------------------------------------------------------------------------

    public void ServerBroadcastRoundStart(int mapIndex)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new RoundStartBroadcast { MapIndex = mapIndex });
    }

    public void ServerBroadcastRoundEnd(int winnerSlot, int points, bool matchOver)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new RoundEndBroadcast
        {
            WinnerSlot = winnerSlot,
            Points     = points,
            MatchOver  = matchOver,
        });
    }

    public void ServerBroadcastRoundTimer(float timeLeft)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new RoundTimerBroadcast { TimeLeft = timeLeft });
    }

    private void OnClientReceiveRoundStart(RoundStartBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return; // host runs NextMatch directly
        _pendingBoxBreaks.Clear(); // stale break messages must not hit freshly reset boxes
        GameController.instance?.OnNetworkRoundStart(msg.MapIndex);
    }

    private void OnClientReceiveRoundEnd(RoundEndBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return;
        GameController.instance?.OnNetworkRoundEnd(msg.WinnerSlot, msg.Points, msg.MatchOver);
    }

    private void OnClientReceiveRoundTimer(RoundTimerBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return;
        GameController.instance?.OnNetworkTimerSync(msg.TimeLeft);
    }

    // -------------------------------------------------------------------------
    // Black hole sync — server's bullet decides the position, clients render it
    // -------------------------------------------------------------------------

    public void ServerBroadcastBlackHole(Vector2 pos)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new BlackHoleSpawnBroadcast { X = pos.x, Y = pos.y });
    }

    private void OnClientReceiveBlackHole(BlackHoleSpawnBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return; // host spawned it directly
        BulletBlackHoleSpawner.SpawnFromNetwork(new Vector2(msg.X, msg.Y));
    }

    // -------------------------------------------------------------------------
    // Mutator (chicken) sync — the server simulates, clients render puppets
    // -------------------------------------------------------------------------

    public void ServerBroadcastMutatorIndicator(Vector2 pos)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new MutatorIndicatorBroadcast { X = pos.x, Y = pos.y });
    }

    public void ServerBroadcastMutatorSpawn(int mutatorId, int prefabIndex, Vector2 pos, bool golden)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new MutatorSpawnBroadcast
        {
            MutatorId   = mutatorId,
            PrefabIndex = prefabIndex,
            X = pos.x, Y = pos.y,
            Golden      = golden,
        });
    }

    public void ServerBroadcastMutatorState(int mutatorId, Vector2 pos, bool flipX, float tiltZ, bool headless)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new MutatorStateBroadcast
        {
            MutatorId = mutatorId,
            X = pos.x, Y = pos.y,
            FlipX = flipX, TiltZ = tiltZ, Headless = headless,
        }, channel: Channel.Unreliable);
    }

    public void ServerBroadcastMutatorHit(int mutatorId)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new MutatorHitBroadcast { MutatorId = mutatorId });
    }

    public void ServerBroadcastMutatorDespawn(int mutatorId, bool died)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new MutatorDespawnBroadcast { MutatorId = mutatorId, Died = died });
    }

    public void ServerBroadcastMutatorChild(int mutatorId, byte childType, int prefabIndex, Vector2 pos, int targetSlot)
    {
        if (!InstanceFinder.IsServerStarted) return;
        InstanceFinder.ServerManager.Broadcast(new MutatorChildSpawnBroadcast
        {
            MutatorId   = mutatorId,
            ChildType   = childType,
            PrefabIndex = prefabIndex,
            X = pos.x, Y = pos.y,
            TargetSlot  = targetSlot,
        });
    }

    private void OnClientReceiveMutatorIndicator(MutatorIndicatorBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return;
        GameController.instance?.OnNetworkMutatorIndicator(new Vector2(msg.X, msg.Y));
    }

    private void OnClientReceiveMutatorSpawn(MutatorSpawnBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return;
        GameController.instance?.OnNetworkMutatorSpawn(msg.MutatorId, msg.PrefabIndex, new Vector2(msg.X, msg.Y), msg.Golden);
    }

    private void OnClientReceiveMutatorState(MutatorStateBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return;
        RainbowChicken.OnNetworkState(msg.MutatorId, new Vector2(msg.X, msg.Y), msg.FlipX, msg.TiltZ, msg.Headless);
    }

    private void OnClientReceiveMutatorHit(MutatorHitBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return;
        RainbowChicken.OnNetworkHit(msg.MutatorId);
    }

    private void OnClientReceiveMutatorDespawn(MutatorDespawnBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return;
        RainbowChicken.OnNetworkDespawn(msg.MutatorId, msg.Died);
    }

    private void OnClientReceiveMutatorChild(MutatorChildSpawnBroadcast msg, Channel channel)
    {
        if (InstanceFinder.IsServerStarted) return;
        RainbowChicken.OnNetworkChild(msg.MutatorId, msg.ChildType, msg.PrefabIndex, new Vector2(msg.X, msg.Y), msg.TargetSlot);
    }
}
