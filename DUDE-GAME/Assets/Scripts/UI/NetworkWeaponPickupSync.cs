using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Attach alongside WeaponPickup on the drop prefab. Handles weapon-name sync to remote clients.
// Also requires NetworkObject + NetworkTransform on the same prefab (added in the editor).
public class NetworkWeaponPickupSync : NetworkBehaviour
{
    private readonly SyncVar<string> _weaponName = new SyncVar<string>("");

    // True when this drop was thrown (not just spawned by a box). Lets clients play
    // the throw sound — WeaponPickup.Throw() only runs on the server, so without
    // this only the host hears thrown weapons.
    private readonly SyncVar<bool> _thrown = new SyncVar<bool>(false);

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsServerStarted && _thrown.Value
            && AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.Throw, transform.position);
    }

    // Call BEFORE ServerManager.Spawn() so the value rides the spawn packet.
    public void SetThrownPreSpawn(bool thrown)
    {
        _thrown.Value = thrown;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _weaponName.OnChange += OnNameChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _weaponName.OnChange -= OnNameChanged;
    }

    private void OnNameChanged(string prev, string next, bool asServer)
    {
        if (asServer) return;
        var pickup = GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            pickup.weaponName = next;
            pickup.RefreshSprite();
        }
    }

    // Call BEFORE ServerManager.Spawn() so the value is included in the spawn packet.
    public void SetWeaponNamePreSpawn(string name)
    {
        _weaponName.Value = name;
    }
}
