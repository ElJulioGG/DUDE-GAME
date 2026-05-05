using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunHolder : MonoBehaviour
{
    // Enable the GRENADE_DEBUG scripting define to surface grenade pickup diagnostics.
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private GameObject[] allWeapons;
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameObject defaultMeleePrefab;

    [SerializeField] private string activeWeapon;
    [SerializeField] private GameObject currentWeapon;

    [SerializeField] private WeaponBase currentGunScript;
    [SerializeField] private MeleeWeaponBase currentMeleeScript;

    private List<WeaponPickup> nearbyPickups = new List<WeaponPickup>();
    [SerializeField] private bool hasWeapon = false;
    [SerializeField] private int playerIndex;
    private Vector2 lastMovementDirection = Vector2.zero; // fallback if no input

    // **New**: Track the last aim direction
    private Vector2 lastAimDirection = Vector2.right; // Default aiming right

    private const float STEAL_RADIUS = 1.0f;
    private const float STEAL_COOLDOWN = 0.35f;
    private float lastStealTime = -999f;

    // Input request flags
    private bool shootRequested = false;
    private bool pickDropRequested = false;

    public void Start()
    {
        playerIndex = playerStats.GetPlayerIndex();

        // Instantiate default melee at start
        if (defaultMeleePrefab != null)
        {
            currentWeapon = Instantiate(defaultMeleePrefab, weaponHolder.position, weaponHolder.rotation, weaponHolder);
            currentMeleeScript = currentWeapon.GetComponent<MeleeWeaponBase>();
            currentGunScript = null;
            hasWeapon = false;
            activeWeapon = "melee";

            // Apply last aim direction to default melee
            if (currentMeleeScript != null)
                currentMeleeScript.SetAimDirection(lastAimDirection);
        }
    }

    public void SetPlayerIndex(int index) => playerIndex = index;
    public int GetPlayerIndex() => playerIndex;
    void FixedUpdate()
    {
        // Track last movement direction with a deadzone threshold
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            if (movement.moveInput.sqrMagnitude > 0.1f)
            {
                lastMovementDirection = movement.moveInput.normalized;
            }
            else
            {
                lastMovementDirection = Vector2.zero;
            }
        }

        // Handle pick/drop first and skip shoot if pickup was handled
        if (pickDropRequested)
        {
            HandlePickDrop();
            pickDropRequested = false;
            shootRequested = false; // <-- cancel shooting this frame
            return;
        }

        // Only shoot if not cancelled
        if (shootRequested)
        {
            HandleShoot();
            shootRequested = false;
        }
    }


    public void RequestShoot() => shootRequested = true;
    public void RequestPickDrop() => pickDropRequested = true;

    public void SetNearbyPickup(WeaponPickup pickup)
    {
        if (pickup != null && !nearbyPickups.Contains(pickup))
            nearbyPickups.Add(pickup);
    }

    public void ClearNearbyPickup(WeaponPickup pickup)
    {
        nearbyPickups.Remove(pickup);
    }

    private WeaponPickup GetClosestPickup()
    {
        nearbyPickups.RemoveAll(p => p == null);
        if (nearbyPickups.Count == 0) return null;

        WeaponPickup closest = null;
        float bestDist = float.MaxValue;
        Vector2 selfPos = transform.position;

        foreach (var p in nearbyPickups)
        {
            float dist = Vector2.SqrMagnitude((Vector2)p.transform.position - selfPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                closest = p;
            }
        }
        return closest;
    }




    public void HandleReload()
    {

        // Only reload if we have a gun equipped (not melee)
        if (currentGunScript != null && !currentGunScript.IsReloading())
        {

            // Only reload if we need to (clip isn't full or we have reserve ammo)
            if (currentGunScript.GetCurrentClipAmmo() < currentGunScript.clipSize &&
                currentGunScript.GetReserveAmmo() > 0)
            {

                StartCoroutine(currentGunScript.Reload());

            }
        }
    }
    public void EquipWeapon(string weaponName, WeaponPickup pickup)
    {
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }

        currentWeapon = null;
        currentGunScript = null;
        currentMeleeScript = null;

        foreach (GameObject weaponPrefab in allWeapons)
        {
            if (weaponPrefab.name == weaponName)
            {
                currentWeapon = Instantiate(weaponPrefab, weaponHolder.position, weaponHolder.rotation, weaponHolder);
                currentGunScript = currentWeapon.GetComponent<WeaponBase>();
                currentMeleeScript = currentWeapon.GetComponent<MeleeWeaponBase>();

                hasWeapon = true;
                activeWeapon = weaponName;

#if GRENADE_DEBUG
                // DEBUG:
                string pickupInfo = pickup != null ? $"clip={pickup.savedClipAmmo} reserve={pickup.savedReserveAmmo}" : "pickup=null";
                Debug.Log($"[PICKUP] {name} EquipWeapon weapon={weaponName} hasGun={currentGunScript != null} {pickupInfo}");
#endif

                if (currentGunScript != null)
                {
                    if (pickup.savedClipAmmo == -1 && pickup.savedReserveAmmo == -1)
                    {
                        currentGunScript.InitializeWeapon(true);
                    }
                    else
                    {
                        currentGunScript.SetAmmo(pickup.savedClipAmmo, pickup.savedReserveAmmo);
                    }

                    currentGunScript.SetAimDirection(lastAimDirection);
                }
                else if (currentMeleeScript != null)
                {
                    currentMeleeScript.SetAimDirection(lastAimDirection);
                }

                return;
            }
        }

        activeWeapon = "no weapon";
        Debug.LogWarning("Weapon not found: " + weaponName);
    }

    public void DropCurrentWeapon()
    {
        if (currentWeapon == null) return;

        var grenadeWeapon = currentGunScript as GrenadeWeapon;

        if (grenadeWeapon != null && grenadeWeapon.HasCooking)
        {
            Vector2 dir = (lastAimDirection.sqrMagnitude > 0.01f) ? lastAimDirection : Vector2.zero;
            if (grenadeWeapon.TryThrowCooked(dir))
                return;
        }

        if (grenadeWeapon != null && grenadeWeapon.WasConsumedInHand)
        {
            if (grenadeWeapon.GetCurrentClipAmmo() <= 0)
            {
                Destroy(currentWeapon);
                currentWeapon = null;
                currentGunScript = null;
                currentMeleeScript = null;
                hasWeapon = false;

                if (defaultMeleePrefab != null)
                {
                    currentWeapon = Instantiate(defaultMeleePrefab, weaponHolder.position, weaponHolder.rotation, weaponHolder);
                    currentMeleeScript = currentWeapon.GetComponent<MeleeWeaponBase>();
                    activeWeapon = "melee";
                    currentMeleeScript?.SetAimDirection(lastAimDirection);
                }
            }
            return;
        }

        if (grenadeWeapon != null && grenadeWeapon.GetCurrentClipAmmo() <= 0)
        {
            Destroy(currentWeapon);
            currentWeapon = null;
            currentGunScript = null;
            currentMeleeScript = null;
            hasWeapon = false;

            if (defaultMeleePrefab != null)
            {
                currentWeapon = Instantiate(defaultMeleePrefab, weaponHolder.position, weaponHolder.rotation, weaponHolder);
                currentMeleeScript = currentWeapon.GetComponent<MeleeWeaponBase>();
                activeWeapon = "melee";
                currentMeleeScript?.SetAimDirection(lastAimDirection);
            }
            return;
        }

        string weaponName = currentWeapon.name.Replace("(Clone)", "").Trim();
        Vector3 spawnPosition = transform.position;
        float dropDistance = 1f;

        bool isMoving = lastMovementDirection.sqrMagnitude > 0.01f;
        if (isMoving)
            spawnPosition += (Vector3)lastMovementDirection.normalized * dropDistance;

        GameObject drop = Instantiate(dropPrefab, spawnPosition, Quaternion.identity);

        if (lastAimDirection != Vector2.zero)
        {
            float angle = Mathf.Atan2(lastAimDirection.y, lastAimDirection.x) * Mathf.Rad2Deg;
            drop.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        WeaponPickup pickup = drop.GetComponent<WeaponPickup>();
        pickup.weaponName = weaponName;
        pickup.savedClipAmmo = currentGunScript != null ? currentGunScript.GetCurrentClipAmmo() : 0;
        pickup.savedReserveAmmo = currentGunScript != null ? currentGunScript.GetReserveAmmo() : 0;

        if (isMoving) pickup.Throw(lastMovementDirection);
        else pickup.Throw(Vector2.zero);

        Destroy(currentWeapon);
        currentWeapon = null;
        currentGunScript = null;
        currentMeleeScript = null;
        hasWeapon = false;

        if (defaultMeleePrefab != null)
        {
            currentWeapon = Instantiate(defaultMeleePrefab, weaponHolder.position, weaponHolder.rotation, weaponHolder);
            currentMeleeScript = currentWeapon.GetComponent<MeleeWeaponBase>();
            currentGunScript = null;
            activeWeapon = "melee";
            currentMeleeScript?.SetAimDirection(lastAimDirection);
        }
    }


    public void DestroyCurrentWeapon()
    {
        // Don't destroy if it's the default melee weapon
        if (currentWeapon != null && activeWeapon == "melee")
        {
            return;
        }

        // Proceed with destruction
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
            currentWeapon = null;
            currentGunScript = null;
            currentMeleeScript = null;
            hasWeapon = false;
            activeWeapon = "no weapon";
        }

        // Always ensure player has the default melee
        if (defaultMeleePrefab != null)
        {
            currentWeapon = Instantiate(defaultMeleePrefab, weaponHolder.position, weaponHolder.rotation, weaponHolder);
            currentMeleeScript = currentWeapon.GetComponent<MeleeWeaponBase>();
            currentGunScript = null;
            activeWeapon = "melee";

            if (currentMeleeScript != null)
                currentMeleeScript.SetAimDirection(lastAimDirection);
        }
    }
    public void HandlePickDrop()
    {
        if (TryStealNearbyGrenade())
            return;

        if (FindNearbyArmedGrenade(out var nearbyGrenade, out var grenadeDistance))
        {
#if GRENADE_DEBUG
            // DEBUG:
            Debug.Log($"[PICKUP] {name} HandlePickDrop adopting live grenade id={nearbyGrenade.Id} dist={grenadeDistance:F2} state={nearbyGrenade.CurrentState} fuseLeft={nearbyGrenade.FuseLeft:F2} def={nearbyGrenade.Definition?.name ?? "null"} owner={nearbyGrenade.OwnerIndex}");
#endif
            GrenadeWeapon gw = currentGunScript as GrenadeWeapon;
            if (gw == null)
            {
                EquipWeapon("GrenadeWeapon", new WeaponPickup { savedClipAmmo = -1, savedReserveAmmo = -1 });
                gw = currentGunScript as GrenadeWeapon;
            }

            if (gw != null)
            {
                gw.AdoptExistingGrenade(nearbyGrenade);
                if (gw.HasCooking)
                {
                    hasWeapon = true;
                    activeWeapon = "GrenadeWeapon";
                    currentMeleeScript = null;
                    gw.SetAimDirection(lastAimDirection);
                    return;
                }
#if GRENADE_DEBUG
                else
                {
                    Debug.LogWarning($"[PICKUP] {name} failed to adopt grenade id={nearbyGrenade.Id} state={nearbyGrenade.CurrentState} fuseLeft={nearbyGrenade.FuseLeft:F2}");
                }
#endif
            }
        }

        var closestPickup = GetClosestPickup();
        if (closestPickup != null && !hasWeapon)
        {
            var pickup = closestPickup;
            nearbyPickups.Remove(pickup);
#if GRENADE_DEBUG
            // DEBUG:
            Debug.Log($"[PICKUP] {name} taking WeaponPickup {pickup.name} weapon={pickup.weaponName} clip={pickup.savedClipAmmo} reserve={pickup.savedReserveAmmo}");
#endif
            EquipWeapon(pickup.weaponName, pickup);
            Destroy(pickup.gameObject);
        }
        else if (hasWeapon)
        {
            DropCurrentWeapon();
        }
    }


    public void HandleShoot()
    {
        if (currentWeapon != null && playerStats.playerAlive)
        {
            if (currentGunScript != null)
            {
                // Change this to use the new firing methods
                currentGunScript.StartFiring();
            }
            else if (currentMeleeScript != null)
            {
                currentMeleeScript.Attack();
            }
        }
    }

    public void HandleStopShoot()
    {
        if (currentGunScript != null)
        {
            currentGunScript.StopFiring();
        }
    }

    public void SetAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            lastAimDirection = direction.normalized;
        else
            lastAimDirection = Vector2.zero;

        if (currentWeapon != null)
        {
            if (currentGunScript != null)
                currentGunScript.SetAimDirection(lastAimDirection);
            else if (currentMeleeScript != null)
                currentMeleeScript.SetAimDirection(lastAimDirection);
        }
    }

    private bool FindNearbyArmedGrenade(out Grenade best, out float bestDist)
    {
        best = null;
        bestDist = float.MaxValue;

        var grenades = FindObjectsByType<Grenade>(FindObjectsSortMode.None);
        Vector2 selfPos = transform.position;

        foreach (var grenade in grenades)
        {
            if (grenade == null) continue;
            if (grenade.CurrentState == Grenade.State.Exploded) continue;
            if (grenade.IsHeld) continue;

            float distance = Vector2.Distance(selfPos, grenade.transform.position);
            if (distance < bestDist && distance <= 1.2f)
            {
                best = grenade;
                bestDist = distance;
            }
        }

#if GRENADE_DEBUG
        if (best != null)
        {
            Debug.Log($"[PICKUP] FindNearbyArmedGrenade picked id={best.Id} dist={bestDist:F2} fuseLeft={best.FuseLeft:F2} def={best.Definition?.name ?? "null"} state={best.CurrentState}");
        }
#endif

        return best != null;
    }

    private bool CanStealNow() => Time.time - lastStealTime >= STEAL_COOLDOWN;

    private bool TryStealNearbyGrenade()
    {
        if (!CanStealNow()) return false;
        if (playerStats != null && !playerStats.playerAlive) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, STEAL_RADIUS);
        if (hits == null || hits.Length == 0) return false;

        GunHolder victimHolder = null;
        GrenadeWeapon victimWeapon = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            var candidateHolder = hit.GetComponentInParent<GunHolder>();
            if (candidateHolder == null || candidateHolder == this) continue;
            if (candidateHolder.playerStats != null && !candidateHolder.playerStats.playerAlive) continue;
            if (Time.time - candidateHolder.lastStealTime < STEAL_COOLDOWN) continue;

            var candidateWeapon = candidateHolder.currentGunScript as GrenadeWeapon;
            if (candidateWeapon == null) continue;
            if (!candidateWeapon.HasCooking) continue;

            float sqrDist = Vector2.SqrMagnitude((Vector2)candidateHolder.transform.position - (Vector2)transform.position);
            if (sqrDist < bestDist)
            {
                bestDist = sqrDist;
                victimHolder = candidateHolder;
                victimWeapon = candidateWeapon;
            }
        }

        if (victimWeapon == null || victimHolder == null)
            return false;

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][STEAL] {name} attempting steal from {victimHolder.name} weaponHasCooking={victimWeapon.HasCooking}");
#endif

        Grenade stolen = victimWeapon.DetachHeldGrenadeForTransfer();
        if (stolen == null)
            return false;

        victimHolder.lastStealTime = Time.time;

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][STEAL] {name} stole grenadeId={stolen.Id} fuseLeft={stolen.FuseLeft:F2} ownerWas={stolen.OwnerIndex}");
#endif

        if (victimHolder != null)
        {
            victimHolder.DestroyCurrentWeapon();
        }

        if (currentWeapon != null && activeWeapon != "melee")
        {
            if (currentGunScript is GrenadeWeapon existingGrenadeWeapon)
            {
#if GRENADE_DEBUG
                Grenade released = existingGrenadeWeapon.DetachHeldGrenadeForTransfer();
                if (released != null)
                {
                    // DEBUG:
                    Debug.Log($"[GRENADE][STEAL] {name} released own grenadeId={released.Id} while preparing to steal fuseLeft={released.FuseLeft:F2}");
                }
#else
                existingGrenadeWeapon.DetachHeldGrenadeForTransfer();
#endif
                DestroyCurrentWeapon();
            }
            else
            {
                DropCurrentWeapon();
            }
        }

        GrenadeWeapon myGrenadeWeapon = currentGunScript as GrenadeWeapon;
        if (myGrenadeWeapon == null)
        {
            EquipWeapon("GrenadeWeapon", new WeaponPickup { savedClipAmmo = -1, savedReserveAmmo = -1 });
            myGrenadeWeapon = currentGunScript as GrenadeWeapon;
        }

        if (myGrenadeWeapon == null)
        {
#if GRENADE_DEBUG
            // DEBUG:
            Debug.LogWarning($"[GRENADE][STEAL] {name} failed to adopt stolen grenade id={stolen.Id} - no GrenadeWeapon equipped");
#endif
            lastStealTime = Time.time;
            return true;
        }

        myGrenadeWeapon.AdoptExistingGrenade(stolen);

        if (!myGrenadeWeapon.HasCooking)
        {
#if GRENADE_DEBUG
            // DEBUG:
            Debug.LogWarning($"[GRENADE][STEAL] {name} failed to hold stolen grenade id={stolen.Id} after adoption");
#endif
            lastStealTime = Time.time;
            return true;
        }

        hasWeapon = true;
        activeWeapon = "GrenadeWeapon";
        currentMeleeScript = null;
        myGrenadeWeapon.SetAimDirection(lastAimDirection);

        lastStealTime = Time.time;

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][STEAL] {name} success grenadeId={stolen.Id} fuseLeft={stolen.FuseLeft:F2} newOwnerIndex={playerIndex}");
#endif

        if (victimHolder != null)
            victimHolder.lastStealTime = Time.time;

        if (SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PlaySoundByName("Pickup", transform, 0.8f, 1f, false);
        }

        return true;
    }
}


