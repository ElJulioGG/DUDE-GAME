using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Attach alongside WeaponPickup on the drop prefab. Handles weapon-name sync to remote clients.
// Also requires NetworkObject + NetworkTransform on the same prefab (added in the editor).
public class NetworkWeaponPickupSync : NetworkBehaviour
{
    private readonly SyncVar<string> _weaponName = new SyncVar<string>("");

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
