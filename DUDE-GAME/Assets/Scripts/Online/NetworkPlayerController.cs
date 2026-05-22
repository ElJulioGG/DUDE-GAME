using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Thin network wrapper on each player GameObject.
/// Movement: owning client moves locally; NetworkTransform (client authority) syncs to server/observers.
/// Actions (shoot, aim, interact, powerup): owning client calls ServerRpc → executes on server.
/// Health/death: server-authoritative via SyncVar<int> _health and SyncVar<bool> _alive.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStats))]
public class NetworkPlayerController : NetworkBehaviour
{
    private readonly SyncVar<int>  _globalIndex = new SyncVar<int>(-1);
    public int GlobalIndex => _globalIndex.Value;

    private readonly SyncVar<int>  _health = new SyncVar<int>(0);
    private readonly SyncVar<bool> _alive  = new SyncVar<bool>(true);
    private readonly SyncVar<string> _weaponName = new SyncVar<string>("");

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
        _health.OnChange += OnHealthChanged;
        _alive.OnChange  += OnAliveChanged;
        _weaponName.OnChange += OnWeaponNameChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _health.OnChange -= OnHealthChanged;
        _alive.OnChange  -= OnAliveChanged;
        _weaponName.OnChange -= OnWeaponNameChanged;
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
