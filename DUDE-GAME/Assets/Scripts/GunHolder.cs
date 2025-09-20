using UnityEngine;
using UnityEngine.InputSystem;

public class GunHolder : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private GameObject[] allWeapons;
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameObject defaultMeleePrefab;

    [SerializeField] private string activeWeapon;
    [SerializeField] private GameObject currentWeapon;

    [SerializeField] private WeaponBase currentGunScript;
    [SerializeField] private MeleeWeaponBase currentMeleeScript;

    [SerializeField] private WeaponPickup nearbyPickup;
    [SerializeField] private bool hasWeapon = false;
    [SerializeField] private int playerIndex;
    private Vector2 lastMovementDirection = Vector2.zero; // fallback if no input

    // **New**: Track the last aim direction
    private Vector2 lastAimDirection = Vector2.right; // Default aiming right

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
        nearbyPickup = pickup;
    }

    public void ClearNearbyPickup(WeaponPickup pickup)
    {
        if (nearbyPickup == pickup)
        {
            nearbyPickup = null;
        }
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
    public void EquipWeapon(string weaponName,WeaponPickup pickup)
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

        // 1) Si hay granada cocinándose, LÁNZALA (o suéltala en sitio si no apuntas) y SAL
        if (grenadeWeapon != null && grenadeWeapon.HasCooking)
        {
            Vector2 dir = (lastAimDirection.sqrMagnitude > 0.01f) ? lastAimDirection : Vector2.zero;
            if (grenadeWeapon.TryThrowCooked(dir))
                return;
        }

        // 2) Si la granada explotó en la mano ESTE FRAME, NO dropear (evita duplicado)
        if (grenadeWeapon != null && grenadeWeapon.WasConsumedInHand)
        {
            // Opcional: si ya no queda muni, asegúrate de limpiar a melee
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

        // 3) Si es un arma de granada sin munición, NO crees pickup (solo limpia a melee)
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

        // --- LÓGICA NORMAL DE DROPEO (igual que ya la tienes) ---
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
        if (nearbyPickup != null && !hasWeapon)
        {
            var pickup = nearbyPickup;
            nearbyPickup = null;
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
            if (currentGunScript != null )
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

}
