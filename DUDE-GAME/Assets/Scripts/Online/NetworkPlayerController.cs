using FishNet;
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
        bool isLocalOwner = Owner != null && Owner.IsLocalClient;
        Debug.Log($"[NET-DIAG] OnStartNetwork name={name} slot={_globalIndex.Value} IsServer={IsServerStarted} IsLocalOwner={isLocalOwner} OwnerId={(Owner != null ? Owner.ClientId : -999)} sceneActive={gameObject.activeInHierarchy}");
        ApplyPhysicsAuthority();

        // Extrapolation guesses ahead during packet gaps and overshoots exactly at
        // impulse moments (punches, throws) — that's the "floaty/flies too far" look
        // on remote players. Forced off in code so it can't regress via the Inspector.
        var nt = GetComponent<FishNet.Component.Transforming.NetworkTransform>();
        if (nt != null) nt.SetExtrapolation(0);
    }

    public override void OnOwnershipServer(FishNet.Connection.NetworkConnection prevOwner)
    {
        base.OnOwnershipServer(prevOwner);
        Debug.Log($"[NET-DIAG] OnOwnershipServer name={name} slot={_globalIndex.Value} NewOwnerId={(Owner != null ? Owner.ClientId : -999)}");
    }

    public override void OnOwnershipClient(FishNet.Connection.NetworkConnection prevOwner)
    {
        base.OnOwnershipClient(prevOwner);
        Debug.Log($"[NET-DIAG] OnOwnershipClient name={name} slot={_globalIndex.Value} IsOwner={IsOwner} NewOwnerId={(Owner != null ? Owner.ClientId : -999)}");
        ApplyPhysicsAuthority();
    }

    // Only the owning machine simulates this player's physics. Everywhere else the
    // body must be kinematic so the local physics engine (stale velocity, collisions,
    // drag) can't fight the NetworkTransform — that fight is what makes remote
    // players rubber-band and teleport.
    private void ApplyPhysicsAuthority()
    {
        if (!GameSession.IsOnline) return;
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        if (IsOwner)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
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

    // The owning machine predicts melee knockback locally the moment the swing
    // connects (see MeleeDefault), so the server's RPC is only a fallback — skip it
    // when a local push was already applied moments ago, otherwise it would double.
    // Window must exceed worst-case relay jitter or spikes let both apply and the
    // victim flies twice as far. A legit second punch inside the window still lands:
    // it's predicted locally; only the redundant RPC fallback is suppressed.
    private const float KnockbackDedupeWindow = 0.6f;

    private bool RecentLocalKnockback() =>
        _stats != null && Time.time - _stats.LastKnockbackTime < KnockbackDedupeWindow;

    [TargetRpc]
    private void TargetApplyKnockback(NetworkConnection conn, Vector2 origin, float force)
    {
        if (RecentLocalKnockback()) return;
        if (_stats != null) _stats.ApplyKnockback(origin, force);
    }

    [Server]
    public void ServerApplyKnockbackDirection(Vector2 direction, float force)
    {
        if (Owner == null || !_stats.playerAlive) return;
        TargetApplyKnockbackDirection(Owner, direction, force);
    }

    [TargetRpc]
    private void TargetApplyKnockbackDirection(NetworkConnection conn, Vector2 direction, float force)
    {
        if (RecentLocalKnockback()) return;
        if (_stats != null) _stats.ApplyKnockbackDirection(direction, force);
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

    // True when this slot belongs to a player in the current match. Unowned slots
    // keep _globalIndex = -1, so they never get re-activated by respawn logic.
    private bool IsSlotInMatch()
    {
        int slot = _globalIndex.Value >= 0 ? _globalIndex.Value : (_stats != null ? _stats.playerIndex : -1);
        return GameController.instance != null && GameController.instance.IsSlotPlayable(slot);
    }

    [Server]
    public void ServerRespawn()
    {
        if (_stats == null) return;
        if (!IsSlotInMatch()) return;
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
        // Respect this machine's playable flags — blindly activating every slot made
        // all 4 players appear on clients even with only 2 connected.
        if (!IsSlotInMatch()) return;
        gameObject.SetActive(true);   // OnEnable resets health + playerAlive
        _gunHolder?.DestroyCurrentWeapon();
    }

    // -------------------------------------------------------------------------
    // SyncVar callbacks
    // -------------------------------------------------------------------------

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        if (asServer) return;
        if (_stats == null) return;
        _stats.SetPlayerHealth(next);
        // Play hit feedback (sound, shake, particles) on every non-server machine when health drops.
        if (next < prev) _stats.PlayDamageFeedback(prev - next);
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

    // Server pushes current ammo state to the owning client so their HUD displays correctly.
    // The owner does not run HandleShoot locally for ranged, so their local WeaponBase ammo
    // would otherwise stay at default and the UI would be wrong.
    [Server]
    public void ServerSyncAmmo(int clip, int reserve, bool reloading)
    {
        if (Owner == null) return;
        TargetSyncAmmo(Owner, clip, reserve, reloading);
    }

    // Last ammo state received from the server. The weapon-name SyncVar and this
    // TargetRpc can arrive in either order, so the values are buffered and re-applied
    // by GunHolder.ShowWeaponVisual once the local weapon instance exists.
    private int  _pendingClip = -1;
    private int  _pendingReserve;
    private bool _pendingReloading;

    [TargetRpc]
    private void TargetSyncAmmo(NetworkConnection conn, int clip, int reserve, bool reloading)
    {
        // The host's weapon IS the server's instance — its values are already
        // authoritative, and applying them via SetAmmo would re-trigger the sync
        // in an infinite loop (see WeaponBase.ApplySyncedAmmo).
        if (InstanceFinder.IsServerStarted) return;

        _pendingClip      = clip;
        _pendingReserve   = reserve;
        _pendingReloading = reloading;

        var weapon = _gunHolder != null ? _gunHolder.CurrentGunScript : null;
        if (weapon == null) return;
        weapon.ApplySyncedAmmo(clip, reserve);
        weapon.SetExternalReloadingState(reloading);
    }

    // Applies the buffered ammo state to a freshly created local weapon visual.
    public void ApplyPendingAmmoTo(WeaponBase weapon)
    {
        if (weapon == null || _pendingClip < 0) return;
        weapon.ApplySyncedAmmo(_pendingClip, _pendingReserve);
        weapon.SetExternalReloadingState(_pendingReloading);
    }

    // Server → everyone else: this player's weapon indicator state (reloading /
    // completely dry) so other machines can read remote guns. The owner runs its
    // own local simulation and the host's weapon is the server instance, so both
    // are excluded.
    [Server]
    public void ServerSyncWeaponIndicators(bool reloading, bool clipEmpty, bool outOfAmmo) =>
        BroadcastWeaponIndicators(reloading, clipEmpty, outOfAmmo);

    // Buffered indicator state — the broadcast can arrive before ShowWeaponVisual
    // has created the local cosmetic weapon (same race as _pendingClip above).
    private bool _hasPendingIndicators;
    private bool _pendingIndReloading;
    private bool _pendingIndClipEmpty;
    private bool _pendingIndOutOfAmmo;

    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void BroadcastWeaponIndicators(bool reloading, bool clipEmpty, bool outOfAmmo)
    {
        _hasPendingIndicators = true;
        _pendingIndReloading = reloading;
        _pendingIndClipEmpty = clipEmpty;
        _pendingIndOutOfAmmo = outOfAmmo;
        var weapon = _gunHolder != null ? _gunHolder.CurrentGunScript : null;
        if (weapon != null) weapon.ApplyRemoteIndicators(reloading, clipEmpty, outOfAmmo);
    }

    // Applies the buffered indicator state to a freshly created cosmetic weapon visual.
    public void ApplyPendingIndicatorsTo(WeaponBase weapon)
    {
        if (weapon == null || !_hasPendingIndicators) return;
        weapon.ApplyRemoteIndicators(_pendingIndReloading, _pendingIndClipEmpty, _pendingIndOutOfAmmo);
    }

    // Server → everyone else: the holder pulled the trigger on an empty chamber.
    // An event (not state) so remote machines play the click/shake at the exact
    // attempt, mirroring what the holder experiences.
    [Server]
    public void ServerDryFire(bool outOfAmmo) => BroadcastDryFire(outOfAmmo);

    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void BroadcastDryFire(bool outOfAmmo)
    {
        var weapon = _gunHolder != null ? _gunHolder.CurrentGunScript : null;
        if (weapon != null) weapon.PlayDryFireFeedback(outOfAmmo);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcOnPlayerKilled()
    {
        if (_stats != null)
        {
            _stats.PlayDeathEffects();
            _stats.playerAlive = false;
            // Keep the client's alive count in sync (round end itself stays server-driven).
            GameController.instance?.OnPlayerDied(_stats);
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
    public void RpcShootStart()
    {
        _gunHolder?.HandleShoot();   // server-authoritative: the server's bullets deal the damage
        // Mirror the shot to all other machines — bullets are local cosmetic simulations
        // everywhere (WeaponBase.IsCosmeticOnly handles the ammo-less remote copies).
        BroadcastShootStart();
    }

    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void BroadcastShootStart() =>
        _gunHolder?.HandleShoot();

    [ServerRpc]
    public void RpcShootStop()
    {
        _gunHolder?.HandleStopShoot();
        BroadcastShootStop();
    }

    // Server → non-owner machines: the exact origin + angles of a fired shot, so
    // their cosmetic bullets start identical to the server's authoritative ones
    // (bounces and spread then stay matched). The owner keeps its instant local roll.
    [Server]
    public void ServerSendShotData(Vector2 origin, float[] angles) =>
        BroadcastShotFired(origin, angles);

    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void BroadcastShotFired(Vector2 origin, float[] angles) =>
        _gunHolder?.SpawnRemoteShot(origin, angles);

    // Owner → server: the exact geometry of the shot the owner just fired locally.
    // The server adopts it for its authoritative bullets (with a sanity clamp in
    // WeaponBase.Shoot), then relays the same data to everyone — so the shooter's
    // instant bullets and what every other machine sees are ONE identical shot.
    // Sent right before RpcShootStart; both are ordered reliable, so the data is
    // always waiting when the server fires.
    private float[] _pendingShotAngles;
    private Vector2 _pendingShotOrigin;

    [ServerRpc]
    public void RpcSubmitShotData(Vector2 origin, float[] angles)
    {
        _pendingShotOrigin = origin;
        _pendingShotAngles = angles;
    }

    public bool TryConsumeShotData(out Vector2 origin, out float[] angles)
    {
        origin = _pendingShotOrigin;
        angles = _pendingShotAngles;
        _pendingShotAngles = null;
        return angles != null && angles.Length > 0;
    }

    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void BroadcastShootStop() =>
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
