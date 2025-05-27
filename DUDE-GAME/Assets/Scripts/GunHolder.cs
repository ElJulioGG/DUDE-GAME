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

    void Update()
    {
        // Track last movement direction
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null && movement.moveInput.sqrMagnitude > 0.01f)
        {
            lastMovementDirection = movement.moveInput.normalized;
        }

        if (pickDropRequested)
        {
            HandlePickDrop();
            pickDropRequested = false;
            return;
        }

        if (shootRequested)
        {
            HandleShoot();
            shootRequested = false;
        }
    }

    public void RequestShoot() => shootRequested = true;
    public void RequestPickDrop() => pickDropRequested = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        WeaponPickup pickup = other.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            nearbyPickup = pickup;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyPickup != null && other.gameObject == nearbyPickup.gameObject)
        {
            nearbyPickup = null;
        }
    }

    public void EquipWeapon(string weaponName)
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

                // Apply the last aim direction to the new weapon
                if (currentGunScript != null)
                    currentGunScript.SetAimDirection(lastAimDirection);
                else if (currentMeleeScript != null)
                    currentMeleeScript.SetAimDirection(lastAimDirection);

                return;
            }
        }

        activeWeapon = "no weapon";
        Debug.LogWarning("Weapon not found: " + weaponName);
    }

    public void DropCurrentWeapon()
    {
        if (currentWeapon == null) return;

        string weaponName = currentWeapon.name.Replace("(Clone)", "").Trim();

        // Get player movement direction (Vector2.zero if idle)
        Vector2 moveDir = lastMovementDirection;

        // Calculate spawn position
        Vector3 spawnPosition = transform.position;
        if (moveDir != Vector2.zero)
            spawnPosition += (Vector3)moveDir;

        // Instantiate the drop
        GameObject drop = Instantiate(dropPrefab, spawnPosition, Quaternion.identity);

        // Rotate drop to face last aim direction
        if (lastAimDirection != Vector2.zero)
        {
            float angle = Mathf.Atan2(lastAimDirection.y, lastAimDirection.x) * Mathf.Rad2Deg;
            drop.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        WeaponPickup pickup = drop.GetComponent<WeaponPickup>();
        pickup.weaponName = weaponName;

        pickup.Throw(moveDir); // Will internally skip if direction is near-zero

        // Destroy equipped weapon
        Destroy(currentWeapon);
        currentWeapon = null;
        currentGunScript = null;
        currentMeleeScript = null;
        hasWeapon = false;

        // Re-instantiate melee
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
            GameObject toDestroy = nearbyPickup.gameObject;
            EquipWeapon(nearbyPickup.weaponName);
            nearbyPickup = null;
            Destroy(toDestroy);
        }
        else if (hasWeapon)
        {
            DropCurrentWeapon();
        }
    }

    public void HandleShoot()
    {
        if (currentWeapon != null)
        {
            if (currentGunScript != null)
                currentGunScript.Shoot();
            else if (currentMeleeScript != null)
                currentMeleeScript.Attack();
        }
    }

    public void SetAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            lastAimDirection = direction.normalized;
        }

        if (currentWeapon != null)
        {
            if (currentGunScript != null)
                currentGunScript.SetAimDirection(lastAimDirection);
            else if (currentMeleeScript != null)
                currentMeleeScript.SetAimDirection(lastAimDirection);
        }
    }
}
