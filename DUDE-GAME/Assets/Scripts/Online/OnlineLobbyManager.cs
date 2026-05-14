using System.Linq;
using UnityEngine;

/// <summary>
/// Bridges your existing PlayerCursor / ControllerMapper CSS with the online session.
///
/// PHASE 1 — Before connecting:
///   Players join and pick slots with their gamepads exactly as in local play
///   (cursors press P1-P4 buttons). When the host presses "Go Online",
///   call PrepareOnlineSession() to snapshot the local cursor state, then call
///   SteamLobbyManager.HostPublicLobby() or JoinLobby().
///
/// PHASE 2 — After the server sends back slot assignments:
///   ApplyNetworkAssignment() is called automatically. It reads LocalPlayerRegistry
///   to find which global indices this machine owns, then reassigns each local
///   PlayerInputHandler to its correct global playerIndex and updates GameManager.
/// </summary>
public class OnlineLobbyManager : MonoBehaviour
{
    public static OnlineLobbyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------------------------------------------------------------
    // PHASE 1: Snapshot cursor state before connecting
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this from your "Go Online" / "Host" / "Join" button handler,
    /// BEFORE calling SteamLobbyManager.Host/Join.
    /// Counts how many local cursors are currently assigned and stores the
    /// result in NetworkGameManager.LocalPlayerCount.
    /// </summary>
    public void PrepareOnlineSession()
    {
        int count = 0;
        var cursors = PlayerCursor.All;
        if (cursors != null)
            foreach (var c in cursors)
                if (c.IsAssigned) count++;

        // At least 1, at most 3 (server always needs to leave 1 slot open per extra machine)
        NetworkGameManager.LocalPlayerCount         = Mathf.Clamp(count, 1, 3);
        NetworkGameManager.LocalCharacterSelections = new int[] { -1, -1, -1 };

        Debug.Log($"[OnlineLobbyManager] PrepareOnlineSession: {NetworkGameManager.LocalPlayerCount} local player(s)");
    }

    // -------------------------------------------------------------------------
    // PHASE 2: Apply global assignments — called by NetworkGameManager
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called after the server assigns global slots to this machine.
    /// Maps each local PlayerInputHandler (ordered by InputSystem device index)
    /// to its assigned global playerIndex, then sets GameManager.playerXPlayable
    /// and triggers the game start (sets assignController = false).
    /// </summary>
    public void ApplyNetworkAssignment()
    {
        var reg = LocalPlayerRegistry.Instance;
        if (reg == null || reg.LocalPlayerCount == 0)
        {
            Debug.LogWarning("[OnlineLobbyManager] ApplyNetworkAssignment called but registry is empty.");
            return;
        }

        // Get all PlayerInputHandlers sorted by their Unity InputSystem player index
        // (0 = first controller that joined, 1 = second, etc.)
        var handlers = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None)
                       .OrderBy(h => h.index)
                       .ToArray();

        // Pair each local handler with the global index the server assigned
        TryApplySlot(handlers, localSlot: 0, reg.GlobalIndex0);
        TryApplySlot(handlers, localSlot: 1, reg.GlobalIndex1);
        TryApplySlot(handlers, localSlot: 2, reg.GlobalIndex2);

        // Signal GameController to unpause and start the match
        if (GameManager.instance != null)
            GameManager.instance.assignController = false;

        Debug.Log($"[OnlineLobbyManager] Applied assignment: slots [{reg.GlobalIndex0},{reg.GlobalIndex1},{reg.GlobalIndex2}]");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void TryApplySlot(PlayerInputHandler[] handlers, int localSlot, int globalIndex)
    {
        if (globalIndex < 0)                    return;
        if (localSlot  >= handlers.Length)      return;

        var handler = handlers[localSlot];
        handler.reasignController(globalIndex);

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
