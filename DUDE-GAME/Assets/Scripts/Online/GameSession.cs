using FishNet;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Global flag set before any network connection is established.
/// Stays false for local-only play; set to true when creating or joining a Steam lobby.
/// Setting it true also applies the runtime settings online play depends on.
/// </summary>
public static class GameSession
{
    private static bool _isOnline = false;

    public static bool IsOnline
    {
        get => _isOnline;
        set
        {
            _isOnline = value;
            if (value)
            {
                ApplyOnlineRuntimeSettings();
                HookConnectionEvents();
            }
        }
    }

    // A stale IsOnline=true with no network running puts the whole game in a phantom
    // online mode: every gameplay authority check fails open — unlimited ammo,
    // damageless bullets, unbreakable boxes. Only SteamLobbyManager.LeaveLobby resets
    // the flag, so hook the transport itself: when the connection fully stops for any
    // reason (disconnect, kick, editor test ending), drop back to offline.
    private static bool _eventsHooked;

    private static void HookConnectionEvents()
    {
        if (_eventsHooked) return;
        if (InstanceFinder.ClientManager == null || InstanceFinder.ServerManager == null) return;
        _eventsHooked = true;

        InstanceFinder.ClientManager.OnClientConnectionState += args =>
        {
            if (args.ConnectionState == LocalConnectionState.Stopped && !InstanceFinder.IsServerStarted)
                _isOnline = false;
        };
        InstanceFinder.ServerManager.OnServerConnectionState += args =>
        {
            if (args.ConnectionState == LocalConnectionState.Stopped && !InstanceFinder.IsClientStarted)
                _isOnline = false;
        };
    }

    // Runs whenever a machine goes online — always before the connection starts,
    // regardless of which entry point (Steam lobby, editor test) set the flag.
    private static void ApplyOnlineRuntimeSettings()
    {
        // An unfocused window must keep simulating, or the other machines see frozen
        // players, missed bullets and bursty teleports.
        Application.runInBackground = true;

        // 60 ticks/s halves FishNet's per-hop buffering vs the default 30 (~17 ms less
        // per hop) and matches the 50 Hz physics rate, so every fresh pose ships
        // immediately. Bandwidth cost is trivial at 4 players. Must be identical on
        // every machine and set before connecting.
        if (InstanceFinder.TimeManager != null)
            InstanceFinder.TimeManager.SetTickRate(60);

        // Sanity check: real physics must be identical to offline. If any of these
        // ever differ from offline values (fixedDeltaTime 0.02, timeScale 1,
        // FixedUpdate simulation), something is tampering with the simulation and
        // "floaty physics" reports are real rather than view smoothing.
        Debug.Log($"[NET-DIAG] Online physics check — fixedDeltaTime={Time.fixedDeltaTime} " +
                  $"timeScale={Time.timeScale} sim2D={Physics2D.simulationMode}");
    }
}
