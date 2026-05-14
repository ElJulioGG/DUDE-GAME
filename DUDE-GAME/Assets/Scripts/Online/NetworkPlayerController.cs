using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Thin network wrapper on each player GameObject.
/// Keeps existing MonoBehaviours (PlayerMovement, GunHolder, PlayerStats) completely unchanged.
/// The owning machine calls the Rpc* methods; they execute server-side and drive the local components.
/// Positions are synced to all clients via NetworkTransform (add that component separately).
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStats))]
public class NetworkPlayerController : NetworkBehaviour
{
    // Synced to all clients — who is this player globally (0-3)?
    private readonly SyncVar<int> _globalIndex = new SyncVar<int>(-1);
    public int GlobalIndex => _globalIndex.Value;

    private PlayerMovement _movement;
    private GunHolder      _gunHolder;
    private PlayerStats    _stats;

    private void Awake()
    {
        _movement  = GetComponent<PlayerMovement>();
        _gunHolder = GetComponent<GunHolder>();
        _stats     = GetComponent<PlayerStats>();
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
    // Input RPCs
    // Each is called by the owning machine and runs on the server (host machine).
    // The server drives the existing MonoBehaviours; NetworkTransform syncs positions.
    // -------------------------------------------------------------------------

    [ServerRpc]
    public void RpcMove(Vector2 input) =>
        _movement.SetInputVector(input);

    [ServerRpc]
    public void RpcAim(Vector2 dir) =>
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
