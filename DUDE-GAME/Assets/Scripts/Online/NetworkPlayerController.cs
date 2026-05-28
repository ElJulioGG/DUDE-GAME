using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Thin network wrapper on each player GameObject.
/// Movement: owning client moves locally; NetworkTransform (client authority) syncs to server/observers.
/// Actions (shoot, aim, interact, powerup): owning client calls ServerRpc → executes on server.
/// Health/death/score: server-authoritative via SyncVars.
/// Knockback: server validates, TargetRpc delivers to owning client who has physics authority.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStats))]
public class NetworkPlayerController : NetworkBehaviour
{
    private readonly SyncVar<int>    _globalIndex = new SyncVar<int>(-1);
    public int GlobalIndex => _globalIndex.Value;

    private readonly SyncVar<int>    _health     = new SyncVar<int>(0);
    private readonly SyncVar<bool>   _alive      = new SyncVar<bool>(true);
    private readonly SyncVar<string> _weaponName = new SyncVar<string>("");
    private readonly SyncVar<int>    _score      = new SyncVar<int>(0);
    public int Score => _score.Value;

    private PlayerMovement _movement;
    private GunHolder      _gunHolder;
    private PlayerStats    _stats;

    private void Awake()
    {
        _movement  = GetComponent<PlayerMovement>();
        _gunHolder = GetComponent<GunHolder>();
        _stats     = GetComponent<PlayerStats>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_stats != null) _health.Value = _stats.baseHealth;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _health.OnChange     += OnHealthChanged;
        _alive.OnChange      += OnAliveChanged;
        _weaponName.OnChange += OnWeaponNameChanged;
        _score.OnChange      += OnScoreChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _health.OnChange     -= OnHealthChanged;
        _alive.OnChange      -= OnAliveChanged;
        _weaponName.OnChange -= OnWeaponNameChanged;
        _score.OnChange      -= OnScoreChanged;
    }

    // -------------------------------------------------------------------------
    // Called by NetworkGameManager on the server right after giving ownership
    // -------------------------------------------------------------------------

    [Server]
    public void ServerInit(int globalIndex)
    {
        _globalIndex.Value = globalIndex;
    }

    // -------------------------------------------------------------------------
    // Server-authoritative damage and death
    // -------------------------------------------------------------------------

    [Server]
    public void ServerTakeDamage(int damage)
    {
        if (_stats == null || !_stats.playerAlive) return;
        int newHealth = Mathf.Max(0, _stats.GetPlayerHealth() - damage);
        _stats.ApplyDamageWithoutKill(damage);
        _health.Value = newHealth;
        if (newHealth <= 0)
        {
            _alive.Value = false;
            RpcOnPlayerKilled();   // broadcast death effects BEFORE server-side SetActive
            _stats.KillPlayer();   // server: weapon drop + SetActive(false)
        }
    }

    // Forces health to an exact value — used by server-side powerups (e.g. Instakill).
    [Server]
    public void ServerSetHealth(int health)
    {
        if (_stats == null || !_stats.playerAlive) return;
        int clamped = Mathf.Clamp(health, 1, _stats.baseHealth);
        _stats.SetPlayerHealth(clamped);
        _health.Value = clamped;
    }

    // -------------------------------------------------------------------------
    // Knockback — server validates, TargetRpc pushes to owning client (physics authority)
    // -------------------------------------------------------------------------

    [Server]
    public void ServerApplyKnockback(Vector2 origin, float force)
    {
        if (Owner == null || !_stats.playerAlive) return;
        TargetApplyKnockback(Owner, origin, force);
    }

    [TargetRpc]
    private void TargetApplyKnockback(NetworkConnection conn, Vector2 origin, float force)
    {
        if (_stats != null) _stats.ApplyKnockback(origin, force);
    }

    // -------------------------------------------------------------------------
    // Score — server increments, SyncVar propagates to all machines
    // -------------------------------------------------------------------------

    [Server]
    public void ServerAddScore(int points)
    {
        _score.Value += points;
        SyncScoreToGameManager(_score.Value);
    }

    private void OnScoreChanged(int prev, int next, bool asServer)
    {
        // Fires on every machine (server: asServer=true, clients: asServer=false)
        if (!asServer) SyncScoreToGameManager(next);
    }

    private void SyncScoreToGameManager(int value)
    {
        if (GameManager.instance == null) return;
        switch (_globalIndex.Value)
        {
            case 0: GameManager.instance.player1Score = value; break;
            case 1: GameManager.instance.player2Score = value; break;
            case 2: GameManager.instance.player3Score = value; break;
            case 3: GameManager.instance.player4Score = value; break;
        }
    }

    // -------------------------------------------------------------------------
    // Respawn — server re-enables the player on all machines for the next round
    // -------------------------------------------------------------------------

    [Server]
    public void ServerRespawn()
    {
        if (_stats == null) return;
        _alive.Value  = true;
        _health.Value = _stats.baseHealth;

        // Notify clients first (FishNet delivers RPCs even to disabled NetworkObjects)
        RpcOnPlayerRespawned();

        // Server handles itself directly
        gameObject.SetActive(true);   // OnEnable resets health + playerAlive via PlayerStats
        _gunHolder?.DestroyCurrentWeapon();
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcOnPlayerRespawned()
    {
        gameObject.SetActive(true);   // OnEnable resets health + playerAlive
        _gunHolder?.DestroyCurrentWeapon();
    }

    // -------------------------------------------------------------------------
    // SyncVar callbacks
    // -------------------------------------------------------------------------

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        if (!asServer) _stats?.SetPlayerHealth(next);
    }

    private void OnAliveChanged(bool prev, bool next, bool asServer)
    {
        // Ensure playerAlive flag is correct on clients even if RPC arrives late
        if (!next && !asServer && _stats != null)
            _stats.playerAlive = false;
    }

    private void OnWeaponNameChanged(string prev, string next, bool asServer)
    {
        if (asServer) return;
        _gunHolder?.ShowWeaponVisual(next);
    }

    [Server]
    public void ServerSetWeapon(string weaponName)
    {
        _weaponName.Value = weaponName;
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcOnPlayerKilled()
    {
        if (_stats != null)
        {
            _stats.PlayDeathEffects();
            _stats.playerAlive = false;
        }
        gameObject.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Input RPCs — called by the owning machine, run on the server.
    // Movement is NOT here: the owning client moves its character directly;
    // NetworkTransform (client authority) syncs the position to the server and all observers.
    // -------------------------------------------------------------------------

    [ServerRpc]
    public void RpcAim(Vector2 dir)
    {
        _gunHolder?.SetAimDirection(dir);
        BroadcastAim(dir);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void BroadcastAim(Vector2 dir) =>
        _gunHolder?.SetAimDirection(dir);

    [ServerRpc]
    public void RpcShootStart() =>
        _gunHolder?.HandleShoot();

    [ServerRpc]
    public void RpcShootStop() =>
        _gunHolder?.HandleStopShoot();

    [ServerRpc]
    public void RpcReload() =>
        _gunHolder?.HandleReload();

    [ServerRpc]
    public void RpcInteract() =>
        _gunHolder?.HandlePickDrop();

    [ServerRpc]
    public void RpcPowerUp()
    {
        if (_stats != null && _stats.playerAlive)
            _stats.usingPowerUp = true;
    }
}
