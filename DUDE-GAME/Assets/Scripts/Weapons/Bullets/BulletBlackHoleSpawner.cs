using FishNet;
using UnityEngine;

public class BulletBlackHoleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject blackHolePrefab;

    // Cached so clients can instantiate the hole from the server's broadcast even
    // after their local cosmetic bullet is already gone.
    private static GameObject _cachedPrefab;

    private void Awake()
    {
        if (blackHolePrefab != null) _cachedPrefab = blackHolePrefab;
    }

    public void SpawnBlackHole()
    {
        if (blackHolePrefab == null) return;

        // Online: bullet bounce trajectories diverge per machine, so only the server's
        // bullet decides where the hole goes. The kill zone lives on the server's copy,
        // so every machine must render the hole at the SERVER's position — otherwise
        // clients dodge an invisible hole while watching a harmless local one.
        if (GameSession.IsOnline)
        {
            if (!InstanceFinder.IsServerStarted) return; // clients spawn via broadcast
            NetworkGameManager.Instance?.ServerBroadcastBlackHole(transform.position);
        }

        Instantiate(blackHolePrefab, transform.position, Quaternion.identity);
    }

    // Client-side: spawn the hole where the server says it formed.
    public static void SpawnFromNetwork(Vector2 pos)
    {
        if (_cachedPrefab == null) return;
        Instantiate(_cachedPrefab, pos, Quaternion.identity);
    }
}
