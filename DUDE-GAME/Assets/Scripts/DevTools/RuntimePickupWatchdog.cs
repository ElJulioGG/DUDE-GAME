#if GRENADE_DEBUG
using UnityEngine;

public class RuntimePickupWatchdog : MonoBehaviour
{
    private float _nextScanTime;

    private void Update()
    {
        if (Time.time < _nextScanTime)
            return;

        _nextScanTime = Time.time + 1f;

        var pickups = FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
        foreach (var pickup in pickups)
        {
            if (pickup == null) continue;

            var grenade = pickup.GetComponentInChildren<Grenade>();
            if (grenade != null)
            {
                Debug.LogWarning($"[WATCHDOG] WeaponPickup {pickup.name} contains Grenade id={grenade.Id} def={grenade.Definition?.name ?? "null"}", pickup);
            }
        }
    }
}
#endif
