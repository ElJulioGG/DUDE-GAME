using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// A breakable weapon box placed in the scene.
///
/// State machine: Intact → Broken. Reset between rounds via MapObjectManager.
///
/// Online: _broken SyncVar drives the active state on all clients.
///         Server is the only machine that can break or reset the box.
///         Box is never Despawned — it stays as a scene NetworkObject and is
///         disabled/enabled so FishNet can track it across rounds without SceneId issues.
///
/// Offline: same SetActive logic, no network calls.
/// </summary>
public class WeaponBox : NetworkBehaviour
{
    [SerializeField] private GameObject[] weaponPickups;
    [SerializeField] private bool         random         = false;
    [SerializeField] private int          selectedWeapon = 0;

    private readonly SyncVar<bool> _broken = new SyncVar<bool>(false);
    private bool _brokenLocal = false; // plain guard for offline mode where SyncVar may not persist

    // -------------------------------------------------------------------------
    // Network lifecycle
    // -------------------------------------------------------------------------

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _broken.OnChange += OnBrokenChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _broken.OnChange -= OnBrokenChanged;
    }

    // Drives the box's active state on clients whenever the server changes _broken.
    // Fires even on a disabled GameObject — FishNet delivers SyncVar updates
    // to scene NetworkObjects regardless of their active state.
    private void OnBrokenChanged(bool prev, bool next, bool asServer)
    {
        if (asServer) return; // server manages itself directly in BreakBox / OnEnable
        gameObject.SetActive(!next);
    }

    // -------------------------------------------------------------------------
    // Reset between rounds
    // Called automatically by MapObjectManager when it re-enables the map.
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        _brokenLocal = false;
        // Server resets the SyncVar so the new state propagates to all clients.
        // OnBrokenChanged fires on clients with next=false → SetActive(true).
        if (GameSession.IsOnline && IsServerStarted)
            _broken.Value = false;
    }

    // -------------------------------------------------------------------------
    // Break logic
    // -------------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_broken.Value || _brokenLocal) return;
        if (!collision.CompareTag("Bullet") && !collision.CompareTag("Melee")) return;
        // Use InstanceFinder so the check doesn't depend on this NetworkObject being spawned.
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted) return;
        BreakBox();
    }

    private void BreakBox()
    {
        _brokenLocal = true;
        if (IsSpawned && InstanceFinder.IsServerStarted) _broken.Value = true;
        SpawnWeapon();

        if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
        {
            // Notify all clients via NetworkGameManager (doesn't depend on this box being spawned).
            NetworkGameManager.Instance?.ServerBroadcastBoxBroken(transform.position);
            AudioManager.Instance.PlaySound(FMODEvents.Instance.BoxBreak, transform.position);
            gameObject.SetActive(false);
        }
        else
        {
            AudioManager.Instance.PlaySound(FMODEvents.Instance.BoxBreak, transform.position);
            gameObject.SetActive(false);
        }
    }

    private void SpawnWeapon()
    {
        if (weaponPickups.Length == 0) return;
        int index = random ? Random.Range(0, weaponPickups.Length) : selectedWeapon;
        if (weaponPickups[index] == null)
        {
            Debug.LogWarning("WeaponBox: missing weapon pickup prefab at index " + index);
            return;
        }

        var go = Instantiate(weaponPickups[index], transform.position, Quaternion.identity);
        if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
        {
            var no = go.GetComponent<NetworkObject>();
            if (no != null)
            {
                no.SetIsNetworked(true); // prefab may ship with _isNetworked=0
                InstanceFinder.ServerManager.Spawn(go);
            }
        }
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcOnBoxBroken()
    {
        AudioManager.Instance.PlaySound(FMODEvents.Instance.BoxBreak, transform.position);
        gameObject.SetActive(false);
    }
}
