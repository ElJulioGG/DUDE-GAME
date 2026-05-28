using System.Linq;
using FishNet;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Bridges PlayerCursor CSS with the online session.
///
/// PHASE 1 — CSS: players pick slots locally. When ready, call SendRegistration().
/// PHASE 2 — MatchStart: server assigns global slots and map, broadcasts to everyone
///            simultaneously. ProcessMatchStartOnHost handles the host directly;
///            OnReceiveMatchStart handles non-host clients via ClientManager broadcast.
/// </summary>
public class OnlineLobbyManager : MonoBehaviour
{
    public static OnlineLobbyManager Instance { get; private set; }

    private bool _registered = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.RegisterBroadcast<NetworkGameManager.MatchStartBroadcast>(OnReceiveMatchStart);
    }

    private void OnDisable()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.UnregisterBroadcast<NetworkGameManager.MatchStartBroadcast>(OnReceiveMatchStart);
    }

    // -------------------------------------------------------------------------
    // Called by NetworkGameManager on the server when it is also the host.
    // Mirrors OnReceiveMatchStart without going through the broadcast loopback.
    // -------------------------------------------------------------------------

    public static void ProcessMatchStartOnHost(NetworkGameManager.MatchStartBroadcast msg)
    {
        if (Instance == null) return;
        if (!InstanceFinder.IsHostStarted) return;
        Instance.ApplyMatchStart(msg);
    }

    // -------------------------------------------------------------------------
    // Client-side: receive MatchStart broadcast from server
    // -------------------------------------------------------------------------

    private void OnReceiveMatchStart(NetworkGameManager.MatchStartBroadcast msg, Channel channel)
    {
        // Host is handled by ProcessMatchStartOnHost — skip to avoid double-apply.
        if (InstanceFinder.IsHostStarted) return;
        ApplyMatchStart(msg);
    }

    // -------------------------------------------------------------------------
    // Core: apply map + slot assignment + trigger match start
    // -------------------------------------------------------------------------

    private void ApplyMatchStart(NetworkGameManager.MatchStartBroadcast msg)
    {
        int mySession = CSSCursorSync.LocalId;
        int[] ownerSessions = { msg.OwnerSession0, msg.OwnerSession1, msg.OwnerSession2, msg.OwnerSession3 };

        // Extract which global slots THIS machine owns.
        int slot0 = -1, slot1 = -1, slot2 = -1;
        int localIdx = 0;
        for (int gi = 0; gi < 4; gi++)
        {
            if (ownerSessions[gi] != mySession) continue;
            if      (localIdx == 0) slot0 = gi;
            else if (localIdx == 1) slot1 = gi;
            else if (localIdx == 2) slot2 = gi;
            localIdx++;
        }

        Debug.Log($"[OnlineLobbyManager] MatchStart — session={mySession} → slots [{slot0},{slot1},{slot2}] map={msg.MapIndex}");

        // Sync map before respawning players so spawn points are correct.
        GameController.instance?.SetMapByIndex(msg.MapIndex);

        // Mark ALL occupied slots as playable across every machine.
        // Without this, each machine only enables its own player and the match ends immediately.
        if (GameManager.instance != null)
        {
            GameManager.instance.player1Playable = ownerSessions[0] != -1;
            GameManager.instance.player2Playable = ownerSessions[1] != -1;
            GameManager.instance.player3Playable = ownerSessions[2] != -1;
            GameManager.instance.player4Playable = ownerSessions[3] != -1;
        }

        LocalPlayerRegistry.Instance?.SetAssignment(slot0, slot1, slot2);
        ApplyNetworkAssignment();
    }

    // -------------------------------------------------------------------------
    // PHASE 1: Send registration when local players have selected in CSS
    // -------------------------------------------------------------------------

    public void ResetForNewSession()
    {
        _registered = false;
        Debug.Log("[OnlineLobbyManager] Reset for new session.");
    }

    public void PrepareOnlineSession()
    {
        GameSession.IsOnline = true;
        Debug.Log("[OnlineLobbyManager] Online session prepared.");
    }

    public void SendRegistration()
    {
        if (_registered) return;
        _registered = true;

        int count = 0;
        int chosen0 = -1, chosen1 = -1, chosen2 = -1;

        var cursors = PlayerCursor.All;
        if (cursors != null)
        {
            int slot = 0;
            foreach (var c in cursors)
            {
                if (!c.IsAssigned || slot >= 3) continue;
                if      (slot == 0) chosen0 = c.AssignedPlayerIndex;
                else if (slot == 1) chosen1 = c.AssignedPlayerIndex;
                else if (slot == 2) chosen2 = c.AssignedPlayerIndex;
                count++;
                slot++;
            }
        }

        count = Mathf.Clamp(count, 1, 3);

        InstanceFinder.ClientManager.Broadcast(new NetworkGameManager.RegisterBroadcast
        {
            SessionId      = CSSCursorSync.LocalId,
            LocalPlayerCount = count,
            ChosenSlot0    = chosen0,
            ChosenSlot1    = chosen1,
            ChosenSlot2    = chosen2,
        });

        Debug.Log($"[OnlineLobbyManager] SendRegistration: {count} player(s), chosen=[{chosen0},{chosen1},{chosen2}]");
    }

    // -------------------------------------------------------------------------
    // PHASE 2: Apply global slot assignments and start the match
    // -------------------------------------------------------------------------

    public void ApplyNetworkAssignment()
    {
        var reg = LocalPlayerRegistry.Instance;
        if (reg == null || reg.LocalPlayerCount == 0)
        {
            Debug.LogWarning("[OnlineLobbyManager] ApplyNetworkAssignment: registry empty — unpausing anyway.");
            if (GameManager.instance != null) GameManager.instance.assignController = false;
            return;
        }

        var handlers = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None)
                       .OrderBy(h => h.index)
                       .ToArray();

        TryApplySlot(handlers, 0, reg.GlobalIndex0);
        TryApplySlot(handlers, 1, reg.GlobalIndex1);
        TryApplySlot(handlers, 2, reg.GlobalIndex2);

        if (GameManager.instance != null)
            GameManager.instance.assignController = false;

        Debug.Log($"[OnlineLobbyManager] Applied assignment: slots [{reg.GlobalIndex0},{reg.GlobalIndex1},{reg.GlobalIndex2}]");
    }

    private void TryApplySlot(PlayerInputHandler[] handlers, int localSlot, int globalIndex)
    {
        if (globalIndex < 0)               return;
        if (localSlot >= handlers.Length)  return;

        handlers[localSlot].reasignController(globalIndex);

        if (GameManager.instance != null)
        {
            switch (globalIndex)
            {
                case 0: GameManager.instance.player1Playable = true; break;
                case 1: GameManager.instance.player2Playable = true; break;
                case 2: GameManager.instance.player3Playable = true; break;
                case 3: GameManager.instance.player4Playable = true; break;
            }
        }
    }
}
