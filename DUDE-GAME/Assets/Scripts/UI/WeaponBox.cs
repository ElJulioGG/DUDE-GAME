using FishNet;
using FishNet.Object;
using UnityEngine;

public class WeaponBox : NetworkBehaviour
{
    [SerializeField] private GameObject[] weaponPickups;
    [SerializeField] private bool random = false;
    [SerializeField] private int selectedWeapon = 0;

    private bool isBroken = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isBroken) return;
        if (!collision.CompareTag("Bullet") && !collision.CompareTag("Melee")) return;
        if (GameSession.IsOnline && !IsServerStarted) return;
        BreakBox();
    }

    private void BreakBox()
    {
        isBroken = true;
        SpawnWeapon();
        DestroyBox();
    }

    private void SpawnWeapon()
    {
        int weaponIndex = random ? Random.Range(0, weaponPickups.Length) : selectedWeapon;

        if (weaponPickups.Length == 0 || weaponPickups[weaponIndex] == null)
        {
            print("Missing weapon pickup prefab!");
            return;
        }

        var go = Instantiate(weaponPickups[weaponIndex], transform.position, Quaternion.identity);
        if (GameSession.IsOnline && IsServerStarted)
            ServerManager.Spawn(go);
    }

    private void DestroyBox()
    {
        if (GameSession.IsOnline && IsServerStarted)
        {
            RpcOnBoxBroken();
            ServerManager.Despawn(NetworkObject);
        }
        else
        {
            AudioManager.Instance.PlaySound(FMODEvents.Instance.BoxBreak, transform.position);
            Destroy(gameObject);
        }
    }

    [ObserversRpc]
    private void RpcOnBoxBroken()
    {
        AudioManager.Instance.PlaySound(FMODEvents.Instance.BoxBreak, transform.position);
    }
}